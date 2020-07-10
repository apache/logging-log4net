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

#if NET_2_0
using System;
using System.Collections.Generic;
using System.Xml;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Filter;
using log4net.Repository;
using NUnit.Framework;

namespace log4net.Tests.Filter
{
    [TestFixture]
    public class FilterTest
    {
        [Test]
        public void FilterConfigurationTest()
        {
            XmlDocument log4netConfig = new XmlDocument();
            #region Load log4netConfig
            log4netConfig.LoadXml(@"
            <log4net>
            <appender name=""MemoryAppender"" type=""log4net.Appender.MemoryAppender, log4net"">
                <filter type=""log4net.Tests.Filter.MultiplePropertyFilter, log4net.Tests"">
                    <condition>
                        <key value=""ABC"" />
                        <stringToMatch value=""123"" />
                    </condition>
                    <condition>
                        <key value=""DEF"" />
                        <stringToMatch value=""456"" />
                    </condition>
                </filter>
            </appender>
            <root>
                <level value=""ALL"" />
                <appender-ref ref=""MemoryAppender"" />
            </root>
            </log4net>");
            #endregion

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            XmlConfigurator.Configure(rep, log4netConfig["log4net"]);

            IAppender[] appenders = LogManager.GetRepository(rep.Name).GetAppenders();
            Assert.IsTrue(appenders.Length == 1);

            IAppender appender = Array.Find(appenders, delegate(IAppender a) {
                    return a.Name == "MemoryAppender";
                });
            Assert.IsNotNull(appender);

            MultiplePropertyFilter multiplePropertyFilter = 
                ((AppenderSkeleton)appender).FilterHead as MultiplePropertyFilter;

            MultiplePropertyFilter.Condition[] conditions = multiplePropertyFilter.GetConditions();
            Assert.AreEqual(2, conditions.Length);
            Assert.AreEqual("ABC", conditions[0].Key);
            Assert.AreEqual("123", conditions[0].StringToMatch);
            Assert.AreEqual("DEF", conditions[1].Key);
            Assert.AreEqual("456", conditions[1].StringToMatch);
        }
    }

    public class MultiplePropertyFilter : FilterSkeleton
    {
        private readonly List<Condition> _conditions = new List<Condition>();

        public override FilterDecision Decide(LoggingEvent loggingEvent)
        {
            return FilterDecision.Accept;
        }

        public Condition[] GetConditions()
        {
            return _conditions.ToArray();
        }

        public void AddCondition(Condition condition)
        {
            _conditions.Add(condition);
        }
        
        public class Condition
        {
            private string key, stringToMatch;
            public string Key {
                get { return key; }
                set { key = value; }
            }
            public string StringToMatch {
                get { return stringToMatch; }
                set { stringToMatch = value; }
            }
        }
    }
}
#endif
