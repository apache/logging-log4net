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

namespace log4net.Ext.EventID;

/// <summary>
/// Implementation for <see cref="IEventIDLog"/>
/// </summary>
/// <inheritdoc/>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public sealed class EventIDLogImpl(ILogger logger) : LogImpl(logger), IEventIDLog
{
  /// <summary>
  /// The fully qualified name of this declaring type not the type of any subclass.
  /// </summary>
  private static readonly Type _thisDeclaringType = typeof(EventIDLogImpl);

  #region Implementation of IEventIDLog

  /// <inheritdoc/>
  public void Info(int eventId, object message) => Info(eventId, message, null);

  /// <inheritdoc/>
  public void Info(int eventId, object message, Exception? t)
  {
    if (IsInfoEnabled)
    {
      LoggingEvent loggingEvent = new(_thisDeclaringType, Logger.Repository, Logger.Name, Level.Info, message, t);
      loggingEvent.Properties["EventID"] = eventId;
      Logger.Log(loggingEvent);
    }
  }

  /// <inheritdoc/>
  public void Warn(int eventId, object message) => Warn(eventId, message, null);

  /// <inheritdoc/>
  public void Warn(int eventId, object message, Exception? t)
  {
    if (IsWarnEnabled)
    {
      LoggingEvent loggingEvent = new(_thisDeclaringType, Logger.Repository, Logger.Name, Level.Warn, message, t);
      loggingEvent.Properties["EventID"] = eventId;
      Logger.Log(loggingEvent);
    }
  }

  /// <inheritdoc/>
  public void Error(int eventId, object message) => Error(eventId, message, null);

  /// <inheritdoc/>
  public void Error(int eventId, object message, Exception? t)
  {
    if (IsErrorEnabled)
    {
      LoggingEvent loggingEvent = new(_thisDeclaringType, Logger.Repository, Logger.Name, Level.Error, message, t);
      loggingEvent.Properties["EventID"] = eventId;
      Logger.Log(loggingEvent);
    }
  }

  /// <inheritdoc/>
  public void Fatal(int eventId, object message) => Fatal(eventId, message, null);

  /// <inheritdoc/>
  public void Fatal(int eventId, object message, Exception? t)
  {
    if (IsFatalEnabled)
    {
      LoggingEvent loggingEvent = new(_thisDeclaringType, Logger.Repository, Logger.Name, Level.Fatal, message, t);
      loggingEvent.Properties["EventID"] = eventId;
      Logger.Log(loggingEvent);
    }
  }

  #endregion Implementation of IEventIDLog
}
