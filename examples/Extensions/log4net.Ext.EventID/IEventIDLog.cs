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

/*
 * Custom Logging Classes to support Event IDs.
 */

using System;

using log4net;

namespace log4net.Ext.EventID
{
  /// <summary>
  /// ILog-Extension
  /// </summary>
  public interface IEventIDLog : ILog
  {
    /// <summary>
    /// Info
    /// </summary>
    void Info(int eventId, object message);

    /// <summary>
    /// Info
    /// </summary>
    void Info(int eventId, object message, Exception t);

    /// <summary>
    /// Warn
    /// </summary>
    void Warn(int eventId, object message);

    /// <summary>
    /// Warn
    /// </summary>
    void Warn(int eventId, object message, Exception t);

    /// <summary>
    /// Error
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
    void Error(int eventId, object message);

    /// <summary>
    /// Error
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
    void Error(int eventId, object message, Exception t);

    /// <summary>
    /// Fatal
    /// </summary>
    void Fatal(int eventId, object message);

    /// <summary>
    /// Fatal
    /// </summary>
    void Fatal(int eventId, object message, Exception t);
  }
}