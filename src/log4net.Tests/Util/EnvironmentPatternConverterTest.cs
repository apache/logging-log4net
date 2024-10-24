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
using System.IO;
using log4net.Util.PatternStringConverters;
using NUnit.Framework;

namespace log4net.Tests.Util;

[TestFixture]
public sealed class EnvironmentPatternConverterTest
{
  private const string EnvironmentVariableName = "LOG4NET_TEST_TEMP";
  private const string SystemLevelValue = "SystemLevelEnvironmentValue";
  private const string UserLevelValue = "UserLevelEnvironmentValue";
  private const string ProcessLevelValue = "ProcessLevelEnvironmentValue";

  /// <summary>
  /// .NET implementations on Unix-like systems only support variables in the process environment block
  /// </summary>
  [Test]
  [Platform(Include = "Win", Reason = @"https://learn.microsoft.com/en-us/dotnet/api/system.environment.setenvironmentvariable")]
  public void SystemLevelEnvironmentVariable()
  {
    EnvironmentPatternConverter converter = new();
    try
    {
      Environment.SetEnvironmentVariable(EnvironmentVariableName, SystemLevelValue, EnvironmentVariableTarget.Machine);
    }
    catch (System.Security.SecurityException)
    {
      Assert.Ignore("Test skipped as current user must not set system level environment variables");
    }

    converter.Option = EnvironmentVariableName;

    using StringWriter sw = new();
    converter.Convert(sw, null);

    Assert.That(sw.ToString(), Is.EqualTo(SystemLevelValue), "System level environment variable not expended correctly.");

    Environment.SetEnvironmentVariable(EnvironmentVariableName, null, EnvironmentVariableTarget.Machine);
  }

  /// <summary>
  /// .NET implementations on Unix-like systems only support variables in the process environment block
  /// </summary>
  [Test]
  [Platform(Include = "Win", Reason = @"https://learn.microsoft.com/en-us/dotnet/api/system.environment.setenvironmentvariable")]
  public void UserLevelEnvironmentVariable()
  {
    EnvironmentPatternConverter converter = new();
    Environment.SetEnvironmentVariable(EnvironmentVariableName, UserLevelValue, EnvironmentVariableTarget.User);

    converter.Option = EnvironmentVariableName;

    using StringWriter sw = new();
    converter.Convert(sw, null);

    Assert.That(sw.ToString(), Is.EqualTo(UserLevelValue), "User level environment variable not expended correctly.");

    Environment.SetEnvironmentVariable(EnvironmentVariableName, null, EnvironmentVariableTarget.User);
  }

  [Test]
  public void ProcessLevelEnvironmentVariable()
  {
    EnvironmentPatternConverter converter = new();
    Environment.SetEnvironmentVariable(EnvironmentVariableName, ProcessLevelValue);

    converter.Option = EnvironmentVariableName;

    using StringWriter sw = new();
    converter.Convert(sw, null);

    Assert.That(sw.ToString(), Is.EqualTo(ProcessLevelValue), "Process level environment variable not expended correctly.");

    Environment.SetEnvironmentVariable(EnvironmentVariableName, null);
  }
}