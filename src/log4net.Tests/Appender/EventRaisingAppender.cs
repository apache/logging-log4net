/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

#if !NET_2_0 && !MONO_2_0

using System;

namespace log4net.Tests.Appender
{
    /// <summary>
    /// Provides data for the <see cref="EventRaisingAppender.LoggingEventAppended"/> event.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class LoggingEventEventArgs : EventArgs
    {
        public log4net.Core.LoggingEvent LoggingEvent { get; private set; }

        public LoggingEventEventArgs(log4net.Core.LoggingEvent loggingEvent)
        {
            if (loggingEvent == null) throw new ArgumentNullException("loggingEvent");
            LoggingEvent = loggingEvent;
        }
    }

    /// <summary>
    /// A log4net appender that raises an event each time a logging event is appended
    /// </summary>
    /// <remarks>
    /// This class is intended to provide a way for test code to inspect logging
    /// events as they are generated.
    /// </remarks>
    public class EventRaisingAppender : log4net.Appender.IAppender
    {
        public event EventHandler<LoggingEventEventArgs> LoggingEventAppended;

        protected void OnLoggingEventAppended(LoggingEventEventArgs e)
        {
            var loggingEventAppended = LoggingEventAppended;
            if (loggingEventAppended != null)
            {
                loggingEventAppended(this, e);
            }
        }

        public void Close()
        {
        }

        public void DoAppend(log4net.Core.LoggingEvent loggingEvent)
        {
            OnLoggingEventAppended(new LoggingEventEventArgs(loggingEvent));
        }

        public string Name
        {
            get; set;
        }
    }
}
#endif