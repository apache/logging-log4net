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
using System.Text;
using log4net.Appender;
using log4net.Appender.Internal;
using log4net.Core;
using log4net.Layout;
using log4net.Tests.Appender.Internal;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Tests for <see cref="RemoteSyslogAppender"/>
/// </summary>
[TestFixture]
public sealed class RemoteSyslogAppenderTest
{
  private sealed class RemoteAppender : RemoteSyslogAppender
  {
    /// <summary>
    /// Mock
    /// </summary>
    internal UdpMock Mock { get; } = new();

    /// <inheritdoc/>
    protected override IUdpConnection CreateUdpConnection() => Mock;

    /// <summary>The socket the base class would have opened.</summary>
    internal System.Net.Sockets.UdpClient? InheritedClient => Client;
  }

  /// <summary>Collects reported errors instead of letting them reach the console.</summary>
  private sealed class RecordingErrorHandler : IErrorHandler
  {
    /// <summary>Reported messages.</summary>
    internal List<string> Messages { get; } = [];

    /// <inheritdoc/>
    public void Error(string message, Exception? e, ErrorCode errorCode) => Messages.Add(message);

    /// <inheritdoc/>
    public void Error(string message, Exception e) => Messages.Add(message);

    /// <inheritdoc/>
    public void Error(string message) => Messages.Add(message);
  }

  private const int FlushTimeoutMillis = 30_000;

  /// <summary>Bounded, so an unreachable server cannot grow the queue without limit.</summary>
  [Test]
  public void SendQueueSizeDefaultsTo500()
    => Assert.That(new RemoteSyslogAppender().SendQueueSize, Is.EqualTo(500));

  /// <summary>The longest a logging call waits when the queue is full.</summary>
  [Test]
  public void EnqueueTimeoutMillisDefaultsTo5000()
    => Assert.That(new RemoteSyslogAppender().EnqueueTimeoutMillis, Is.EqualTo(5_000));

  /// <summary>A queue with no room for anything cannot work.</summary>
  [TestCase(0)]
  [TestCase(-1)]
  public void SendQueueSizeRejectsValuesThatAreNotPositive(int value)
    => Assert.That(() => new RemoteSyslogAppender().SendQueueSize = value,
      Throws.TypeOf<ArgumentOutOfRangeException>());

  /// <summary>0 is allowed and never waits, a negative wait is meaningless.</summary>
  [Test]
  public void EnqueueTimeoutMillisRejectsANegativeValue()
    => Assert.That(() => new RemoteSyslogAppender().EnqueueTimeoutMillis = -1,
      Throws.TypeOf<ArgumentOutOfRangeException>());

  /// <summary>
  /// This appender sends through the connection its pump owns, so the inherited one would be a
  /// second socket, holding a second binding of LocalPort, that nothing ever uses.
  /// </summary>
  [Test]
  public void TheInheritedSocketIsNotOpened()
  {
    RemoteAppender appender = new()
    {
      RemoteAddress = new System.Net.IPAddress([127, 0, 0, 1]),
      Layout = new PatternLayout("%message")
    };
    appender.ActivateOptions();
    try
    {
      Assert.That(appender.InheritedClient, Is.Null);
    }
    finally
    {
      appender.Close();
    }
  }

  /// <summary>
  /// Connecting happens outside the send loop, so a failure there used to fault the pump with
  /// nobody watching and every later event queued behind a sender that was gone.
  /// </summary>
  [Test]
  public void AConnectFailureIsReported()
  {
    RecordingErrorHandler errorHandler = new();
    RemoteAppender appender = new()
    {
      RemoteAddress = new System.Net.IPAddress([127, 0, 0, 1]),
      Layout = new PatternLayout("%message"),
      ErrorHandler = errorHandler
    };
    appender.Mock.ConnectException = new InvalidOperationException("simulated connect failure");

    appender.ActivateOptions();
    appender.Close();

    Assert.That(errorHandler.Messages, Has.Some.Contains("Unable to connect to remote syslog"));
  }

  /// <summary>
  /// Simple Test for the <see cref="RemoteSyslogAppender"/>
  /// </summary>
  /// <remarks>
  /// https://github.com/apache/logging-log4net/issues/255
  /// </remarks>
  [Test]
  public void RemoteSyslogTest()
  {
    List<byte[]> sentBytes = ExecuteAppend("Test message");
    const string expectedData = @"<14>TestDomain: INFO  - Test message";
    Assert.That(sentBytes, Has.Count.EqualTo(1));
    Assert.That(Encoding.ASCII.GetString(sentBytes[0]), Is.EqualTo(expectedData));
  }
  
  /// <summary>
  /// Test for the <see cref="RemoteSyslogAppender.NewLineHandling"/>
  /// with <see cref="RemoteSyslogAppender.SyslogNewLineHandling.Escape"/>
  /// </summary>
  /// <remarks>
  /// https://github.com/apache/logging-log4net/issues/274
  /// </remarks>
  [Test]
  public void RemoteSyslogNewLineHandlingEscapeTest()
  {
    List<byte[]> sentBytes = ExecuteAppend("Test\r\nmessage");
    // ReSharper disable once StringLiteralTypo
    const string expectedData = @"<14>TestDomain: INFO  - Test\r\nmessage";
    Assert.That(sentBytes, Has.Count.EqualTo(1));
    Assert.That(Encoding.ASCII.GetString(sentBytes[0]), Is.EqualTo(expectedData));
  }
  
