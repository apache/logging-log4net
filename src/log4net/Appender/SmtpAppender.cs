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

using System.Net.Mail;
using log4net.Core;
using log4net.Util;

namespace log4net.Appender;

/// <summary>
/// Send an e-mail when a specific logging event occurs, typically on errors 
/// or fatal errors.
/// </summary>
/// <remarks>
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
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
public class SmtpAppender : BufferingAppenderSkeleton
{
  /// <summary>
  /// Default constructor
  /// </summary>
  /// <remarks>
  /// <para>
  /// Default constructor
  /// </para>
  /// </remarks>
  public SmtpAppender()
  {
  }

  /// <summary>
  /// Gets or sets a comma-delimited list of recipient e-mail addresses.
  /// </summary>
  public string? To
  {
    get { return _to; }
    set { _to = MaybeTrimSeparators(value); }
  }

  /// <summary>
  /// Gets or sets a comma-delimited list of recipient e-mail addresses 
  /// that will be carbon copied.
  /// </summary>
  public string? Cc
  {
    get => _cc;
    set => _cc = MaybeTrimSeparators(value);
  }

  /// <summary>
  /// Gets or sets a semicolon-delimited list of recipient e-mail addresses
  /// that will be blind carbon copied.
  /// </summary>
  /// <value>
  /// A semicolon-delimited list of e-mail addresses.
  /// </value>
  /// <remarks>
  /// <para>
  /// A semicolon-delimited list of recipient e-mail addresses.
  /// </para>
  /// </remarks>
  public string? Bcc
  {
    get => _bcc;
    set => _bcc = MaybeTrimSeparators(value);
  }

  /// <summary>
  /// Gets or sets the e-mail address of the sender.
  /// </summary>
  /// <value>
  /// The e-mail address of the sender.
  /// </value>
  /// <remarks>
  /// <para>
  /// The e-mail address of the sender.
  /// </para>
  /// </remarks>
  public string? From { get; set; }

  /// <summary>
  /// Gets or sets the subject line of the e-mail message.
  /// </summary>
  /// <value>
  /// The subject line of the e-mail message.
  /// </value>
  /// <remarks>
  /// <para>
  /// The subject line of the e-mail message.
  /// </para>
  /// </remarks>
  public string? Subject { get; set; }

  /// <summary>
  /// Gets or sets the name of the SMTP relay mail server to use to send 
  /// the e-mail messages.
  /// </summary>
  /// <value>
  /// The name of the e-mail relay server. If SmtpServer is not set, the 
  /// name of the local SMTP server is used.
  /// </value>
  /// <remarks>
  /// <para>
  /// The name of the e-mail relay server. If SmtpServer is not set, the 
  /// name of the local SMTP server is used.
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
  /// When using <see cref="SmtpAuthentication.Ntlm"/> the Windows credentials for the current
  /// thread, if impersonating, or the process will be used to authenticate. 
  /// </para>
  /// </remarks>
  public SmtpAuthentication Authentication { get; set; } = SmtpAuthentication.None;

  /// <summary>
  /// The username to use to authenticate with the SMTP server
  /// </summary>
  /// <remarks>
  /// <para>
  /// A <see cref="Username"/> and <see cref="Password"/> must be specified when 
  /// <see cref="Authentication"/> is set to <see cref="SmtpAuthentication.Basic"/>, 
  /// otherwise the username will be ignored. 
  /// </para>
  /// </remarks>
  public string? Username { get; set; }

  /// <summary>
  /// The password to use to authenticate with the SMTP server
  /// </summary>
  /// <remarks>
  /// <para>
  /// A <see cref="Username"/> and <see cref="Password"/> must be specified when 
  /// <see cref="Authentication"/> is set to <see cref="SmtpAuthentication.Basic"/>, 
  /// otherwise the password will be ignored. 
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
  /// </remarks>
  public MailPriority Priority { get; set; } = MailPriority.Normal;

