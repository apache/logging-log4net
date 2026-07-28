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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using log4net.Ext.Mail.Appender;

using MailKit.Security;

using MimeKit;

namespace log4net.Ext.Mail.Tests.Appender;

/// <summary>
/// An <see cref="ISmtpTransport"/> that records what the appender asked it to do
/// instead of talking to an SMTP server.
/// </summary>
internal sealed class FakeSmtpTransport : ISmtpTransport
{
  /// <summary>
  /// The names of the transport methods that were called, in order.
  /// </summary>
  internal List<string> Calls { get; } = [];

  internal string? ConnectedHost { get; private set; }

  internal int ConnectedPort { get; private set; }

  internal SecureSocketOptions SecureSocketOptions { get; private set; }

  internal ICredentials? Credentials { get; private set; }

  internal SaslMechanism? SaslMechanism { get; private set; }

  /// <summary>
  /// A snapshot of every message passed to <see cref="Send"/>, taken before the
  /// appender disposes the <see cref="MimeMessage"/>.
  /// </summary>
  internal List<SentMail> SentMails { get; } = [];

  internal bool DisconnectedWithQuit { get; private set; }

  internal bool IsDisposed { get; private set; }

  /// <summary>
  /// When set, <see cref="Send"/> throws this instead of recording the message.
  /// </summary>
  internal Exception? SendException { get; set; }

  public bool IsConnected { get; private set; }

  public bool IsAuthenticated { get; private set; }

  public void Connect(string host, int port, SecureSocketOptions secureSocketOptions)
  {
    Calls.Add(nameof(Connect));
    ConnectedHost = host;
    ConnectedPort = port;
    SecureSocketOptions = secureSocketOptions;
    IsConnected = true;
  }

  public void Authenticate(ICredentials credentials)
  {
    Calls.Add(nameof(Authenticate));
    Credentials = credentials;
    IsAuthenticated = true;
  }

  public void Authenticate(SaslMechanism mechanism)
  {
    Calls.Add(nameof(Authenticate));
    SaslMechanism = mechanism;
    IsAuthenticated = true;
  }

  public void Send(MimeMessage message)
  {
    Calls.Add(nameof(Send));
    if (SendException is Exception exception)
    {
      throw exception;
    }
    SentMails.Add(new SentMail(message));
  }

  public void Disconnect(bool quit)
  {
    Calls.Add(nameof(Disconnect));
    DisconnectedWithQuit = quit;
    IsConnected = false;
  }

  public void Dispose()
  {
    Calls.Add(nameof(Dispose));
    IsDisposed = true;
  }
}

/// <summary>
/// The parts of a <see cref="MimeMessage"/> the tests assert on, captured eagerly
/// because the appender disposes the message once it has been sent.
/// </summary>
internal sealed class SentMail
{
  internal SentMail(MimeMessage message)
  {
    From = Addresses(message.From);
    To = Addresses(message.To);
    Cc = Addresses(message.Cc);
    Bcc = Addresses(message.Bcc);
    ReplyTo = Addresses(message.ReplyTo);
    Subject = message.Subject;
    Priority = message.Priority;

    Header? subjectHeader = message.Headers.FirstOrDefault(h => h.Id == HeaderId.Subject);
    RawSubjectHeader = subjectHeader is null
      ? string.Empty
      : Encoding.ASCII.GetString(subjectHeader.RawValue);

    TextPart? textPart = message.Body as TextPart;
    Body = textPart?.Text ?? string.Empty;
    BodyCharset = textPart?.ContentType.Charset;
  }

  internal string[] From { get; }

  internal string[] To { get; }

  internal string[] Cc { get; }

  internal string[] Bcc { get; }

  internal string[] ReplyTo { get; }

  internal string? Subject { get; }

  /// <summary>
  /// The on-the-wire subject header, so that tests can check the applied charset.
  /// </summary>
  internal string RawSubjectHeader { get; }

  internal MessagePriority Priority { get; }

  internal string Body { get; }

  internal string? BodyCharset { get; }

  private static string[] Addresses(InternetAddressList list)
    => list.Mailboxes.Select(m => m.Address).ToArray();
}
