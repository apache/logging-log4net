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
using System.Globalization;
using System.Text;
using log4net.Core;
using log4net.Layout;
using log4net.Util;

namespace log4net.Appender.Internal;

/// <summary>
/// Makes rendered content safe for a sink, without discarding any of it.
/// </summary>
internal static class ContentEscape
{
  /// <param name="message">The rendered content.</param>
  extension(string message)
  {
    /// <summary>
    /// Replaces NUL characters with a visible <c>\0</c> escape.
    /// </summary>
    /// <returns>The message with every NUL character escaped.</returns>
    /// <remarks>
    /// <para>
    /// A NUL ends the string for the native sink, dropping everything the layout rendered after it,
    /// trailing fields and exception text included. Logged content is not trusted and may well
    /// contain a NUL, so the character is escaped rather than passed through.
    /// </para>
    /// </remarks>
    internal string EscapeNulCharacters()
      => message.IndexOf('\0') < 0 ? message : message.Replace("\0", "\\0");

    /// <summary>
    /// Replaces the surrogates that are not part of a pair with a visible <c>\uXXXX</c> escape.
    /// </summary>
    /// <returns>The message with every unpaired surrogate escaped.</returns>
    /// <remarks>
    /// An unpaired surrogate is a legal <see cref="string"/> but cannot be encoded, and an encoder
    /// that throws costs the whole event, so it is escaped rather than left to fail.
    /// </remarks>
    internal string EscapeUnpairedSurrogates()
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
          builder.Append(Escape(c));
        }
        else
        {
          builder.Append(c);
        }
      }

      return builder.ToString();
    }

    /// <summary>
    /// Escapes what <paramref name="encoding"/> cannot carry, for a sink that takes a string and an encoding.
    /// </summary>
    /// <param name="encoding">The configured encoding.</param>
    /// <returns>The content with every unencodable character escaped.</returns>
    internal string EscapeUnencodableText(Encoding encoding)
    {
      Encoding escaping = encoding.WithEscapingFallback();
      return escaping.GetString(escaping.GetBytes(message));
    }

    /// <summary>
    /// Cuts the text to <paramref name="maxBytes"/> in <paramref name="encoding"/>, ending in <c>...[truncated]</c>.
    /// </summary>
    /// <param name="encoding">The encoding the text goes out in, with the escaping fallback.</param>
    /// <param name="maxBytes">The budget, the marker included.</param>
    /// <param name="byteCount">The encoded length of the result.</param>
    /// <returns>
    /// The text itself when it fits, else the longest start that splits neither a character nor a pair,
    /// and the marker. A budget too small for any of the text gives the marker alone.
    /// </returns>
    internal string TruncateToBytes(Encoding encoding, int maxBytes, out int byteCount)
    {
      byteCount = encoding.GetByteCount(message);
      if (byteCount <= maxBytes)
      {
        return message;
      }

      int markerBytes = encoding.GetByteCount(TruncationMarker);
      string kept = message.Substring(0, CountCharsThatFit(message, encoding, maxBytes - markerBytes));
      byteCount = encoding.GetByteCount(kept) + markerBytes;
      while (byteCount > maxBytes && kept.Length > 0)
      {
        // The encoder counts a character whose escape it could only partly write: drop it whole.
        int drop = kept.Length > 1 && char.IsLowSurrogate(kept[kept.Length - 1]) ? 2 : 1;
        kept = kept.Substring(0, kept.Length - drop);
        byteCount = encoding.GetByteCount(kept) + markerBytes;
      }

      return kept + TruncationMarker;
    }
  }

  /// <param name="encoding">The configured encoding.</param>
  extension(Encoding encoding)
  {
    /// <summary>
    /// Returns a copy of the encoding that writes what it cannot carry as <c>\uXXXX</c>.
    /// </summary>
    /// <returns>The encoding to write rendered content with.</returns>
    /// <remarks>
    /// A best fit fallback runs after the layout has escaped the text, and writes U+FF1C as <c>&lt;</c>.
    /// </remarks>
    internal Encoding WithEscapingFallback()
    {
      Encoding escaping = encoding.EnsureNotNull().Clone().EnsureIs<Encoding>();
      escaping.EncoderFallback = EscapingEncoderFallback.Instance;
      return escaping;
    }
  }

  /// <param name="layout">A layout other than <see cref="AppenderSkeleton.Layout"/>, such as a category.</param>
  extension(LayoutSkeleton layout)
  {
    /// <summary>
    /// Formats the layout with unpaired surrogates escaped, since it does not pass the escape in
    /// <c>RenderLoggingEvent</c>.
    /// </summary>
    /// <param name="loggingEvent">The event to format.</param>
    /// <returns>The formatted text with every unpaired surrogate escaped.</returns>
    internal string FormatWithEscapedSurrogates(LoggingEvent loggingEvent)
      => layout.Format(loggingEvent).EscapeUnpairedSurrogates();
  }

  /// <summary>
  /// Marks text that was cut to fit a sink.
  /// </summary>
  internal const string TruncationMarker = "...[truncated]";

  internal static string Escape(char c) => "\\u" + ((int)c).ToString("x4", CultureInfo.InvariantCulture);

  /// <summary>
  /// Counts how many characters from the start of <paramref name="text"/> encode into <paramref name="budget"/> bytes.
  /// </summary>
  /// <remarks>
  /// Works in small chunks, so a long text is neither copied whole nor encoded into a buffer of the budget's size.
  /// </remarks>
  private static int CountCharsThatFit(string text, Encoding encoding, int budget)
  {
    if (budget <= 0)
    {
      return 0;
    }

    Encoder encoder = encoding.GetEncoder();
    char[] chars = new char[Math.Min(text.Length, 1024)];
    byte[] bytes = new byte[4096];
    int count = 0;
    while (count < text.Length && budget > 0)
    {
      int length = Math.Min(chars.Length, text.Length - count);
      text.CopyTo(count, chars, 0, length);
      int used;
      try
      {
        // Unflushed, the encoder holds back a trailing high surrogate instead of escaping half a pair.
        encoder.Convert(chars, 0, length, bytes, 0, Math.Min(bytes.Length, budget), flush: false,
          out used, out int written, out _);
        budget -= written;
      }
      catch (ArgumentException)
      {
        // The next character does not fit.
        break;
      }

      if (used == 0)
      {
        break;
      }

      count += used;
    }

    // A high surrogate held back at the cut has no room for its low half.
    return count > 0 && char.IsHighSurrogate(text[count - 1]) ? count - 1 : count;
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

  /// <summary>Writes an unencodable character as <c>\uXXXX</c>, a surrogate pair as two escapes.</summary>
  private sealed class EscapingEncoderFallback : EncoderFallback
  {
    internal static EscapingEncoderFallback Instance { get; } = new();

    /// <inheritdoc/>
    public override int MaxCharCount => 12;

    /// <inheritdoc/>
    public override EncoderFallbackBuffer CreateFallbackBuffer() => new Buffer();

    private sealed class Buffer : EncoderFallbackBuffer
    {
      private string _escape = string.Empty;
      private int _position;

      /// <inheritdoc/>
      public override int Remaining => _escape.Length - _position;

      /// <inheritdoc/>
      public override bool Fallback(char charUnknown, int index) => Start(Escape(charUnknown));

      /// <inheritdoc/>
      public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        => Start(Escape(charUnknownHigh) + Escape(charUnknownLow));

      /// <inheritdoc/>
      public override char GetNextChar() => Remaining > 0 ? _escape[_position++] : '\0';

      /// <inheritdoc/>
      public override bool MovePrevious()
      {
        if (_position == 0)
        {
          return false;
        }

        _position--;
        return true;
      }

      private bool Start(string escape)
      {
        _escape = escape;
        _position = 0;
        return true;
      }
    }
  }
}
