#region Copyright & License
//
// Copyright 2001-2005 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
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
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using log4net.Util;
using log4net.Layout;
using log4net.Core;
using log4net.Appender;
using IRemoteLoggingSink = log4net.Appender.RemotingAppender.IRemoteLoggingSink;

using NUnit.Framework;

namespace log4net.Tests.Appender
{
	/// <summary>
	/// Used for internal unit testing the <see cref="RemotingAppender"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="RemotingAppender"/> class.
	/// </remarks>
	[TestFixture] public class RemotingAppenderTest
	{
		private IChannel m_remotingChannel = null;

		/// <summary>
		/// Test that the Message property is correctly remoted
		/// </summary>
		[Test] public void TestRemotedMessage()
		{
			// Setup the remoting appender
			ConfigureRootAppender(FixFlags.Partial);

			RemoteLoggingSinkImpl.Instance.Events = null;

			log4net.Repository.Hierarchy.Logger root = null;
			root = ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;	

			string testMessage = string.Format("test message [ {0} ]", (new Random()).Next());

			// Log a message that will be remoted
			root.Log(Level.Debug, testMessage, null);

			// Wait for the remoted object to be delivered
			System.Threading.Thread.Sleep(1000);

			LoggingEvent[] events = RemoteLoggingSinkImpl.Instance.Events;
			Assertion.AssertEquals("Expect to receive 1 remoted event", 1, events.Length);

			Assertion.AssertEquals("Expect Message match after remoting event", testMessage, events[0].RenderedMessage);
		}

		/// <summary>
		/// Test that the UserName property is not remoted when doing a Fix.Partial
		/// </summary>
		[Test] public void TestPartialFix()
		{
			// Setup the remoting appender
			ConfigureRootAppender(FixFlags.Partial);

			RemoteLoggingSinkImpl.Instance.Events = null;

			log4net.Repository.Hierarchy.Logger root = null;
			root = ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;	

			// Log a message that will be remoted
			root.Log(Level.Debug, "test message", null);

			// Wait for the remoted object to be delivered
			System.Threading.Thread.Sleep(1000);

			LoggingEvent[] events = RemoteLoggingSinkImpl.Instance.Events;
			Assertion.AssertEquals("Expect to receive 1 remoted event", 1, events.Length);

			// Grab the event data
			LoggingEventData eventData = GetLoggingEventData(events[0]);

			Assertion.AssertNull("Expect username to be null because only doing a partial fix", eventData.UserName);
		}

		/// <summary>
		/// Test that the UserName property is remoted when doing a Fix.All
		/// </summary>
		[Test] public void TestFullFix()
		{
			// Setup the remoting appender
			ConfigureRootAppender(FixFlags.All);

			RemoteLoggingSinkImpl.Instance.Events = null;

			log4net.Repository.Hierarchy.Logger root = null;
			root = ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;	

			// Log a message that will be remoted
			root.Log(Level.Debug, "test message", null);

			// Wait for the remoted object to be delivered
			System.Threading.Thread.Sleep(1000);

			LoggingEvent[] events = RemoteLoggingSinkImpl.Instance.Events;
			Assertion.AssertEquals("Expect to receive 1 remoted event", 1, events.Length);

			// Grab the event data
			LoggingEventData eventData = GetLoggingEventData(events[0]);

			Assertion.AssertNotNull("Expect username to not be null because doing a full fix", eventData.UserName);
		}

		private void RegisterRemotingServerChannel()
		{
			if (m_remotingChannel == null)
			{
				m_remotingChannel = new TcpChannel(8085);

				// Setup remoting server
				try
				{
					ChannelServices.RegisterChannel(m_remotingChannel);
				}
				catch(Exception)
				{
				}

				// Marshal the sink object
				RemotingServices.Marshal(RemoteLoggingSinkImpl.Instance, "LoggingSink", typeof(IRemoteLoggingSink));
			}
		}

		/// <summary>
		/// Shuts down any loggers in the hierarchy, along
		/// with all appenders.
		/// </summary>
		private void ResetRepository()
		{
			// Regular users should not use the clear method lightly!
			LogManager.GetRepository().ResetConfiguration();
			LogManager.GetRepository().Shutdown();
			((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Clear();
		}

		/// <summary>
		/// Any initialization that happens before each test can
		/// go here
		/// </summary>
		[SetUp] public void SetUp() 
		{
			ResetRepository();
			RegisterRemotingServerChannel();
		}

		/// <summary>
		/// Any steps that happen after each test go here
		/// </summary>
		[TearDown] public void TearDown() 
		{
			ResetRepository();
		}

		/// <summary>
		/// Configures the root appender for counting and rolling
		/// </summary>
		private void ConfigureRootAppender(FixFlags fixFlags)
		{
			log4net.Repository.Hierarchy.Logger root = null;
			root = ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;	
			root.Level = Level.Debug;
			root.AddAppender(CreateAppender(fixFlags));
			root.Repository.Configured = true;
		}

		private RemotingAppender CreateAppender(FixFlags fixFlags)
		{
			RemotingAppender appender = new RemotingAppender();
			appender.Sink = "tcp://localhost:8085/LoggingSink";
			appender.Lossy = false;
			appender.BufferSize = 1;
			appender.Fix = fixFlags;

			appender.ActivateOptions();

			return appender;
		}

		public class RemoteLoggingSinkImpl : MarshalByRefObject, IRemoteLoggingSink
		{
			public static readonly RemoteLoggingSinkImpl Instance = new RemoteLoggingSinkImpl();

			public LoggingEvent[] Events = null;

			#region Public Instance Constructors

			private RemoteLoggingSinkImpl()
			{
			}

			#endregion Public Instance Constructors

			#region Implementation of IRemoteLoggingSink

			/// <summary>
			/// Logs the events to the repository.
			/// </summary>
			/// <param name="events">The events to log.</param>
			/// <remarks>
			/// The events passed are logged to the <see cref="LoggerRepository"/>
			/// </remarks>
			public void LogEvents(LoggingEvent[] events)
			{
				Events = events;
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
