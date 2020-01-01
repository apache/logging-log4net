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

#if !NET_2_0 && !MONO_2_0
using System;
using System.Data;
using System.Xml;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Util;
using NUnit.Framework;
using System.Globalization;

namespace log4net.Tests.Appender
{
    [TestFixture]
    public class RecursiveLoggingTest
    {
        private EventRaisingAppender m_eventRaisingAppender;
        private Repository.Hierarchy.Hierarchy m_hierarchy;
        private int m_eventCount;
        private ILogger m_logger;
        private const int MaxRecursion = 3;

        private void SetupRepository()
        {
            m_hierarchy = new Repository.Hierarchy.Hierarchy();

            m_eventRaisingAppender = new EventRaisingAppender();
            m_eventRaisingAppender.LoggingEventAppended += eventRaisingAppender_LoggingEventAppended;

            m_hierarchy.Root.Level = Level.All;
            m_hierarchy.Root.AddAppender(m_eventRaisingAppender);

            BasicConfigurator.Configure(m_hierarchy, m_eventRaisingAppender);

            m_logger = m_hierarchy.GetLogger("test");

        }

        void eventRaisingAppender_LoggingEventAppended(object sender, LoggingEventEventArgs e)
        {
            if (m_eventCount < MaxRecursion && m_logger != null)
            {
                m_eventCount++;
                string message = String.Format(CultureInfo.CurrentCulture, "Log event {0} from EventRaisingAppender", m_eventCount);
                Console.WriteLine("Logging message: " + message);
                m_logger.Log(typeof(RecursiveLoggingTest), Level.Warn, message, null);
            }
        }

        [Test]
        public void TestAllowRecursiveLoggingFromAppender()
        {
            SetupRepository();

            m_eventCount = 0;
            m_logger.Log(typeof(RecursiveLoggingTest), Level.Warn, "Message logged", null);

            Assert.AreEqual(MaxRecursion, m_eventCount, "Expected MaxRecursion recursive calls");
        }

    }
}
#endif