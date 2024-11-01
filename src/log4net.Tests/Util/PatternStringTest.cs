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

#if NET462_OR_GREATER

using System;
using log4net.Util;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using System.Configuration;

namespace log4net.Tests.Util;

[TestFixture]
public class PatternStringTest : MarshalByRefObject
{
  [Test]
  public void TestEnvironmentFolderPathPatternConverter()
  {
    string[] specialFolderNames = Enum.GetNames(typeof(Environment.SpecialFolder));

    foreach (string specialFolderName in specialFolderNames)
    {
      string pattern = "%envFolderPath{" + specialFolderName + "}";

      PatternString patternString = new(pattern);

      string evaluatedPattern = patternString.Format();

      Environment.SpecialFolder specialFolder =
          (Environment.SpecialFolder)Enum.Parse(typeof(Environment.SpecialFolder), specialFolderName);

      Assert.That(evaluatedPattern, Is.EqualTo(Environment.GetFolderPath(specialFolder)));
    }
  }

  [Test]
  public void TestAppSettingPathConverter()
  {
    const string configurationFileContent = 
      """
      <configuration>
        <appSettings>
          <add key="TestKey" value = "TestValue" />
        </appSettings>
      </configuration>
      """;
    string? configurationFileName = null;
    AppDomain? appDomain = null;
    try
    {
      configurationFileName = CreateTempConfigFile(configurationFileContent);
      appDomain = CreateConfiguredDomain("AppSettingsTestDomain", configurationFileName);

      PatternStringTest pst = (PatternStringTest)appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, this.GetType().FullName);
      pst.TestAppSettingPathConverterInConfiguredDomain();
    }
    finally
    {
      if (appDomain is not null)
      {
        AppDomain.Unload(appDomain);
      }
      if (configurationFileName is not null)
      {
        File.Delete(configurationFileName);
      }
    }
  }

  private void TestAppSettingPathConverterInConfiguredDomain()
  {
    _ = this; // force instance method for AppDomain-Call
    string pattern = "%appSetting{TestKey}";
    PatternString patternString = new(pattern);
    string evaluatedPattern = patternString.Format();
    string appSettingValue = ConfigurationManager.AppSettings["TestKey"];
    Assert.That(appSettingValue, Is.EqualTo("TestValue"), 
      "Expected configuration file to contain a key TestKey with the value TestValue");
    Assert.That(evaluatedPattern, Is.EqualTo(appSettingValue), 
      "Evaluated pattern expected to be identical to appSetting value");

    string badPattern = "%appSetting{UnknownKey}";
    patternString = new PatternString(badPattern);
    evaluatedPattern = patternString.Format();
    Assert.That(evaluatedPattern, Is.EqualTo("(null)"), 
      "Evaluated pattern expected to be \"(null)\" for non-existent appSettings key");
  }

  private static string CreateTempConfigFile(string configurationFileContent)
  {
    string fileName = Path.GetTempFileName();
    File.WriteAllText(fileName, configurationFileContent);
    return fileName;
  }

  private static AppDomain CreateConfiguredDomain(string domainName, string configurationFileName)
  {
    AppDomainSetup ads = new()
    {
      ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
      ConfigurationFile = configurationFileName
    };
    return AppDomain.CreateDomain(domainName, null, ads);
  }
}
#endif