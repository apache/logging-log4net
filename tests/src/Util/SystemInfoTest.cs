#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
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
using System.Diagnostics;
using System.Globalization;

using log4net.Config;
using log4net.Util;
using log4net.Layout;
using log4net.Core;
using log4net.Appender;
using log4net.Repository;

using log4net.Tests.Appender;

using NUnit.Framework;

namespace log4net.Tests.Util
{
	/// <summary>
	/// Used for internal unit testing the <see cref="SystemInfo"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="SystemInfo"/> class.
	/// </remarks>
	[TestFixture] public class SystemInfoTest
	{
		[Test] public void TestGetTypeFromStringFullyQualified()
		{
			Type t = null;
			
			t = SystemInfo.GetTypeFromString("log4net.Tests.Util.SystemInfoTest,log4net.Tests", false, false);
			Assertion.AssertSame("Test explicit case sensitive type load", typeof(SystemInfoTest), t);

			t = SystemInfo.GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,LOG4NET.TESTS", false, true);
			Assertion.AssertSame("Test explicit case in-sensitive type load caps", typeof(SystemInfoTest), t);

			t = SystemInfo.GetTypeFromString("log4net.tests.util.systeminfotest,log4net.tests", false, true);
			Assertion.AssertSame("Test explicit case in-sensitive type load lower", typeof(SystemInfoTest), t);
		}

		[Test] public void TestGetTypeFromStringRelative()
		{
			Type t = null;
			
			t = SystemInfo.GetTypeFromString("log4net.Tests.Util.SystemInfoTest", false, false);
			Assertion.AssertSame("Test explicit case sensitive type load", typeof(SystemInfoTest), t);

			t = SystemInfo.GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST", false, true);
			Assertion.AssertSame("Test explicit case in-sensitive type load caps", typeof(SystemInfoTest), t);

			t = SystemInfo.GetTypeFromString("log4net.tests.util.systeminfotest", false, true);
			Assertion.AssertSame("Test explicit case in-sensitive type load lower", typeof(SystemInfoTest), t);
		}

		[Test] public void TestGetTypeFromStringSearch()
		{
			Type t = null;
			
			t = SystemInfo.GetTypeFromString("log4net.Util.SystemInfo", false, false);
			Assertion.AssertSame("Test explicit case sensitive type load", typeof(SystemInfo), t);

			t = SystemInfo.GetTypeFromString("LOG4NET.UTIL.SYSTEMINFO", false, true);
			Assertion.AssertSame("Test explicit case in-sensitive type load caps", typeof(SystemInfo), t);

			t = SystemInfo.GetTypeFromString("log4net.util.systeminfo", false, true);
			Assertion.AssertSame("Test explicit case in-sensitive type load lower", typeof(SystemInfo), t);
		}

		[Test, ExpectedException(typeof(TypeLoadException))] public void TestGetTypeFromStringFails1()
		{
			Type t = null;
			
			t = SystemInfo.GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,LOG4NET.TESTS", false, false);
			Assertion.AssertSame("Test explicit case sensitive fails type load", null, t);

			t = SystemInfo.GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,LOG4NET.TESTS", true, false);
		}

		[Test, ExpectedException(typeof(TypeLoadException))] public void TestGetTypeFromStringFails2()
		{
			Type t = null;
			
			t = SystemInfo.GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST", false, false);
			Assertion.AssertSame("Test explicit case sensitive fails type load", null, t);

			t = SystemInfo.GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST", true, false);
		}

	}
}
