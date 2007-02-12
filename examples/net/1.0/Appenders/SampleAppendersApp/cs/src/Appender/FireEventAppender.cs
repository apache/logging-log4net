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

using log4net.Core;

namespace SampleAppendersApp.Appender
{
	public delegate void MessageLoggedEventHandler(object sender, MessageLoggedEventArgs e);

	public class MessageLoggedEventArgs : EventArgs
	{
		private LoggingEvent m_loggingEvent;

		public MessageLoggedEventArgs(LoggingEvent loggingEvent)
		{
			m_loggingEvent = loggingEvent;
		}
		public LoggingEvent LoggingEvent
		{
			get { return m_loggingEvent; }
		}
	}

	/// <summary>
	/// Appender that raises an event for each LoggingEvent received
	/// </summary>
	/// <remarks>
	/// Raises a MessageLoggedEvent for each LoggingEvent object received
	/// by this appender.
	/// </remarks>
	public class FireEventAppender : log4net.Appender.AppenderSkeleton
	{
		private static FireEventAppender m_instance;

		private FixFlags m_fixFlags = FixFlags.All;

		// Event handler
		public event MessageLoggedEventHandler MessageLoggedEvent;

		// Easy singleton, gets the last instance created
		public static FireEventAppender Instance
		{
			get { return m_instance; }
		}

		public FireEventAppender()
		{
			// Store the instance created
			m_instance = this;
		}

		virtual public FixFlags Fix
		{
			get { return m_fixFlags; }
			set { m_fixFlags = value; }
		}

		override protected void Append(LoggingEvent loggingEvent) 
		{
			// Because we the LoggingEvent may be used beyond the lifetime 
			// of the Append() method we must fix any volatile data in the event
			loggingEvent.Fix = this.Fix;

			// Raise the event
			MessageLoggedEventHandler handler = MessageLoggedEvent;
			if (handler != null)
			{
				handler(this, new MessageLoggedEventArgs(loggingEvent));
			}
		} 
	}
}
