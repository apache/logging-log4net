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

using System.Configuration;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace log4net.Tests.Util;

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
    var method = typeof(SystemInfoTest).GetMethod(nameof(TestAssemblyLocationInfoMethod), []);
    var methodCall = Expression.Call(null, method!, []);
    return Expression.Lambda<Func<string>>(methodCall, []).Compile();
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage("Structure", "NUnit1028:The non-test method is public",
    Justification = "Reflection")]
  public static string TestAssemblyLocationInfoMethod()
    => SystemInfo.AssemblyLocationInfo(Assembly.GetCallingAssembly());

  [Test]
  public void TestGetTypeFromStringFullyQualified()
  {
    Type? t = GetTypeFromString("log4net.Tests.Util.SystemInfoTest,log4net.Tests", false, false);
    Assert.That(t, Is.SameAs(typeof(SystemInfoTest)), "Test explicit case sensitive type load");

    t = GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,log4net.Tests", false, true);
    Assert.That(t, Is.SameAs(typeof(SystemInfoTest)), "Test explicit case in-sensitive type load caps");

    t = GetTypeFromString("log4net.tests.util.systeminfotest,log4net.Tests", false, true);
    Assert.That(t, Is.SameAs(typeof(SystemInfoTest)), "Test explicit case in-sensitive type load lower");
  }

  [Test]
  [Platform(Include = "Win")]
  public void TestGetTypeFromStringCaseInsensitiveOnAssemblyName()
  {
    Type? t = GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,LOG4NET.TESTS", false, true);
    Assert.That(t, Is.SameAs(typeof(SystemInfoTest)), "Test explicit case in-sensitive type load caps");

    t = GetTypeFromString("log4net.tests.util.systeminfotest,log4net.tests", false, true);
    Assert.That(t, Is.SameAs(typeof(SystemInfoTest)), "Test explicit case in-sensitive type load lower");
  }

  [Test]
  public void TestGetTypeFromStringRelative()
  {
    Type? t = GetTypeFromString("log4net.Tests.Util.SystemInfoTest", false, false);
    Assert.That(t, Is.SameAs(typeof(SystemInfoTest)), "Test explicit case sensitive type load");

    t = GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST", false, true);
    Assert.That(t, Is.SameAs(typeof(SystemInfoTest)), "Test explicit case in-sensitive type load caps");

    t = GetTypeFromString("log4net.tests.util.systeminfotest", false, true);
    Assert.That(t, Is.SameAs(typeof(SystemInfoTest)), "Test explicit case in-sensitive type load lower");
  }

  [Test]
  public void TestGetTypeFromStringSearch()
  {
    Type? t = GetTypeFromString("log4net.Util.SystemInfo", false, false);
    Assert.That(t, Is.SameAs(typeof(SystemInfo)),
                                     string.Format("Test explicit case sensitive type load found {0} rather than {1}",
                                                   t?.AssemblyQualifiedName, typeof(SystemInfo).AssemblyQualifiedName));

    t = GetTypeFromString("LOG4NET.UTIL.SYSTEMINFO", false, true);
    Assert.That(t, Is.SameAs(typeof(SystemInfo)), "Test explicit case in-sensitive type load caps");

    t = GetTypeFromString("log4net.util.systeminfo", false, true);
    Assert.That(t, Is.SameAs(typeof(SystemInfo)), "Test explicit case in-sensitive type load lower");
  }

  [Test]
  public void TestGetTypeFromStringFails1()
  {
    Type? t = GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,LOG4NET.TESTS", false, false);
    Assert.That(t, Is.Null, "Test explicit case sensitive fails type load");

    Assert.That(() => GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST,LOG4NET.TESTS", true, false),
      Throws.TypeOf<TypeLoadException>());
  }

  [Test]
  public void TestGetTypeFromStringFails2()
  {
    Type? t = GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST", false, false);
    Assert.That(t, Is.Null, "Test explicit case sensitive fails type load");

    Assert.That(() => GetTypeFromString("LOG4NET.TESTS.UTIL.SYSTEMINFOTEST", true, false),
      Throws.TypeOf<TypeLoadException>());
  }

  // Wraps SystemInfo.GetTypeFromString because the method relies on GetCallingAssembly, which is
  // unavailable in CoreFX. As a workaround, only overloads which explicitly take a Type or Assembly
  // are exposed for NETSTANDARD1_3.
  private static Type? GetTypeFromString(string typeName, bool throwOnError, bool ignoreCase)
    => SystemInfo.GetTypeFromString(typeName, throwOnError, ignoreCase);

  [Test]
  public void EqualsIgnoringCase_BothNull_true()
    => Assert.That(SystemInfo.EqualsIgnoringCase(null, null), Is.True);

  [Test]
  public void EqualsIgnoringCase_LeftNull_false()
    => Assert.That(SystemInfo.EqualsIgnoringCase(null, "foo"), Is.False);

  [Test]
  public void EqualsIgnoringCase_RightNull_false()
    => Assert.That(SystemInfo.EqualsIgnoringCase("foo", null), Is.False);

  [Test]
  public void EqualsIgnoringCase_SameStringsSameCase_true()
    => Assert.That(SystemInfo.EqualsIgnoringCase("foo", "foo"), Is.True);

  [Test]
  public void EqualsIgnoringCase_SameStringsDifferentCase_true()
    => Assert.That(SystemInfo.EqualsIgnoringCase("foo", "FOO"), Is.True);

  [Test]
  public void EqualsIgnoringCase_DifferentStrings_false()
    => Assert.That(SystemInfo.EqualsIgnoringCase("foo", "foobar"), Is.False);

  [Test]
  [Platform(Include = "Win,Linux,MacOsX")]
  public void IsAndroid()
    => Assert.That(typeof(SystemInfo).GetProperty("IsAndroid", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null), Is.False);

  /// <summary>
  /// <see cref="SystemInfo.GetAppSetting"/> falls back to environment variables once the
  /// configuration system has failed - which is what happens under Native AOT, where
  /// System.Configuration is trimmed away.
  /// </summary>
  /// <remarks>
  /// <para>
  /// That failure cannot be provoked on a JIT runtime, so the latch that records it is flipped
  /// directly, the same way <see cref="IsAndroid"/> reaches a non-public member. The environment
  /// must stay untouched while the configuration system still works, otherwise a malformed
  /// <c>app.config</c> would silently change where every setting comes from.
  /// </para>
  /// </remarks>
  [Test]
  [NonParallelizable]
  public void GetAppSettingFallsBackToTheEnvironmentOnceConfigurationIsUnavailable()
  {
    const string Key = "log4net.Tests.AppSettingFallback";
    const string Value = "from-the-environment";

    FieldInfo latch = AppSettingsUnavailableLatch();
    bool originalLatch = (bool)latch.GetValue(null)!;
    Environment.SetEnvironmentVariable(Key, Value);
    try
    {
      latch.SetValue(null, false);
      Assert.That(SystemInfo.GetAppSetting(Key), Is.Null);

      latch.SetValue(null, true);
      Assert.That(SystemInfo.GetAppSetting(Key), Is.EqualTo(Value));
    }
    finally
    {
      latch.SetValue(null, originalLatch);
      Environment.SetEnvironmentVariable(Key, null);
    }
  }

  /// <summary>
  /// A key that is missing from the environment as well reads as <see langword="null"/>, so the
  /// fallback leaves callers with the same "no such setting" answer they get from a working
  /// configuration system.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void GetAppSettingReturnsNullForAnUnsetEnvironmentVariable()
  {
    FieldInfo latch = AppSettingsUnavailableLatch();
    bool originalLatch = (bool)latch.GetValue(null)!;
    try
    {
      latch.SetValue(null, true);
      Assert.That(SystemInfo.GetAppSetting("log4net.Tests.NoSuchSettingAnywhere"), Is.Null);
    }
    finally
    {
      latch.SetValue(null, originalLatch);
    }
  }

  /// <summary>
  /// A configuration file that does not parse is reported, not routed to the environment - the
  /// behaviour on every runtime that has a working configuration system is unchanged.
  /// </summary>
  [Test]
  public void MalformedConfigurationIsNotTreatedAsAMissingConfigurationSystem()
    => Assert.That(IsMissingConfigurationSystem(new ConfigurationErrorsException("malformed")), Is.False);

  /// <summary>
  /// Native AOT surfaces a trimmed configuration system as a <see cref="ConfigurationErrorsException"/>,
  /// the same type a malformed file produces, so only the inner exception tells them apart.
  /// </summary>
  [Test]
  public void TrimmedConfigurationSystemIsRecognisedThroughTheInnerException()
    => Assert.That(IsMissingConfigurationSystem(
        new ConfigurationErrorsException("Configuration system failed to initialize",
          new MissingMethodException("No parameterless constructor defined for type 'System.Configuration.ClientConfigurationHost'."))),
      Is.True);

  /// <summary>
  /// A deployment without the System.Configuration.ConfigurationManager assembly fails on the
  /// outermost exception rather than an inner one.
  /// </summary>
  [Test]
  public void MissingConfigurationAssemblyIsRecognised()
    => Assert.That(IsMissingConfigurationSystem(new FileNotFoundException("System.Configuration.ConfigurationManager")), Is.True);

  private static bool IsMissingConfigurationSystem(Exception exception)
  {
    MethodInfo method = typeof(SystemInfo).GetMethod("IsMissingConfigurationSystem", BindingFlags.Static | BindingFlags.NonPublic)
      ?? throw new InvalidOperationException("SystemInfo.IsMissingConfigurationSystem no longer exists - update this test along with it.");
    return (bool)method.Invoke(null, [exception])!;
  }

  private static FieldInfo AppSettingsUnavailableLatch()
    => typeof(SystemInfo).GetField("_configurationSystemUnavailable", BindingFlags.Static | BindingFlags.NonPublic)
      ?? throw new InvalidOperationException("SystemInfo._configurationSystemUnavailable no longer exists - update this test along with it.");
}