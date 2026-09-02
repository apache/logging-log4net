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

using System.Globalization;
using System.Text;

namespace log4net.Appender.Internal;

/// <summary>
/// Makes rendered content safe for a sink, without discarding any of it.
/// </summary>
internal static class ContentEscape
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

  /// <summary>
  /// Replaces the surrogates that are not part of a pair with a visible <c>\uXXXX</c> escape.
  /// </summary>
  /// <param name="message">The rendered message.</param>
  /// <returns>The message with every unpaired surrogate escaped.</returns>
  /// <remarks>
  /// An unpaired surrogate is a legal <see cref="string"/> but cannot be encoded, and an encoder
  /// that throws costs the whole event, so it is escaped rather than left to fail.
  /// </remarks>
  internal static string EscapeUnpairedSurrogates(string message)
  {
    if (!ContainsUnpairedSurrogate(message))
    {
      return message;
    }

    StringBuilder builder = new(message.Length);
    for (int i = 0; i < message.Length; i++)
    {
      char c = message[i];
      if (char.IsHighSurrogate(c) && i + 1 < message.Length && char.IsLowSurrogate(message[i + 1]))
      {
        builder.Append(c).Append(message[i + 1]);
        i++;
      }
      else if (char.IsSurrogate(c))
      {
        builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
      }
      else
      {
        builder.Append(c);
      }
    }

    return builder.ToString();
  }

  private static bool ContainsUnpairedSurrogate(string message)
  {
    for (int i = 0; i < message.Length; i++)
    {
      if (!char.IsSurrogate(message[i]))
      {
        continue;
      }

      if (char.IsHighSurrogate(message[i]) && i + 1 < message.Length && char.IsLowSurrogate(message[i + 1]))
      {
        i++;
        continue;
      }

      return true;
    }

    return false;
  }
}
