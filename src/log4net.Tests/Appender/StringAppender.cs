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

using System.Text;

using log4net.Appender;
using log4net.Core;

namespace log4net.Tests.Appender;

/// <summary>
/// Write events to a string
/// </summary>
/// <author>Nicko Cadell</author>
public class StringAppender : AppenderSkeleton
{
  private readonly StringBuilder _buffer = new();

  /// <summary>
  /// Get the string logged so far
  /// </summary>
  /// <returns></returns>
  public string GetString() => _buffer.ToString();

  /// <summary>
  /// Reset the string
  /// </summary>
  public void Reset() => _buffer.Length = 0;

  /// <summary>
  /// </summary>
  /// <param name="loggingEvent">the event to log</param>
  protected override void Append(LoggingEvent loggingEvent)
    => _buffer.Append(RenderLoggingEvent(loggingEvent));

  /// <summary>
  /// This appender requires a <see cref="Layout"/> to be set.
  /// </summary>
  /// <value><c>true</c></value>
  protected override bool RequiresLayout => true;
}