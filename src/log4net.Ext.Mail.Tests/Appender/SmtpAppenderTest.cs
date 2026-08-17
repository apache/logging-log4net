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
using System.Net;
using System.Net.Mail;
using System.Text;
using log4net.Core;
using log4net.Ext.Mail.Appender;
using log4net.Layout;
using MailKit.Security;
using MimeKit;
using NUnit.Framework;

namespace log4net.Ext.Mail.Tests.Appender;

/// <summary>
/// Unit tests for the MailKit based <see cref="SmtpAppender"/>. No mail leaves the
/// process: every test drives a <see cref="FakeSmtpTransport"/>.
/// </summary>
[TestFixture]
public class SmtpAppenderTest
{
  /// <summary>
  /// An <see cref="IErrorHandler"/> that collects what the appender reported.
  /// </summary>
  private sealed class SilentErrorHandler : IErrorHandler
  {
    private readonly StringBuilder _buffer = new();

    public string Message => _buffer.ToString();

    public void Error(string message) => _buffer.Append(message + '\n');

    public void Error(string message, Exception e) => _buffer.Append(message + '\n' + e.Message + '\n');

    public void Error(string message, Exception? e, ErrorCode errorCode)
      => _buffer.Append(message + '\n' + e?.Message + '\n');
  }

  private FakeSmtpTransport _transport = null!;
  private SilentErrorHandler _errorHandler = null!;

  [SetUp]
  public void SetUp()
  {
    _transport = new FakeSmtpTransport();
    _errorHandler = new SilentErrorHandler();
  }

  [TearDown]
  public void TearDown() => _transport.Dispose();

  /// <summary>
  /// Creates an appender wired to <see cref="_transport"/> with the minimum required options,
  /// configured so that every appended event is sent immediately.
  /// </summary>
  private SmtpAppender CreateAppender(string? header = null, string? footer = null)
  {
    PatternLayout layout = new() { ConversionPattern = "%m%n", Header = header, Footer = footer };
    layout.ActivateOptions();

    return new(() => _transport)
    {
      Layout = layout,
      ErrorHandler = _errorHandler,
      SmtpHost = "mail.example.com",
      From = "from@example.com",
      To = "to@example.com",
      Subject = "subject",
      // BufferSize of 1 makes BufferingAppenderSkeleton send each event straight away.
      BufferSize = 1,
    };
  }

  private static LoggingEvent CreateEvent(string message)
    => new(new LoggingEventData
    {
      LoggerName = "TestLogger",
      Level = Level.Error,
      Message = message,
      TimeStampUtc = DateTime.UtcNow,
    });

  /// <summary>
  /// Activates the appender and appends a single event, which triggers one send.
  /// </summary>
  private static void Append(SmtpAppender appender, string message = "log message")
  {
    appender.ActivateOptions();
    appender.DoAppend(CreateEvent(message));
  }

  [Test]
  public void SendsOneMailPerEventWhenNotBuffering()
  {
    SmtpAppender appender = CreateAppender();

    Append(appender);

    Assert.That(_errorHandler.Message, Is.Empty);
    Assert.That(_transport.SentMails, Has.Count.EqualTo(1));
  }

  [Test]
  public void ConnectsToConfiguredHostAndPort()
  {
    SmtpAppender appender = CreateAppender();
    appender.SmtpHost = "smtp.internal";
    appender.Port = 2525;

    Append(appender);

    Assert.That(_transport.ConnectedHost, Is.EqualTo("smtp.internal"));
    Assert.That(_transport.ConnectedPort, Is.EqualTo(2525));
  }

  [Test]
  public void DefaultPortIs25()
  {
    SmtpAppender appender = CreateAppender();

    Append(appender);

    Assert.That(_transport.ConnectedPort, Is.EqualTo(25));
  }

  [Test]
  public void ConnectAuthenticateSendDisconnectHappenInOrder()
  {
    SmtpAppender appender = CreateAppender();
    appender.Authentication = SmtpAppender.SmtpAuthentication.Basic;
    appender.Username = "user";
    appender.Password = "secret";

    Append(appender);

    Assert.That(_transport.Calls, Is.EqualTo(new[] { "Connect", "Authenticate", "Send", "Disconnect", "Dispose" }));
  }

