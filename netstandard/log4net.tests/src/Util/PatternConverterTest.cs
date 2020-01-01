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
using System.IO;
using System.Xml;
using log4net.Config;
using log4net.Core;
using log4net.Layout.Pattern;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Util
{
    [TestFixture]
    public class PatternConverterTest
    {
        [Test]
        public void PatternLayoutConverterProperties()
        {
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.LoadXml(@"
                <log4net>
                  <appender name=""StringAppender"" type=""log4net.Tests.Appender.StringAppender, log4net.Tests"">
                    <layout type=""log4net.Layout.PatternLayout"">
                        <converter>
                            <name value=""propertyKeyCount"" />
                            <type value=""log4net.Tests.Util.PropertyKeyCountPatternLayoutConverter, log4net.Tests"" />
                            <property>
                                <key value=""one-plus-one"" />
                                <value value=""2"" />
                            </property>
                            <property>
                               <key value=""two-plus-two"" />
                               <value value=""4"" />
                            </property> 
                        </converter>
                        <conversionPattern value=""%propertyKeyCount"" />
                    </layout>
                  </appender>
                  <root>
                    <level value=""ALL"" />                  
                    <appender-ref ref=""StringAppender"" />
                  </root>  
                </log4net>");

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            XmlConfigurator.Configure(rep, log4netConfig["log4net"]);

            ILog log = LogManager.GetLogger(rep.Name, "PatternLayoutConverterProperties");
            log.Debug("Message");

            PropertyKeyCountPatternLayoutConverter converter = 
                PropertyKeyCountPatternLayoutConverter.MostRecentInstance;
            Assert.AreEqual(2, converter.Properties.Count);
            Assert.AreEqual("4", converter.Properties["two-plus-two"]);

            StringAppender appender = 
                (StringAppender)LogManager.GetRepository(rep.Name).GetAppenders()[0];
            Assert.AreEqual("2", appender.GetString());
        }

        [Test]
        public void PatternConverterProperties()
        {
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.LoadXml(@"
                <log4net>
                  <appender name=""PatternStringAppender"" type=""log4net.Tests.Util.PatternStringAppender, log4net.Tests"">
                    <layout type=""log4net.Layout.SimpleLayout"" />
                    <setting>
                        <converter>
                            <name value=""propertyKeyCount"" />
                            <type value=""log4net.Tests.Util.PropertyKeyCountPatternConverter, log4net.Tests"" />
                            <property>
                                <key value=""one-plus-one"" />
                                <value value=""2"" />
                            </property>
                            <property>
                               <key value=""two-plus-two"" />
                               <value value=""4"" />
                            </property> 
                        </converter>
                        <conversionPattern value=""%propertyKeyCount"" />
                    </setting>
                  </appender>
                  <root>
                    <level value=""ALL"" />                  
                    <appender-ref ref=""PatternStringAppender"" />
                  </root>  
                </log4net>");

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            XmlConfigurator.Configure(rep, log4netConfig["log4net"]);

            ILog log = LogManager.GetLogger(rep.Name, "PatternConverterProperties");
            log.Debug("Message");

            PropertyKeyCountPatternConverter converter =
                PropertyKeyCountPatternConverter.MostRecentInstance;
            Assert.AreEqual(2, converter.Properties.Count);
            Assert.AreEqual("4", converter.Properties["two-plus-two"]);

            PatternStringAppender appender =
                (PatternStringAppender)LogManager.GetRepository(rep.Name).GetAppenders()[0];
            Assert.AreEqual("2", appender.Setting.Format());
        }
    }

    public class PropertyKeyCountPatternLayoutConverter : PatternLayoutConverter
    {
        private static PropertyKeyCountPatternLayoutConverter mostRecentInstance;

        public PropertyKeyCountPatternLayoutConverter()
        {
            mostRecentInstance = this;
        }

        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
        {
            writer.Write(Properties.GetKeys().Length);
        }

        public static PropertyKeyCountPatternLayoutConverter MostRecentInstance
        {
            get { return mostRecentInstance; }
        }
    }

    public class PropertyKeyCountPatternConverter : PatternConverter
    {
        private static PropertyKeyCountPatternConverter mostRecentInstance;

        public PropertyKeyCountPatternConverter()
        {
            mostRecentInstance = this;
        }

        protected override void Convert(TextWriter writer, object state)
        {
            writer.Write(Properties.GetKeys().Length);
        }

        public static PropertyKeyCountPatternConverter MostRecentInstance
        {
            get { return mostRecentInstance; }
        }
    }

    public class PatternStringAppender : StringAppender
    {
        private static PatternStringAppender mostRecentInstace;

        private PatternString setting;

        public PatternStringAppender()
        {
            mostRecentInstace = this;
        }

        public PatternString Setting
        {
            get { return setting; }
            set { setting = value; }
        }

        public static PatternStringAppender MostRecentInstace
        {
            get { return mostRecentInstace; }
        }
    }
}
