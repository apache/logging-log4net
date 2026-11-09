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
    // Sending is asynchronous.
    appender.Flush(FlushTimeoutMillis);
  }

  private const int FlushTimeoutMillis = 30_000;

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

  /// <summary>An operator who configures nothing must not get a cleartext session.</summary>
  [Test]
  public void TransportSecurityDefaultsToRequired()
    => Assert.That(CreateAppender().TransportSecurity, Is.EqualTo(SmtpTransportSecurity.Required));

  /// <summary>The shorthand follows the default of the property it stands for.</summary>
  [Test]
  public void EnableSslDefaultsToTrue()
    => Assert.That(CreateAppender().EnableSsl, Is.True);

  /// <summary>
  /// The default port is 25, so the default must mean mandatory STARTTLS, not the downgradable Auto.
  /// </summary>
  [Test]
  public void TheDefaultConnectionRequiresStartTls()
  {
    SmtpAppender appender = CreateAppender();

    Append(appender);

    Assert.That(_transport.SecureSocketOptions, Is.EqualTo(SecureSocketOptions.StartTls));
  }

  [Test]
  public void EnableSslOffConnectsWithoutTransportSecurity()
  {
    SmtpAppender appender = CreateAppender();
    appender.EnableSsl = false;

    Append(appender);

    Assert.That(_transport.SecureSocketOptions, Is.EqualTo(SecureSocketOptions.None));
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

    appender.EnableSsl = false;
    Assert.That(appender.TransportSecurity, Is.EqualTo(SmtpTransportSecurity.None));

    appender.EnableSsl = true;
    Assert.That(appender.TransportSecurity, Is.EqualTo(SmtpTransportSecurity.Required));

    appender.TransportSecurity = SmtpTransportSecurity.StartTlsWhenAvailable;
    Assert.That(appender.EnableSsl, Is.True);

    appender.TransportSecurity = SmtpTransportSecurity.None;
    Assert.That(appender.EnableSsl, Is.False);
  }

  /// <summary>A best fit fallback turns U+FF1C into <c>&lt;</c> after the layout. The charset stays.</summary>
  [Test]
  public void BodyEscapesWhatTheEncodingCannotCarry()
  {
    SmtpAppender appender = CreateAppender();
    appender.BodyEncoding = Encoding.GetEncoding("iso-8859-1");

    Append(appender, "a\uff1cb\u00e4");

    Assert.That(_transport.SentMails[0].Body, Does.StartWith("a\\uff1cb\u00e4"));
    Assert.That(_transport.SentMails[0].BodyCharset, Is.EqualTo("iso-8859-1"));
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

    Assert.That(_errorHandler.Message, Does.Contain("Failed to send a logging event."));
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
    appender.Flush(FlushTimeoutMillis);

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

    Assert.That(appender.Flush(FlushTimeoutMillis), Is.True);

    Assert.That(_transport.SentMails, Has.Count.EqualTo(1));
    string body = _transport.SentMails[0].Body;
    Assert.That(body, Does.Contain("one"));
    Assert.That(body, Does.Contain("two"));
    Assert.That(body, Does.Contain("three"));
  }

  /// <summary>
  /// SMTP allows a CR only before LF. A lone CR from content goes out quoted-printable as <c>=0D</c>, so a
  /// strict relay accepts the mail, and comes back as CR.
  /// </summary>
  [TestCase("login failed for user mallory\rx", TestName = "ALoneCarriageReturn")]
  [TestCase("a line\r.and a dot after it", TestName = "ADotAfterALoneCarriageReturn")]
  public void ALoneCarriageReturnDoesNotReachTheWireBare(string message)
  {
    SmtpAppender appender = CreateAppender();

    Append(appender, message);

    Assert.That(_transport.SentMails, Has.Count.EqualTo(1));
    Assert.That(_transport.SentMails[0].HasBareCarriageReturn(), Is.False);
    Assert.That(_transport.SentMails[0].BodyTransferEncoding, Is.EqualTo(ContentEncoding.QuotedPrintable));
    Assert.That(_transport.SentMails[0].Body, Contains.Substring(message).Using(StringComparison.Ordinal));
  }

  /// <summary>
  /// In UTF-16 the bytes of CR and LF occur inside other characters, which quoted-printable would treat as
  /// line breaks: such a body goes out base64.
  /// </summary>
  [TestCase("utf-16", TestName = "Utf16")]
  [TestCase("utf-32", TestName = "Utf32")]
  [TestCase("utf-32BE", TestName = "Utf32BigEndian")]
  [TestCase("unicodeFFFE", TestName = "Utf16BigEndian")]
  public void AUtf16OrUtf32BodyGoesOutBase64(string encoding)
  {
    SmtpAppender appender = CreateAppender();
    appender.BodyEncoding = Encoding.GetEncoding(encoding);

    Append(appender, "a\rb \u0a41");

    Assert.That(_transport.SentMails[0].HasBareCarriageReturn(), Is.False);
    Assert.That(_transport.SentMails[0].BodyTransferEncoding, Is.EqualTo(ContentEncoding.Base64));
    Assert.That(_transport.SentMails[0].Body, Contains.Substring("a\rb \u0a41").Using(StringComparison.Ordinal));
  }

  /// <summary>1 MiB stays under the 10 MB limit of common servers even when quoted-printable triples it.</summary>
  [Test]
  public void MaxBodySizeDefaultsTo1MiB()
    => Assert.That(CreateAppender().MaxBodySize, Is.EqualTo(1048576));

  /// <summary>Below 4096 header and footer would crowd out the records.</summary>
  [Test]
  public void MaxBodySizeRejectsValuesBelow4096()
    => Assert.That(() => CreateAppender().MaxBodySize = 4095, Throws.TypeOf<ArgumentOutOfRangeException>());

  /// <summary>
  /// A server refuses an oversize mail as a whole, so a batch over the budget goes out as several
  /// mails, each with header and footer.
  /// </summary>
  [Test]
  public void ABatchOverTheBudgetIsSplitIntoSeveralMails()
  {
    SmtpAppender appender = CreateBatchAppender();

    SendBatch(appender, new string('a', 1500), new string('b', 1500), new string('c', 1500), new string('d', 1500));

    Assert.That(_transport.SentMails, Has.Count.EqualTo(2));
    Assert.That(Lf(_transport.SentMails[0].Body), Is.EqualTo(Body(new string('a', 1500), new string('b', 1500))));
    Assert.That(Lf(_transport.SentMails[1].Body), Is.EqualTo(Body(new string('c', 1500), new string('d', 1500))));
    Assert.That(_errorHandler.Message, Is.Empty);
  }

  /// <summary>A record that does not fit even alone is cut and reported.</summary>
  [Test]
  public void ARecordOverTheBudgetIsCutAndReported()
  {
    SmtpAppender appender = CreateBatchAppender();

    SendBatch(appender, new string('a', 5000));

    // 4096 bytes less header, footer and the 14 byte marker.
    Assert.That(_transport.SentMails, Has.Count.EqualTo(1));
    Assert.That(Lf(_transport.SentMails[0].Body), Is.EqualTo("H\n" + new string('a', 4078) + "...[truncated]F\n"));
    Assert.That(_errorHandler.Message, Does.Contain("Truncated 1 logging event"));
  }

  /// <summary>The budget counts bytes in the body's own encoding, not in UTF-8.</summary>
  [Test]
  public void TheBudgetCountsInTheBodyEncoding()
  {
    SmtpAppender appender = CreateBatchAppender();
    appender.BodyEncoding = Encoding.GetEncoding("iso-8859-1");

    // 1501 bytes per record in Latin-1, 3001 in UTF-8: two fit one mail only in Latin-1.
    SendBatch(appender, new string('\u00e4', 1500), new string('\u00e4', 1500));

    Assert.That(_transport.SentMails, Has.Count.EqualTo(1));
  }

  /// <summary>Each mail is sent on its own: one refused part does not cost the rest of the batch.</summary>
  [Test]
  public void ARefusedMailDoesNotCostTheRestOfTheBatch()
  {
    SmtpAppender appender = CreateBatchAppender();
    _transport.SendsToRefuse = 1;

    SendBatch(appender, new string('a', 1500), new string('b', 1500), new string('c', 1500));

    Assert.That(_transport.SentMails, Has.Count.EqualTo(1));
    Assert.That(Lf(_transport.SentMails[0].Body), Is.EqualTo(Body(new string('c', 1500))));
  }

  /// <summary>
  /// Header and footer that leave no room for record text are reported at activation, and every
  /// record then goes out as the marker alone rather than losing the batch.
  /// </summary>
  [Test]
  public void HeaderAndFooterOverTheBudgetAreReportedAndTheMarkerIsSent()
  {
    SmtpAppender appender = CreateAppender(header: new string('h', 4090), footer: "F\n");
    appender.BufferSize = 10;
    appender.MaxBodySize = 4096;

    // 4 bytes are left for records: too few for this one and its marker.
    SendBatch(appender, new string('x', 10));

    Assert.That(_errorHandler.Message, Does.Contain("no room"));
    Assert.That(_transport.SentMails, Has.Count.EqualTo(1));
    Assert.That(Lf(_transport.SentMails[0].Body), Is.EqualTo(new string('h', 4090) + "...[truncated]F\n"));
  }

  private SmtpAppender CreateBatchAppender()
  {
    SmtpAppender appender = CreateAppender(header: "H\n", footer: "F\n");
    appender.BufferSize = 10;
    appender.MaxBodySize = 4096;
    return appender;
  }

  private static void SendBatch(SmtpAppender appender, params string[] messages)
  {
    appender.ActivateOptions();
    foreach (string message in messages)
    {
      appender.DoAppend(CreateEvent(message));
    }
    Assert.That(appender.Flush(FlushTimeoutMillis), Is.True);
  }

  /// <summary>MimeKit and <c>%n</c> write <c>\r\n</c> on Windows; the tests compare with <c>\n</c>.</summary>
  private static string Lf(string body) => body.Replace("\r\n", "\n", StringComparison.Ordinal);

  private static string Body(params string[] messages) => "H\n" + string.Join("\n", messages) + "\n" + "F\n";

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
    appender.Flush(FlushTimeoutMillis);

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
    Assert.That(appender.EnableSsl, Is.True);
  }

  /// <summary>An unbounded send stalls every thread logging through the appender.</summary>
  [Test]
  public void SendTimeoutMillisDefaultsToFifteenSeconds()
    => Assert.That(CreateAppender().SendTimeoutMillis, Is.EqualTo(15_000));

  /// <summary>Neither 0 nor a negative value is a usable deadline.</summary>
  [TestCase(0)]
  [TestCase(-1)]
  public void SendTimeoutMillisRejectsValuesThatAreNotPositive(int value)
    => Assert.That(() => CreateAppender().SendTimeoutMillis = value,
      Throws.TypeOf<ArgumentOutOfRangeException>());

  /// <summary>MailKit needs it too, for the reads and writes between the cancellation checks.</summary>
  [Test]
  public void TheTimeoutReachesTheTransport()
  {
    const int sendTimeoutMillis = 1_234;
    SmtpAppender appender = CreateAppender();
    appender.SendTimeoutMillis = sendTimeoutMillis;

    Append(appender);

    Assert.That(_transport.Timeout, Is.EqualTo(sendTimeoutMillis));
  }

  /// <summary>
  /// Every single operation stays inside the timeout here, their sum does not. A per operation
  /// timeout would let this run on; the deadline stops it.
  /// </summary>
  [Test]
  public void TheDeadlineCoversTheWholeSendAndNotOneOperation()
  {
    // Under the timeout per call, over it in sum.
    const int sendTimeoutMillis = 300;
    const int delayMillisPerCall = 200;

    SmtpAppender appender = CreateAppender();
    appender.SendTimeoutMillis = sendTimeoutMillis;
    _transport.DelayMillisPerCall = delayMillisPerCall;

    Append(appender);

    Assert.That(_transport.SentMails, Is.Empty);
    Assert.That(_errorHandler.Message, Does.Contain("Failed to send a logging event."));
    Assert.That(_transport.CancelledCalls, Is.Not.Empty, "the deadline never stopped the send");
  }

  /// <summary>Bounded, so an unreachable server cannot grow the queue without limit.</summary>
  [Test]
  public void SendQueueSizeDefaultsTo500()
    => Assert.That(CreateAppender().SendQueueSize, Is.EqualTo(500));

  /// <summary>The longest a logging call waits when the queue is full.</summary>
  [Test]
  public void EnqueueTimeoutMillisDefaultsTo5000()
    => Assert.That(CreateAppender().EnqueueTimeoutMillis, Is.EqualTo(5_000));

  /// <summary>A queue with no room for anything cannot work.</summary>
  [TestCase(0)]
  [TestCase(-1)]
  public void SendQueueSizeRejectsValuesThatAreNotPositive(int value)
    => Assert.That(() => CreateAppender().SendQueueSize = value,
      Throws.TypeOf<ArgumentOutOfRangeException>());

  /// <summary>0 is allowed and never waits, a negative wait is meaningless.</summary>
  [Test]
  public void EnqueueTimeoutMillisRejectsANegativeValue()
    => Assert.That(() => CreateAppender().EnqueueTimeoutMillis = -1,
      Throws.TypeOf<ArgumentOutOfRangeException>());
}
