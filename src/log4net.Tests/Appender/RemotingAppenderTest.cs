#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

// netstandard has no support for System.Runtime.Remoting
#if NET462_OR_GREATER

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;

using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Tests.Appender.Remoting.Data;
using log4net.Tests.Appender.Remoting.UserInterfaces;

using NUnit.Framework;

namespace log4net.Tests.Appender
{
  /// <summary>
  /// Used for internal unit testing the <see cref="RemotingAppender"/> class.
  /// </summary>
  /// <remarks>
  /// Used for internal unit testing the <see cref="RemotingAppender"/> class.
  /// </remarks>
  [TestFixture]
  public class RemotingAppenderTest
  {
    private IChannel? m_remotingChannel;

    /// <summary>
    /// Test that the Message property is correctly remoted
    /// </summary>
    [Test]
    public void TestRemotedMessage()
    {
      // Set up the remoting appender
      ConfigureRootAppender(FixFlags.Partial);

      RemoteLoggingSinkImpl.Instance.Reset();

      Logger root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;

      string testMessage = $"test message [ {(new Random()).Next()} ]";

      // Log a message that will be remoted
      root.Log(Level.Debug, testMessage, null);

      // Wait for the remoted object to be delivered
      WaitFor("Remote instance should have received all remoting events", () => RemoteLoggingSinkImpl.Instance.Events.Length > 0);

      LoggingEvent[] events = RemoteLoggingSinkImpl.Instance.Events;
      Assert.AreEqual(1, events.Length, "Expect to receive 1 remoted event");

      Assert.AreEqual(testMessage, events[0].RenderedMessage, "Expect Message match after remoting event");
    }

    /// <summary>
    /// Test that the LocationInfo property is not remoted when doing a Fix.Partial
    /// </summary>
    [Test]
    public void TestPartialFix()
    {
      // Set up the remoting appender
      ConfigureRootAppender(FixFlags.Partial);

      RemoteLoggingSinkImpl.Instance.Reset();

      Logger root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;

      // Log a message that will be remoted
      root.Log(Level.Debug, "test message", null);

      // Wait for the remoted object to be delivered
      WaitFor("Remote instance should have received all remoting events", () => RemoteLoggingSinkImpl.Instance.Events.Length > 0);

      LoggingEvent[] events = RemoteLoggingSinkImpl.Instance.Events;
      Assert.AreEqual(1, events.Length, "Expect to receive 1 remoted event");

      Assert.IsNull(events[0].LocationInfo, "Expect LocationInfo to be null because only doing a partial fix");
    }

    /// <summary>
    /// Test that the LocationInfo property is remoted when doing a Fix.All
    /// </summary>
    [Test]
    public void TestFullFix()
    {
      // Set up the remoting appender
      ConfigureRootAppender(FixFlags.All);

      RemoteLoggingSinkImpl.Instance.Reset();

      Logger root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;

      // Log a message that will be remoted
      root.Log(Level.Debug, "test message", null);

      // Wait for the remoted object to be delivered
      WaitFor("Remote instance should have received a remoting event", () => RemoteLoggingSinkImpl.Instance.Events.Length > 0);
      LoggingEvent[] events = RemoteLoggingSinkImpl.Instance.Events;
      Assert.AreEqual(1, events.Length, "Expect to receive 1 remoted event");

      Assert.IsNotNull(events[0].LocationInfo, "Expect LocationInfo to not be null because doing a full fix");
    }

    private static void WaitFor(
      string failMessage,
      Func<bool> condition,
      int maxWaitMilliseconds = 5000)
    {
      var start = DateTime.Now;
      do
      {
        if (condition())
        {
          return;
        }
        Thread.Sleep(100);
      } while ((DateTime.Now - start).TotalMilliseconds < maxWaitMilliseconds);
      throw new TimeoutException($"Condition not achieved within {maxWaitMilliseconds}ms: {failMessage}");
    }

    /// <summary>
    /// Test that the Message property is correctly remoted
    /// </summary>
    [Test]
    public void TestRemotedMessageNdcPushPop()
    {
      // Set up the remoting appender
      ConfigureRootAppender(FixFlags.Partial);

      RemoteLoggingSinkImpl.Instance.Reset();

      Logger root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;

      string testMessage = $"test message [ {(new Random()).Next()} ]";

      using (NDC.Push("value"))
      {
      }

      // Log a message that will be remoted
      root.Log(Level.Debug, testMessage, null);

      // Wait for the remoted object to be delivered
      WaitFor("Remote instance should have received all remoting events", () => RemoteLoggingSinkImpl.Instance.Events.Length > 0);

      LoggingEvent[] events = RemoteLoggingSinkImpl.Instance.Events;
      Assert.AreEqual(1, events.Length, "Expect to receive 1 remoted event");

      Assert.AreEqual(testMessage, events[0].RenderedMessage, "Expect Message match after remoting event");
    }

    [Test]
    public void TestNestedNdc()
    {
      // Set up the remoting appender
      ConfigureRootAppender(FixFlags.Partial);

      RemoteLoggingSinkImpl.Instance.Reset();

      var t = new TestService();
      t.Test();

      WaitFor("Remote instance should have received all remoting events", () => RemoteLoggingSinkImpl.Instance.Events.Length == 5);

      LoggingEvent[] events = RemoteLoggingSinkImpl.Instance.Events;

      // RemotingAppender dispatches events asynchronously, messages could be in any order.
      LoggingEvent beingTest = events.First(e => e.RenderedMessage == "begin test");
      Assert.IsNull(beingTest.Properties["NDC"], "Verify 'being test' event Properties");

      LoggingEvent feature = events.First(e => e.RenderedMessage == "feature");
      Assert.AreEqual("test1", feature.Properties["NDC"], "Verify 'feature' event Properties");

      LoggingEvent return1 = events.First(e => e.RenderedMessage == "return" && Equals(e.Properties["NDC"], "test1 test2"));

      LoggingEvent return2 = events.First(e => e.RenderedMessage == "return" && Equals(e.Properties["NDC"], "test1"));

      LoggingEvent endTest = events.First(e => e.RenderedMessage == "end test");
      Assert.IsNull(endTest.Properties["NDC"], "Verify 'end test' event Properties");
    }

