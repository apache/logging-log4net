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
using System.Threading;
using log4net.Appender;
using log4net.Core;
using log4net.Util;

namespace log4net.Appender
{
	/// <summary>
	/// Appender that forwards LoggingEvents asynchronously
	/// </summary>
	/// <remarks>
	/// This appender forwards LoggingEvents to a list of attached appenders.
	/// The events are forwarded asynchronously using the ThreadPool.
	/// This allows the calling thread to be released quickly, however it does
	/// not guarantee the ordering of events delivered to the attached appenders.
	/// </remarks>
	public sealed class AsyncAppender : ForwardingAppender, IBulkAppender
	{

		/// <summary>
		/// Gets or sets a the fields that will be fixed in the event
		/// </summary>
		/// <value>
		/// The event fields that will be fixed before the event is forwarded
		/// </value>
		/// <remarks>
		/// <para>
		/// The logging event needs to have certain thread specific values 
		/// captured before it can be forwarded to a different thread.
		/// See <see cref="LoggingEvent.Fix"/> for details.
		/// </para>
		/// </remarks>
		/// <seealso cref="LoggingEvent.Fix"/>
		public FixFlags Fix
		{
			get { return m_fixFlags; }
			set { m_fixFlags = value; }
		}

		/// <summary>
		/// Forward the logging event to the attached appenders on a ThreadPool thread
		/// </summary>
		/// <param name="loggingEvent">The event to log.</param>
		/// <remarks>
		/// <para>
		/// Delivers the logging event to all the attached appenders on a ThreadPool thread.
		/// </para>
		/// </remarks>
		override protected void Append(LoggingEvent loggingEvent) 
		{
			loggingEvent.Fix = m_fixFlags;
			ThreadPool.QueueUserWorkItem(AsyncAppend, loggingEvent);
		}

		/// <summary>
		/// Forward the logging events to the attached appenders on a ThreadPool thread
		/// </summary>
		/// <param name="loggingEvents">The array of events to log.</param>
		/// <remarks>
		/// <para>
		/// Delivers the logging events to all the attached appenders on a ThreadPool thread.
		/// </para>
		/// </remarks>
		override protected void Append(LoggingEvent[] loggingEvents) 
		{
			foreach(LoggingEvent loggingEvent in loggingEvents)
			{
				loggingEvent.Fix = m_fixFlags;
			}
			ThreadPool.QueueUserWorkItem(AsyncAppend, loggingEvents);
		}

		private void AsyncAppend(object state)
		{
			LoggingEvent loggingEvent = state as LoggingEvent;
			LoggingEvent[] loggingEvents = state as LoggingEvent[];
			if (loggingEvent != null)
			{
				base.Append(loggingEvent);
			}
			else if (loggingEvents != null)
			{
				base.Append(loggingEvents);
			}
		}

		private FixFlags m_fixFlags = FixFlags.All;
	}
}
