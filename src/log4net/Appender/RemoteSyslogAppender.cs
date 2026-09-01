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
using System.Text;
using System.Threading;
using log4net.Appender.Internal;
using log4net.Core;
using log4net.Layout;
using log4net.Util;

namespace log4net.Appender;

/// <summary>
/// Logs events to a remote syslog daemon.
/// </summary>
/// <remarks>
/// <para>
/// The BSD syslog protocol is used to remotely log to
/// a syslog daemon. The syslogd listens for messages
/// on UDP port 514.
/// </para>
/// <para>
/// The syslog UDP protocol is not authenticated. Most syslog daemons
/// do not accept remote log messages because of the security implications.
/// You may be able to use the LocalSyslogAppender to talk to a local
/// syslog service.
/// </para>
/// <para>
/// There is an RFC 3164 that claims to document the BSD Syslog Protocol.
/// This RFC can be seen here: http://www.faqs.org/rfcs/rfc3164.html.
/// This appender generates what the RFC calls an "Original Device Message",
/// i.e. does not include the TIMESTAMP or HOSTNAME fields. By observation
/// this format of message will be accepted by all current syslog daemon
/// implementations. The daemon will attach the current time and the source
/// hostname or IP address to any messages received.
/// </para>
/// <para>
/// Syslog messages must have a facility and a severity. The severity
/// is derived from the Level of the logging event.
/// The facility must be chosen from the set of defined syslog 
/// <see cref="SyslogFacility"/> values. The facilities list is predefined
/// and cannot be extended.
/// </para>
/// <para>
/// An identifier is specified with each log message. This can be specified
/// by setting the <see cref="Identity"/> property. The identity (also known 
/// as the tag) must not contain white space. The default value for the
/// identity is the application name (from <see cref="LoggingEvent.Domain"/>).
/// </para>
/// </remarks>
/// <author>Rob Lyon</author>
/// <author>Nicko Cadell</author>
public class RemoteSyslogAppender : UdpAppender
{
  /// <summary>
  /// Syslog port 514
  /// </summary>
  private const int DefaultSyslogPort = 514;

  /// <summary>
  /// syslog severities
  /// </summary>
  /// <remarks>
  /// <para>
  /// The syslog severities.
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
  /// The syslog facilities
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
  /// Options for handling newlines (\r or \n) in <see ref="AppendMessage" />
  /// </summary>
  public enum SyslogNewLineHandling
  {
    /// <summary>
    /// escape the newlines (\\r for \r and \\n for \n)
    /// </summary>
    Escape,
    
    /// <summary>
    /// split the message at new lines
    /// </summary>
    Split,
    
    /// <summary>
    /// keep newlines as is (many syslog servers can handle newlines in the message part)
    /// </summary>
    Keep
  }
  
  private const int CloseTimeoutMillis = 5_000;
  private IUdpConnection? _connection;
  private BackgroundSender<byte[]>? _sender;
  private int _sendQueueSize = 500;
  private int _enqueueTimeoutMillis = 5_000;

  /// <summary>
  /// Initializes a new instance of the <see cref="RemoteSyslogAppender" /> class.
  /// </summary>
  /// <remarks>
  /// This instance of the <see cref="RemoteSyslogAppender" /> class is set up to write 
  /// to a remote syslog daemon.
  /// </remarks>
  public RemoteSyslogAppender()
  {
    // syslog udp defaults
    RemotePort = DefaultSyslogPort;
    RemoteAddress = System.Net.IPAddress.Parse("127.0.0.1");
    Encoding = Encoding.ASCII;
  }

  /// <summary>
  /// Message identity
  /// </summary>
  /// <remarks>
  /// <para>
  /// An identifier is specified with each log message. This can be specified
  /// by setting the <see cref="Identity"/> property. The identity (also known
  /// as the tag) must not contain white space. The default value for the
  /// identity is the application name (from <see cref="LoggingEvent.Domain"/>).
  /// </para>
  /// </remarks>
  public PatternLayout? Identity { get; set; }

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
  /// NewLine handling
  /// </summary>
  /// <remarks>The default value is <see cref="SyslogNewLineHandling.Escape"/>.</remarks>
  public SyslogNewLineHandling NewLineHandling { get; set; }
  
