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
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender.Internal;
using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Appender;

#pragma warning disable CS0618 // obsolete, but still shipped

/// <summary>
/// Tests for <see cref="SmtpAppender"/>.
/// </summary>
[TestFixture]
public class SmtpAppenderTest
{
  /// <summary>The obsolete appender splits an oversize batch as the Ext.Mail one does.</summary>
  [Test]
  public void ABatchOverTheBudgetIsSplitIntoSeveralMails()
  {
    RecordingSmtpAppender appender = CreateRecordingAppender();

    SendBatch(appender, new string('a', 1500), new string('b', 1500), new string('c', 1500), new string('d', 1500));

    Assert.That(appender.Bodies, Is.EqualTo(new[]
    {
      "H\n" + new string('a', 1500) + "\n" + new string('b', 1500) + "\nF\n",
      "H\n" + new string('c', 1500) + "\n" + new string('d', 1500) + "\nF\n",
    }));
  }

  /// <summary>It sends synchronously, so each mail needs its own catch: one refused part does not cost the rest.</summary>
  [Test]
  public void ARefusedMailDoesNotCostTheRestOfTheBatch()
  {
    RecordingSmtpAppender appender = CreateRecordingAppender();
    appender.SendsToRefuse = 1;

    SendBatch(appender, new string('a', 1500), new string('b', 1500), new string('c', 1500));

    Assert.That(appender.Bodies, Is.EqualTo(new[] { "H\n" + new string('c', 1500) + "\nF\n" }));
  }

  /// <summary>A record that does not fit even alone is cut and reported.</summary>
  [Test]
  public void ARecordOverTheBudgetIsCutAndReported()
  {
    RecordingSmtpAppender appender = CreateRecordingAppender();
    RecordingErrorHandler errorHandler = new();
    appender.ErrorHandler = errorHandler;

    SendBatch(appender, new string('a', 5000));

    Assert.That(appender.Bodies, Is.EqualTo(new[] { "H\n" + new string('a', 4078) + "...[truncated]F\n" }));
    Assert.That(errorHandler.Messages, Has.Count.EqualTo(1));
  }

  /// <summary>Header and footer that leave no room for record text are reported at activation.</summary>
  [Test]
  public void HeaderAndFooterOverTheBudgetAreReported()
  {
    PatternLayout layout = new() { ConversionPattern = "%m%n", Header = new string('h', 4090), Footer = "F\n" };
    layout.ActivateOptions();
    RecordingErrorHandler errorHandler = new();
    RecordingSmtpAppender appender = new() { Layout = layout, MaxBodySize = 4096, ErrorHandler = errorHandler };

    appender.ActivateOptions();

    Assert.That(errorHandler.Messages, Has.Count.EqualTo(1));
  }

  /// <summary>1 MiB, as for the Ext.Mail appender.</summary>
  [Test]
  public void MaxBodySizeDefaultsTo1MiB()
    => Assert.That(new SmtpAppender().MaxBodySize, Is.EqualTo(1048576));

  private static RecordingSmtpAppender CreateRecordingAppender()
  {
    // A literal \n rather than %n, so that the bodies are the same on every platform.
    PatternLayout layout = new() { ConversionPattern = "%m\n", Header = "H\n", Footer = "F\n" };
    layout.ActivateOptions();
    return new() { Layout = layout, BufferSize = 10, MaxBodySize = 4096, ErrorHandler = new RecordingErrorHandler() };
  }

  private static void SendBatch(RecordingSmtpAppender appender, params string[] messages)
  {
    appender.ActivateOptions();
    foreach (string message in messages)
    {
      appender.DoAppend(new LoggingEvent(typeof(SmtpAppenderTest), null, "test", Level.Error, message, null));
    }
    appender.Flush(1000);
  }

  /// <summary>Records the bodies instead of sending them, and refuses the first ones on request.</summary>
  private sealed class RecordingSmtpAppender : SmtpAppender
  {
    internal List<string> Bodies { get; } = [];

