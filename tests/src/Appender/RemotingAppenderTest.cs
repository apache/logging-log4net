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
using System.Collections;
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
		private IChannel m_remotingChannel = null;

		/// <summary>
		/// Test that the Message property is correctly remoted
		/// </summary>
		[Test]
		public void TestRemotedMessage()
		{
			// Setup the remoting appender
			ConfigureRootAppender(FixFlags.Partial);

			RemoteLoggingSinkImpl.Instance.Reset();

			Logger root;
			root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;

			string testMessage = string.Format("test message [ {0} ]", (new Random()).Next());

			// Log a message that will be remoted
			root.Log(Level.Debug, testMessage, null);

			// Wait for the remoted object to be delivered
			Thread.Sleep(2000);

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
			// Setup the remoting appender
			ConfigureRootAppender(FixFlags.Partial);

			RemoteLoggingSinkImpl.Instance.Reset();

			Logger root;
			root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;

			// Log a message that will be remoted
			root.Log(Level.Debug, "test message", null);

			// Wait for the remoted object to be delivered
			Thread.Sleep(2000);

			LoggingEvent[] events = RemoteLoggingSinkImpl.Instance.Events;
			Assert.AreEqual(1, events.Length, "Expect to receive 1 remoted event");

			// Grab the event data
			LoggingEventData eventData = GetLoggingEventData(events[0]);

			Assert.IsNull(eventData.LocationInfo, "Expect LocationInfo to be null because only doing a partial fix");
		}

		/// <summary>
		/// Test that the LocationInfo property is remoted when doing a Fix.All
		/// </summary>
		[Test]
		public void TestFullFix()
		{
			// Setup the remoting appender
			ConfigureRootAppender(FixFlags.All);

			RemoteLoggingSinkImpl.Instance.Reset();

			Logger root;
			root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;

			// Log a message that will be remoted
			root.Log(Level.Debug, "test message", null);

			// Wait for the remoted object to be delivered
			Thread.Sleep(2000);

			LoggingEvent[] events = RemoteLoggingSinkImpl.Instance.Events;
			Assert.AreEqual(1, events.Length, "Expect to receive 1 remoted event");

			// Grab the event data
			LoggingEventData eventData = GetLoggingEventData(events[0]);

			Assert.IsNotNull(eventData.LocationInfo, "Expect LocationInfo to not be null because doing a full fix");
		}

		/// <summary>
		/// Test that the Message property is correctly remoted
		/// </summary>
		[Test]
		public void TestRemotedMessageNdcPushPop()
		{
			// Setup the remoting appender
			ConfigureRootAppender(FixFlags.Partial);

			RemoteLoggingSinkImpl.Instance.Reset();

			Logger root;
			root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;

			string testMessage = string.Format("test message [ {0} ]", (new Random()).Next());

			using(NDC.Push("value"))
			{
			}

			// Log a message that will be remoted
			root.Log(Level.Debug, testMessage, null);

			// Wait for the remoted object to be delivered
			Thread.Sleep(2000);

			LoggingEvent[] events = RemoteLoggingSinkImpl.Instance.Events;
			Assert.AreEqual(1, events.Length, "Expect to receive 1 remoted event");

			Assert.AreEqual(testMessage, events[0].RenderedMessage, "Expect Message match after remoting event");
		}

		[Test]
		public void TestNestedNdc()
		{
			// This test can suffer from timing and ordering issues as the RemotingAppender does dispatch events asynchronously

			// Setup the remoting appender
			ConfigureRootAppender(FixFlags.Partial);

			RemoteLoggingSinkImpl.Instance.Reset();

			TestService t;
			t = new TestService();
			t.Test();

			// Wait for the remoted objects to be delivered
			Thread.Sleep(3000);

			LoggingEvent[] events = RemoteLoggingSinkImpl.Instance.Events;
			Assert.AreEqual(5, events.Length, "Expect to receive 5 remoted event");

			Assert.AreEqual("begin test", events[0].RenderedMessage, "Verify event 1 RenderedMessage");
			Assert.AreEqual("feature", events[1].RenderedMessage, "Verify event 2 RenderedMessage");
			Assert.AreEqual("return", events[2].RenderedMessage, "Verify event 3 RenderedMessage");
			Assert.AreEqual("return", events[3].RenderedMessage, "Verify event 4 RenderedMessage");
			Assert.AreEqual("end test", events[4].RenderedMessage, "Verify event 5 RenderedMessage");

			Assert.IsNull(events[0].Properties["NDC"], "Verify event 1 Properties");
			Assert.AreEqual("test1", events[1].Properties["NDC"], "Verify event 2 Properties");
			Assert.AreEqual("test1 test2", events[2].Properties["NDC"], "Verify event 3 Properties");
			Assert.AreEqual("test1", events[3].Properties["NDC"], "Verify event 4 Properties");
			Assert.IsNull(events[4].Properties["NDC"], "Verify event 5 Properties");
		}


		private void RegisterRemotingServerChannel()
		{
			if (m_remotingChannel == null)
			{
				m_remotingChannel = new TcpChannel(8085);

				// Setup remoting server
				try
				{
#if NET_2_0 || MONO_2_0
					ChannelServices.RegisterChannel(m_remotingChannel, false);
#else
					ChannelServices.RegisterChannel(m_remotingChannel);
#endif
				}
				catch(Exception ex)
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
        [TestFixtureTearDown]
        public void UnregisterRemotingServerChannel() {
            if (m_remotingChannel != null) {
                ((TcpChannel) m_remotingChannel).StopListening(null);
                try {
                    ChannelServices.UnregisterChannel(m_remotingChannel);
                }
                catch (Exception) {
                }
                m_remotingChannel = null;
            }
        }

		/// <summary>
		/// Configures the root appender for counting and rolling
		/// </summary>
		private static void ConfigureRootAppender(FixFlags fixFlags)
		{
			Logger root;
			root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;
			root.Level = Level.Debug;
			root.AddAppender(CreateAppender(fixFlags));
			root.Repository.Configured = true;
		}

		private static RemotingAppender CreateAppender(FixFlags fixFlags)
		{
			RemotingAppender appender = new RemotingAppender();
			appender.Sink = "tcp://localhost:8085/LoggingSink";
			appender.Lossy = false;
			appender.BufferSize = 1;
			appender.Fix = fixFlags;

			appender.ActivateOptions();

			return appender;
		}

		public class RemoteLoggingSinkImpl : MarshalByRefObject, RemotingAppender.IRemoteLoggingSink
		{
			public static readonly RemoteLoggingSinkImpl Instance = new RemoteLoggingSinkImpl();

			private ArrayList m_events = new ArrayList();

			#region Public Instance Constructors
			private RemoteLoggingSinkImpl()
			{
			}
			#endregion Public Instance Constructors

			#region Implementation of IRemoteLoggingSink
			/// <summary>
			/// Logs the events to to an internal buffer
			/// </summary>
			/// <param name="events">The events to log.</param>
			/// <remarks>
			/// Logs the events to to an internal buffer. The logged events can 
			/// be retrieved via the <see cref="Events"/> property. To clear
			/// the buffer call the <see cref="Reset"/> method.
			/// </remarks>
			public void LogEvents(LoggingEvent[] events)
			{
				m_events.AddRange(events);
			}
			#endregion Implementation of IRemoteLoggingSink

			#region Override implementation of MarshalByRefObject
			/// <summary>
			/// Obtains a lifetime service object to control the lifetime 
			/// policy for this instance.
			/// </summary>
			/// <returns>
			/// <c>null</c> to indicate that this instance should live
			/// forever.
			/// </returns>
			public override object InitializeLifetimeService()
			{
				return null;
			}
			#endregion Override implementation of MarshalByRefObject

			public void Reset()
			{
				m_events.Clear();
			}

			public LoggingEvent[] Events
			{
				get { return (LoggingEvent[])m_events.ToArray(typeof(LoggingEvent)); }
			}
		}

		//
		// Helper functions to dig into the appender
		//

		private static LoggingEventData GetLoggingEventData(LoggingEvent loggingEvent)
		{
			return (LoggingEventData)Utils.GetField(loggingEvent, "m_data");
		}
	}
}

// helper for TestNestedNdc

namespace log4net.Tests.Appender.Remoting.UserInterfaces
{
	public class TestService
	{
		private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void Test()
		{
			log.Info("begin test");
			Thread.Sleep(100);

			Feature f = new Feature();
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
		private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void Test()
		{
			using(NDC.Push("test1"))
			{
				log.Info("feature");
				Thread.Sleep(100);

				Dal d = new Dal();
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
		private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void Test()
		{
			using(NDC.Push("test2"))
			{
				log.Info("return");
				Thread.Sleep(100);
			}
		}
	}
}