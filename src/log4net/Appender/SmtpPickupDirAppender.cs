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

namespace log4net.Appender
{
  /// <summary>
  /// Send an email when a specific logging event occurs, typically on errors 
  /// or fatal errors. Rather than sending via smtp it writes a file into the
  /// directory specified by <see cref="PickupDir"/>. This allows services such
  /// as the IIS SMTP agent to manage sending the messages.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The configuration for this appender is identical to that of the <c>SMTPAppender</c>,
  /// except that instead of specifying the <c>SMTPAppender.SMTPHost</c> you specify
  /// <see cref="PickupDir"/>.
  /// </para>
  /// <para>
  /// The number of logging events delivered in this e-mail depend on
  /// the value of <see cref="BufferingAppenderSkeleton.BufferSize"/> option. The
  /// <see cref="SmtpPickupDirAppender"/> keeps only the last
  /// <see cref="BufferingAppenderSkeleton.BufferSize"/> logging events in its 
  /// cyclic buffer. This keeps memory requirements at a reasonable level while 
  /// still delivering useful application context.
  /// </para>
  /// </remarks>
  /// <author>Niall Daley</author>
  /// <author>Nicko Cadell</author>
  public class SmtpPickupDirAppender : BufferingAppenderSkeleton
  {
    /// <summary>
    /// Gets or sets a semicolon-delimited list of recipient e-mail addresses.
    /// </summary>
    public string? To { get; set; }

    /// <summary>
    /// Gets or sets the e-mail address of the sender.
    /// </summary>
    public string? From { get; set; }

    /// <summary>
    /// Gets or sets the subject line of the e-mail message.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the path to write the messages to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Gets or sets the path to write the messages to. This should be the same
    /// as that used by the agent sending the messages.
    /// </para>
    /// </remarks>
    public string? PickupDir { get; set; }

    /// <summary>
    /// Gets or sets the file extension for the generated files
    /// </summary>
    public string? FileExtension
    {
      get => m_fileExtension;
      set
      {
        m_fileExtension = value ?? string.Empty;
        // Make sure any non-empty extension starts with a dot
        if (!string.IsNullOrEmpty(m_fileExtension) && !m_fileExtension.StartsWith("."))
        {
          m_fileExtension = "." + m_fileExtension;
        }
      }
    }

    /// <summary>
    /// Gets or sets the <see cref="SecurityContext"/> used to write to the pickup directory.
    /// </summary>
    /// <value>
    /// The <see cref="SecurityContext"/> used to write to the pickup directory.
    /// </value>
    /// <remarks>
    /// <para>
    /// Unless a <see cref="SecurityContext"/> specified here for this appender
    /// the <see cref="SecurityContextProvider.DefaultProvider"/> is queried for the
    /// security context to use. The default behavior is to use the security context
    /// of the current thread.
    /// </para>
    /// </remarks>
    public SecurityContext? SecurityContext { get; set; }

    /// <summary>
    /// Sends the contents of the cyclic buffer as an e-mail message.
    /// </summary>
    /// <param name="events">The logging events to send.</param>
    /// <remarks>
    /// <para>
    /// Sends the contents of the cyclic buffer as an e-mail message.
    /// </para>
    /// </remarks>
    protected override void SendBuffer(LoggingEvent[] events)
    {
      // Note: this code already owns the monitor for this
      // appender. This frees us from needing to synchronize again.
      try
      {
        StreamWriter writer;

        // Impersonate to open the file
        string filePath = Path.Combine(PickupDir, Guid.NewGuid().ToString("N") + m_fileExtension);
        using (SecurityContext?.Impersonate(this))
        {
          writer = File.CreateText(filePath);
        }

        using (writer)
        {
          writer.WriteLine("To: " + To);
          writer.WriteLine("From: " + From);
          writer.WriteLine("Subject: " + Subject);
          writer.WriteLine("Date: " + DateTime.UtcNow.ToString("r"));
          writer.WriteLine();

          string? t = Layout?.Header;
          if (t is not null)
          {
            writer.Write(t);
          }

          for (int i = 0; i < events.Length; i++)
          {
            // Render the event and append the text to the buffer
            RenderLoggingEvent(writer, events[i]);
          }

          t = Layout?.Footer;
          if (t is not null)
          {
            writer.Write(t);
          }

          writer.WriteLine();
          writer.WriteLine(".");
        }
      }
      catch (Exception e)
      {
        ErrorHandler.Error("Error occurred while sending e-mail notification.", e);
      }
    }

    /// <summary>
    /// Activate the options on this appender. 
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
    public override void ActivateOptions()
    {
      if (PickupDir is null)
      {
        throw new ArgumentException($"{nameof(PickupDir)} must be specified");
      }

      base.ActivateOptions();

      SecurityContext ??= SecurityContextProvider.DefaultProvider.CreateSecurityContext(this);

      using (SecurityContext.Impersonate(this))
      {
        PickupDir = ConvertToFullPath(PickupDir.Trim());
      }
    }

    /// <summary>
    /// This appender requires a <see cref="Layout"/> to be set.
    /// </summary>
    protected override bool RequiresLayout => true;

    /// <summary>
    /// Convert a path into a fully qualified path.
    /// </summary>
    /// <param name="path">The path to convert.</param>
    /// <returns>The fully qualified path.</returns>
    /// <remarks>
    /// <para>
    /// Converts the path specified to a fully
    /// qualified path. If the path is relative it is
    /// taken as relative from the application base 
    /// directory.
    /// </para>
    /// </remarks>
    protected static string ConvertToFullPath(string path)
    {
      return SystemInfo.ConvertToFullPath(path);
    }

    private string m_fileExtension = string.Empty;
  }
}