  [Test]
  public void DoesNotAuthenticateWhenAuthenticationIsNone()
  {
    SmtpAppender appender = CreateAppender();

    Append(appender);

    Assert.That(_transport.IsAuthenticated, Is.False);
    Assert.That(_transport.Calls, Does.Not.Contain("Authenticate"));
  }

  [Test]
  public void BasicAuthenticationPassesUsernameAndPassword()
  {
    SmtpAppender appender = CreateAppender();
    appender.Authentication = SmtpAppender.SmtpAuthentication.Basic;
    appender.Username = "user";
    appender.Password = "secret";

    Append(appender);

    Assert.That(_transport.SaslMechanism, Is.Null);
    NetworkCredential credential = (NetworkCredential)_transport.Credentials!;
    Assert.That(credential.UserName, Is.EqualTo("user"));
    Assert.That(credential.Password, Is.EqualTo("secret"));
  }

  [Test]
  public void NtlmAuthenticationUsesTheNtlmSaslMechanism()
  {
    SmtpAppender appender = CreateAppender();
    appender.Authentication = SmtpAppender.SmtpAuthentication.Ntlm;
    appender.Username = "user";
    appender.Password = "secret";

    Append(appender);

    Assert.That(_transport.Credentials, Is.Null);
    Assert.That(_transport.SaslMechanism, Is.InstanceOf<SaslMechanismNtlm>());
    Assert.That(_transport.SaslMechanism!.Credentials.GetCredential(null, null).UserName, Is.EqualTo("user"));
  }

  [Test]
  public void EnableSslOffConnectsWithoutTransportSecurity()
  {
    SmtpAppender appender = CreateAppender();

    Append(appender);

    Assert.That(_transport.SecureSocketOptions, Is.EqualTo(SecureSocketOptions.None));
  }

  /// <summary>
  /// SecureSocketOptions.Auto is opportunistic away from port 465, so an attacker who strips
  /// STARTTLS from the EHLO response downgrades the session to plaintext. Asking for SSL has to
  /// mean mandatory STARTTLS.
  /// </summary>
  [Test]
  public void EnableSslOnRequiresStartTls()
  {
    SmtpAppender appender = CreateAppender();
    appender.EnableSsl = true;

    Append(appender);

    Assert.That(_transport.SecureSocketOptions, Is.EqualTo(SecureSocketOptions.StartTls));
  }

  /// <summary>
  /// Port 465 is implicit TLS: the session is encrypted before the SMTP greeting, so STARTTLS is
  /// never offered there and must not be demanded.
  /// </summary>
  [Test]
  public void EnableSslOnPort465UsesImplicitTls()
  {
    SmtpAppender appender = CreateAppender();
    appender.EnableSsl = true;
    appender.Port = 465;

    Append(appender);

    Assert.That(_transport.SecureSocketOptions, Is.EqualTo(SecureSocketOptions.SslOnConnect));
  }

  /// <summary>
  /// A server doing implicit TLS on a port other than 465 cannot be reached with Required, which
  /// would try to negotiate STARTTLS there.
  /// </summary>
  [Test]
  public void ImplicitTlsUsesSslOnConnectRegardlessOfThePort()
  {
    SmtpAppender appender = CreateAppender();
    appender.TransportSecurity = SmtpTransportSecurity.ImplicitTls;
    appender.Port = 8465;

    Append(appender);

    Assert.That(_transport.SecureSocketOptions, Is.EqualTo(SecureSocketOptions.SslOnConnect));
  }

  /// <summary>
  /// Opportunistic transport security stays available, but only for an operator who asks for it.
  /// </summary>
  [Test]
  public void StartTlsWhenAvailableIsOpportunistic()
  {
    SmtpAppender appender = CreateAppender();
    appender.TransportSecurity = SmtpTransportSecurity.StartTlsWhenAvailable;

    Append(appender);

    Assert.That(_transport.SecureSocketOptions, Is.EqualTo(SecureSocketOptions.StartTlsWhenAvailable));
  }

