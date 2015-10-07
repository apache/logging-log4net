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
using System.Management.Instrumentation;

namespace log4net.Appender
{
	public class WmiLayout
	{
		/// <summary>
		/// Formats a <see cref="LoggingEvent"/> for instrumentation
		/// </summary>
		/// <param name="loggingEvent">the <see cref="LoggingEvent"/> containing the data</param>
		/// <returns>an instrumentation event that can be fired</returns>
		/// <remarks>
		/// <para>
		/// If the <see cref="LoggingEvent.MessageObject"/> of the 
		/// <paramref name="loggingEvent" /> is an <see cref="IEvent"/> then
		/// that instance is returned. If the instance also implements the
		/// <see cref="IWmiBoundEvent"/> interface then the <see cref="IWmiBoundEvent.Bind"/>
		/// method will be called on the instance with the <paramref name="loggingEvent" />
		/// parameter.
		/// </para>
		/// <para>
		/// If the <see cref="LoggingEvent.MessageObject"/> of the 
		/// <paramref name="loggingEvent" /> is not an <see cref="IEvent"/>
		/// then the <see cref="CreateEvent"/> method will be called
		/// to create an appropriate instrumentation event object.
		/// </para>
		/// </remarks>
		public virtual IEvent Format(LoggingEvent loggingEvent)
		{
			// See if the caller gave us an Instrumentation Event
			IEvent instrumentationEvent = loggingEvent.MessageObject as IEvent;
			if (instrumentationEvent != null)
			{
				// See if the caller gave us a Bound Instrumentation Event
				IWmiBoundEvent boundEvent = instrumentationEvent as IWmiBoundEvent;
				if (boundEvent != null)
				{
					// Attach the logging event to the bound instrumentation event
					boundEvent.Bind(loggingEvent);
				}

				return instrumentationEvent;
			}

			// We must create our own IEvent
			return CreateEvent(loggingEvent);
		}

		/// <summary>
		/// Create the <see cref="IEvent"/> instance that should be fired
		/// </summary>
		/// <param name="loggingEvent">the <see cref="LoggingEvent"/> containing the data</param>
		/// <returns>an instrumentation event that can be fired</returns>
		/// <remarks>
		/// <para>
		/// The default implementation of this method creates a <see cref="WmiLoggingEvent"/>
		/// instance using the data from the <paramref name="loggingEvent" />.
		/// </para>
		/// <para>
		/// Subclasses should override this method to return their own custom 
		/// instrumentation event object.
		/// </para>
		/// </remarks>
		protected virtual IEvent CreateEvent(LoggingEvent loggingEvent)
		{
			WmiLoggingEvent wmiEvent = new WmiLoggingEvent();

			wmiEvent.TimeStamp = loggingEvent.TimeStamp;
			wmiEvent.LoggerName = loggingEvent.LoggerName;
			wmiEvent.Level = loggingEvent.Level.DisplayName;
			wmiEvent.Message = loggingEvent.RenderedMessage;
			wmiEvent.ThreadName = loggingEvent.ThreadName;
			wmiEvent.ExceptionString = loggingEvent.GetExceptionString();
			wmiEvent.Domain = loggingEvent.Domain;

			return wmiEvent;
		}
	}
}
