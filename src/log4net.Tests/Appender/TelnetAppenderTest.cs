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
  /// Simple Test für the <see cref="TelnetAppender"/>
  /// </summary>
  /// <remarks>
  /// https://github.com/apache/logging-log4net/issues/194
  /// https://stackoverflow.com/questions/79053363/log4net-telnetappender-doesnt-work-after-migrate-to-log4net-3
  /// </remarks>
  [Test]
  public void TelnetTest()
  {
    List<string> received = [];

    XmlDocument log4NetConfig = new();
    int port = 9090;
    log4NetConfig.LoadXml($"""
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
    using (SimpleTelnetClient telnetClient = new(Received, port))
    {
      telnetClient.Run();
      WaitForReceived(1); // wait for welcome message
      ILogger logger = repository.GetLogger("Telnet");
      logger.Log(typeof(TelnetAppenderTest), Level.Info, logId, null);
      WaitForReceived(2); // wait for log message
    }
    repository.Shutdown();
    Assert.That(received, Has.Count.EqualTo(2));
    Assert.That(received[1], Does.Contain(logId));

    void Received(string message) => received.Add(message);

    void WaitForReceived(int count)
    {
      while (received.Count < count)
      {
        Thread.Sleep(10);
      }
    }
  }
}