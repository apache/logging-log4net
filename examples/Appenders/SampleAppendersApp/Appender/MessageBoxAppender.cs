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
using System.Windows.Forms;

using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Util;

namespace SampleAppendersApp.Appender;

/// <summary>
/// Displays messages as message boxes
/// </summary>
/// <remarks>
/// Displays each LoggingEvent as a MessageBox. The message box is UI modal
/// and will block the calling thread until it is dismissed by the user.
/// </remarks>
public sealed class MessageBoxAppender : AppenderSkeleton
{
  private readonly LevelMapping _levelMapping = new();

  /// <inheritdoc/>
  public void AddMapping(LevelIcon mapping) => _levelMapping.Add(mapping);

  /// <inheritdoc/>
  public PatternLayout? TitleLayout { get; set; }

  /// <inheritdoc/>
  protected override void Append(LoggingEvent loggingEvent)
  {
    ArgumentNullException.ThrowIfNull(loggingEvent);

    MessageBoxIcon messageBoxIcon = MessageBoxIcon.Information;

    if (_levelMapping.Lookup(loggingEvent.Level) is LevelIcon levelIcon)
    {
      // Prepend the Ansi Color code
      messageBoxIcon = levelIcon.Icon;
    }

    string message = RenderLoggingEvent(loggingEvent);

    string? title = null;
    if (TitleLayout is null)
    {
      title = "LoggingEvent: " + loggingEvent.Level?.Name;
    }
    else
    {
      using StringWriter titleWriter = new(System.Globalization.CultureInfo.InvariantCulture);
      TitleLayout.Format(titleWriter, loggingEvent);
      title = titleWriter.ToString();
    }

    MessageBox.Show(message, title, MessageBoxButtons.OK, messageBoxIcon);
  }

  /// <inheritdoc/>
  public override void ActivateOptions()
  {
    base.ActivateOptions();
    _levelMapping.ActivateOptions();
  }

  /// <inheritdoc/>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible")]
  public sealed class LevelIcon : LevelMappingEntry
  {
    /// <inheritdoc/>
    public MessageBoxIcon Icon { get; set; }
  }
}
