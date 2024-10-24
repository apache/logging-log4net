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

using System.Runtime.InteropServices;

using log4net.Core;
using log4net.Util;

namespace log4net.Appender;

/// <summary>
/// Appends log events to the OutputDebugString system.
/// </summary>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
public class OutputDebugStringAppender : AppenderSkeleton
{
  /// <summary>
  /// Writes the logging event to the output debug string API
  /// </summary>
  /// <param name="loggingEvent">the event to log</param>
  [System.Security.SecuritySafeCritical]
  [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, UnmanagedCode = true)]
  protected override void Append(LoggingEvent loggingEvent)
  {
#if NETSTANDARD2_0_OR_GREATER
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      throw new System.PlatformNotSupportedException("OutputDebugString is only available on Windows");
    }
#endif

     NativeMethods.OutputDebugString(RenderLoggingEvent(loggingEvent));
  }

  /// <summary>
  /// This appender requires a <see cref="Layout"/> to be set.
  /// </summary>
  protected override bool RequiresLayout => true;
}