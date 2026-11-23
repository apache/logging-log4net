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
using System.IO;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender;
using NUnit.Framework;

namespace log4net.Tests.Layout
{
  /// <summary>
  /// Tests for <see cref="XmlLayoutSchemaLog4J"/>
  /// </summary>
  [TestFixture]
  public class XmlLayoutSchemaLog4JTest
  {
    /// <summary>
    /// Tests a regression from 3.0.0 (log4j:data instead of log4j:throwable)
    /// </summary>
    [Test]
    public void LogExceptionTest()
    {
      StringAppender stringAppender = new() { Layout = new XmlLayoutSchemaLog4J() };

      ILoggerRepository repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
      BasicConfigurator.Configure(repository, stringAppender);
      ILog log = LogManager.GetLogger(repository.Name, "TestLogger");
      
      ThrowAndLog(42);

      string logEventXml = stringAppender.GetString();
      Assert.That(logEventXml, Does.Contain("log4j:throwable"));
      
      void ThrowAndLog(int foo)
      {
        try
        {
          throw new TimeoutException();
        }
        catch (TimeoutException ex)
        {
          log.Error($"Error {foo}", ex);
        }
      }
    }
    
    /// <summary>
    /// Tests the serialization of invalid characters in the Properties dictionary
    /// </summary>
    [Test]
    public void InvalidCharacterTest()
    {
      StringAppender stringAppender = new() { Layout = new XmlLayoutSchemaLog4J() };

      ILoggerRepository repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
      BasicConfigurator.Configure(repository, stringAppender);
      ILog log = LogManager.GetLogger(repository.Name, "TestLogger");
      
      Log();

      string logEventXml = stringAppender.GetString();
      Assert.That(logEventXml, Does.Contain("us?er"));
      Assert.That(logEventXml, Does.Contain("A?B"));
      Assert.That(logEventXml, Does.Contain("Log?ger"));
      Assert.That(logEventXml, Does.Contain("Thread?Name"));
      Assert.That(logEventXml, Does.Contain("Do?main"));
      Assert.That(logEventXml, Does.Contain("Ident?ity"));
      Assert.That(logEventXml, Does.Contain("User?Name"));
      Assert.That(logEventXml, Does.Contain("Mess?age"));
      Assert.That(logEventXml, Does.Contain("oh?my"));

      void Log()
      {
        // Build a LoggingEvent with an XML invalid character in a property value
        LoggingEventData data = new()
        {
          LoggerName = "Log\u0001ger",
          Level = Level.Info,
          TimeStampUtc = DateTime.UtcNow,
          ThreadName = "Thread\u0001Name",
          Domain = "Do\u0001main",
          Identity = "Ident\u0001ity",
          UserName = "User\u0001Name",
          Message = "Mess\u0001age",
          ExceptionString = "oh\u0001my",
          Properties = new()
        };

        // Value contains U+0001 which is illegal in XML 1.0
        data.Properties["us\u0001er"] = "A\u0001B";

        // Log the event
        log.Logger.Log(new(null, repository, data));
      }
    }

    /// <summary>
    /// Message, NDC and throwable keep their CR LF line ends as character references.
    /// </summary>
    [TestCase("NDC")]
    [TestCase("message")]
    [TestCase("throwable")]
    public void LineEndsInElementTextReadBackExactly(string element)
    {
      StringAppender stringAppender = new() { Layout = new XmlLayoutSchemaLog4J() };
      ILoggerRepository repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
      BasicConfigurator.Configure(repository, stringAppender);
      ILog log = LogManager.GetLogger(repository.Name, "TestLogger");
      LoggingEventData data = new()
      {
        LoggerName = "TestLogger",
        Level = Level.Info,
        TimeStampUtc = DateTime.UtcNow,
        Message = "a\r\nb",
        ExceptionString = "a\r\nb",
        Properties = new()
      };
      data.Properties["NDC"] = "a\r\nb";

      log.Logger.Log(new(null, repository, data));

      string record = stringAppender.GetString();
      Assert.That(XmlRecords.RawContent(record, element), Is.EqualTo("a&#xD;&#xA;b"));
      Assert.That(XmlRecords.ElementText(record, element), Is.EqualTo("a\r\nb"));
    }

