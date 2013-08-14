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

#if NET_4_0
using System.Linq.Expressions;
using System.Reflection;
#endif

namespace log4net.Tests.Util
{
	/// <summary>
	/// Used for internal unit testing the <see cref="SystemInfo"/> class.
	/// </summary>
	[TestFixture]
	public class SystemInfoTest
	{

#if NET_4_0
		/// <summary>
		/// It's "does not throw not supported exception" NOT
		/// "returns 'Dynamic Assembly' string for dynamic assemblies" by purpose.
		/// <see cref="Assembly.GetCallingAssembly"/> can be JITted and inlined in different release configurations,
		/// thus we cannot determine what the exact result of this test will be.
		/// In 'Debug' GetCallingAssembly should return dynamic assembly named: 'Anonymously Hosted DynamicMethods Assembly'
		/// whereas in 'Release' this will be inlined and the result will be something like 'X:\Y\Z\log4net.Tests.dll'.
		/// Therefore simple check against dynamic assembly
		/// in <see cref="SystemInfo.AssemblyLocationInfo"/> to avoid <see cref="NotSupportedException"/> 'Debug' release.
		/// </summary>
		[Test]
		public void TestAssemblyLocationInfoDoesNotThrowNotSupportedExceptionForDynamicAssembly()
		{
			var systemInfoAssemblyLocationMethod = GetAssemblyLocationInfoMethodCall();

			Assert.DoesNotThrow(() => systemInfoAssemblyLocationMethod());
		}

		private static Func<string> GetAssemblyLocationInfoMethodCall()
		{
			var method = typeof(SystemInfoTest).GetMethod("TestAssemblyLocationInfoMethod", new Type[0]);
			var methodCall = Expression.Call(null, method, new Expression[0]);
			return Expression.Lambda<Func<string>>(methodCall, new ParameterExpression[0]).Compile();
		}

		public static string TestAssemblyLocationInfoMethod()
		{
			return SystemInfo.AssemblyLocationInfo(Assembly.GetCallingAssembly());
		}
#endif

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
