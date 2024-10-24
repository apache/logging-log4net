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

namespace SampleAppendersApp.Appender;

/// <inheritdoc/>
public sealed class MessageLoggedEventArgs(LoggingEvent loggingEvent) : EventArgs
{
  /// <inheritdoc/>
  public LoggingEvent LoggingEvent { get; } = loggingEvent;
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
  /// <summary>
  /// Event handler
  /// </summary>
  public event EventHandler<MessageLoggedEventArgs>? MessageLoggedEvent;

  /// <summary>
  /// Easy singleton, gets the last instance created
  /// </summary>
  public static FireEventAppender? Instance { get; private set; }

  /// <inheritdoc/>
  public FireEventAppender() => Instance = this; // Store the instance created

  /// <inheritdoc/>
  public virtual FixFlags Fix { get; set; } = FixFlags.All;

  /// <inheritdoc/>
  protected override void Append(LoggingEvent loggingEvent)
  {
    ArgumentNullException.ThrowIfNull(loggingEvent);
    // Because we the LoggingEvent may be used beyond the lifetime 
    // of the Append() method we must fix any volatile data in the event
    loggingEvent.Fix = Fix;

    // Raise the event
    MessageLoggedEvent?.Invoke(this, new MessageLoggedEventArgs(loggingEvent));
  }
}