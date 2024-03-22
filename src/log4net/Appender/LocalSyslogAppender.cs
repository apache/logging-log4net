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
using System.Runtime.InteropServices;

using log4net.Core;
using log4net.Util;

namespace log4net.Appender 
{
  /// <summary>
  /// Logs events to a local syslog service.
  /// </summary>
  /// <remarks>
  /// <note>
  /// This appender uses the POSIX libc library functions <c>openlog</c>, <c>syslog</c>, and <c>closelog</c>.
  /// If these functions are not available on the local system then this appender will not work!
  /// </note>
  /// <para>
  /// The functions <c>openlog</c>, <c>syslog</c>, and <c>closelog</c> are specified in SUSv2 and 
  /// POSIX 1003.1-2001 standards. These are used to log messages to the local syslog service.
  /// </para>
  /// <para>
  /// This appender talks to a local syslog service. If you need to log to a remote syslog
  /// daemon and you cannot configure your local syslog service to do this you may be
  /// able to use the <see cref="RemoteSyslogAppender"/> to log via UDP.
  /// </para>
  /// <para>
  /// Syslog messages must have a facility and and a severity. The severity
  /// is derived from the Level of the logging event.
  /// The facility must be chosen from the set of defined syslog 
  /// <see cref="SyslogFacility"/> values. The facilities list is predefined
  /// and cannot be extended.
  /// </para>
  /// <para>
  /// An identifier is specified with each log message. This can be specified
  /// by setting the <see cref="Identity"/> property. The identity (also know 
  /// as the tag) must not contain white space. The default value for the
  /// identity is the application name (from <see cref="SystemInfo.ApplicationFriendlyName"/>).
  /// </para>
  /// </remarks>
  /// <author>Rob Lyon</author>
  /// <author>Nicko Cadell</author>
  public class LocalSyslogAppender : AppenderSkeleton 
  {
    /// <summary>
    /// syslog severities
    /// </summary>
    /// <remarks>
    /// <para>
    /// The log4net Level maps to a syslog severity using the
    /// <see cref="LocalSyslogAppender.AddMapping"/> method and the <see cref="LevelSeverity"/>
    /// class. The severity is set on <see cref="LevelSeverity.Severity"/>.
    /// </para>
    /// </remarks>
    public enum SyslogSeverity
    {
      /// <summary>
      /// system is unusable
      /// </summary>
      Emergency = 0,

      /// <summary>
      /// action must be taken immediately
      /// </summary>
      Alert = 1,

      /// <summary>
      /// critical conditions
      /// </summary>
      Critical = 2,

      /// <summary>
      /// error conditions
      /// </summary>
      Error = 3,

      /// <summary>
      /// warning conditions
      /// </summary>
      Warning = 4,

      /// <summary>
      /// normal but significant condition
      /// </summary>
      Notice = 5,

      /// <summary>
      /// informational
      /// </summary>
      Informational = 6,

      /// <summary>
      /// debug-level messages
      /// </summary>
      Debug = 7
    };

    /// <summary>
    /// syslog facilities
    /// </summary>
    /// <remarks>
    /// <para>
    /// The syslog facility defines which subsystem the logging comes from.
    /// This is set on the <see cref="Facility"/> property.
    /// </para>
    /// </remarks>
    public enum SyslogFacility
    {
      /// <summary>
      /// kernel messages
      /// </summary>
      Kernel = 0,

      /// <summary>
      /// random user-level messages
      /// </summary>
      User = 1,

      /// <summary>
      /// mail system
      /// </summary>
      Mail = 2,

      /// <summary>
      /// system daemons
      /// </summary>
      Daemons = 3,

      /// <summary>
      /// security/authorization messages
      /// </summary>
      Authorization = 4,

      /// <summary>
      /// messages generated internally by syslogd
      /// </summary>
      Syslog = 5,

      /// <summary>
      /// line printer subsystem
      /// </summary>
      Printer = 6,

      /// <summary>
      /// network news subsystem
      /// </summary>
      News = 7,

