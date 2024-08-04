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
using log4net.Core;

namespace log4net.Appender
{
  /// <summary>
  /// Stores logging events in an array.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The memory appender stores all the logging events
  /// that are appended in an in-memory array.
  /// </para>
  /// <para>
  /// Use the <see cref="M:PopAllEvents()"/> method to get
  /// and clear the current list of events that have been appended.
  /// </para>
  /// <para>
  /// Use the <see cref="M:GetEvents()"/> method to get the current
  /// list of events that have been appended.  Note there is a
  /// race-condition when calling <see cref="M:GetEvents()"/> and
  /// <see cref="M:Clear()"/> in pairs, you better use <see
  /// mref="M:PopAllEvents()"/> in that case.
  /// </para>
  /// <para>
  /// Use the <see cref="M:Clear()"/> method to clear the
  /// current list of events.  Note there is a
  /// race-condition when calling <see cref="M:GetEvents()"/> and
  /// <see cref="M:Clear()"/> in pairs, you better use <see
  /// mref="M:PopAllEvents()"/> in that case.
  /// </para>
  /// </remarks>
  /// <author>Julian Biddle</author>
  /// <author>Nicko Cadell</author>
  /// <author>Gert Driesen</author>
  public class MemoryAppender : AppenderSkeleton
  {
    /// <summary>
    /// Gets the events that have been logged.
    /// </summary>
    /// <returns>The events that have been logged</returns>
    public virtual LoggingEvent[] GetEvents()
    {
      lock (m_lockObj)
      {
        return m_eventsList.ToArray();
      }
    }

    /// <summary>
    /// Gets or sets the fields that will be fixed in the event
    /// </summary>
    /// <remarks>
    /// <para>
    /// The logging event needs to have certain thread specific values 
    /// captured before it can be buffered. See <see cref="LoggingEvent.Fix"/>
    /// for details.
    /// </para>
    /// </remarks>
    public virtual FixFlags Fix { get; set; } = FixFlags.All;

    /// <summary>
    /// This method is called by the <see cref="M:AppenderSkeleton.DoAppend(LoggingEvent)"/> method. 
    /// </summary>
    /// <param name="loggingEvent">the event to log</param>
    /// <remarks>
    /// <para>Stores the <paramref name="loggingEvent"/> in the events list.</para>
    /// </remarks>
    protected override void Append(LoggingEvent loggingEvent)
    {
      // Because we are caching the LoggingEvent beyond the
      // lifetime of the Append() method we must fix any
      // volatile data in the event.
      loggingEvent.Fix = Fix;

      lock (m_lockObj)
      {
        m_eventsList.Add(loggingEvent);
      }
    }

    /// <summary>
    /// Clear the list of events
    /// </summary>
    /// <remarks>
    /// Clear the list of events
    /// </remarks>
    public virtual void Clear()
    {
      lock (m_lockObj)
      {
        m_eventsList.Clear();
      }
    }

    /// <summary>
    /// Gets the events that have been logged and clears the list of events.
    /// </summary>
    /// <returns>The events that have been logged</returns>
    public virtual LoggingEvent[] PopAllEvents()
    {
      lock (m_lockObj)
      {
        LoggingEvent[] tmp = m_eventsList.ToArray();
        m_eventsList.Clear();
        return tmp;
      }
    }

    /// <summary>
    /// The list of events that have been appended.
    /// </summary>
    protected List<LoggingEvent> m_eventsList = new();

    private readonly object m_lockObj = new();
  }
}
