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
using System.IO;
using System.Text;
using log4net.DateFormatter;
using NUnit.Framework;

namespace log4net.Tests.DateFormatter
{

    [TestFixture]
    public class AbsoluteTimeDateFormatterTest
    {

        [TearDown]
        public void resetCounts()
        {
            FormatterOne.invocations = 0;
        }

        [Test]
        public void CacheWorksForSameTicks()
        {
            StringWriter sw = new StringWriter();
            FormatterOne f1 = new FormatterOne();
            FormatterOne f2 = new FormatterOne();
            DateTime dt = DateTime.Now;
            f1.FormatDate(dt, sw);
            f2.FormatDate(dt, sw);
            Assert.AreEqual(1, FormatterOne.invocations);
        }

        [Test]
        public void CacheWorksForSameSecond()
        {
            StringWriter sw = new StringWriter();
            FormatterOne f1 = new FormatterOne();
            FormatterOne f2 = new FormatterOne();
            DateTime dt1 = DateTime.Today;
            DateTime dt2 = dt1.AddMilliseconds(600);
            f1.FormatDate(dt1, sw);
            f2.FormatDate(dt2, sw);
            Assert.AreEqual(1, FormatterOne.invocations);
        }

        [Test]
        public void CacheExpiresWhenCrossingSecond()
        {
            StringWriter sw = new StringWriter();
            FormatterOne f1 = new FormatterOne();
            FormatterOne f2 = new FormatterOne();
            DateTime dt1 = DateTime.Today.AddMinutes(1);
            DateTime dt2 = dt1.AddMilliseconds(1100);
            f1.FormatDate(dt1, sw);
            f2.FormatDate(dt2, sw);
            Assert.AreEqual(2, FormatterOne.invocations);
        }

        [Test]
        public void CacheIsLocalToSubclass()
        {
            StringWriter sw = new StringWriter();
            FormatterOne f1 = new FormatterOne();
            FormatterTwo f2 = new FormatterTwo();
            DateTime dt1 = DateTime.Today.AddMinutes(10);
            f1.FormatDate(dt1, sw);
            f2.FormatDate(dt1, sw);
            Assert.AreEqual(2, FormatterOne.invocations);
        }
    }

    internal class FormatterOne : AbsoluteTimeDateFormatter
    {
        internal static int invocations = 0;

        override protected void FormatDateWithoutMillis(DateTime dateToFormat,
                                                        StringBuilder buffer)
        {
            invocations++;
        }
        
    }

    internal class FormatterTwo : FormatterOne
    {
    }
}
