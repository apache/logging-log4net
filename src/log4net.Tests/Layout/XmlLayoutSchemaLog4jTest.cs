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
using System.Xml;

using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;

using NUnit.Framework;
using System.Globalization;

namespace log4net.Tests.Layout
{
  [TestFixture]
  public class XmlLayoutSchemaLog4jTest
  {
    [Test]
    public void LogExceptionTest()
    {
      XmlLayoutSchemaLog4j layout = new();
      StringAppender stringAppender = new() { Layout = layout };

      ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
      BasicConfigurator.Configure(rep, stringAppender);
      ILog log1 = LogManager.GetLogger(rep.Name, "TestLogger");
      Bar(42);

      // really only asserts there is no exception
      XmlDocument loggedDoc = new();
      loggedDoc.LoadXml(stringAppender.GetString());

      void Bar(int foo)
      {
        try
        {
          throw new TimeoutException();
        }
        catch (Exception ex)
        {
          log1.Error(string.Format("Error {0}", foo), ex);
        }
      }
    }
  }
}