    /// <summary>
    /// A tab in a property name, a property value and the identity reads back as a tab.
    /// </summary>
    [Test]
    public void ATabInAnAttributeReadsBackExactly()
    {
      StringAppender stringAppender = new() { Layout = new XmlLayoutSchemaLog4J() };
      ILoggerRepository repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
      BasicConfigurator.Configure(repository, stringAppender);
      ILog log = LogManager.GetLogger(repository.Name, "TestLogger");
      LoggingEventData data = new()
      {
        LoggerName = "TestLogger",
        Level = Level.Info,
        TimeStampUtc = DateTime.UtcNow,
        Message = "message",
        Identity = "alice\tadmin",
        Properties = new()
      };
      data.Properties["alice\tadmin"] = "alice\tadmin";

      log.Logger.Log(new(null, repository, data));

      string record = stringAppender.GetString();
      Assert.That(record, Contains.Substring("name=\"alice&#x9;admin\" value=\"alice&#x9;admin\"").Using(StringComparison.Ordinal));
      Assert.That(record, Contains.Substring("name=\"log4net:Identity\" value=\"alice&#x9;admin\"").Using(StringComparison.Ordinal));
      Assert.That(XmlRecords.AttributeValues(record, "name"), Has.Member("alice\tadmin"));
      Assert.That(XmlRecords.AttributeValues(record, "value"), Has.Exactly(2).EqualTo("alice\tadmin"));
    }

    /// <summary>
    /// Formatting leaves the event's properties as they were: the event is shared with other appenders.
    /// </summary>
    [TestCase(true, TestName = "FixedEvent")]
    [TestCase(false, TestName = "UnfixedEvent")]
    public void FormattingLeavesTheEventsPropertiesAlone(bool isFixed)
    {
      ILoggerRepository repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
      LoggingEvent loggingEvent = isFixed
        ? new(null, repository, CreateData())
        : new(typeof(XmlLayoutSchemaLog4JTest), repository, "TestLogger", Level.Info, "message", null);
      string[] keys = loggingEvent.GetProperties().GetKeys();
      using StringWriter writer = new();

      new XmlLayoutSchemaLog4J().Format(writer, loggingEvent);

      Assert.That(loggingEvent.GetProperties().GetKeys(), Is.EquivalentTo(keys));
      Assert.That(loggingEvent.LookupProperty("log4japp"), Is.Null);
    }

    /// <summary>
    /// The translated properties still reach the output.
    /// </summary>
    [Test]
    public void TheTranslatedPropertiesAreWritten()
    {
      ILoggerRepository repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
      using StringWriter writer = new();

      new XmlLayoutSchemaLog4J().Format(writer, new(null, repository, CreateData()));

      string record = writer.ToString();
      Assert.That(record, Does.Contain("name=\"log4jmachinename\" value=\"host\""));
      Assert.That(record, Does.Contain("name=\"log4japp\" value=\"domain\""));
      Assert.That(record, Does.Contain("name=\"log4net:Identity\" value=\"alice\""));
      Assert.That(record, Does.Contain("name=\"log4net:UserName\" value=\"bob\""));
    }

    /// <summary>
    /// A property the event already has wins over the translated value.
    /// </summary>
    [Test]
    public void AnExistingPropertyWins()
    {
      ILoggerRepository repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
      LoggingEventData data = CreateData();
      data.Properties!["log4japp"] = "mine";
      using StringWriter writer = new();

      new XmlLayoutSchemaLog4J().Format(writer, new(null, repository, data));

      Assert.That(XmlRecords.AttributeValues(writer.ToString(), "value"), Has.Member("mine").And.No.Member("domain"));
    }

    /// <summary>
    /// An empty identity is not translated.
    /// </summary>
    [Test]
    public void AnEmptyValueIsNotTranslated()
    {
      ILoggerRepository repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
      LoggingEventData data = CreateData();
      data.Identity = "";
      using StringWriter writer = new();

      new XmlLayoutSchemaLog4J().Format(writer, new(null, repository, data));

      Assert.That(XmlRecords.AttributeValues(writer.ToString(), "name"), Has.No.Member(LoggingEvent.IdentityProperty));
    }

    private static LoggingEventData CreateData() => new()
    {
      LoggerName = "TestLogger",
      Level = Level.Info,
      TimeStampUtc = DateTime.UtcNow,
      Message = "message",
      Domain = "domain",
      Identity = "alice",
      UserName = "bob",
      Properties = new() { [LoggingEvent.HostNameProperty] = "host" }
    };
  }
}