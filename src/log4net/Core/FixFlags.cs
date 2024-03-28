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

namespace log4net.Core
{
  /// <summary>
  /// Flags passed to the <see cref="LoggingEvent.Fix"/> property
  /// </summary>
  /// <remarks>
  /// <para>
  /// Flags passed to the <see cref="LoggingEvent.Fix"/> property
  /// </para>
  /// </remarks>
  /// <author>Nicko Cadell</author>
  [Flags]
  public enum FixFlags
  {
    /// <summary>
    /// Fix the MDC
    /// </summary>
    [Obsolete("Replaced by composite Properties")]
    Mdc = 0x01,

    /// <summary>
    /// Fix the NDC
    /// </summary>
    Ndc = 0x02,

    /// <summary>
    /// Fix the rendered message
    /// </summary>
    Message = 0x04,

    /// <summary>
    /// Fix the thread name
    /// </summary>
    ThreadName = 0x08,

    /// <summary>
    /// Fix the callers location information
    /// </summary>
    /// <remarks>
    /// CAUTION: Very slow to generate
    /// </remarks>
    LocationInfo = 0x10,

    /// <summary>
    /// Fix the callers windows user name
    /// </summary>
    /// <remarks>
    /// CAUTION: Slow to generate
    /// </remarks>
    UserName = 0x20,

    /// <summary>
    /// Fix the domain friendly name
    /// </summary>
    Domain = 0x40,

    /// <summary>
    /// Fix the callers principal name
    /// </summary>
    /// <remarks>
    /// CAUTION: May be slow to generate
    /// </remarks>
    Identity = 0x80,

    /// <summary>
    /// Fix the exception text
    /// </summary>
    Exception = 0x100,

    /// <summary>
    /// Fix the event properties. Active properties must implement <see cref="IFixingRequired"/> in order to be eligible for fixing.
    /// </summary>
    Properties = 0x200,

    /// <summary>
    /// No fields fixed
    /// </summary>
    None = 0x0,

    /// <summary>
    /// All fields fixed
    /// </summary>
    All = 0xFFFFFFF,

    /// <summary>
    /// Partial fields fixed
    /// </summary>
    /// <remarks>
    /// <para>
    /// This set of partial fields gives good performance. The following fields are fixed:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="Message"/></description></item>
    /// <item><description><see cref="ThreadName"/></description></item>
    /// <item><description><see cref="Exception"/></description></item>
    /// <item><description><see cref="Domain"/></description></item>
    /// <item><description><see cref="Properties"/></description></item>
    /// </list>
    /// </remarks>
    Partial = Message | ThreadName | Exception | Domain | Properties,
  }
}