      /// <summary>
      /// UUCP subsystem
      /// </summary>
      Uucp = 8,

      /// <summary>
      /// clock (cron/at) daemon
      /// </summary>
      Clock = 9,

      /// <summary>
      /// security/authorization  messages (private)
      /// </summary>
      Authorization2 = 10,

      /// <summary>
      /// ftp daemon
      /// </summary>
      Ftp = 11,

      /// <summary>
      /// NTP subsystem
      /// </summary>
      Ntp = 12,

      /// <summary>
      /// log audit
      /// </summary>
      Audit = 13,

      /// <summary>
      /// log alert
      /// </summary>
      Alert = 14,

      /// <summary>
      /// clock daemon
      /// </summary>
      Clock2 = 15,

      /// <summary>
      /// reserved for local use
      /// </summary>
      Local0 = 16,

      /// <summary>
      /// reserved for local use
      /// </summary>
      Local1 = 17,

      /// <summary>
      /// reserved for local use
      /// </summary>
      Local2 = 18,

      /// <summary>
      /// reserved for local use
      /// </summary>
      Local3 = 19,

      /// <summary>
      /// reserved for local use
      /// </summary>
      Local4 = 20,

      /// <summary>
      /// reserved for local use
      /// </summary>
      Local5 = 21,

      /// <summary>
      /// reserved for local use
      /// </summary>
      Local6 = 22,

      /// <summary>
      /// reserved for local use
      /// </summary>
      Local7 = 23
    }

    /// <summary>
    /// Message identity
    /// </summary>
    /// <remarks>
    /// <para>
    /// An identifier is specified with each log message. This can be specified
    /// by setting the <see cref="Identity"/> property. The identity (also known 
    /// as the tag) must not contain white space. The default value for the
    /// identity is the application name (from <see cref="SystemInfo.ApplicationFriendlyName"/>).
    /// </para>
    /// </remarks>
    public string? Identity { get; set; }

    /// <summary>
    /// Syslog facility
    /// </summary>
    /// <remarks>
    /// Set to one of the <see cref="SyslogFacility"/> values. The list of
    /// facilities is predefined and cannot be extended. The default value
    /// is <see cref="SyslogFacility.User"/>.
    /// </remarks>
    public SyslogFacility Facility { get; set; } = SyslogFacility.User;

    /// <summary>
    /// Add a mapping of level to severity
    /// </summary>
    /// <param name="mapping">The mapping to add</param>
    /// <remarks>
    /// <para>
    /// Adds a <see cref="LevelSeverity"/> to this appender.
    /// </para>
    /// </remarks>
    public void AddMapping(LevelSeverity mapping)
    {
      m_levelMapping.Add(mapping);
    }

    /// <summary>
    /// Initialize the appender based on the options set.
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
    [System.Security.SecuritySafeCritical]
    public override void ActivateOptions()
    {
      base.ActivateOptions();
      
      m_levelMapping.ActivateOptions();

      // Set to app name by default
      string? identString = Identity ?? SystemInfo.ApplicationFriendlyName;

      // create the native heap ansi string. Note this is a copy of our string
      // so we do not need to hold on to the string itself, holding on to the
      // handle will keep the heap ansi string alive.
      m_handleToIdentity = Marshal.StringToHGlobalAnsi(identString);

      // open syslog
      openlog(m_handleToIdentity, 1, Facility);
    }