    private void RegisterRemotingServerChannel()
    {
      if (m_remotingChannel is null)
      {
        BinaryClientFormatterSinkProvider clientSinkProvider = new BinaryClientFormatterSinkProvider();

        BinaryServerFormatterSinkProvider serverSinkProvider = new BinaryServerFormatterSinkProvider();
        serverSinkProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

        var channelProperties = new Hashtable();
        channelProperties["port"] = 8085;

        m_remotingChannel = new TcpChannel(channelProperties, clientSinkProvider, serverSinkProvider);
        // Setup remoting server
        try
        {
          ChannelServices.RegisterChannel(m_remotingChannel, false);
        }
        catch (Exception ex)
        {
          Assert.Fail("Failed to set up LoggingSink: {0}", ex);
        }

        // Marshal the sink object
        RemotingServices.Marshal(RemoteLoggingSinkImpl.Instance, "LoggingSink", typeof(RemotingAppender.IRemoteLoggingSink));
      }
    }

    /// <summary>
    /// Shuts down any loggers in the hierarchy, along
    /// with all appenders.
    /// </summary>
    private static void ResetRepository()
    {
      // Regular users should not use the clear method lightly!
      LogManager.GetRepository().ResetConfiguration();
      LogManager.GetRepository().Shutdown();
      ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Clear();
    }

    /// <summary>
    /// Any initialization that happens before each test can
    /// go here
    /// </summary>
    [SetUp]
    public void SetUp()
    {
      ResetRepository();
      RegisterRemotingServerChannel();
    }

    /// <summary>
    /// Any steps that happen after each test go here
    /// </summary>
    [TearDown]
    public void TearDown()
    {
      ResetRepository();
    }

    /// <summary>
    /// Close down remoting infrastructure
    /// </summary>
    [OneTimeTearDown]
    public void UnregisterRemotingServerChannel()
    {
      if (m_remotingChannel is not null)
      {
        ((TcpChannel)m_remotingChannel).StopListening(null);
        try
        {
          ChannelServices.UnregisterChannel(m_remotingChannel);
        }
        catch (Exception)
        {
        }
        m_remotingChannel = null;
      }
    }

    /// <summary>
    /// Configures the root appender for counting and rolling
    /// </summary>
    private static void ConfigureRootAppender(FixFlags fixFlags)
    {
      Logger root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;
      root.Level = Level.Debug;
      root.AddAppender(CreateAppender(fixFlags));
      root.Repository!.Configured = true;
    }

    private static RemotingAppender CreateAppender(FixFlags fixFlags)
    {
      var appender = new RemotingAppender
      {
        Sink = "tcp://localhost:8085/LoggingSink",
        Lossy = false,
        BufferSize = 1,
        Fix = fixFlags
      };

      appender.ActivateOptions();

      return appender;
    }

    public class RemoteLoggingSinkImpl : MarshalByRefObject, RemotingAppender.IRemoteLoggingSink
    {
      public static readonly RemoteLoggingSinkImpl Instance = new();

      private readonly List<LoggingEvent> m_events = new();

      private RemoteLoggingSinkImpl()
      {
      }

      /// <summary>
      /// Logs the events to an internal buffer.
      /// </summary>
      /// <param name="events">The events to log.</param>
      /// <remarks>
      /// The logged events can 
      /// be retrieved via the <see cref="Events"/> property. To clear
      /// the buffer call the <see cref="Reset"/> method.
      /// </remarks>
      public void LogEvents(LoggingEvent[] events) => m_events.AddRange(events);

      /// <summary>
      /// Obtains a lifetime service object to control the lifetime 
      /// policy for this instance.
      /// </summary>
      /// <returns>
      /// <c>null</c> to indicate that this instance should live
      /// forever.
      /// </returns>
      public override object? InitializeLifetimeService()
      {
        return null;
      }

      public void Reset() => m_events.Clear();

      public LoggingEvent[] Events => m_events.ToArray();
    }
  }
}

// helper for TestNestedNdc

namespace log4net.Tests.Appender.Remoting.UserInterfaces
{
  public class TestService
  {
    private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    public void Test()
    {
      log.Info("begin test");
      Thread.Sleep(100);

      var f = new Feature();
      f.Test();
      log.Info("end test");
      Thread.Sleep(100);
    }
  }
}

// helper for TestNestedNdc

namespace log4net.Tests.Appender.Remoting
{
  public class Feature
  {
    private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    public void Test()
    {
      using (NDC.Push("test1"))
      {
        log.Info("feature");
        Thread.Sleep(100);

        var d = new Dal();
        d.Test();
        log.Info("return");
        Thread.Sleep(100);
      }
    }
  }
}

// helper for TestNestedNdc

namespace log4net.Tests.Appender.Remoting.Data
{
  public class Dal
  {
    private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    public void Test()
    {
      using (NDC.Push("test2"))
      {
        log.Info("return");
        Thread.Sleep(100);
      }
    }
  }
}

#endif // NET462_OR_GREATER