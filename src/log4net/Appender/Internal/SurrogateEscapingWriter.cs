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
using System.IO;
using System.Text;

namespace log4net.Appender.Internal;

/// <summary>
/// Passes one rendered event on to a writer with its lone surrogates escaped.
/// </summary>
/// <remarks>
/// A high surrogate at the end of a write is held back, because the next write may complete the pair.
/// Disposing this writer ends the event and leaves the target open.
/// </remarks>
internal sealed class SurrogateEscapingWriter : TextWriter
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
    Justification = "The target belongs to the appender")]
  private readonly TextWriter _target;
  private char _heldHighSurrogate;

  /// <summary>
  /// Creates the writer for one event.
  /// </summary>
  /// <param name="target">The writer that receives the text.</param>
  internal SurrogateEscapingWriter(TextWriter target) : base(target.FormatProvider)
  {
    _target = target;
    if (!string.Equals(target.NewLine, Environment.NewLine, StringComparison.Ordinal))
    {
      NewLine = target.NewLine;
    }
  }

  /// <inheritdoc/>
  public override Encoding Encoding => _target.Encoding;

  /// <inheritdoc/>
  public override void Write(char value)
  {
    if (_heldHighSurrogate != '\0' && char.IsLowSurrogate(value))
    {
      // One write: a counting writer sizes each write on its own, so two would count a pair twice.
      _target.Write(new[] { _heldHighSurrogate, value }, 0, 2);
      _heldHighSurrogate = '\0';
      return;
    }

    Complete();
    if (char.IsHighSurrogate(value))
    {
      _heldHighSurrogate = value;
    }
    else if (char.IsLowSurrogate(value))
    {
      _target.Write(ContentEscape.Escape(value));
    }
    else
    {
      _target.Write(value);
    }
  }

  /// <inheritdoc/>
  public override void Write(string? value)
  {
    if (value is not { Length: > 0 })
    {
      return;
    }

    int start = 0;
    while (_heldHighSurrogate != '\0' && start < value.Length)
    {
      Write(value[start++]);
    }

    int end = value.Length;
    if (start < end && char.IsHighSurrogate(value[end - 1]))
    {
      _heldHighSurrogate = value[--end];
    }

    if (start < end)
    {
      string text = end - start == value.Length ? value : value.Substring(start, end - start);
      _target.Write(text.EscapeUnpairedSurrogates());
    }
  }

  /// <inheritdoc/>
  public override void Write(char[] buffer, int index, int count)
  {
    if (_heldHighSurrogate == '\0' && !ContainsSurrogate(buffer, index, count))
    {
      _target.Write(buffer, index, count);
      return;
    }

    Write(new string(buffer, index, count));
  }

  /// <inheritdoc/>
  /// <remarks>Keeps a held surrogate, whose low half may still come.</remarks>
  public override void Flush() => _target.Flush();

  /// <summary>
  /// Ends the event: a surrogate still held has no low half, so it is written as an escape.
  /// </summary>
  internal void Complete()
  {
    if (_heldHighSurrogate != '\0')
    {
      string escape = ContentEscape.Escape(_heldHighSurrogate);
      _heldHighSurrogate = '\0';
      _target.Write(escape);
    }
  }

  /// <inheritdoc/>
  protected override void Dispose(bool disposing) => Complete();

  private static bool ContainsSurrogate(char[] buffer, int index, int count)
  {
    for (int i = index; i < index + count; i++)
    {
      if (char.IsSurrogate(buffer[i]))
      {
        return true;
      }
    }

    return false;
  }
}