  /// <summary>
  /// EnableSsl and TransportSecurity are the same setting, so they cannot disagree.
  /// </summary>
  [Test]
  public void EnableSslIsAShorthandForTransportSecurity()
  {
    SmtpAppender appender = CreateAppender();

    Assert.That(appender.TransportSecurity, Is.EqualTo(SmtpTransportSecurity.None));
    Assert.That(appender.EnableSsl, Is.False);

    appender.EnableSsl = true;
    Assert.That(appender.TransportSecurity, Is.EqualTo(SmtpTransportSecurity.Required));

    appender.TransportSecurity = SmtpTransportSecurity.StartTlsWhenAvailable;
    Assert.That(appender.EnableSsl, Is.True);

    appender.EnableSsl = false;
    Assert.That(appender.TransportSecurity, Is.EqualTo(SmtpTransportSecurity.None));
  }

  [Test]
  public void BodyContainsTheRenderedEvent()
  {
    SmtpAppender appender = CreateAppender();

    Append(appender, "something broke");

    Assert.That(_transport.SentMails[0].Body, Does.Contain("something broke"));
  }

  [Test]
  public void BodyIsWrappedInLayoutHeaderAndFooter()
  {
    SmtpAppender appender = CreateAppender(header: "<<HEADER>>", footer: "<<FOOTER>>");

    Append(appender, "the event");

    string body = _transport.SentMails[0].Body;
    Assert.That(body, Does.StartWith("<<HEADER>>"));
    Assert.That(body, Does.Contain("the event"));
    Assert.That(body, Does.EndWith("<<FOOTER>>"));
  }

  [Test]
  public void BodyUsesTheConfiguredEncoding()
  {
    SmtpAppender appender = CreateAppender();
    appender.BodyEncoding = Encoding.UTF8;

    Append(appender, "weißes Schönwetter");

    SentMail mail = _transport.SentMails[0];
    Assert.That(mail.BodyCharset, Is.EqualTo("utf-8"));
    Assert.That(mail.Body, Does.Contain("weißes Schönwetter"));
  }

  [Test]
  public void SubjectIsSet()
  {
    SmtpAppender appender = CreateAppender();
    appender.Subject = "error in production";

    Append(appender);

    Assert.That(_transport.SentMails[0].Subject, Is.EqualTo("error in production"));
  }

  [Test]
  public void SubjectUsesTheConfiguredEncoding()
  {
    SmtpAppender appender = CreateAppender();
    appender.Subject = "Störung im Betrieb";
    appender.SubjectEncoding = Encoding.UTF8;

    Append(appender);

    SentMail mail = _transport.SentMails[0];
    Assert.That(mail.Subject, Is.EqualTo("Störung im Betrieb"));
    // A non-ASCII subject must be transfer-encoded, naming the configured charset.
    Assert.That(mail.RawSubjectHeader.ToLowerInvariant(), Does.Contain("utf-8"));
  }

  [Test]
  public void FromAndToAreSet()
  {
    SmtpAppender appender = CreateAppender();
    appender.From = "logger@example.com";
    appender.To = "ops@example.com";

    Append(appender);

    SentMail mail = _transport.SentMails[0];
    Assert.That(mail.From, Is.EqualTo(new[] { "logger@example.com" }));
    Assert.That(mail.To, Is.EqualTo(new[] { "ops@example.com" }));
  }

  [Test]
  public void CcAndBccAreOmittedWhenNotConfigured()
  {
    SmtpAppender appender = CreateAppender();

    Append(appender);

    SentMail mail = _transport.SentMails[0];
    Assert.That(mail.Cc, Is.Empty);
    Assert.That(mail.Bcc, Is.Empty);
    Assert.That(mail.ReplyTo, Is.Empty);
  }

  [Test]
  public void ReplyToIsSet()
  {
    SmtpAppender appender = CreateAppender();
    appender.ReplyTo = "noreply@example.com";

    Append(appender);

    Assert.That(_transport.SentMails[0].ReplyTo, Is.EqualTo(new[] { "noreply@example.com" }));
  }

