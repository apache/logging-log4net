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
using System.Diagnostics;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using log4net.Repository;
using NUnit.Framework;

namespace log4net.Tests.Appender
{
    [TestFixture]
    public class TraceAppenderTest
    {
        [Test]
        public void DefaultCategoryTest()
        {
            CategoryTraceListener categoryTraceListener = new CategoryTraceListener();
            Trace.Listeners.Clear();
            Trace.Listeners.Add(categoryTraceListener);

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

            TraceAppender traceAppender = new TraceAppender();
            traceAppender.Layout = new SimpleLayout();
            traceAppender.ActivateOptions();

            BasicConfigurator.Configure(rep, traceAppender);

            ILog log = LogManager.GetLogger(rep.Name, GetType());
            log.Debug("Message");

            Assert.AreEqual(
                GetType().ToString(),
                categoryTraceListener.Category);
        }

        [Test]
        public void MethodNameCategoryTest()
        {
            CategoryTraceListener categoryTraceListener = new CategoryTraceListener();
            Trace.Listeners.Clear();
            Trace.Listeners.Add(categoryTraceListener);

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

            TraceAppender traceAppender = new TraceAppender();
            PatternLayout methodLayout = new PatternLayout("%method");
            methodLayout.ActivateOptions();
            traceAppender.Category = methodLayout;
            traceAppender.Layout = new SimpleLayout();
            traceAppender.ActivateOptions();

            BasicConfigurator.Configure(rep, traceAppender);

            ILog log = LogManager.GetLogger(rep.Name, GetType());
            log.Debug("Message");

            Assert.AreEqual(
                System.Reflection.MethodInfo.GetCurrentMethod().Name,
                categoryTraceListener.Category);
        }
    }

    public class CategoryTraceListener : TraceListener
    {
        private string lastCategory;

        public override void Write(string message)
        {
            // empty
        }

        public override void WriteLine(string message)
        {
            Write(message);
        }

        public override void Write(string message, string category)
        {
            lastCategory = category;
            base.Write(message, category);
        }

        public string Category
        {
            get { return lastCategory; }
        }
    }
}
