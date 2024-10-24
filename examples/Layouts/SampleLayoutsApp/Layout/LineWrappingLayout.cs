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
using log4net.Core;

namespace SampleLayoutsApp.Layout;

/// <summary>
/// The LineWrappingLayout wraps the output of a nested layout
/// </summary>
/// <remarks>
/// The output of the nested layout is wrapped at
/// <see cref="LineWidth"/>. Each of the continuation lines
/// is prefixed with a number of spaces specified by <see cref="Indent"/>.
/// </remarks>
public sealed class LineWrappingLayout : ForwardingLayout
{
  /// <inheritdoc/>
  public int LineWidth { get; set; } = 76;

  /// <inheritdoc/>
  public int Indent { get; set; } = 4;

  /// <inheritdoc/>
  public override void Format(TextWriter writer, LoggingEvent loggingEvent)
  {
    ArgumentNullException.ThrowIfNull(writer);
    using StringWriter stringWriter = new();

    base.Format(stringWriter, loggingEvent);

    string formattedString = stringWriter.ToString();

    WrapText(writer, formattedString);
  }

  private void WrapText(TextWriter writer, string text)
  {
    if (text.Length <= LineWidth)
    {
      writer.Write(text);
    }
    else
    {
      // Do the first line
      writer.WriteLine(text.AsSpan(0, LineWidth));
      string rest = text[LineWidth..];

      string indentString = new(' ', Indent);
      int continuationLineWidth = LineWidth - Indent;

      // Do the continuation lines
      while (true)
      {
        writer.Write(indentString);

        if (rest.Length > continuationLineWidth)
        {
          writer.WriteLine(rest[..continuationLineWidth]);
          rest = rest[continuationLineWidth..];
        }
        else
        {
          writer.Write(rest);
          break;
        }
      }
    }
  }
}