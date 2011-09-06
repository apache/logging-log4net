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

using System.Collections;
using System.Diagnostics;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Util
{
    [TestFixture]
    public class LogLogTest
    {
        [Test]
        public void TraceListenerCounterTest()
        {
            TraceListenerCounter listTraceListener = new TraceListenerCounter();

            Trace.Listeners.Clear();
            Trace.Listeners.Add(listTraceListener);

            Trace.Write("Hello");
            Trace.Write("World");

            Assert.AreEqual(2, listTraceListener.Count);
        }

        [Test]
        public void EmitInternalMessages()
        {
            TraceListenerCounter listTraceListener = new TraceListenerCounter();
            Trace.Listeners.Clear();
            Trace.Listeners.Add(listTraceListener);
            LogLog.Error(GetType(), "Hello");
            LogLog.Error(GetType(), "World");
            Trace.Flush();
            Assert.AreEqual(2, listTraceListener.Count);

            try {
                LogLog.EmitInternalMessages = false;

                LogLog.Error(GetType(), "Hello");
                LogLog.Error(GetType(), "World");
                Assert.AreEqual(2, listTraceListener.Count);
            }
            finally {
                LogLog.EmitInternalMessages = true;
            }
        }

        [Test]
        public void LogReceivedAdapter()
        {
            ArrayList messages = new ArrayList();

            using (new LogLog.LogReceivedAdapter(messages))
            {
                LogLog.Debug(GetType(), "Won't be recorded");
                LogLog.Error(GetType(), "This will be recorded.");
                LogLog.Error(GetType(), "This will be recorded.");
            }

            Assert.AreEqual(2, messages.Count);
        }
    }

    public class TraceListenerCounter : TraceListener
    {
        private int count = 0;

        public override void Write(string message)
        {
            count++;
        }

        public override void WriteLine(string message)
        {
            Write(message);
        }

        public void Reset()
        {
            count = 0;
        }

        public int Count
        {
            get { return count; }
        }
    }
}
