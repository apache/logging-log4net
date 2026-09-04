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

namespace log4net.Appender;

/// <summary>
/// Options for handling the newlines (\r or \n) in logged content, used by
/// <see cref="LocalSyslogAppender"/> and <see cref="RemoteSyslogAppender"/>.
/// </summary>
/// <remarks>
/// <para>
/// A newline ends the record for a syslog daemon that writes the message through to a line
/// oriented log, so content could otherwise forge a second, authentic looking entry.
/// </para>
/// </remarks>
public enum SyslogNewLineHandling
{
  /// <summary>
  /// escape the newlines (\\r for \r and \\n for \n)
  /// </summary>
  Escape,

  /// <summary>
  /// split the message at new lines
  /// </summary>
  Split,

  /// <summary>
  /// keep newlines as is (many syslog servers can handle newlines in the message part)
  /// </summary>
  Keep
}
