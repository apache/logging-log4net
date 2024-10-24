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
using log4net.Appender;
using log4net.Util;
using log4net.Layout;
using log4net.Core;

namespace SampleAppendersApp.Appender;

/// <summary>
/// Appender that writes to a file named using a pattern
/// </summary>
/// <remarks>
/// The file to write to is selected for each event using a
/// PatternLayout specified in the File property. This allows
/// each LoggingEvent to be written to a file based on properties
/// of the event.
/// The output file is opened to write each LoggingEvent as it arrives
/// and closed afterwards.
/// </remarks>
public sealed class PatternFileAppender : AppenderSkeleton
{
  /// <inheritdoc/>
  public PatternLayout? File { get; set; }

  /// <inheritdoc/>
  public Encoding Encoding { get; set; } = Encoding.Default;

  /// <inheritdoc/>
  public SecurityContext? SecurityContext { get; set; }

  /// <inheritdoc/>
  public override void ActivateOptions()
  {
    base.ActivateOptions();
    SecurityContext ??= SecurityContextProvider.DefaultProvider.CreateSecurityContext(this);
  }

  /// <inheritdoc/>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types")]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
  protected override void Append(LoggingEvent loggingEvent)
  {
    try
    {
      // Render the file name
      using StringWriter stringWriter = new();
      ArgumentNullException.ThrowIfNull(File);
      File.Format(stringWriter, loggingEvent);
      string fileName = stringWriter.ToString();

      fileName = SystemInfo.ConvertToFullPath(fileName);

      FileStream? fileStream = null;

      ArgumentNullException.ThrowIfNull(SecurityContext);
      using (SecurityContext.Impersonate(this))
      {
        // Ensure that the directory structure exists
        string? directoryFullName = Path.GetDirectoryName(fileName);
        ArgumentNullException.ThrowIfNull(directoryFullName);

        // Only create the directory if it does not exist
        // doing this check here resolves some permissions failures
        if (!Directory.Exists(directoryFullName))
        {
          Directory.CreateDirectory(directoryFullName);
        }

        // Open file stream while impersonating
        fileStream = new(fileName, FileMode.Append, FileAccess.Write, FileShare.Read);
      }

      if (fileStream is not null)
      {
        using (StreamWriter streamWriter = new(fileStream, Encoding))
        {
          RenderLoggingEvent(streamWriter, loggingEvent);
        }

        fileStream.Close();
      }
    }
    catch (Exception ex)
    {
      ErrorHandler.Error("Failed to append to file", ex);
    }
  }
}