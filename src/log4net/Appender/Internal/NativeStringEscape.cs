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

namespace log4net.Appender.Internal;

/// <summary>
/// Prepares rendered content for a sink that takes a null terminated string.
/// </summary>
internal static class NativeStringEscape
{
  /// <summary>
  /// Replaces NUL characters with a visible <c>\0</c> escape.
  /// </summary>
  /// <param name="message">The rendered message.</param>
  /// <returns>The message with every NUL character escaped.</returns>
  /// <remarks>
  /// <para>
  /// A NUL ends the string for the native sink, dropping everything the layout rendered after it,
  /// trailing fields and exception text included. Logged content is not trusted and may well
  /// contain a NUL, so the character is escaped rather than passed through.
  /// </para>
  /// </remarks>
  internal static string EscapeNulCharacters(string message)
    => message.IndexOf('\0') < 0 ? message : message.Replace("\0", "\\0");
}
