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
  }
}