  /// <summary>
  /// Enable or disable use of SSL when sending e-mail message
  /// </summary>
  /// <remarks>
  /// This is available on MS .NET 2.0 runtime and higher
  /// </remarks>
  public bool EnableSsl { get; set; }

  /// <summary>
  /// Gets or sets the reply-to e-mail address.
  /// </summary>
  public string? ReplyTo { get; set; }

  /// <summary>
  /// Gets or sets the subject encoding to be used.
  /// </summary>
  /// <remarks>
  /// The default encoding is the operating system's current ANSI codepage.
  /// </remarks>
  public Encoding SubjectEncoding { get; set; } = Encoding.UTF8;

  /// <summary>
  /// Gets or sets the body encoding to be used.
  /// </summary>
  /// <remarks>
  /// The default encoding is the operating system's current ANSI codepage.
  /// </remarks>
  public Encoding BodyEncoding { get; set; } = Encoding.UTF8;

  /// <summary>
  /// Sends the contents of the cyclic buffer as an e-mail message.
  /// </summary>
  /// <param name="events">The logging events to send.</param>
  protected override void SendBuffer(LoggingEvent[] events)
  {
    // Note: this code already owns the monitor for this
    // appender. This frees us from needing to synchronize again.
    try
    {
      using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);

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

      SendEmail(writer.ToString());
    }
    catch (Exception e)
    {
      ErrorHandler.Error("Error occurred while sending e-mail notification.", e);
    }
  }

  /// <summary>
  /// This appender requires a <see cref="Layout"/> to be set.
  /// </summary>
  protected override bool RequiresLayout => true;

  /// <summary>
  /// Send the email message
  /// </summary>
  /// <param name="messageBody">the body text to include in the mail</param>
  protected virtual void SendEmail(string messageBody)
  {
    // Create and configure the smtp client
#pragma warning disable CS0618 // Type or member is obsolete
    using SmtpClient smtpClient = new();
#pragma warning restore CS0618 // Type or member is obsolete
    if (!string.IsNullOrEmpty(SmtpHost))
    {
      smtpClient.Host = SmtpHost;
    }
    smtpClient.Port = Port;
    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
    smtpClient.EnableSsl = EnableSsl;

    if (Authentication == SmtpAuthentication.Basic)
    {
      // Perform basic authentication
      smtpClient.Credentials = new System.Net.NetworkCredential(Username, Password);
    }
    else if (Authentication == SmtpAuthentication.Ntlm)
    {
      // Perform integrated authentication (NTLM)
      smtpClient.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
    }

    using var mailMessage = new MailMessage();
    mailMessage.Body = messageBody;
    mailMessage.BodyEncoding = BodyEncoding;
    mailMessage.From = new MailAddress(From.EnsureNotNull());
    mailMessage.To.Add(_to.EnsureNotNull());
    if (!string.IsNullOrEmpty(_cc))
    {
      mailMessage.CC.Add(_cc);
    }
    if (!string.IsNullOrEmpty(_bcc))
    {
      mailMessage.Bcc.Add(_bcc);
    }
    if (!string.IsNullOrEmpty(ReplyTo))
    {
      mailMessage.ReplyToList.Add(new MailAddress(ReplyTo));
    }
    mailMessage.Subject = Subject;
    mailMessage.SubjectEncoding = SubjectEncoding;
    mailMessage.Priority = Priority;

    // TODO: Consider using SendAsync to send the message without blocking. We would need a SendCompletedCallback to log errors.
    smtpClient.Send(mailMessage);
  }

  private string? _to;
  private string? _cc;
  private string? _bcc;

  // authentication fields

  // server port, default port 25

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
    /// Integrated authentication
    /// </summary>
    /// <remarks>
    /// Uses the Windows credentials from the current thread or process to authenticate.
    /// </remarks>
    Ntlm
  }

  // Allow semicolon delimiters for backward compatibility.
  private static readonly char[] _addressDelimiters = [',', ';'];

  /// <summary>
  /// Trims leading and trailing commas or semicolons
  /// </summary>
  private static string? MaybeTrimSeparators(string? s) => s?.Trim(_addressDelimiters);
}