  [Test]
  public void CommaDelimitedRecipientsAreAllAdded()
  {
    SmtpAppender appender = CreateAppender();
    appender.To = "a@example.com,b@example.com";
    appender.Cc = "c@example.com, d@example.com";
    appender.Bcc = "e@example.com,f@example.com";

    Append(appender);

    SentMail mail = _transport.SentMails[0];
    Assert.That(mail.To, Is.EqualTo(new[] { "a@example.com", "b@example.com" }));
    Assert.That(mail.Cc, Is.EqualTo(new[] { "c@example.com", "d@example.com" }));
    Assert.That(mail.Bcc, Is.EqualTo(new[] { "e@example.com", "f@example.com" }));
  }

  [Test]
  public void SemicolonDelimitedRecipientsAreAllAdded()
  {
    SmtpAppender appender = CreateAppender();
    appender.To = "a@example.com;b@example.com";
    appender.Bcc = "e@example.com; f@example.com";

    Append(appender);

    SentMail mail = _transport.SentMails[0];
    Assert.That(mail.To, Is.EqualTo(new[] { "a@example.com", "b@example.com" }));
    Assert.That(mail.Bcc, Is.EqualTo(new[] { "e@example.com", "f@example.com" }));
  }

  [Test]
  public void DisplayNamesContainingCommasSurviveParsing()
  {
    SmtpAppender appender = CreateAppender();
    appender.To = "\"Doe, John\" <john@example.com>, jane@example.com";

    Append(appender);

    Assert.That(_transport.SentMails[0].To, Is.EqualTo(new[] { "john@example.com", "jane@example.com" }));
  }

  [Test]
  public void LeadingAndTrailingSeparatorsAreTrimmedFromRecipients()
  {
    SmtpAppender appender = CreateAppender();
    appender.To = ",to@example.com;";
    appender.Cc = ";cc@example.com,";
    appender.Bcc = ",bcc@example.com,";

    Assert.That(appender.To, Is.EqualTo("to@example.com"));
    Assert.That(appender.Cc, Is.EqualTo("cc@example.com"));
    Assert.That(appender.Bcc, Is.EqualTo("bcc@example.com"));
  }

  [TestCase(MailPriority.Low, MessagePriority.NonUrgent)]
  [TestCase(MailPriority.Normal, MessagePriority.Normal)]
  [TestCase(MailPriority.High, MessagePriority.Urgent)]
  public void PriorityIsMappedOntoTheMimePriorityHeader(MailPriority configured, MessagePriority expected)
  {
    SmtpAppender appender = CreateAppender();
    appender.Priority = configured;

    Append(appender);

    Assert.That(_transport.SentMails[0].Priority, Is.EqualTo(expected));
  }

  [Test]
  public void DefaultPriorityIsNormal()
  {
    SmtpAppender appender = CreateAppender();

    Assert.That(appender.Priority, Is.EqualTo(MailPriority.Normal));
  }

  [Test]
  public void TransportIsDisconnectedAndDisposedAfterSending()
  {
    SmtpAppender appender = CreateAppender();

    Append(appender);

    Assert.That(_transport.DisconnectedWithQuit, Is.True);
    Assert.That(_transport.IsConnected, Is.False);
    Assert.That(_transport.IsDisposed, Is.True);
  }

  [Test]
  public void TransportIsDisconnectedAndDisposedWhenSendingFails()
  {
    SmtpAppender appender = CreateAppender();
    _transport.SendException = new InvalidOperationException("relay refused");

    Append(appender);

    Assert.That(_transport.Calls, Does.Contain("Disconnect"));
    Assert.That(_transport.IsDisposed, Is.True);
  }

  [Test]
  public void SendFailureIsReportedToTheErrorHandlerAndNotThrown()
  {
    SmtpAppender appender = CreateAppender();
    _transport.SendException = new InvalidOperationException("relay refused");

    Assert.DoesNotThrow(() => Append(appender));

    Assert.That(_errorHandler.Message, Does.Contain("Error occurred while sending e-mail notification."));
    Assert.That(_errorHandler.Message, Does.Contain("relay refused"));
  }

