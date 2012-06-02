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
using System.Xml;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using log4net.Tests.Appender;
using NUnit.Framework;

namespace log4net.Tests.Hierarchy
{
    [TestFixture]
    public class Hierarchy
    {
        [Test]
        public void SetRepositoryPropertiesInConfigFile()
        {
            // LOG4NET-53: Allow repository properties to be set in the config file
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.LoadXml(@"
                <log4net>
                  <property>
                    <key value=""two-plus-two"" />
                    <value value=""4"" />
                  </property>
                  <appender name=""StringAppender"" type=""log4net.Tests.Appender.StringAppender, log4net.Tests"">
                    <layout type=""log4net.Layout.SimpleLayout"" />
                  </appender>
                  <root>
                    <level value=""ALL"" />
                    <appender-ref ref=""StringAppender"" />
                  </root>
                </log4net>");

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            XmlConfigurator.Configure(rep, log4netConfig["log4net"]);

            Assert.AreEqual("4", rep.Properties["two-plus-two"]);
            Assert.IsNull(rep.Properties["one-plus-one"]);
        }

        [Test]
        public void AddingMultipleAppenders()
        {
            CountingAppender alpha = new CountingAppender();
            CountingAppender beta = new CountingAppender();

            Repository.Hierarchy.Hierarchy hierarchy = 
                (Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
            hierarchy.Root.AddAppender(alpha);
            hierarchy.Root.AddAppender(beta);
            hierarchy.Configured = true;

            ILog log = LogManager.GetLogger(GetType());
            log.Debug("Hello World");

            Assert.AreEqual(1, alpha.Counter);
            Assert.AreEqual(1, beta.Counter);
        }

        [Test]
        public void AddingMultipleAppenders2()
        {
            CountingAppender alpha = new CountingAppender();
            CountingAppender beta = new CountingAppender();

            BasicConfigurator.Configure(alpha, beta);

            ILog log = LogManager.GetLogger(GetType());
            log.Debug("Hello World");

            Assert.AreEqual(1, alpha.Counter);
            Assert.AreEqual(1, beta.Counter);
        }

        [Test]
	// LOG4NET-343
        public void LoggerNameCanConsistOfASingleDot()
        {
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.LoadXml(@"
                <log4net>
                  <appender name=""StringAppender"" type=""log4net.Tests.Appender.StringAppender, log4net.Tests"">
                    <layout type=""log4net.Layout.SimpleLayout"" />
                  </appender>
                  <root>
                    <level value=""ALL"" />
                    <appender-ref ref=""StringAppender"" />
                  </root>
                  <logger name=""."">
                    <level value=""WARN"" />
                  </logger>
                </log4net>");

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            XmlConfigurator.Configure(rep, log4netConfig["log4net"]);
        }

        [Test]
        public void LoggerNameCanConsistOfASingleNonDot()
        {
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.LoadXml(@"
                <log4net>
                  <appender name=""StringAppender"" type=""log4net.Tests.Appender.StringAppender, log4net.Tests"">
                    <layout type=""log4net.Layout.SimpleLayout"" />
                  </appender>
                  <root>
                    <level value=""ALL"" />
                    <appender-ref ref=""StringAppender"" />
                  </root>
                  <logger name=""L"">
                    <level value=""WARN"" />
                  </logger>
                </log4net>");

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            XmlConfigurator.Configure(rep, log4netConfig["log4net"]);
        }

        [Test]
        public void LoggerNameCanContainSequenceOfDots()
        {
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.LoadXml(@"
                <log4net>
                  <appender name=""StringAppender"" type=""log4net.Tests.Appender.StringAppender, log4net.Tests"">
                    <layout type=""log4net.Layout.SimpleLayout"" />
                  </appender>
                  <root>
                    <level value=""ALL"" />
                    <appender-ref ref=""StringAppender"" />
                  </root>
                  <logger name=""L..M"">
                    <level value=""WARN"" />
                  </logger>
                </log4net>");

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            XmlConfigurator.Configure(rep, log4netConfig["log4net"]);
        }
    }
}
