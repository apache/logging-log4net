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
using System.Text;
using System.Threading;
using System.Xml;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using log4net.Tests.Appender.Internal;
using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Tests for <see cref="TelnetAppender"/>
/// </summary>
[TestFixture]
public sealed class TelnetAppenderTest
{
  /// <summary>
  /// Simple Test for the <see cref="TelnetAppender"/>
  /// </summary>
  /// <remarks>
  /// https://github.com/apache/logging-log4net/issues/194
  /// https://stackoverflow.com/questions/79053363/log4net-telnetappender-doesnt-work-after-migrate-to-log4net-3
  /// </remarks>
  /// <summary>
  /// Maximum time to wait for a message to arrive at the client.
  /// </summary>
  private static readonly TimeSpan _receiveTimeout = TimeSpan.FromSeconds(30);

  private const string WelcomeMessage = "TelnetAppender";

  [Test]
  public void TelnetTest()
  {
    // The received data is a TCP byte stream - the server writes are not necessarily
    // mapped 1:1 to the client reads, so the test asserts on the accumulated text
    // instead of counting the individual reads.
    StringBuilder received = new();
    object receivedSyncRoot = new();

    XmlDocument log4NetConfig = new();
    int port = FindFreeTcpPort();
    log4NetConfig.LoadXml(
      $"""
      <log4net>
        <appender name="TelnetAppender" type="log4net.Appender.TelnetAppender">
          <port value="{port}" />
          <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%date %-5level - %message%newline" />
          </layout>
        </appender>
        <root>
          <level value="INFO"/>
          <appender-ref ref="TelnetAppender"/>
        </root>
      </log4net>
      """);
    string logId = Guid.NewGuid().ToString();
    ILoggerRepository repository = LogManager.CreateRepository(logId);
    XmlConfigurator.Configure(repository, log4NetConfig["log4net"]!);
    try
    {
      using (SimpleTelnetClient telnetClient = new(Received, port))
      {
        telnetClient.Run(TestContext.Out.WriteLine);
        WaitForReceived("welcome message", WelcomeMessage);
        ILogger logger = repository.GetLogger("Telnet");
        logger.Log(typeof(TelnetAppenderTest), Level.Info, logId, null);
        WaitForReceived("log message", logId);
      }
    }
    finally
    {
      repository.Shutdown();
    }
    Assert.That(ReceivedText(), Does.StartWith(WelcomeMessage).And.Contain(logId));

    void Received(string message)
    {
      lock (receivedSyncRoot)
      {
        received.Append(message);
      }
    }

    string ReceivedText()
    {
      lock (receivedSyncRoot)
      {
        return received.ToString();
      }
    }

    void WaitForReceived(string what, string expected)
    {
      Stopwatch stopwatch = Stopwatch.StartNew();
      while (ReceivedText().IndexOf(expected, StringComparison.Ordinal) < 0)
      {
        if (stopwatch.Elapsed > _receiveTimeout)
        {
          Assert.Fail($"Timeout waiting for {what} - received so far: '{ReceivedText()}'");
        }
        Thread.Sleep(20);
      }
    }
  }

  /// <summary>
  /// Writes to a client block while the appender lock is held, so the send timeout has to be
  /// finite by default - otherwise a client that stops reading suspends all logging.
  /// </summary>
  [Test]
  public void SendTimeoutMillisDefaultsToAFiniteValue()
    => Assert.That(new TelnetAppender().SendTimeoutMillis, Is.EqualTo(5000));

  /// <summary>
  /// 0 is the documented opt-out that restores blocking indefinitely; a negative timeout has no
  /// meaning for <see cref="Socket.SendTimeout"/> and is rejected instead of being silently
  /// reinterpreted.
  /// </summary>
  [Test]
  public void SendTimeoutMillisRejectsNegativeValuesButAllowsZero()
  {
    TelnetAppender appender = new();

    Assert.That(() => appender.SendTimeoutMillis = -1, Throws.TypeOf<ArgumentOutOfRangeException>());

    appender.SendTimeoutMillis = 0;
    Assert.That(appender.SendTimeoutMillis, Is.EqualTo(0));

    appender.SendTimeoutMillis = 250;
    Assert.That(appender.SendTimeoutMillis, Is.EqualTo(250));
  }

  /// <summary>
  /// Asks the OS for a currently unused TCP port - a fixed port would collide with
  /// other tests or processes on the build machine.
  /// </summary>
  private static int FindFreeTcpPort()
  {
    using Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    socket.Bind(new IPEndPoint(IPAddress.Any, 0));
    if (socket.LocalEndPoint is IPEndPoint endPoint)
    {
      return endPoint.Port;
    }
    throw new InvalidOperationException("Could not determine a free TCP port");
  }
}