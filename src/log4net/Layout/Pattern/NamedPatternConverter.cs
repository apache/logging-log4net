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
using log4net.Util;

namespace log4net.Layout.Pattern;

/// <summary>
/// Converter to output and truncate <c>'.'</c> separated strings
/// </summary>
/// <remarks>
/// <para>
/// This abstract class supports truncating a <c>'.'</c> separated string
/// to show a specified number of elements from the right hand side.
/// This is used to truncate class names that are fully qualified.
/// </para>
/// <para>
/// Subclasses should override the <see cref="GetFullyQualifiedName"/> method to
/// return the fully qualified string.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
public abstract class NamedPatternConverter : PatternLayoutConverter, IOptionHandler
{
  private int _precision;

  /// <summary>
  /// Initialize the converter 
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is part of the <see cref="IOptionHandler"/> delayed object
  /// activation scheme. The <see cref="ActivateOptions"/> method must 
  /// be called on this object after the configuration properties have
  /// been set. Until <see cref="ActivateOptions"/> is called this
  /// object is in an undefined state and must not be used. 
  /// </para>
  /// <para>
  /// If any of the configuration properties are modified then 
  /// <see cref="ActivateOptions"/> must be called again.
  /// </para>
  /// </remarks>
  public void ActivateOptions()
  {
    _precision = 0;

    if (Option is not null)
    {
      string optStr = Option.Trim();
      if (optStr.Length > 0)
      {
        if (SystemInfo.TryParse(optStr, out int precisionVal))
        {
          if (precisionVal <= 0)
          {
            LogLog.Error(_declaringType, $"NamedPatternConverter: Precision option ({optStr}) isn't a positive integer.");
          }
          else
          {
            _precision = precisionVal;
          }
        }
        else
        {
          LogLog.Error(_declaringType, $"NamedPatternConverter: Precision option \"{optStr}\" not a decimal integer.");
        }
      }
    }
  }

  /// <summary>
  /// Gets the fully qualified <c>'.'</c> (dot/period) separated name for an event.
  /// </summary>
  /// <param name="loggingEvent">the event being logged</param>
  /// <returns>the fully qualified name</returns>
  /// <remarks>
  /// <para>
  /// Overridden by subclasses to get the fully qualified name before the
  /// precision is applied to it.
  /// </para>
  /// </remarks>
  protected abstract string? GetFullyQualifiedName(LoggingEvent loggingEvent);

  /// <summary>
  /// Converts the pattern to the rendered message
  /// </summary>
  /// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
  /// <param name="loggingEvent">the event being logged</param>
  /// <remarks>
  /// Render the <see cref="GetFullyQualifiedName"/> to the precision
  /// specified by the <see cref="PatternConverter.Option"/> property.
  /// </remarks>
  protected sealed override void Convert(TextWriter writer, LoggingEvent loggingEvent)
  {
    writer.EnsureNotNull();
    string? name = GetFullyQualifiedName(loggingEvent);
    if (_precision <= 0 || name is null || name.Length < 2)
    {
      writer.Write(name);
    }
    else
    {
      int len = name.Length;
      string trailingDot = string.Empty;
      if (name.EndsWith(Dot))
      {
        trailingDot = Dot;
        name = name.Substring(0, len - 1);
        len--;
      }

      int end = name.LastIndexOf(Dot);
      for (int i = 1; end > 0 && i < _precision; i++)
      {
        end = name.LastIndexOf('.', end - 1);
      }
      if (end == -1)
      {
        writer.Write(name + trailingDot);
      }
      else
      {
        writer.Write(name.Substring(end + 1, len - end - 1) + trailingDot);
      }
    }
  }

  /// <summary>
  /// The fully qualified type of the NamedPatternConverter class.
  /// </summary>
  /// <remarks>
  /// Used by the internal logger to record the Type of the
  /// log message.
  /// </remarks>
  private static readonly Type _declaringType = typeof(NamedPatternConverter);

  private const string Dot = ".";
}
