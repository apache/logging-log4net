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
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using log4net.Appender;
using log4net.Config;
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
  /// <summary>An unbounded send stalls every thread logging through the appender.</summary>
  [Test]
  public void SendTimeoutMillisDefaultsToFifteenSeconds()
    => Assert.That(new SmtpAppender().SendTimeoutMillis, Is.EqualTo(15_000));

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
