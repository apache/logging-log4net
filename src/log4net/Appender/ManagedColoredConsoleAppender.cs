#region  Apache  License
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

// Compatibility:
//   http://msdn.microsoft.com/en-us/library/system.console.foregroundcolor.aspx

// The original ColoredConsoleAppender was written before the .NET framework
// (and Mono) had built-in support for console colors so it was written using
// Win32 API calls. The AnsiColorTerminalAppender, while it works, isn't
// understood by the Windows command prompt.
// This is a replacement for both that uses the new Console colors
// and works on both platforms.

// On Mono/Linux (at least), setting the background color to 'Black' is
// not the same as the default background color, as it is after
// Console.Reset(). The difference becomes apparent while running in a
// terminal application that supports background transparency; the
// default color is treated as transparent while 'Black' isn't.
// For this reason, we always reset the colors and only set those
// explicitly specified in the configuration (Console.BackgroundColor
// isn't set if omitted).

using System;
using System.IO;
using log4net.Core;
using log4net.Util;

namespace log4net.Appender;

/// <summary>
/// Appends colorful logging events to the console, using .NET built-in capabilities.
/// </summary>
/// <remarks>
/// <para>
/// ManagedColoredConsoleAppender appends log events to the standard output stream
/// or the error output stream using a layout specified by the
/// user. It also allows the color of a specific type of message to be set.
/// </para>
/// <para>
/// By default, all output is written to the console's standard output stream.
/// The <see cref="Target"/> property can be set to direct the output to the
/// error stream.
/// </para>
/// <para>
/// When configuring the colored console appender, mappings should be
/// specified to map logging levels to colors. For example:
/// </para>
/// <code lang="XML" escaped="true">
/// <mapping>
///  <level value="ERROR" />
///  <foreColor value="DarkRed" />
///  <backColor value="White" />
/// </mapping>
/// <mapping>
///  <level value="WARN" />
///  <foreColor value="Yellow" />
/// </mapping>
/// <mapping>
///  <level value="INFO" />
///  <foreColor value="White" />
/// </mapping>
/// <mapping>
///  <level value="DEBUG" />
///  <foreColor value="Blue" />
/// </mapping>
/// </code>
/// <para>
/// The Level is the standard log4net logging level while
/// ForeColor and BackColor are the values of <see cref="ConsoleColor"/>
/// enumeration.
/// </para>
/// <para>
/// Based on the ColoredConsoleAppender
/// </para>
/// </remarks>
/// <author>Rick Hobbs</author>
/// <author>Nicko Cadell</author>
/// <author>Pavlos Touboulidis</author>
public class ManagedColoredConsoleAppender : AppenderSkeleton
{
  /// <summary>
  /// Gets or sets the console output stream.
  /// This is either <c>"Console.Out"</c> or <c>"Console.Error"</c>.
  /// </summary>
  public virtual string Target
  {
    get => _writeToErrorStream ? ConsoleError : ConsoleOut;
    set => _writeToErrorStream = SystemInfo.EqualsIgnoringCase(ConsoleError, value.Trim());
  }

  /// <summary>
  /// Add a mapping of level to color - done by the config file
  /// </summary>
  /// <param name="mapping">The mapping to add</param>
  /// <remarks>
  /// <para>
  /// Each mapping defines the foreground and background colors
  /// for a level.
  /// </para>
  /// </remarks>
  public void AddMapping(LevelColors mapping)
  {
    _levelMapping.Add(mapping);
  }

  /// <summary>
  /// Writes the event to the console.
  /// </summary>
  /// <param name="loggingEvent">The event to log.</param>
  /// <remarks>
  /// <para>
  /// This method is called by the <see cref="M:AppenderSkeleton.DoAppend(log4net.Core.LoggingEvent)"/> method.
  /// </para>
  /// <para>
  /// The format of the output will depend on the appender's layout.
  /// </para>
  /// </remarks>
  protected override void Append(LoggingEvent loggingEvent)
  {
    TextWriter writer = _writeToErrorStream ? Console.Error : Console.Out;

    Console.ResetColor();

    // See if there is a specified lookup
    if (_levelMapping.Lookup(loggingEvent.EnsureNotNull().Level) is LevelColors levelColors)
    {
      // If the backColor has been explicitly set
      if (levelColors.HasBackColor)
      {
        Console.BackgroundColor = levelColors.BackColor;
      }

      // If the foreColor has been explicitly set
      if (levelColors.HasForeColor)
      {
        Console.ForegroundColor = levelColors.ForeColor;
      }
    }

    // Render the event to a string
    string strLoggingMessage = RenderLoggingEvent(loggingEvent);
    // and write it
    writer.Write(strLoggingMessage);

    // Reset color again
    Console.ResetColor();
  }

  /// <summary>
  /// This appender requires a <see cref="Layout"/> to be set.
  /// </summary>
  protected override bool RequiresLayout => true;

  /// <summary>
  /// Initializes the options for this appender.
  /// </summary>
  public override void ActivateOptions()
  {
    base.ActivateOptions();
    _levelMapping.ActivateOptions();
  }

  /// <summary>
  /// The <see cref="Target"/> to use when writing to the Console
  /// standard output stream.
  /// </summary>
  public const string ConsoleOut = "Console.Out";

  /// <summary>
  /// The <see cref="Target"/> to use when writing to the Console
  /// standard error output stream.
  /// </summary>
  public const string ConsoleError = "Console.Error";

  /// <summary>
  /// Flag to write output to the error stream rather than the standard output stream
  /// </summary>
  private bool _writeToErrorStream;

  /// <summary>
  /// Mapping from level object to color value
  /// </summary>
  private readonly LevelMapping _levelMapping = new();

  /// <summary>
  /// A class to act as a mapping between the level that a logging call is made at and
  /// the color it should be displayed as.
  /// </summary>
  public class LevelColors : LevelMappingEntry
  {
    /// <summary>
    /// The mapped foreground color for the specified level
    /// </summary>
    public ConsoleColor ForeColor
    {
      get => _foreColor;
      // Keep a flag that the color has been set
      // and is no longer the default.
      set { _foreColor = value; HasForeColor = true; }
    }
    private ConsoleColor _foreColor;

    internal bool HasForeColor { get; private set; }

    /// <summary>
    /// Gets or sets the mapped background color for the specified level
    /// </summary>
    public ConsoleColor BackColor
    {
      get => _backColor;
      // Keep a flag that the color has been set
      // and is no longer the default.
      set { _backColor = value; HasBackColor = true; }
    }
    private ConsoleColor _backColor;

    internal bool HasBackColor { get; private set; }
  }
}
