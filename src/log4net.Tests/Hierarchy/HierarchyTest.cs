/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Threading.Tasks;
using System.Xml;
using log4net.Config;
using log4net.Repository;
using log4net.Tests.Appender;
using NUnit.Framework;

namespace log4net.Tests.Hierarchy;

[TestFixture]
public class HierarchyTest
{
  [Test]
  public void SetRepositoryPropertiesInConfigFile()
  {
    // LOG4NET-53: Allow repository properties to be set in the config file
    XmlDocument log4NetConfig = new();
    log4NetConfig.LoadXml(
      """
      <log4net>
        <property>
          <key value="two-plus-two" />
          <value value="4" />
        </property>
        <appender name="StringAppender" type="log4net.Tests.Appender.StringAppender, log4net.Tests">
          <layout type="log4net.Layout.SimpleLayout" />
        </appender>
        <root>
          <level value="ALL" />
          <appender-ref ref="StringAppender" />
        </root>
      </log4net>
      """);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    XmlConfigurator.Configure(rep, log4NetConfig["log4net"]!);

    Assert.That(rep.Properties["two-plus-two"], Is.EqualTo("4"));
    Assert.That(rep.Properties["one-plus-one"], Is.Null);
  }

  [Test]
  public void AddingMultipleAppenders()
  {
    CountingAppender alpha = new();
    CountingAppender beta = new();

    Repository.Hierarchy.Hierarchy hierarchy = (Repository.Hierarchy.Hierarchy)LogManager.GetRepository();

    hierarchy.Root.AddAppender(alpha);
    hierarchy.Root.AddAppender(beta);
    hierarchy.Configured = true;

    ILog log = LogManager.GetLogger(GetType());
    log.Debug("Hello World");

    Assert.That(alpha.Counter, Is.EqualTo(1));
    Assert.That(beta.Counter, Is.EqualTo(1));
  }

  [Test]
  public void AddingMultipleAppenders2()
  {
    CountingAppender alpha = new();
    CountingAppender beta = new();

    BasicConfigurator.Configure(LogManager.GetRepository(), alpha, beta);

    ILog log = LogManager.GetLogger(GetType());
    log.Debug("Hello World");

    Assert.That(alpha.Counter, Is.EqualTo(1));
    Assert.That(beta.Counter, Is.EqualTo(1));
  }

  [Test]
  // LOG4NET-343
  public void LoggerNameCanConsistOfASingleDot()
  {
    XmlDocument log4NetConfig = new();
    log4NetConfig.LoadXml(
      """
      <log4net>
        <appender name="StringAppender" type="log4net.Tests.Appender.StringAppender, log4net.Tests">
          <layout type="log4net.Layout.SimpleLayout" />
        </appender>
        <root>
          <level value="ALL" />
          <appender-ref ref="StringAppender" />
        </root>
        <logger name=".">
          <level value="WARN" />
        </logger>
      </log4net>
      """);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    XmlConfigurator.Configure(rep, log4NetConfig["log4net"]!);
  }

  [Test]
  public void LoggerNameCanConsistOfASingleNonDot()
  {
    XmlDocument log4NetConfig = new();
    log4NetConfig.LoadXml(
      """
      <log4net>
        <appender name="StringAppender" type="log4net.Tests.Appender.StringAppender, log4net.Tests">
          <layout type="log4net.Layout.SimpleLayout" />
        </appender>
        <root>
          <level value="ALL" />
          <appender-ref ref="StringAppender" />
        </root>
        <logger name="L">
          <level value="WARN" />
        </logger>
      </log4net>
      """);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    XmlConfigurator.Configure(rep, log4NetConfig["log4net"]!);
  }

  [Test]
  public void LoggerNameCanContainSequenceOfDots()
  {
    XmlDocument log4NetConfig = new();
    log4NetConfig.LoadXml(
      """
      <log4net>
        <appender name="StringAppender" type="log4net.Tests.Appender.StringAppender, log4net.Tests">
          <layout type="log4net.Layout.SimpleLayout" />
        </appender>
        <root>
          <level value="ALL" />
          <appender-ref ref="StringAppender" />
        </root>
        <logger name="L..M">
          <level value="WARN" />
        </logger>
      </log4net>
      """);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    XmlConfigurator.Configure(rep, log4NetConfig["log4net"]!);
  }

  /// <summary>
  /// https://github.com/apache/logging-log4net/issues/156
  /// Regression: Creating nested loggers in reverse order fails in 3.0.0-preview.1
  /// </summary>
  [Test]
  public void CreateNestedLoggersInReverseOrder()
  {
    XmlDocument log4NetConfig = new();
    log4NetConfig.LoadXml(
      """
      <log4net>
        <appender name="StringAppender" type="log4net.Tests.Appender.StringAppender, log4net.Tests">
          <layout type="log4net.Layout.SimpleLayout" />
        </appender>
        <root>
          <level value="ALL" />
          <appender-ref ref="StringAppender" />
        </root>
      </log4net>
      """);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    XmlConfigurator.Configure(rep, log4NetConfig["log4net"]!);
    Assert.That(rep.GetLogger("A.B.C").Name, Is.EqualTo("A.B.C"));
    Assert.That(rep.GetLogger("A.B").Name, Is.EqualTo("A.B"));
  }

  /// <summary>
  /// https://github.com/apache/logging-log4net/issues/197
  /// IndexOutOfRangeException when creating child loggers multithreaded
  /// </summary>
  [Test]
  public void CreateChildLoggersMultiThreaded()
  {
    XmlDocument log4NetConfig = new();
    log4NetConfig.LoadXml(
      """
      <log4net>
        <appender name="StringAppender" type="log4net.Tests.Appender.StringAppender, log4net.Tests">
          <layout type="log4net.Layout.SimpleLayout" />
        </appender>
        <root>
          <level value="ALL" />
          <appender-ref ref="StringAppender" />
        </root>
      </log4net>
      """);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    XmlConfigurator.Configure(rep, log4NetConfig["log4net"]!);

    Parallel.For(0, 100, i => Assert.That(rep.GetLogger($"A.{i}").Name, Is.EqualTo($"A.{i}")));
  }
}