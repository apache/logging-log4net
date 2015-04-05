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
#if FRAMEWORK_4_0_OR_ABOVE
using System.Threading.Tasks;
#else
using System.Threading;
#endif
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
    public sealed class AsyncAppender : ForwardingAppender
    {
        /// <summary>
        ///   Creates a new AsyncAppender.
        /// </summary>
        public AsyncAppender()
        {
#if FRAMEWORK_4_0_OR_ABOVE
            logTask = new Task(() => { });
            logTask.Start();
#endif
        }

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
            lock (lockObject)
            {
                if (closed)
                {
                    return;
                }
                events.Add(loggingEvent);
            }
#if FRAMEWORK_4_0_OR_ABOVE
            logTask.ContinueWith(AsyncAppend);
#else
            ThreadPool.QueueUserWorkItem(AsyncAppend, null);
#endif
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
            foreach (LoggingEvent loggingEvent in loggingEvents)
            {
                loggingEvent.Fix = m_fixFlags;
            }
            lock (lockObject)
            {
                if (closed)
                {
                    return;
                }
                events.AddRange(loggingEvents);
            }
#if FRAMEWORK_4_0_OR_ABOVE
            logTask.ContinueWith(AsyncAppend);
#else
            ThreadPool.QueueUserWorkItem(AsyncAppend, null);
#endif
        }

        /// <summary>
        /// Closes the appender and releases resources.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Releases any resources allocated within the appender such as file handles, 
        /// network connections, etc.
        /// </para>
        /// <para>
        /// It is a programming error to append to a closed appender.
        /// </para>
        /// </remarks>
        override protected void OnClose()
        {
            lock (lockObject)
            {
#if FRAMEWORK_4_0_OR_ABOVE
                if (!closed)
                {
                    logTask.Wait();
                }
#endif
                closed = true;
            }
            base.OnClose();
        }

        private void AsyncAppend(object _ignored)
        {
#if FRAMEWORK_4_0_OR_ABOVE // ContinueWith already ensures there is only one thread executing this method at a time
            ForwardEvents();
#else
            lock (lockObject)
            {
                if (inLoggingLoop)
                {
                    return;
                }
                inLoggingLoop = true;
            }
            try
            {
                while (true)
                {
                    if (!ForwardEvents())
                    {
                        break;
                    }
                }
            }
            finally
            {
                lock (lockObject)
                {
                    inLoggingLoop = false;
                }
            }
#endif
        }

        /// <summary>
        ///   Forwards the queued events to the nested appenders.
        /// </summary>
        /// <returns>whether there have been any events to forward.</returns>
        private bool ForwardEvents()
        {
            LoggingEvent[] loggingEvents = null;
            lock (lockObject)
            {
                loggingEvents = events.ToArray();
                events.Clear();
            }
            if (loggingEvents.Length == 0)
            {
                return false;
            }
            base.Append(loggingEvents);
            return true;
        }

        private FixFlags m_fixFlags = FixFlags.All;
        private readonly object lockObject = new object();
        private readonly List<LoggingEvent> events = new List<LoggingEvent>();
        private bool closed = false;
#if FRAMEWORK_4_0_OR_ABOVE
        private readonly Task logTask;
#else
        private bool inLoggingLoop = false;
#endif
    }
}
