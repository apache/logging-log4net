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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender.Internal;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Tests for <see cref="TelnetAppender"/>
/// </summary>
[TestFixture]
public sealed class TelnetAppenderTest
{
  /// <summary>
  /// Simple Test for the <see cref="TelnetAppender"/>
  /// </summary>
  /// <remarks>
  /// https://github.com/apache/logging-log4net/issues/194
  /// https://stackoverflow.com/questions/79053363/log4net-telnetappender-doesnt-work-after-migrate-to-log4net-3
  /// </remarks>
  /// <summary>
  /// An unpaired surrogate used to throw while encoding, and Send reads any throw as a client
  /// that hung up, so one such event reached nobody and disconnected everybody.
  /// </summary>
  [Test]
  public void ContentThatCannotBeEncodedDoesNotDisconnectTheClient()
  {
    StringBuilder received = new();
    object receivedSyncRoot = new();

    int port = FindFreeTcpPort();
    XmlDocument log4NetConfig = new();
    log4NetConfig.LoadXml(
      $"""
      <log4net>
        <appender name="TelnetAppender" type="log4net.Appender.TelnetAppender">
          <port value="{port}" />
          <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%message%newline" />
          </layout>
        </appender>
        <root>
          <level value="INFO"/>
          <appender-ref ref="TelnetAppender"/>
        </root>
      </log4net>
      """);
    string marker = Guid.NewGuid().ToString();
    ILoggerRepository repository = LogManager.CreateRepository(marker);
    XmlConfigurator.Configure(repository, log4NetConfig["log4net"]!);
    try
    {
      using (SimpleTelnetClient telnetClient = new(Received, port))
      {
        telnetClient.Run(TestContext.Out.WriteLine);
        WaitFor("welcome message", WelcomeMessage);

        ILogger logger = repository.GetLogger("Telnet");
        logger.Log(typeof(TelnetAppenderTest), Level.Info, "poison\ud800event", null);
        // The event after it only arrives if the client survived the one before.
        logger.Log(typeof(TelnetAppenderTest), Level.Info, marker, null);
        WaitFor("the event after the unencodable one", marker);
      }
    }
    finally
    {
      repository.Shutdown();
    }

    Assert.That(ReceivedText(), Does.Contain(@"poison\ud800event"));

    void Received(string message)
    {
      lock (receivedSyncRoot)
      {
        received.Append(message);
      }
    }

    string ReceivedText()
    {
      lock (receivedSyncRoot)
      {
        return received.ToString();
      }
    }

    void WaitFor(string what, string expected)
    {
      Stopwatch stopwatch = Stopwatch.StartNew();
      while (ReceivedText().IndexOf(expected, StringComparison.Ordinal) < 0)
      {
        if (stopwatch.Elapsed > _receiveTimeout)
        {
          Assert.Fail($"Timeout waiting for {what} - received so far: '{ReceivedText()}'");
        }
        Thread.Sleep(20);
      }
    }
  }

  /// <summary>
  /// Maximum time to wait for a message to arrive at the client.
  /// </summary>
  private static readonly TimeSpan _receiveTimeout = TimeSpan.FromSeconds(30);

  private const string WelcomeMessage = "TelnetAppender";

  [Test]
  public void TelnetTest()
  {
    // The received data is a TCP byte stream - the server writes are not necessarily
    // mapped 1:1 to the client reads, so the test asserts on the accumulated text
    // instead of counting the individual reads.
    StringBuilder received = new();
    object receivedSyncRoot = new();

    XmlDocument log4NetConfig = new();
    int port = FindFreeTcpPort();
    log4NetConfig.LoadXml(
      $"""
      <log4net>
        <appender name="TelnetAppender" type="log4net.Appender.TelnetAppender">
          <port value="{port}" />
          <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%date %-5level - %message%newline" />
          </layout>
        </appender>
        <root>
          <level value="INFO"/>
          <appender-ref ref="TelnetAppender"/>
        </root>
      </log4net>
      """);
    string logId = Guid.NewGuid().ToString();
    ILoggerRepository repository = LogManager.CreateRepository(logId);
    XmlConfigurator.Configure(repository, log4NetConfig["log4net"]!);
    try
    {
      using (SimpleTelnetClient telnetClient = new(Received, port))
      {
        telnetClient.Run(TestContext.Out.WriteLine);
        WaitForReceived("welcome message", WelcomeMessage);
        ILogger logger = repository.GetLogger("Telnet");
        logger.Log(typeof(TelnetAppenderTest), Level.Info, logId, null);
        WaitForReceived("log message", logId);
      }
    }
    finally
    {
      repository.Shutdown();
    }
    Assert.That(ReceivedText(), Does.StartWith(WelcomeMessage).And.Contain(logId));

    void Received(string message)
    {
      lock (receivedSyncRoot)
      {
        received.Append(message);
      }
    }

    string ReceivedText()
    {
      lock (receivedSyncRoot)
      {
        return received.ToString();
      }
    }

    void WaitForReceived(string what, string expected)
    {
      Stopwatch stopwatch = Stopwatch.StartNew();
      while (ReceivedText().IndexOf(expected, StringComparison.Ordinal) < 0)
      {
        if (stopwatch.Elapsed > _receiveTimeout)
        {
          Assert.Fail($"Timeout waiting for {what} - received so far: '{ReceivedText()}'");
        }
        Thread.Sleep(20);
      }
    }
  }

