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
using log4net.Tests.Appender;
using System.Globalization;

namespace log4net.Tests.Filter
{
    [TestFixture]
    public class ExceptionTypeFilterTest
    {

#if !NETSTANDARD1_3
        private CultureInfo _currentCulture;
        private CultureInfo _currentUICulture;

        [SetUp]
        public void SetUp()
        {
            // set correct thread culture
            _currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            _currentUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown()
        {
            // restore previous culture
            System.Threading.Thread.CurrentThread.CurrentCulture = _currentCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = _currentUICulture;
        }
#endif

        protected ILoggerRepository loggerRepository { get; set; }

        [SetUp]
        public void SetUpConfiguration()
        {
            XmlDocument log4netConfig = new XmlDocument();
            #region Load log4netConfig
            log4netConfig.LoadXml(@"
            <log4net>
            <appender name=""StringAppender"" type=""log4net.Tests.Appender.StringAppender, log4net.Tests"">
                <filter type=""log4net.Filter.ExceptionTypeFilter, log4net"">
                    <ExceptionTypeName value=""log4net.Tests.Filter.ExceptionTypeFilterTestException, log4net.Tests"" />
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

            loggerRepository = LogManager.CreateRepository(Guid.NewGuid().ToString());
        }

        [Test]
        public void TestFilterCustomException()
        {
            IAppender[] appenders = LogManager.GetRepository(loggerRepository.Name).GetAppenders();
            Assert.IsTrue(appenders.Length == 1);

            IAppender appender = Array.Find(appenders, delegate(IAppender a) {
                    return a.Name == "MemoryAppender";
                });
            Assert.IsNotNull(appender);

            StringAppender stringAppender = appender as StringAppender;
            Assert.IsNotNull(stringAppender);

            ILog log1 = LogManager.GetLogger(loggerRepository.Name, "TestFormatString");
            Assert.IsNotNull(log1);

            // ***
            log1.Info("Testing for ExceptionTypeFilterTestException", new ExceptionTypeFilterTestException());
            Assert.AreEqual("Testing for ExceptionTypeFilterTestException", stringAppender.GetString(), "Test ExceptionTypeFilterTestException can be filter.");

        }
    }

    public class ExceptionTypeFilterTestException : Exception
    { }

}
#endif
