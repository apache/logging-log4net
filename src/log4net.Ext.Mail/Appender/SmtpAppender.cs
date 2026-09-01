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
using System.Net;
using System.Threading;
using System.Net.Mail;
using System.Text;

using log4net.Appender;
using log4net.Core;
using log4net.Ext.Mail.Appender.Internal;
using log4net.Util;
using MailKit.Security;

using MimeKit;
using MimeKit.Text;

namespace log4net.Ext.Mail.Appender;

/// <summary>
/// Send an e-mail when a specific logging event occurs, typically on errors
/// or fatal errors, using MailKit as the SMTP client.
/// </summary>
/// <remarks>
/// <para>
/// This appender exposes the same options as <see cref="log4net.Appender.SmtpAppender"/>,
/// so an existing configuration can be pointed at this type without change. The difference
/// is the transport: it sends through <see cref="MailKit.Net.Smtp.SmtpClient"/> instead of
/// the obsolete <see cref="System.Net.Mail.SmtpClient"/>.
/// </para>
/// <para>
/// The number of logging events delivered in this e-mail depend on
/// the value of <see cref="BufferingAppenderSkeleton.BufferSize"/> option. The
/// <see cref="SmtpAppender"/> keeps only the last
/// <see cref="BufferingAppenderSkeleton.BufferSize"/> logging events in its
/// cyclic buffer. This keeps memory requirements at a reasonable level while
/// still delivering useful application context.
/// </para>
/// <para>
/// Authentication is supported by setting the <see cref="Authentication"/> property to
/// either <see cref="SmtpAuthentication.Basic"/> or <see cref="SmtpAuthentication.Ntlm"/>.
/// If using <see cref="SmtpAuthentication.Basic"/> authentication then the <see cref="Username"/>
/// and <see cref="Password"/> properties must also be set.
/// </para>
/// <para>
/// To set the SMTP server port use the <see cref="Port"/> property. The default port is 25.
/// </para>
/// <para>
/// Unlike <see cref="System.Net.Mail.SmtpClient"/>, MailKit has no notion of a machine-wide
/// default SMTP server, so <see cref="SmtpHost"/> is required.
/// </para>
/// </remarks>
public class SmtpAppender : BufferingAppenderSkeleton
{
  /// <summary>
  /// The port reserved for SMTP over implicit TLS, where TLS starts before the SMTP greeting
  /// rather than being negotiated with <c>STARTTLS</c>.
  /// </summary>
  private const int ImplicitTlsPort = 465;

  private readonly Func<ISmtpTransport> _transportFactory;

  /// <summary>
  /// Default constructor. Sends through <see cref="MailKitSmtpTransport"/>.
  /// </summary>
  public SmtpAppender()
    : this(static () => new MailKitSmtpTransport())
  { }

  /// <summary>
  /// Creates an appender that obtains its transport from <paramref name="transportFactory"/>.
  /// </summary>
  /// <param name="transportFactory">
  /// Called once per e-mail to create the transport used to send it.
  /// </param>
  internal SmtpAppender(Func<ISmtpTransport> transportFactory)
    => _transportFactory = transportFactory.EnsureNotNull();

  /// <summary>
  /// Gets or sets a comma-delimited list of recipient e-mail addresses.
  /// </summary>
  public string? To
  {
    get;
    set => field = MaybeTrimSeparators(value);
  }

  /// <summary>
  /// Gets or sets a comma-delimited list of recipient e-mail addresses
  /// that will be carbon copied.
  /// </summary>
  public string? Cc
  {
    get;
    set => field = MaybeTrimSeparators(value);
  }

  /// <summary>
  /// Gets or sets a comma-delimited list of recipient e-mail addresses
  /// that will be blind carbon copied.
  /// </summary>
  /// <value>
  /// A comma-delimited list of e-mail addresses.
  /// </value>
  /// <remarks>
  /// <para>
  /// Semicolons are also accepted as separators, for backward compatibility.
  /// </para>
  /// </remarks>
  public string? Bcc
  {
    get;
    set => field = MaybeTrimSeparators(value);
  }

  /// <summary>
  /// Gets or sets the e-mail address of the sender.
  /// </summary>
  /// <value>
  /// The e-mail address of the sender.
  /// </value>
  public string? From { get; set; }

