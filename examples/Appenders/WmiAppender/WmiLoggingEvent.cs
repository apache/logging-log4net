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
using System.Management.Instrumentation;

namespace log4net.Appender
{
  /// <summary>
  /// The default instrumented event raised by the <see cref="WmiAppender"/>
  /// </summary>
  /// <para>
  /// This is the default event fired by the <see cref="WmiAppender"/>.
  /// To fire a custom event set the <see cref="WmiAppender.Layout"/> to a
  /// subclass of <see cref="WmiLayout"/> that overrides the <see cref="WmiLayout.CreateEvent"/>
  /// method.
  /// </para>
  public sealed class WmiLoggingEvent : BaseEvent
  {
    /// <inheritdoc/>
    public DateTime TimeStamp { get; set; }
    /// <inheritdoc/>
    public string? LoggerName { get; set; }
    /// <inheritdoc/>
    public string? Level { get; set; }
    /// <inheritdoc/>
    public string? Message { get; set; }
    /// <inheritdoc/>
    public string? ThreadName { get; set; }
    /// <inheritdoc/>
    public string? ExceptionString { get; set; }
    /// <inheritdoc/>
    public string? Domain { get; set; }
  }
}