  /// <summary>
  /// Writes to a client block while the appender lock is held, so the send timeout has to be
  /// finite by default - otherwise a client that stops reading suspends all logging.
  /// </summary>
  [Test]
  public void SendTimeoutMillisDefaultsToAFiniteValue()
    => Assert.That(new TelnetAppender().SendTimeoutMillis, Is.EqualTo(5000));

  /// <summary>
  /// 0 is the documented opt-out that restores blocking indefinitely; a negative timeout has no
  /// meaning for <see cref="Socket.SendTimeout"/> and is rejected instead of being silently
  /// reinterpreted.
  /// </summary>
  [Test]
  public void SendTimeoutMillisRejectsNegativeValuesButAllowsZero()
  {
    TelnetAppender appender = new();

    Assert.That(() => appender.SendTimeoutMillis = -1, Throws.TypeOf<ArgumentOutOfRangeException>());

    appender.SendTimeoutMillis = 0;
    Assert.That(appender.SendTimeoutMillis, Is.EqualTo(0));

    appender.SendTimeoutMillis = 250;
    Assert.That(appender.SendTimeoutMillis, Is.EqualTo(250));
  }

  /// <summary>
  /// The stream is unauthenticated, so an appender nobody configured an address for must not be
  /// reachable from another machine.
  /// </summary>
  [Test]
  public void ListenAddressDefaultsToLoopback()
    => Assert.That(new TelnetAppender().ListenAddress, Is.EqualTo(IPAddress.Loopback));

  /// <summary>
  /// Remote monitoring is still available, it just has to be asked for.
  /// </summary>
  [TestCase("::", TestName = "EveryIPv6InterfaceCanBeAskedFor")]
  [TestCase("0.0.0.0", TestName = "EveryIPv4InterfaceCanBeAskedFor")]
  public void EveryInterfaceCanBeAskedFor(string address)
    => Assert.That(new TelnetAppender { ListenAddress = IPAddress.Parse(address) }.ListenAddress,
      Is.EqualTo(IPAddress.Parse(address)));

  /// <summary>
  /// Clients are written to from a background thread, so the queue has to be bounded and the wait
  /// for room short: it is the only delay a connected client can impose on the application.
  /// </summary>
  [Test]
  public void SendQueueDefaults()
  {
    TelnetAppender appender = new();

    Assert.That(appender.SendQueueSize, Is.EqualTo(500));
    Assert.That(appender.EnqueueTimeoutMillis, Is.EqualTo(50));
  }

  /// <summary>
  /// A queue of no size cannot hold anything, and a negative wait has no meaning.
  /// </summary>
  [Test]
  public void SendQueueSettingsRejectMeaninglessValues()
  {
    TelnetAppender appender = new();

    Assert.That(() => appender.SendQueueSize = 0, Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => appender.EnqueueTimeoutMillis = -1, Throws.TypeOf<ArgumentOutOfRangeException>());

    appender.EnqueueTimeoutMillis = 0;
    Assert.That(appender.EnqueueTimeoutMillis, Is.EqualTo(0));
  }