  /// <summary>
  /// Gets or sets the subject line of the e-mail message.
  /// </summary>
  /// <value>
  /// The subject line of the e-mail message.
  /// </value>
  public string? Subject { get; set; }

  /// <summary>
  /// Gets or sets the name of the SMTP relay mail server to use to send
  /// the e-mail messages.
  /// </summary>
  /// <value>
  /// The name of the e-mail relay server.
  /// </value>
  /// <remarks>
  /// <para>
  /// This option is required. MailKit, unlike <see cref="System.Net.Mail.SmtpClient"/>,
  /// has no machine-wide default SMTP server to fall back on.
  /// </para>
  /// </remarks>
  public string? SmtpHost { get; set; }

  /// <summary>
  /// The mode to use to authentication with the SMTP server
  /// </summary>
  /// <remarks>
  /// <para>
  /// Valid Authentication mode values are: <see cref="SmtpAuthentication.None"/>,
  /// <see cref="SmtpAuthentication.Basic"/>, and <see cref="SmtpAuthentication.Ntlm"/>.
  /// The default value is <see cref="SmtpAuthentication.None"/>. When using
  /// <see cref="SmtpAuthentication.Basic"/> you must specify the <see cref="Username"/>
  /// and <see cref="Password"/> to use to authenticate.
  /// </para>
  /// <para>
  /// <see cref="SmtpAuthentication.Ntlm"/> authenticates with the NTLM SASL mechanism.
  /// Note that MailKit cannot reuse the Windows logon session of the current thread or
  /// process the way <see cref="System.Net.Mail.SmtpClient"/> could, so
  /// <see cref="Username"/> and <see cref="Password"/> must be supplied for NTLM as well.
  /// </para>
  /// </remarks>
  public SmtpAuthentication Authentication { get; set; } = SmtpAuthentication.None;

  /// <summary>
  /// The username to use to authenticate with the SMTP server
  /// </summary>
  /// <remarks>
  /// <para>
  /// A <see cref="Username"/> and <see cref="Password"/> must be specified when
  /// <see cref="Authentication"/> is set to <see cref="SmtpAuthentication.Basic"/>
  /// or <see cref="SmtpAuthentication.Ntlm"/>, otherwise the username will be ignored.
  /// </para>
  /// </remarks>
  public string? Username { get; set; }

  /// <summary>
  /// The password to use to authenticate with the SMTP server
  /// </summary>
  /// <remarks>
  /// <para>
  /// A <see cref="Username"/> and <see cref="Password"/> must be specified when
  /// <see cref="Authentication"/> is set to <see cref="SmtpAuthentication.Basic"/>
  /// or <see cref="SmtpAuthentication.Ntlm"/>, otherwise the password will be ignored.
  /// </para>
  /// </remarks>
  public string? Password { get; set; }

  /// <summary>
  /// The port on which the SMTP server is listening
  /// </summary>
  /// <remarks>
  /// <para>
  /// The port on which the SMTP server is listening. The default
  /// port is <c>25</c>.
  /// </para>
  /// </remarks>
  public int Port { get; set; } = 25;

  /// <summary>
  /// Gets or sets the priority of the e-mail message
  /// </summary>
  /// <value>
  /// One of the <see cref="MailPriority"/> values.
  /// </value>
  /// <remarks>
  /// <para>
  /// Sets the priority of the e-mails generated by this
  /// appender. The default priority is <see cref="MailPriority.Normal"/>.
  /// </para>
  /// <para>
  /// If you are using this appender to report errors then
  /// you may want to set the priority to <see cref="MailPriority.High"/>.
  /// </para>
  /// <para>
  /// The value is mapped onto the MIME <c>Priority</c> header:
  /// <see cref="MailPriority.Low"/> becomes <see cref="MessagePriority.NonUrgent"/>,
  /// <see cref="MailPriority.Normal"/> becomes <see cref="MessagePriority.Normal"/> and
  /// <see cref="MailPriority.High"/> becomes <see cref="MessagePriority.Urgent"/>.
  /// </para>
  /// </remarks>
  public MailPriority Priority { get; set; } = MailPriority.Normal;