    internal int SendsToRefuse { get; set; }

    /// <inheritdoc/>
    protected override void SendEmail(string messageBody)
    {
      if (SendsToRefuse > 0)
      {
        SendsToRefuse--;
        throw new InvalidOperationException("552 5.3.4 Message size exceeds fixed limit");
      }
      Bodies.Add(messageBody);
    }
  }

  /// <summary>An unbounded send stalls every thread logging through the appender.</summary>
  [Test]
  public void SendTimeoutMillisDefaultsToFifteenSeconds()
    => Assert.That(new SmtpAppender().SendTimeoutMillis, Is.EqualTo(15_000));

  /// <summary>
  /// A best fit fallback turns U+FF1C into <c>&lt;</c> after the layout. The configured encoding
  /// object stays on the message: a clone would change the transfer encoding it picks.
  /// </summary>
  [Test]
  public void BodyEscapesWhatTheEncodingCannotCarry()
  {
    Encoding latin1 = Encoding.GetEncoding("iso-8859-1");
    SmtpAppender appender = new() { From = "from@example.com", To = "to@example.com", BodyEncoding = latin1 };

    using MailMessage message = new();
    appender.Invoke<object?>("FillMailMessage", [message, "a\uff1cb\u00e4\ud800"]);

    Assert.That(message.Body, Is.EqualTo("a\\uff1cb\u00e4\\ud800"));
    Assert.That(message.BodyEncoding, Is.SameAs(latin1));
  }

  /// <summary><see cref="System.Net.Mail.SmtpClient"/> treats 0 as "do not wait".</summary>
  [TestCase(0)]
  [TestCase(-1)]
  public void SendTimeoutMillisRejectsValuesThatAreNotPositive(int value)
    => Assert.That(() => new SmtpAppender().SendTimeoutMillis = value,
      Throws.TypeOf<ArgumentOutOfRangeException>());

  /// <summary>A silent server must not hold the logging call for the 100 second default.</summary>
  [Test]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
    Justification = "TcpListener is not IDisposable on net462. Stop() in the finally.")]
  public void AnUnresponsiveServerDoesNotStallTheLoggingCall()
  {
    const int sendTimeoutMillis = 1_000;
    const int generousBoundMillis = 30_000;

    using ManualResetEventSlim finished = new(false);
    TcpListener listener = new(IPAddress.Loopback, 0);
    listener.Start();
    int port = ((IPEndPoint)listener.LocalEndpoint).Port;

    // Accepts, then stays silent: the greeting never comes.
    Task stub = Task.Run(() =>
    {
      try
      {
        using TcpClient client = listener.AcceptTcpClient();
        finished.Wait(generousBoundMillis);
      }
      catch (Exception e) when (!e.IsFatal())
      {
        // Stopped while the accept was pending.
      }
    });

    RecordingErrorHandler errorHandler = new();
    try
    {
      SmtpAppender appender = new()
      {
        SmtpHost = "127.0.0.1",
        Port = port,
        From = "from@example.com",
        To = "to@example.com",
        Subject = "test",
        BufferSize = 1,
        Layout = new PatternLayout("%message"),
        SendTimeoutMillis = sendTimeoutMillis,
        ErrorHandler = errorHandler
      };
      appender.ActivateOptions();

      ILoggerRepository repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
      BasicConfigurator.Configure(repository, appender);
      ILog log = LogManager.GetLogger(repository.Name, nameof(AnUnresponsiveServerDoesNotStallTheLoggingCall));

      Stopwatch stopwatch = Stopwatch.StartNew();
      log.Error("Message");
      stopwatch.Stop();

      Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(generousBoundMillis));
      Assert.That(errorHandler.Messages, Is.Not.Empty);
    }
    finally
    {
      finished.Set();
      listener.Stop();
      stub.Wait(generousBoundMillis);
    }
  }

}
