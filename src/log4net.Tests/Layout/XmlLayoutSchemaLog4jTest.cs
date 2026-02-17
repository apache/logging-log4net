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
  }
}