  /// <summary>
  /// Enable or disable use of SSL/TLS when sending e-mail message
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is a shorthand for <see cref="TransportSecurity"/>: setting it to
  /// <see langword="true"/> selects <see cref="SmtpTransportSecurity.Required"/> and setting it to
  /// <see langword="false"/> selects <see cref="SmtpTransportSecurity.None"/>. The two properties
  /// are the same setting, so the one assigned last wins.
  /// </para>
  /// <para>
  /// When <see langword="true"/>, transport security is required: implicit TLS on port 465 and
  /// <c>STARTTLS</c> on every other port. Connecting fails if the server does not offer TLS,
  /// rather than continuing unencrypted, which matches the behaviour of
  /// <see cref="System.Net.Mail.SmtpClient.EnableSsl"/>. Use <see cref="TransportSecurity"/> when
  /// the server needs something else.
  /// </para>
  /// </remarks>
  public bool EnableSsl
  {
    get => TransportSecurity != SmtpTransportSecurity.None;
    set => TransportSecurity = value ? SmtpTransportSecurity.Required : SmtpTransportSecurity.None;
  }

  /// <summary>
  /// Gets or sets how the connection to the SMTP server is secured.
  /// </summary>
  /// <value>
  /// One of the <see cref="SmtpTransportSecurity"/> values. The default is
  /// <see cref="SmtpTransportSecurity.None"/>.
  /// </value>
  /// <remarks>
  /// <para>
  /// <see cref="EnableSsl"/> is a shorthand for this property and covers the usual cases; set this
  /// one when the server expects implicit TLS on a port other than 465, or when only opportunistic
  /// <c>STARTTLS</c> is possible.
  /// </para>
  /// </remarks>
  public SmtpTransportSecurity TransportSecurity { get; set; } = SmtpTransportSecurity.None;

  /// <summary>
  /// Gets or sets the reply-to e-mail address.
  /// </summary>
  public string? ReplyTo { get; set; }

  /// <summary>
  /// Gets or sets the subject encoding to be used.
  /// </summary>
  /// <remarks>
  /// The default encoding is <see cref="Encoding.UTF8"/>.
  /// </remarks>
  public Encoding SubjectEncoding { get; set; } = Encoding.UTF8;

  /// <summary>
  /// Gets or sets the body encoding to be used.
  /// </summary>
  /// <remarks>
  /// The default encoding is <see cref="Encoding.UTF8"/>.
  /// </remarks>
  public Encoding BodyEncoding { get; set; } = Encoding.UTF8;