  /// <summary>
  /// A client that connects and then stops reading fills its receive window, and writing to it
  /// used to block the logging thread for the whole send timeout, once per client. Logging must
  /// now return promptly however badly the client behaves.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void SlowReadersDoNotDelayLogging()
  {
    const int deadReaderCount = 4;
    const int sendTimeoutMillis = 2_000;
    // A loopback write blocks once roughly 1 MB is outstanding against the client's 1 KB receive
    // buffer, measured, so send comfortably past that.
    const int eventCount = 400;

    int port = FindFreeTcpPort();
    TelnetAppender appender = new()
    {
      Port = port,
      ListenAddress = IPAddress.Loopback,
      Layout = new PatternLayout("%message%newline"),
      SendTimeoutMillis = sendTimeoutMillis
    };
    appender.ActivateOptions();

    List<SimpleTelnetClient> deadReaders = [];
    try
    {
      for (int i = 0; i < deadReaderCount; i++)
      {
        SimpleTelnetClient deadReader = new(_ => { }, port);
        deadReaders.Add(deadReader);
        deadReader.ConnectAndStopReading();
      }

      string message = new('x', 4_096);
      Stopwatch stopwatch = Stopwatch.StartNew();
      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        for (int i = 0; i < eventCount; i++)
        {
          appender.DoAppend(CreateEvent(message));
        }
      });
      stopwatch.Stop();

      // Writing synchronously costs SendTimeoutMillis per stalled client before it is evicted,
      // serially and under the appender lock: 8s here, and 100s with the 20-client cap and the
      // default timeout. Queueing costs the enqueue wait at worst.
      Assert.That(stopwatch.Elapsed,
        Is.LessThan(TimeSpan.FromMilliseconds(deadReaderCount * sendTimeoutMillis / 2)),
        "logging blocked behind clients that stopped reading");
    }
    finally
    {
      // Let the pump finish before Close waits for a drain.
      foreach (SimpleTelnetClient deadReader in deadReaders)
      {
        deadReader.Dispose();
      }
      LogLog.ExecuteWithoutEmittingInternalMessages(appender.Close);
    }
  }

  /// <summary>
  /// A queue that cannot keep up drops, rather than growing or making the logging thread wait.
  /// The loss costs the telnet stream only, so it is counted and reported once instead of per
  /// event, which would be a denial of service of its own.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void AFullQueueDropsAndReportsOnce()
  {
    int port = FindFreeTcpPort();
    RecordingErrorHandler errorHandler = new();
    TelnetAppender appender = new()
    {
      Port = port,
      ListenAddress = IPAddress.Loopback,
      Layout = new PatternLayout("%message%newline"),
      // A queue this small fills as soon as the client stops reading, and nothing waits for room.
      SendQueueSize = 4,
      EnqueueTimeoutMillis = 0,
      ErrorHandler = errorHandler
    };
    appender.ActivateOptions();

    using SimpleTelnetClient deadReader = new(_ => { }, port);
    try
    {
      deadReader.ConnectAndStopReading();

      for (int i = 0; i < 200; i++)
      {
        appender.DoAppend(CreateEvent(new string('x', 4_096)));
      }

      Assert.That(errorHandler.Messages.FindAll(m => m.IndexOf("was dropped", StringComparison.Ordinal) >= 0),
        Has.Count.EqualTo(1), "the drop must be reported exactly once, however many events are lost");
    }
    finally
    {
      appender.Close();
    }
  }

  private static LoggingEvent CreateEvent(string message)
    => new(new LoggingEventData { Level = Level.Info, Message = message, LoggerName = "TelnetTest" });

  /// <summary>
  /// Binding to the loopback address has to keep the port unreachable from other machines, which
  /// is what an operator asking for it wants.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ListenAddressBindsOnlyThatAddress()
  {
    int port = FindFreeTcpPort();
    TelnetAppender appender = new()
    {
      Port = port,
      ListenAddress = IPAddress.Loopback,
      Layout = new PatternLayout("%message%newline")
    };
    appender.ActivateOptions();
    try
    {
      // The loopback listener accepts a loopback connection.
      using (Socket loopback = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
      {
        loopback.Connect(new IPEndPoint(IPAddress.Loopback, port));
        Assert.That(loopback.Connected, Is.True);
      }

      // and nothing is listening on the machine's other addresses
      IPAddress? routable = Array.Find(
        Dns.GetHostAddresses(Dns.GetHostName()),
        address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address));
      if (routable is null)
      {
        Assert.Ignore("no non-loopback IPv4 address on this machine to test against");
      }

      using Socket external = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      Assert.That(() => external.Connect(new IPEndPoint(routable!, port)), Throws.TypeOf<SocketException>());
    }
    finally
    {
      appender.Close();
    }
  }

  /// <summary>
  /// Asks the OS for a currently unused TCP port - a fixed port would collide with
  /// other tests or processes on the build machine.
  /// </summary>
  private static int FindFreeTcpPort()
  {
    using Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    socket.Bind(new IPEndPoint(IPAddress.Any, 0));
    if (socket.LocalEndPoint is IPEndPoint endPoint)
    {
      return endPoint.Port;
    }
    throw new InvalidOperationException("Could not determine a free TCP port");
  }
}