    /// <summary>
    /// This method is called by the <see cref="M:AppenderSkeleton.DoAppend(LoggingEvent)"/> method.
    /// </summary>
    /// <param name="loggingEvent">The event to log.</param>
    /// <remarks>
    /// <para>
    /// Writes the event to a remote syslog daemon.
    /// </para>
    /// <para>
    /// The format of the output will depend on the appender's layout.
    /// </para>
    /// </remarks>
    [System.Security.SecuritySafeCritical]
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, UnmanagedCode = true)]
    protected override void Append(LoggingEvent loggingEvent) 
    {
      int priority = GeneratePriority(Facility, GetSeverity(loggingEvent.Level));
      string message = RenderLoggingEvent(loggingEvent);

      // Call the local libc syslog method
      // The second argument is a printf style format string
      syslog(priority, "%s", message);
    }

    /// <summary>
    /// Close the syslog when the appender is closed
    /// </summary>
    /// <remarks>
    /// <para>
    /// Close the syslog when the appender is closed
    /// </para>
    /// </remarks>
    [System.Security.SecuritySafeCritical]
    protected override void OnClose()
    {
      base.OnClose();

      try
      {
        // close syslog
        closelog();
      }
      catch (DllNotFoundException)
      {
        // Ignore dll not found at this point
      }
    
      if (m_handleToIdentity != IntPtr.Zero)
      {
        // free global ident
        Marshal.FreeHGlobal(m_handleToIdentity);
      }
    }

    /// <summary>
    /// This appender requires a <see cref="AppenderSkeleton.Layout"/> to be set.
    /// </summary>
    protected override bool RequiresLayout => true;

    /// <summary>
    /// Translates a log4net level to a syslog severity.
    /// </summary>
    /// <param name="level">A log4net level.</param>
    /// <returns>A syslog severity.</returns>
    protected virtual SyslogSeverity GetSeverity(Level? level)
    {
      if (m_levelMapping.Lookup(level) is LevelSeverity levelSeverity)
      {
        return levelSeverity.Severity;
      }

      //
      // Fall back to sensible default values
      //

      if (level >= Level.Alert) 
      {
        return SyslogSeverity.Alert;
      } 
      else if (level >= Level.Critical) 
      {
        return SyslogSeverity.Critical;
      } 
      else if (level >= Level.Error) 
      {
        return SyslogSeverity.Error;
      } 
      else if (level >= Level.Warn) 
      {
        return SyslogSeverity.Warning;
      } 
      else if (level >= Level.Notice) 
      {
        return SyslogSeverity.Notice;
      } 
      else if (level >= Level.Info) 
      {
        return SyslogSeverity.Informational;
      } 
      // Default setting
      return SyslogSeverity.Debug;
    }

    /// <summary>
    /// Generate a syslog priority.
    /// </summary>
    /// <param name="facility">The syslog facility.</param>
    /// <param name="severity">The syslog severity.</param>
    /// <returns>A syslog priority.</returns>
    private static int GeneratePriority(SyslogFacility facility, SyslogSeverity severity)
    {
      return ((int)facility * 8) + (int)severity;
    }

    /// <summary>
    /// Marshaled handle to the identity string. We have to hold on to the
    /// string as the <c>openlog</c> and <c>syslog</c> APIs just hold the
    /// pointer to the ident and dereference it for each log message.
    /// </summary>
    private IntPtr m_handleToIdentity = IntPtr.Zero;

    /// <summary>
    /// Mapping from level object to syslog severity
    /// </summary>
    private readonly LevelMapping m_levelMapping = new();
    
    /// <summary>
    /// Open connection to system logger.
    /// </summary>
    [DllImport("libc")]
    private static extern void openlog(IntPtr ident, int option, SyslogFacility facility);

    /// <summary>
    /// Generate a log message.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The libc syslog method takes a format string and a variable argument list similar
    /// to the classic printf function. As this type of vararg list is not supported
    /// by C# we need to specify the arguments explicitly. Here we have specified the
    /// format string with a single message argument. The caller must set the format 
    /// string to <c>"%s"</c>.
    /// </para>
    /// </remarks>
    [DllImport("libc", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    private static extern void syslog(int priority, string format, string message);

    /// <summary>
    /// Close descriptor used to write to system logger.
    /// </summary>
    [DllImport("libc")]
    private static extern void closelog();

    /// <summary>
    /// A class to act as a mapping between the level that a logging call is made at and
    /// the syslog severity that is should be logged at.
    /// </summary>
    public class LevelSeverity : LevelMappingEntry
    {
      /// <summary>
      /// The mapped syslog severity for the specified level
      /// </summary>
      public SyslogSeverity Severity { get; set; }
    }
  }
}