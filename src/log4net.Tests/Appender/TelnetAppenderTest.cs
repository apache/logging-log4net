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
using System.Threading.Tasks;
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
public class TelnetAppenderTest
{
  /// <summary>
  /// https://stackoverflow.com/questions/79053363/log4net-telnetappender-doesnt-work-after-migrate-to-log4net-3
  /// </summary>
  [Test]
  public void TelnetTest()
  {
    List<string> received = [];
    
    XmlDocument log4netConfig = new();
    log4netConfig.LoadXml("""
    <log4net>
      <appender name="TelnetAppender" type="log4net.Appender.TelnetAppender">
        <port value="9090" />
        <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%date{HH:mm:ss.fff} %-5level - %message%newline" />
        </layout>
      </appender>
      <root>
        <level value="INFO"/>
        <appender-ref ref="TelnetAppender"/>
      </root>
    </log4net>
""");
    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    XmlConfigurator.Configure(rep, log4netConfig["log4net"]!);
    Task task = Task.Run(() => new SimpleTelnetClient(Received).Run());
    task.Wait(500);
    
    rep.GetLogger("Telnet").Log(typeof(TelnetAppenderTest), Level.Info, "Log-Message", null);

    Assert.AreEqual(1, received.Count);
    
    void Received(string message) => received.Add(message);
  }
}