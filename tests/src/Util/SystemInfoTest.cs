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

using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Util
{
	/// <summary>
	/// Used for internal unit testing the <see cref="SystemInfo"/> class.
	/// </summary>
	[TestFixture]
	public class SystemInfoTest
	{
		[Test]
		public void TestGetTypeFromStringFullyQualified()
		{
			Type t;

			t = SystemInfo.GetTypeFromString("log4net.Tests.Util.SystemInfoTest,log4net.Tests", false, false);
			Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case sensitive type load");

			t = SystemInfo.GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,log4net.Tests", false, true);
			Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case in-sensitive type load caps");

			t = SystemInfo.GetTypeFromString("log4net.tests.util.systeminfotest,log4net.Tests", false, true);
			Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case in-sensitive type load lower");
		}

                [Test][Platform(Include="Win")]
		public void TestGetTypeFromStringCaseInsensitiveOnAssemblyName()
		{
			Type t;

			t = SystemInfo.GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,LOG4NET.TESTS", false, true);
			Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case in-sensitive type load caps");

			t = SystemInfo.GetTypeFromString("log4net.tests.util.systeminfotest,log4net.tests", false, true);
			Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case in-sensitive type load lower");
		}

		[Test]
		public void TestGetTypeFromStringRelative()
		{
			Type t;

			t = SystemInfo.GetTypeFromString("log4net.Tests.Util.SystemInfoTest", false, false);
			Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case sensitive type load");

			t = SystemInfo.GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST", false, true);
			Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case in-sensitive type load caps");

			t = SystemInfo.GetTypeFromString("log4net.tests.util.systeminfotest", false, true);
			Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case in-sensitive type load lower");
		}

		[Test]
		public void TestGetTypeFromStringSearch()
		{
			Type t;

			t = SystemInfo.GetTypeFromString("log4net.Util.SystemInfo", false, false);
			Assert.AreSame(typeof(SystemInfo), t, "Test explicit case sensitive type load");

			t = SystemInfo.GetTypeFromString("LOG4NET.UTIL.SYSTEMINFO", false, true);
			Assert.AreSame(typeof(SystemInfo), t, "Test explicit case in-sensitive type load caps");

			t = SystemInfo.GetTypeFromString("log4net.util.systeminfo", false, true);
			Assert.AreSame(typeof(SystemInfo), t, "Test explicit case in-sensitive type load lower");
		}

		[Test, ExpectedException(typeof(TypeLoadException))]
		public void TestGetTypeFromStringFails1()
		{
			Type t;

			t = SystemInfo.GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,LOG4NET.TESTS", false, false);
			Assert.AreSame(null, t, "Test explicit case sensitive fails type load");

			t = SystemInfo.GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,LOG4NET.TESTS", true, false);
		}

		[Test, ExpectedException(typeof(TypeLoadException))]
		public void TestGetTypeFromStringFails2()
		{
			Type t;

			t = SystemInfo.GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST", false, false);
			Assert.AreSame(null, t, "Test explicit case sensitive fails type load");

			t = SystemInfo.GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST", true, false);
		}
	}
}
