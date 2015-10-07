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
using System.Collections;
using System.Xml;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.LoggerRepository
{
    [TestFixture]
    public class ConfigurationMessages
    {
        [Test]
        public void ConfigurationMessagesTest()
        {
            try {
                LogLog.EmitInternalMessages = false;
                LogLog.InternalDebugging = true;

                XmlDocument log4netConfig = new XmlDocument();
                log4netConfig.LoadXml(@"
                <log4net>
                  <appender name=""LogLogAppender"" type=""log4net.Tests.LoggerRepository.LogLogAppender, log4net.Tests"">
                    <layout type=""log4net.Layout.SimpleLayout"" />
                  </appender>
                  <appender name=""MemoryAppender"" type=""log4net.Appender.MemoryAppender"">
                    <layout type=""log4net.Layout.SimpleLayout"" />
                  </appender>
                  <root>
                    <level value=""ALL"" />
                    <appender-ref ref=""LogLogAppender"" />
                    <appender-ref ref=""MemoryAppender"" />
                  </root>  
                </log4net>");

                ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
                rep.ConfigurationChanged += new LoggerRepositoryConfigurationChangedEventHandler(rep_ConfigurationChanged);

                ICollection configurationMessages = XmlConfigurator.Configure(rep, log4netConfig["log4net"]);

                Assert.IsTrue(configurationMessages.Count > 0);
            }
            finally {
                LogLog.EmitInternalMessages = true;
                LogLog.InternalDebugging = false;
            }
        }

        static void rep_ConfigurationChanged(object sender, EventArgs e)
        {
            ConfigurationChangedEventArgs configChanged = (ConfigurationChangedEventArgs)e;

            Assert.IsTrue(configChanged.ConfigurationMessages.Count > 0);
        }
    }

    public class LogLogAppender : AppenderSkeleton
    {
        private readonly static Type declaringType = typeof(LogLogAppender);

        public override void ActivateOptions()
        {
            LogLog.Debug(declaringType, "Debug - Activating options...");
            LogLog.Warn(declaringType, "Warn - Activating options...");
            LogLog.Error(declaringType, "Error - Activating options...");

            base.ActivateOptions();
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            LogLog.Debug(declaringType, "Debug - Appending...");
            LogLog.Warn(declaringType, "Warn - Appending...");
            LogLog.Error(declaringType, "Error - Appending...");
        }
    }
}
