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
using System.Globalization;
using System.IO;
using System.Text;
using log4net.DateFormatter;
using NUnit.Framework;

namespace log4net.Tests.DateFormatter
{
  [TestFixture]
  public class DateTimeDateFormatterTest
  {
    [Test]
    public void TestFormattingResults()
    {
      var formatter = new DateTimeDateFormatter();
      var sb = new StringBuilder();
      using var writer = new StringWriter(sb, CultureInfo.InvariantCulture);

      // Tests for prepended 0 characters for 2-digit and 3-digit portions.
      formatter.FormatDate(new DateTime(1970, 1, 1, 1, 1, 1).AddMilliseconds(1), writer);
      Assert.AreEqual("01 Jan 1970 01:01:01,001", sb.ToString());
      sb.Clear();

      // Non-zero-prepend case.
      formatter.FormatDate(new DateTime(2100, 12, 30, 11, 59, 59).AddMilliseconds(100), writer);
      Assert.AreEqual("30 Dec 2100 11:59:59,100", sb.ToString());
      sb.Clear();
    }
  }
}