  [TestCase(nameof(SmtpAppender.SmtpHost))]
  [TestCase(nameof(SmtpAppender.From))]
  [TestCase(nameof(SmtpAppender.To))]
  public void MissingRequiredOptionIsReportedToTheErrorHandler(string option)
  {
    SmtpAppender appender = CreateAppender();
    switch (option)
    {
      case nameof(SmtpAppender.SmtpHost):
        appender.SmtpHost = null;
        break;
      case nameof(SmtpAppender.From):
        appender.From = null;
        break;
      default:
        appender.To = null;
        break;
    }

    Assert.DoesNotThrow(() => Append(appender));

    Assert.That(_transport.SentMails, Is.Empty);
    Assert.That(_errorHandler.Message, Does.Contain(option));
  }

  [Test]
  public void EachSendUsesAFreshTransport()
  {
    List<FakeSmtpTransport> transports = [];
    PatternLayout layout = new() { ConversionPattern = "%m%n" };
    layout.ActivateOptions();
    SmtpAppender appender = new(() =>
    {
      FakeSmtpTransport transport = new();
      transports.Add(transport);
      return transport;
    })
    {
      Layout = layout,
      ErrorHandler = _errorHandler,
      SmtpHost = "mail.example.com",
      From = "from@example.com",
      To = "to@example.com",
      BufferSize = 1,
    };
    appender.ActivateOptions();

    appender.DoAppend(CreateEvent("first"));
    appender.DoAppend(CreateEvent("second"));

    Assert.That(transports, Has.Count.EqualTo(2));
    Assert.That(transports[0].SentMails[0].Body, Does.Contain("first"));
    Assert.That(transports[1].SentMails[0].Body, Does.Contain("second"));
  }

  [Test]
  public void BufferedEventsAreSentInASingleMail()
  {
    SmtpAppender appender = CreateAppender();
    appender.BufferSize = 10;
    appender.ActivateOptions();

    appender.DoAppend(CreateEvent("one"));
    appender.DoAppend(CreateEvent("two"));
    appender.DoAppend(CreateEvent("three"));
    Assert.That(_transport.SentMails, Is.Empty, "the buffer is not full yet");

    appender.Flush(true);

    Assert.That(_transport.SentMails, Has.Count.EqualTo(1));
    string body = _transport.SentMails[0].Body;
    Assert.That(body, Does.Contain("one"));
    Assert.That(body, Does.Contain("two"));
    Assert.That(body, Does.Contain("three"));
  }

  [Test]
  public void RequiresALayout()
  {
    PatternLayout layout = new() { ConversionPattern = "%m%n" };
    layout.ActivateOptions();
    SmtpAppender appender = new(() => _transport)
    {
      ErrorHandler = _errorHandler,
      SmtpHost = "mail.example.com",
      From = "from@example.com",
      To = "to@example.com",
      BufferSize = 1,
    };
    appender.ActivateOptions();

    appender.DoAppend(CreateEvent("no layout"));

    Assert.That(_transport.SentMails, Is.Empty);
    Assert.That(_errorHandler.Message, Is.Not.Empty);
  }

  [Test]
  public void NullTransportFactoryIsRejected()
    => Assert.Throws<ArgumentNullException>(() => new SmtpAppender(null!));

  [Test]
  public void DefaultConstructorUsesTheMailKitTransport()
  {
    // Nothing is sent here; this only pins down that the log4net-configurable
    // parameterless constructor exists and produces a usable appender.
    SmtpAppender appender = new();

    Assert.That(appender.Port, Is.EqualTo(25));
    Assert.That(appender.Authentication, Is.EqualTo(SmtpAppender.SmtpAuthentication.None));
    Assert.That(appender.SubjectEncoding, Is.EqualTo(Encoding.UTF8));
    Assert.That(appender.BodyEncoding, Is.EqualTo(Encoding.UTF8));
    Assert.That(appender.EnableSsl, Is.False);
  }
}
