/*
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
using System.Diagnostics;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using NUnit.Framework;

namespace log4net.Tests.Appender
{
    [TestFixture]
    public class DebugAppenderTest
    {
        [Test]
        public void NullCategoryTest()
        {
            CategoryTraceListener categoryTraceListener = new CategoryTraceListener();
            Debug.Listeners.Add(categoryTraceListener);

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

            DebugAppender debugAppender = new DebugAppender();
            debugAppender.Layout = new SimpleLayout();
            debugAppender.ActivateOptions();

            debugAppender.Category = null;

            TestErrorHandler testErrHandler = new TestErrorHandler();
            debugAppender.ErrorHandler = testErrHandler;            

            BasicConfigurator.Configure(rep, debugAppender);

            ILog log = LogManager.GetLogger(rep.Name, GetType());
            log.Debug("Message");

            Assert.AreEqual(
                null,
                categoryTraceListener.Category);

            Assert.IsFalse(testErrHandler.ErrorOccured);

            Debug.Listeners.Remove(categoryTraceListener);
        }

        [Test]
        public void EmptyStringCategoryTest()
        {
            CategoryTraceListener categoryTraceListener = new CategoryTraceListener();
            Debug.Listeners.Add(categoryTraceListener);

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

            DebugAppender debugAppender = new DebugAppender();
            debugAppender.Layout = new SimpleLayout();
            debugAppender.ActivateOptions();

            debugAppender.Category = new PatternLayout("");

            BasicConfigurator.Configure(rep, debugAppender);

            ILog log = LogManager.GetLogger(rep.Name, GetType());
            log.Debug("Message");

            Assert.AreEqual(
                null,
                categoryTraceListener.Category);

            Debug.Listeners.Remove(categoryTraceListener);
        }

        [Test]
        public void DefaultCategoryTest()
        {
            CategoryTraceListener categoryTraceListener = new CategoryTraceListener();
            Debug.Listeners.Add(categoryTraceListener);

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

            DebugAppender debugAppender = new DebugAppender();
            debugAppender.Layout = new SimpleLayout();
            debugAppender.ActivateOptions();

            BasicConfigurator.Configure(rep, debugAppender);

            ILog log = LogManager.GetLogger(rep.Name, GetType());
            log.Debug("Message");

            Assert.AreEqual(
                GetType().ToString(),
                categoryTraceListener.Category);

            Debug.Listeners.Remove(categoryTraceListener);
        }

#if !NETSTANDARD1_3 // "LocationInfo can't get method names on NETSTANDARD1_3 due to unavailable stack frame APIs"
        [Test]
        public void MethodNameCategoryTest()
        {
            CategoryTraceListener categoryTraceListener = new CategoryTraceListener();
            Debug.Listeners.Add(categoryTraceListener);

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

            DebugAppender debugAppender = new DebugAppender();
            PatternLayout methodLayout = new PatternLayout("%method");
            methodLayout.ActivateOptions();
            debugAppender.Category = methodLayout;
            debugAppender.Layout = new SimpleLayout();
            debugAppender.ActivateOptions();

            BasicConfigurator.Configure(rep, debugAppender);

            ILog log = LogManager.GetLogger(rep.Name, GetType());
            log.Debug("Message");

            Assert.AreEqual(
                System.Reflection.MethodInfo.GetCurrentMethod().Name,
                categoryTraceListener.Category);

            Debug.Listeners.Remove(categoryTraceListener);
        }
#endif

        private class TestErrorHandler : IErrorHandler
        {
            private bool m_errorOccured = false;

            public bool ErrorOccured
            { 
                get { return m_errorOccured; }
            }
            #region IErrorHandler Members

            public void Error(string message, Exception e, ErrorCode errorCode)
            {
                m_errorOccured = true;
            }

            public void Error(string message, Exception e)
            {
                Error(message, e, ErrorCode.GenericFailure);
            }

            public void Error(string message)
            {
                Error(message, null, ErrorCode.GenericFailure);
            }

            #endregion
        }
    }
}