  /// <summary>
  /// Sends the contents of the cyclic buffer as an e-mail message.
  /// </summary>
  /// <param name="events">The logging events to send.</param>
  protected override void SendBuffer(LoggingEvent[] events)
  {
    events.EnsureNotNull();
    // Note: this code already owns the monitor for this
    // appender. This frees us from needing to synchronize again.
    try
    {
      using StringWriter writer = new(System.Globalization.CultureInfo.InvariantCulture);

      if (Layout?.Header is string header)
      {
        writer.Write(header);
      }

      for (int i = 0; i < events.Length; i++)
      {
        // Render the event and append the text to the buffer
        RenderLoggingEvent(writer, events[i]);
      }

      if (Layout?.Footer is string footer)
      {
        writer.Write(footer);
      }

      string body = writer.ToString();
      if (_sender is BackgroundSender<string> sender)
      {
        // A full queue is reported by the sender itself.
        sender.TryEnqueue(body, EnqueueTimeoutMillis);
      }
      else
      {
        SendEmail(body);
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      ErrorHandler.Error("Error occurred while sending e-mail notification.", e);
    }
  }

  /// <inheritdoc/>
  public override void ActivateOptions()
  {
    base.ActivateOptions();
    CloseSender();
    _sender = new(nameof(SmtpAppender), SendQueueSize, (body, _) => SendEmail(body), Report);
  }

  /// <inheritdoc/>
  public override bool Flush(int millisecondsTimeout)
  {
    base.Flush();
    return _sender?.Flush(millisecondsTimeout) ?? true;
  }

  /// <inheritdoc/>
  protected override void OnClose()
  {
    base.OnClose();
    CloseSender();
  }

  /// <summary>
  /// How many rendered mails may wait to be sent. Defaults to 500.
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

  private void CloseSender()
  {
    if (_sender is BackgroundSender<string> sender)
    {
      _sender = null;
      sender.Close(SendTimeoutMillis);
      sender.Dispose();
    }
  }

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

  private BackgroundSender<string>? _sender;
  private int _sendQueueSize = 500;
  private int _enqueueTimeoutMillis = 5_000;

  /// <summary>
  /// This appender requires a <see cref="AppenderSkeleton.Layout"/> to be set.
  /// </summary>
  protected override bool RequiresLayout => true;

  /// <summary>
  /// Translates <see cref="TransportSecurity"/> into the mail library's own representation.
  /// </summary>
  /// <returns>The transport security to connect with.</returns>
  /// <remarks>
  /// <para>
  /// <see cref="SecureSocketOptions.Auto"/> is deliberately never used: away from port 465 it is
  /// opportunistic, so an attacker who strips <c>STARTTLS</c> from the EHLO response silently
  /// downgrades the session to plaintext, taking the credentials and the log content with it.
  /// Opportunistic behavior is available, but only by asking for it with
  /// <see cref="SmtpTransportSecurity.StartTlsWhenAvailable"/>.
  /// </para>
  /// </remarks>
  private SecureSocketOptions ResolveSecureSocketOptions() => TransportSecurity switch
  {
    SmtpTransportSecurity.None => SecureSocketOptions.None,
    SmtpTransportSecurity.Required => Port == ImplicitTlsPort
      ? SecureSocketOptions.SslOnConnect
      : SecureSocketOptions.StartTls,
    SmtpTransportSecurity.ImplicitTls => SecureSocketOptions.SslOnConnect,
    SmtpTransportSecurity.StartTls => SecureSocketOptions.StartTls,
    SmtpTransportSecurity.StartTlsWhenAvailable => SecureSocketOptions.StartTlsWhenAvailable,
    _ => throw SystemInfo.CreateArgumentOutOfRangeException(nameof(TransportSecurity), TransportSecurity,
      $"The value specified for TransportSecurity is not one of the {nameof(SmtpTransportSecurity)} values.")
  };

  /// <summary>
  /// Send the email message
  /// </summary>
  /// <param name="messageBody">the body text to include in the mail</param>
  protected virtual void SendEmail(string messageBody)
  {
    using CancellationTokenSource deadline = new(SendTimeoutMillis);
    using MimeMessage message = CreateMessage(messageBody);
    using ISmtpTransport transport = _transportFactory().EnsureNotNull();
    transport.Timeout = SendTimeoutMillis;

    transport.Connect(SmtpHost.EnsureNotNullOrEmpty(), Port, ResolveSecureSocketOptions(), deadline.Token);
    try
    {
      switch (Authentication)
      {
        case SmtpAuthentication.Basic:
          transport.Authenticate(new NetworkCredential(Username, Password), deadline.Token);
          break;
        case SmtpAuthentication.Ntlm:
          transport.Authenticate(new SaslMechanismNtlm(new NetworkCredential(Username, Password)), deadline.Token);
          break;
        case SmtpAuthentication.None:
        default:
          break;
      }

      transport.Send(message, deadline.Token);
    }
    finally
    {
      // No token: already cancelled, it would replace the failure that got us here.
      transport.Disconnect(true);
    }
  }

  /// <summary>
  /// A deadline for the whole send, in milliseconds. Defaults to 15000.
  /// </summary>
  /// <remarks>
  /// MailKit's own timeout applies per operation, so a server answering just inside it can still
  /// take a multiple of it. The mail goes out under the appender lock.
  /// </remarks>
  /// <exception cref="ArgumentOutOfRangeException">The value specified is not positive.</exception>
  public int SendTimeoutMillis
  {
    get => _sendTimeoutMillis;
    set
    {
      if (value <= 0)
      {
        throw SystemInfo.CreateArgumentOutOfRangeException(nameof(value), value,
          "The value specified for SendTimeoutMillis is not positive.");
      }
      _sendTimeoutMillis = value;
    }
  }

  private int _sendTimeoutMillis = 15_000;

  /// <summary>
  /// Builds the <see cref="MimeMessage"/> for the given body text from the configured options.
  /// </summary>
  /// <param name="messageBody">the body text to include in the mail</param>
  /// <returns>the message to send</returns>
  /// <remarks>
  /// <para>
  /// Not CLS compliant, because <see cref="MimeMessage"/> comes from MimeKit, which does not
  /// declare itself CLS compliant.
  /// </para>
  /// </remarks>
  [CLSCompliant(false)]
  protected virtual MimeMessage CreateMessage(string messageBody)
  {
    MimeMessage message = new();
    message.From.AddRange(ParseAddresses(From.EnsureNotNullOrEmpty()));
    message.To.AddRange(ParseAddresses(To.EnsureNotNullOrEmpty()));
    if (!string.IsNullOrEmpty(Cc))
    {
      message.Cc.AddRange(ParseAddresses(Cc!));
    }
    if (!string.IsNullOrEmpty(Bcc))
    {
      message.Bcc.AddRange(ParseAddresses(Bcc!));
    }
    if (!string.IsNullOrEmpty(ReplyTo))
    {
      message.ReplyTo.AddRange(ParseAddresses(ReplyTo!));
    }

    if (Subject is not null)
    {
      // Set through the header collection so that the configured encoding is honoured.
      message.Headers.Replace(HeaderId.Subject, SubjectEncoding, Subject);
    }

    message.Priority = Priority switch
    {
      MailPriority.Low => MessagePriority.NonUrgent,
      MailPriority.High => MessagePriority.Urgent,
      _ => MessagePriority.Normal,
    };

    TextPart body = new(TextFormat.Plain);
    body.SetText(BodyEncoding, messageBody);
    message.Body = body;

    return message;
  }

  /// <summary>
  /// Values for the <see cref="Authentication"/> property.
  /// </summary>
  /// <remarks>
  /// <para>
  /// SMTP authentication modes.
  /// </para>
  /// </remarks>
  public enum SmtpAuthentication
  {
    /// <summary>
    /// No authentication
    /// </summary>
    None,

    /// <summary>
    /// Basic authentication.
    /// </summary>
    /// <remarks>
    /// Requires a username and password to be supplied
    /// </remarks>
    Basic,

    /// <summary>
    /// NTLM authentication.
    /// </summary>
    /// <remarks>
    /// Requires a username and password to be supplied; MailKit cannot reuse the
    /// Windows logon session of the current thread or process.
    /// </remarks>
    Ntlm
  }

  // Allow semicolon delimiters for backward compatibility.
  private static readonly char[] _addressDelimiters = [',', ';'];

  /// <summary>
  /// Trims leading and trailing commas or semicolons
  /// </summary>
  private static string? MaybeTrimSeparators(string? s) => s?.Trim(_addressDelimiters);

  /// <summary>
  /// Parses a comma- or semicolon-delimited list of addresses.
  /// </summary>
  /// <remarks>
  /// RFC 5322 only allows commas, which is what <see cref="InternetAddressList.Parse(string)"/>
  /// accepts, so semicolon-delimited lists are retried after normalization.
  /// </remarks>
  private static InternetAddressList ParseAddresses(string addresses)
    => InternetAddressList.TryParse(addresses, out InternetAddressList? list)
      ? list
      : InternetAddressList.Parse(ReplaceUnquotedSemicolons(addresses));

  /// <summary>
  /// Replaces every semicolon that is not inside a quoted string with a comma.
  /// </summary>
  private static string ReplaceUnquotedSemicolons(string addresses)
  {
    StringBuilder result = new(addresses.Length);
    bool inQuotes = false;
    bool escaped = false;
    foreach (char character in addresses)
    {
      if (escaped)
      {
        escaped = false;
        result.Append(character);
        continue;
      }

      switch (character)
      {
        case '\\' when inQuotes:
          escaped = true;
          result.Append(character);
          break;
        case '"':
          inQuotes = !inQuotes;
          result.Append(character);
          break;
        case ';' when !inQuotes:
          result.Append(',');
          break;
        default:
          result.Append(character);
          break;
      }
    }
    return result.ToString();
  }
}
