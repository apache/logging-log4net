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

using System.Linq.Expressions;
using System.Reflection;

namespace log4net.Tests.Util
{
  /// <summary>
  /// Used for internal unit testing the <see cref="SystemInfo"/> class.
  /// </summary>
  [TestFixture]
  public class SystemInfoTest
  {

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
      var method = typeof(SystemInfoTest).GetMethod(nameof(TestAssemblyLocationInfoMethod), Array.Empty<Type>());
      var methodCall = Expression.Call(null, method!, Array.Empty<Expression>());
      return Expression.Lambda<Func<string>>(methodCall, Array.Empty<ParameterExpression>()).Compile();
    }

    public static string TestAssemblyLocationInfoMethod()
    {
      return SystemInfo.AssemblyLocationInfo(Assembly.GetCallingAssembly());
    }

    [Test]
    public void TestGetTypeFromStringFullyQualified()
    {
      Type? t;

      t = GetTypeFromString("log4net.Tests.Util.SystemInfoTest,log4net.Tests", false, false);
      Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case sensitive type load");

      t = GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,log4net.Tests", false, true);
      Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case in-sensitive type load caps");

      t = GetTypeFromString("log4net.tests.util.systeminfotest,log4net.Tests", false, true);
      Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case in-sensitive type load lower");
    }

    [Test]
    [Platform(Include = "Win")]
    public void TestGetTypeFromStringCaseInsensitiveOnAssemblyName()
    {
      Type? t;

      t = GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,LOG4NET.TESTS", false, true);
      Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case in-sensitive type load caps");

      t = GetTypeFromString("log4net.tests.util.systeminfotest,log4net.tests", false, true);
      Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case in-sensitive type load lower");
    }

    [Test]
    public void TestGetTypeFromStringRelative()
    {
      Type? t;

      t = GetTypeFromString("log4net.Tests.Util.SystemInfoTest", false, false);
      Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case sensitive type load");

      t = GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST", false, true);
      Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case in-sensitive type load caps");

      t = GetTypeFromString("log4net.tests.util.systeminfotest", false, true);
      Assert.AreSame(typeof(SystemInfoTest), t, "Test explicit case in-sensitive type load lower");
    }

    [Test]
    public void TestGetTypeFromStringSearch()
    {
      Type? t;

      t = GetTypeFromString("log4net.Util.SystemInfo", false, false);
      Assert.AreSame(typeof(SystemInfo), t,
                                       string.Format("Test explicit case sensitive type load found {0} rather than {1}",
                                                     t?.AssemblyQualifiedName, typeof(SystemInfo).AssemblyQualifiedName));

      t = GetTypeFromString("LOG4NET.UTIL.SYSTEMINFO", false, true);
      Assert.AreSame(typeof(SystemInfo), t, "Test explicit case in-sensitive type load caps");

      t = GetTypeFromString("log4net.util.systeminfo", false, true);
      Assert.AreSame(typeof(SystemInfo), t, "Test explicit case in-sensitive type load lower");
    }

    [Test]
    public void TestGetTypeFromStringFails1()
    {
      Type? t;

      t = GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,LOG4NET.TESTS", false, false);
      Assert.AreSame(null, t, "Test explicit case sensitive fails type load");

      Assert.Throws<TypeLoadException>(() => GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,LOG4NET.TESTS", true, false));
    }

    [Test]
    public void TestGetTypeFromStringFails2()
    {
      Type? t;

      t = GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST", false, false);
      Assert.AreSame(null, t, "Test explicit case sensitive fails type load");

      Assert.Throws<TypeLoadException>(() => GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST", true, false));
    }

    // Wraps SystemInfo.GetTypeFromString because the method relies on GetCallingAssembly, which is
    // unavailable in CoreFX. As a workaround, only overloads which explicitly take a Type or Assembly
    // are exposed for NETSTANDARD1_3.
    private Type? GetTypeFromString(string typeName, bool throwOnError, bool ignoreCase)
    {
      return SystemInfo.GetTypeFromString(typeName, throwOnError, ignoreCase);
    }

    [Test]
    public void EqualsIgnoringCase_BothNull_true()
    {
      Assert.True(SystemInfo.EqualsIgnoringCase(null, null));
    }

    [Test]
    public void EqualsIgnoringCase_LeftNull_false()
    {
      Assert.False(SystemInfo.EqualsIgnoringCase(null, "foo"));
    }

    [Test]
    public void EqualsIgnoringCase_RightNull_false()
    {
      Assert.False(SystemInfo.EqualsIgnoringCase("foo", null));
    }

    [Test]
    public void EqualsIgnoringCase_SameStringsSameCase_true()
    {
      Assert.True(SystemInfo.EqualsIgnoringCase("foo", "foo"));
    }

    [Test]
    public void EqualsIgnoringCase_SameStringsDifferentCase_true()
    {
      Assert.True(SystemInfo.EqualsIgnoringCase("foo", "FOO"));
    }

    [Test]
    public void EqualsIgnoringCase_DifferentStrings_false()
    {
      Assert.False(SystemInfo.EqualsIgnoringCase("foo", "foobar"));
    }
  }
}