  /// <summary>
  /// Test for the <see cref="RemoteSyslogAppender.NewLineHandling"/>
  /// with <see cref="RemoteSyslogAppender.SyslogNewLineHandling.Keep"/>
  /// </summary>
  /// <remarks>
  /// https://github.com/apache/logging-log4net/issues/274
  /// </remarks>
  [Test]
  public void RemoteSyslogNewLineHandlingKeepTest()
  {
    List<byte[]> sentBytes = ExecuteAppend("Test\r\nmessage",
      RemoteSyslogAppender.SyslogNewLineHandling.Keep);
    // ReSharper disable once StringLiteralTypo
    const string expectedData = "<14>TestDomain: INFO  - Test\r\nmessage";
    Assert.That(sentBytes, Has.Count.EqualTo(1));
    Assert.That(Encoding.ASCII.GetString(sentBytes[0]), Is.EqualTo(expectedData));
  }
  
  /// <summary>
  /// Test for the <see cref="RemoteSyslogAppender.NewLineHandling"/>
  /// with <see cref="RemoteSyslogAppender.SyslogNewLineHandling.Split"/>
  /// </summary>
  /// <remarks>
  /// https://github.com/apache/logging-log4net/issues/274
  /// </remarks>
  [Test]
  public void RemoteSyslogNewLineHandlingSplitTest()
  {
    List<byte[]> sentBytes = ExecuteAppend("Test\r\nmessage",
      RemoteSyslogAppender.SyslogNewLineHandling.Split);
    // ReSharper disable once StringLiteralTypo
    Assert.That(sentBytes, Has.Count.EqualTo(2));
    const string expectedData0 = "<14>TestDomain: INFO  - Test";
    Assert.That(Encoding.ASCII.GetString(sentBytes[0]), Is.EqualTo(expectedData0));
    const string expectedData1 = "<14>TestDomain: message";
    Assert.That(Encoding.ASCII.GetString(sentBytes[1]), Is.EqualTo(expectedData1));
  }
  
  /// <summary>
  /// The Identity becomes the TAG of the record. A control character in it would end the record,
  /// so that the rest is read as a second record with its own facility and severity.
  /// </summary>
  [Test]
  public void IdentityCannotSplitTheRecord()
  {
    List<byte[]> sentBytes = [];
    // The malformed Identity is reported, which is expected here and should not clutter the output.
    LogLog.ExecuteWithoutEmittingInternalMessages(
      () => sentBytes = ExecuteAppend("Test message", identity: "app\r\n<34>sshd"));

    Assert.That(sentBytes, Has.Count.EqualTo(1));
    const string expectedData = "<14>app<34>sshd: INFO  - Test message";
    Assert.That(Encoding.ASCII.GetString(sentBytes[0]), Is.EqualTo(expectedData));
  }

  /// <summary>
  /// Removing the characters is not enough on its own: a malformed structural identifier is a
  /// configuration error and has to be reported rather than quietly repaired.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void IdentityWithControlCharactersIsReported()
  {
    List<LogLog> messages = [];
    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      using LogLog.LogReceivedAdapter _ = new(messages);
      ExecuteAppend("Test message", identity: "app\r\n<34>sshd");
    });

    Assert.That(messages.ConvertAll(m => m.Message),
      Has.Some.Contains("Identity of appender"));
  }

  /// <summary>
  /// An Identity without control characters has to reach the record untouched, including a space,
  /// which the application friendly name used by default may well contain.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void IdentityWithoutControlCharactersIsUnchangedAndNotReported()
  {
    List<LogLog> messages = [];
    List<byte[]> sentBytes = [];
    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      using LogLog.LogReceivedAdapter _ = new(messages);
      sentBytes = ExecuteAppend("Test message", identity: "My App");
    });

    Assert.That(sentBytes, Has.Count.EqualTo(1));
    const string expectedData = "<14>My App: INFO  - Test message";
    Assert.That(Encoding.ASCII.GetString(sentBytes[0]), Is.EqualTo(expectedData));
    Assert.That(messages.ConvertAll(m => m.Message), Has.None.Contains("Identity of appender"));
  }

  private static List<byte[]> ExecuteAppend(string message,
    RemoteSyslogAppender.SyslogNewLineHandling newLineHandling = default,
    string? identity = null)
  {
    System.Net.IPAddress ipAddress = new([127, 0, 0, 1]);
    RemoteAppender appender = new()
    {
      RemoteAddress = ipAddress,
      Layout = new PatternLayout("%-5level - %message"),
      NewLineHandling = newLineHandling,
      Identity = identity is null ? null : new PatternLayout(identity)
    };
    appender.ActivateOptions();
    LoggingEvent loggingEvent = new(new()
    {
      Level = Level.Info,
      Message = message,
      LoggerName = "TestLogger",
      Domain = "TestDomain",
    });
    appender.DoAppend(loggingEvent);
    Assert.That(appender.Flush(FlushTimeoutMillis), Is.True);
    appender.Close();
    Assert.That(appender.Mock.ConnectedTo, Is.EqualTo((0, ipAddress, 514)));
    Assert.That(appender.Mock.WasDisposed, Is.True);
    return appender.Mock.Sent.Select(item => item.Datagram).ToList();
  }
}