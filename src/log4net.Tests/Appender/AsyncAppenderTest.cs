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
using System.Threading;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;

using NUnit.Framework;

namespace log4net.Tests.Appender
{

    [TestFixture]
    public class AsyncAppenderTest
    {
        private AsyncAppender asyncAppender;
        private StringAppender stringAppender;
        private Repository.Hierarchy.Hierarchy hierarchy;

        [SetUp]
        public void SetupRepository()
        {
            hierarchy = new Repository.Hierarchy.Hierarchy();

            stringAppender = new StringAppender();
            stringAppender.Layout = new SimpleLayout();
            stringAppender.ActivateOptions();

            asyncAppender = new AsyncAppender();
            asyncAppender.AddAppender(stringAppender);
            asyncAppender.ActivateOptions();

            BasicConfigurator.Configure(hierarchy, asyncAppender);
        }

        [TearDown]
        public void ResetRepository()
        {
            hierarchy.ResetConfiguration();
            hierarchy.Shutdown();
            hierarchy.Clear();
        }

        [Test]
        public void ShouldLogSingleEvent()
        {
            ILogger logger = hierarchy.GetLogger("test");
            logger.Log(GetType(), Level.Warn, "foo", null);
            string log = WaitSomeTimeAndReturnLogWithLineFeedsStripped();
            Assert.AreEqual("WARN - foo", log);
        }

        [Test]
        public void ShouldMaintainOrderOfEvents()
        {
            ILogger logger = hierarchy.GetLogger("test");
            logger.Log(GetType(), Level.Warn, "foo", null);
            logger.Log(GetType(), Level.Warn, "bar", null);
            logger.Log(GetType(), Level.Warn, "baz", null);
            logger.Log(GetType(), Level.Warn, "xyzzy", null);
            string log = WaitSomeTimeAndReturnLogWithLineFeedsStripped();
            Assert.AreEqual("WARN - fooWARN - barWARN - bazWARN - xyzzy", log);
        }

        [Test]
        public void ShouldForwardBulkOfEvents()
        {
            LoggingEvent[] events = new LoggingEvent[] {
                new LoggingEvent(GetType(), hierarchy, "test", Level.Warn, "foo", null),
                new LoggingEvent(GetType(), hierarchy, "test", Level.Warn, "bar", null),
                new LoggingEvent(GetType(), hierarchy, "test", Level.Warn, "baz", null),
                new LoggingEvent(GetType(), hierarchy, "test", Level.Warn, "xyzzy", null)
            };
            asyncAppender.DoAppend(events);
            string log = WaitSomeTimeAndReturnLogWithLineFeedsStripped();
            Assert.AreEqual("WARN - fooWARN - barWARN - bazWARN - xyzzy", log);
        }

        [Test]
        public void ShouldNotLogAfterClose()
        {
            ILogger logger = hierarchy.GetLogger("test");
            logger.Log(GetType(), Level.Warn, "foo", null);
            Thread.Sleep(200);
            hierarchy.Shutdown();
            logger.Log(GetType(), Level.Warn, "bar", null);
            string log = WaitSomeTimeAndReturnLogWithLineFeedsStripped();
            Assert.AreEqual("WARN - foo", log);
        }

        private string WaitSomeTimeAndReturnLogWithLineFeedsStripped()
        {
            Thread.Sleep(200);
            return stringAppender.GetString().Replace(Environment.NewLine, string.Empty);
        }
    }
}