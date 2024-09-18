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

using System.Xml;
using System;
using log4net.Appender;
using NUnit.Framework;
using log4net.Repository;
using log4net.Config;
using log4net.Util;

namespace log4net.Tests.Appender;

/// <summary>
/// Used for internal unit testing the <see cref="FileAppender"/> class.
/// </summary>
[TestFixture]
public sealed class FileAppenderTest
{
  /// <summary>
  /// Shuts down any loggers in the hierarchy, along with all appenders
  /// </summary>
  private static void Reset()
  {
    // Regular users should not use the clear method lightly!
    LogManager.GetRepository().ResetConfiguration();
    LogManager.GetRepository().Shutdown();
    ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Clear();
  }

  /// <summary>
  /// Any initialization that happens before each test can
  /// go here
  /// </summary>
  [SetUp]
  public void SetUp() => Reset();

  /// <summary>
  /// Any steps that happen after each test go here
  /// </summary>
  [TearDown]
  public void TearDown() => Reset();

  /// <summary>
  /// Verifies that the <see cref="FileAppender.File"/> property accepts a <see cref="PatternString"/>
  /// </summary>
  [Test]
  public void FilenameWithPatternStringTest()
  {
    LogLog.LogReceived += LogReceived;
    try
    {
      XmlDocument log4netConfig = new();
      log4netConfig.LoadXml("""
  <log4net>
    <appender name="ConsoleAppender" type="log4net.Appender.ConsoleAppender">
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="%date{ABSOLUTE} [%logger] %level - %message%newline%exception"/>
      </layout>
    </appender>
    <appender name="GeneralFileAppender" type="log4net.Appender.FileAppender">
      <file type="log4net.Util.PatternString" value="Logs\file_%property{LogName}_%date{yyyyMMddHHmmss}.Log"/>
      <appendToFile value="true"/>
      <lockingModel type="log4net.Appender.FileAppender+MinimalLock"/>
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="%date{ABSOLUTE} [%logger] %level - %message%newline%exception"/>
      </layout>
    </appender>
    <root>
      <level value="INFO"/>
      <appender-ref ref="GeneralFileAppender"/>
    </root>
  </log4net>
""");
      ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
      XmlConfigurator.Configure(rep, log4netConfig["log4net"]!);
    }
    finally
    {
      LogLog.LogReceived += LogReceived;
    }

    static void LogReceived(object? source, LogReceivedEventArgs e) => Assert.Fail(e.LogLog.Message);
  }
}