  /// <summary>
  /// Gets or sets the delegate used to create instances of <see cref="IUdpConnection"/>.
  /// </summary>
  protected virtual IUdpConnection CreateUdpConnection() => new UdpConnection();

  /// <summary>
  /// Add a mapping of level to severity
  /// </summary>
  /// <param name="mapping">The mapping to add</param>
  /// <remarks>
  /// <para>
  /// Add a <see cref="LevelSeverity"/> mapping to this appender.
  /// </para>
  /// </remarks>
  public void AddMapping(LevelSeverity mapping) => _levelMapping.Add(mapping);

  /// <summary>
  /// Writes the event to a remote syslog daemon.
  /// </summary>
  /// <param name="loggingEvent">The event to log.</param>
  /// <remarks>
  /// <para>
  /// This method is called by the <see cref="AppenderSkeleton.DoAppend(LoggingEvent)"/> method.
  /// </para>
  /// <para>
  /// The format of the output will depend on the appender's layout.
  /// </para>
  /// </remarks>
  protected override void Append(LoggingEvent loggingEvent)
  {
    loggingEvent.EnsureNotNull();
    try
    {
      // Priority
      int priority = GeneratePriority(Facility, GetSeverity(loggingEvent.Level));

      // Identity
      string? identity = ValidateIdentity(Identity?.Format(loggingEvent) ?? loggingEvent.Domain);

      // Message. The message goes after the tag/identity
      string message = RenderLoggingEvent(loggingEvent);

      int i = 0;

      StringBuilder builder = new();

      while (i < message.Length)
      {
        // Clear StringBuilder
        builder.Length = 0;

        // Write priority
        builder.Append('<').Append(priority).Append('>');

        // Write identity
        builder.Append(identity).Append(": ");

        AppendMessage(message, ref i, builder);

        // Grab as a byte array
        byte[] buffer = Encoding.GetBytes(builder.ToString());

        // A full queue is reported by the sender itself.
        _sender?.TryEnqueue(buffer, EnqueueTimeoutMillis);
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      ErrorHandler.Error(
        $"Unable to enqueue logging event to remote syslog {RemoteAddress} on port {RemotePort}.",
        e, ErrorCode.WriteFailure);
    }
  }

  /// <summary>
  /// Checks that <paramref name="identity"/> is usable as the TAG part of a syslog record, and
  /// reports it through the <see cref="AppenderSkeleton.ErrorHandler"/> when it is not.
  /// </summary>
  /// <param name="identity">The formatted <see cref="Identity"/>.</param>
  /// <returns>
  /// The identity, with any control character removed.
  /// </returns>
  /// <remarks>
  /// <para>
  /// The TAG is a structural identifier and therefore expected to be a constant chosen by the
  /// developer or operator, not something derived from a logging event. A malformed one is a
  /// configuration error rather than untrusted input, so it is reported instead of being altered
  /// silently: a carriage return or line feed in the TAG would split the record and let the text
  /// after it be read as a second record with its own facility and severity.
  /// </para>
  /// <para>
  /// The offending characters are removed rather than the event being dropped, so that an
  /// <see cref="Identity"/> pattern which does contain event data cannot be used to suppress
  /// records.
  /// </para>
  /// </remarks>
  private string? ValidateIdentity(string? identity)
  {
    if (identity is null)
    {
      return null;
    }

    StringBuilder? sanitized = null;
    for (int i = 0; i < identity.Length; i++)
    {
      // Control characters only. A carriage return or line feed ends the record, so the text
      // after it is read as a record of its own. Printable characters are left alone, including
      // the space that an application friendly name may contain, because they cannot break the
      // record apart.
      if (identity[i] is >= ' ' and not (char)127)
      {
        sanitized?.Append(identity[i]);
      }
      else
      {
        sanitized ??= new StringBuilder(identity.Length).Append(identity, 0, i);
      }
    }

    if (sanitized is null)
    {
      return identity;
    }

    ErrorHandler.Error(
      $"The Identity of appender [{Name}] rendered control characters, which would have split the syslog record, and they were removed. "
      + "Identity is a structural identifier and is expected to be a constant rather than a pattern that renders logging event data.");

    return sanitized.ToString();
  }

  /// <summary>
  /// Appends the rendered message to the buffer
  /// </summary>
  /// <param name="message">rendered message</param>
  /// <param name="characterIndex">index of the current character in the message</param>
  /// <param name="builder">buffer</param>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1045:Do not pass types by reference")]
  protected virtual void AppendMessage(string message, ref int characterIndex, StringBuilder builder)
  {
    message.EnsureNotNull();
    builder.EnsureNotNull();
    for (; characterIndex < message.Length; characterIndex++)
    {
      char c = message[characterIndex];

      // Accept only visible ASCII characters and space. See RFC 3164 section 4.1.3
      if (c is >= ' ' and <= (char)126)
      {
        builder.Append(c);
      }
      // If character is newline
      else if (c is '\r' or '\n')
      {
        if (NewLineHandling == SyslogNewLineHandling.Escape)
        {
          // escape
          builder.Append(c == '\r' ? "\\r" : "\\n");
        }
        else if (NewLineHandling == SyslogNewLineHandling.Keep)
        {
          // keep
          builder.Append(c);
        }
        else if (NewLineHandling == SyslogNewLineHandling.Split)
        {
          // break and send the current line
          // Check the next character to handle \r\n or \n\r
          if ((message.Length > characterIndex + 1)
              && ((message[characterIndex + 1] == '\r') || (message[characterIndex + 1] == '\n')))
          {
            characterIndex++;
          }
          characterIndex++;
          break;
        }
      }
    }
  }

  /// <summary>
  /// Initialize the options for this appender
  /// </summary>
  /// <remarks>
  /// <para>
  /// Initialize the level to syslog severity mappings set on this appender.
  /// </para>
  /// </remarks>
  public override void ActivateOptions()
  {
    base.ActivateOptions();
    if (NewLineHandling is not (SyslogNewLineHandling.Escape or SyslogNewLineHandling.Keep or SyslogNewLineHandling.Split))
    {
      throw SystemInfo.CreateArgumentOutOfRangeException(nameof(NewLineHandling), NewLineHandling,
        $"The NewLineHandling is not {SyslogNewLineHandling.Escape} or {SyslogNewLineHandling.Keep} or {SyslogNewLineHandling.Split}.");
    }
    _levelMapping.ActivateOptions();
    StopSending();

    IUdpConnection connection = CreateUdpConnection();
    try
    {
      connection.Connect(LocalPort, RemoteAddress.EnsureNotNull(), RemotePort);
    }
    catch (Exception e) when (!e.IsFatal())
    {
      ErrorHandler.Error(
        $"Unable to connect to remote syslog {RemoteAddress} on port {RemotePort} from local port {LocalPort}. "
        + "No logging event will be sent.",
        e, ErrorCode.GenericFailure);
      connection.Dispose();
      return;
    }

    _connection = connection;
    _sender = new(nameof(RemoteSyslogAppender), SendQueueSize, Send, Report);
  }

  /// <summary>
  /// How many datagrams may wait to be sent. Defaults to 500.
  /// </summary>
  /// <exception cref="ArgumentOutOfRangeException">The value specified is not positive.</exception>
  public int SendQueueSize
  {
    get => _sendQueueSize;
    set
    {
      if (value <= 0)
      {
        throw SystemInfo.CreateArgumentOutOfRangeException(nameof(value), value,
          "The value specified for SendQueueSize is not positive.");
      }
      _sendQueueSize = value;
    }
  }

  /// <summary>
  /// How long a logging call waits for room in a full queue. Defaults to 5000, 0 never waits.
  /// </summary>
  /// <exception cref="ArgumentOutOfRangeException">The value specified is negative.</exception>
  public int EnqueueTimeoutMillis
  {
    get => _enqueueTimeoutMillis;
    set
    {
      if (value < 0)
      {
        throw SystemInfo.CreateArgumentOutOfRangeException(nameof(value), value,
          "The value specified for EnqueueTimeoutMillis is negative.");
      }
      _enqueueTimeoutMillis = value;
    }
  }

  private void Send(byte[] datagram, CancellationToken cancellationToken)
    => _connection?.SendAsync(datagram, datagram.Length).GetAwaiter().GetResult();

  private void Report(string message, Exception? exception)
  {
    if (exception is null)
    {
      ErrorHandler.Error(message);
    }
    else
    {
      ErrorHandler.Error(message, exception);
    }
  }

  private void StopSending()
  {
    if (_sender is BackgroundSender<byte[]> sender)
    {
      _sender = null;
      sender.Close(CloseTimeoutMillis);
      sender.Dispose();
    }

    _connection?.Dispose();
    _connection = null;
  }

  /// <summary>
  /// Translates a log4net level to a syslog severity.
  /// </summary>
  /// <param name="level">A log4net level.</param>
  /// <returns>A syslog severity.</returns>
  protected virtual SyslogSeverity GetSeverity(Level? level)
  {
    if (_levelMapping.Lookup(level) is LevelSeverity levelSeverity)
    {
      return levelSeverity.Severity;
    }

    //
    // Fallback to sensible default values
    //

    if (level is null)
    {
      // Default setting
      return SyslogSeverity.Debug;
    }
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
  /// <remarks>
  /// <para>
  /// Generate a syslog priority.
  /// </para>
  /// </remarks>
  public static int GeneratePriority(SyslogFacility facility, SyslogSeverity severity)
  {
    if (facility is < SyslogFacility.Kernel or > SyslogFacility.Local7)
    {
      throw new ArgumentException($"{nameof(SyslogFacility)} out of range", nameof(facility));
    }

    if (severity is < SyslogSeverity.Emergency or > SyslogSeverity.Debug)
    {
      throw new ArgumentException($"{nameof(SyslogSeverity)} out of range", nameof(severity));
    }

    unchecked
    {
      return ((int)facility * 8) + (int)severity;
    }
  }

  /// <summary>
  /// Mapping from level object to syslog severity
  /// </summary>
  private readonly LevelMapping _levelMapping = new();

  /// <summary>
  /// A class to act as a mapping between the level that a logging call is made at and
  /// the syslog severity that it should be logged at.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A class to act as a mapping between the level that a logging call is made at and
  /// the syslog severity that it should be logged at.
  /// </para>
  /// </remarks>
  public class LevelSeverity : LevelMappingEntry
  {
    /// <summary>
    /// The mapped syslog severity for the specified level
    /// </summary>
    /// <remarks>
    /// <para>
    /// Required property.
    /// </para>
    /// </remarks>
    public SyslogSeverity Severity { get; set; }
  }

  /// <summary>
  /// Waits until everything queued so far has been sent.
  /// </summary>
  /// <param name="millisecondsTimeout">The maximum time to wait.</param>
  /// <returns><see langword="false"/> on timeout, or when the sender is no longer running.</returns>
  public override bool Flush(int millisecondsTimeout) => _sender?.Flush(millisecondsTimeout) ?? true;

  /// <inheritdoc/>
  protected override void OnClose()
  {
    StopSending();
    base.OnClose();
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Deliberately empty: this appender sends through the connection its pump owns, so the
  /// inherited client would be a second socket that nothing ever uses.
  /// </remarks>
  protected override void InitializeClientConnection()
  {
  }
}