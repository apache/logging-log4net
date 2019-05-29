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
using log4net.Util;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using System.Configuration;
using System.Globalization;
using System.Diagnostics;

namespace log4net.Tests.Util
{
	[TestFixture]
	public class PatternStringTest : MarshalByRefObject
	{
		#region Test EnvironmentFolderPathPatternConverter
		[Test]
		public void TestEnvironmentFolderPathPatternConverter()
		{
			string[] specialFolderNames = Enum.GetNames(typeof(Environment.SpecialFolder));

			foreach (string specialFolderName in specialFolderNames)
			{
				string pattern = "%envFolderPath{" + specialFolderName + "}";

				PatternString patternString = new PatternString(pattern);

				string evaluatedPattern = patternString.Format();

				Environment.SpecialFolder specialFolder =
					(Environment.SpecialFolder)Enum.Parse(typeof(Environment.SpecialFolder), specialFolderName);

				Assert.AreEqual(Environment.GetFolderPath(specialFolder), evaluatedPattern);
			}
		}
		#endregion Test EnvironmentFolderPathPatternConverter

		#region Test AppSettingPathConverter
		[Test]
		public void TestAppSettingPathConverter()
		{
			string configurationFileContent = @"
<configuration>
  <appSettings>
	<add key=""TestKey"" value = ""TestValue"" />
  </appSettings>
</configuration>
";
			string configurationFileName = null;
			AppDomain appDomain = null;
			try
			{
				configurationFileName = CreateTempConfigFile(configurationFileContent);
				appDomain = CreateConfiguredDomain("AppSettingsTestDomain", configurationFileName);

				PatternStringTest pst = (PatternStringTest)appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, this.GetType().FullName);
				pst.TestAppSettingPathConverterInConfiguredDomain();
			}
			finally
			{
				if (appDomain != null) AppDomain.Unload(appDomain);
				if (configurationFileName != null) File.Delete(configurationFileName);
			}
		}

		public void TestAppSettingPathConverterInConfiguredDomain()
		{
			string pattern = "%appSetting{TestKey}";
			PatternString patternString = new PatternString(pattern);
			string evaluatedPattern = patternString.Format();
			string appSettingValue = ConfigurationManager.AppSettings["TestKey"];
			Assert.AreEqual("TestValue", appSettingValue, "Expected configuration file to contain a key TestKey with the value TestValue");
			Assert.AreEqual(appSettingValue, evaluatedPattern, "Evaluated pattern expected to be identical to appSetting value");

			string badPattern = "%appSetting{UnknownKey}";
			patternString = new PatternString(badPattern);
			evaluatedPattern = patternString.Format();
			Assert.AreEqual("(null)", evaluatedPattern, "Evaluated pattern expected to be \"(null)\" for non-existent appSettings key");
		}

		private static string CreateTempConfigFile(string configurationFileContent)
		{
			string fileName = Path.GetTempFileName();
			File.WriteAllText(fileName, configurationFileContent);
			return fileName;
		}

		private static AppDomain CreateConfiguredDomain(string domainName, string configurationFileName)
		{
			AppDomainSetup ads = new AppDomainSetup();
			ads.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
			ads.ConfigurationFile = configurationFileName;
			AppDomain ad = AppDomain.CreateDomain(domainName, null, ads);
			return ad;
		}
		#endregion Test AppSettingPathConverter

		#region Test ThreadIdPatternConverter
		[Test]
		public void TestThreadIdPatternConverter()
		{
			// Arrange:
			string pattern = "%thread";
			PatternString patternString = new PatternString(pattern);

			string threadId = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture);

			// Act:
			string evaluatedPattern = patternString.Format();


			// Assert:
			Assert.AreEqual(threadId, evaluatedPattern, "Evaluated pattern expected to be identical to Thread.CurrentThread.ManagedThreadId value");
		}
		#endregion Test ThreadIdPatternConverter


		#region Test PatternString.CreateForLogLog()
		[Test]
		public void TestNormalPatternStringWithLogLogExtraPatterns()
		{
			// Arrange:
			string pattern = "%date{yyyy-MM-dd} [%7processid][%3thread][%appdomain] %level - %logger - %message%newline";
			PatternString patternString = new PatternString(pattern);

			LogLog logObj = new LogLog(GetType(), "myInfoPrefix", "Hello world!", null);

			// Act:
			string evaluatedPattern = patternString.FormatWithState(logObj);

			string expectedOutput = string.Format(
				CultureInfo.InvariantCulture,
				"{0:yyyy-MM-dd} [{1,7}][{2,3}][{3}] level - logger - message{4}",
				DateTime.Now,
				Process.GetCurrentProcess().Id,
				System.Threading.Thread.CurrentThread.ManagedThreadId,
				AppDomain.CurrentDomain.FriendlyName,
				SystemInfo.NewLine
				);

			// Assert:
			Assert.AreEqual(expectedOutput, evaluatedPattern, $"Evaluated pattern expected to be identical to value: {expectedOutput}");
		}

		[Test]
		public void TestPatternStringCreateForLogLog()
		{
			// Arrange:
			string pattern = "%date{yyyy-MM-dd} [%7processid][%3thread][%appdomain] %level - %logger - %message%newline";
			PatternString patternString = PatternString.CreateForLogLog(pattern);

			LogLog logObj = new LogLog(GetType(), "myInfoPrefix", "Hello world!", null);

			// Act:
			string evaluatedPattern = patternString.FormatWithState(logObj);

			string expectedOutput = string.Format(
				CultureInfo.InvariantCulture,
				"{0:yyyy-MM-dd} [{1,7}][{2,3}][{3}] {4} - {5} - {6}{7}",
				DateTime.Now,
				Process.GetCurrentProcess().Id,
				System.Threading.Thread.CurrentThread.ManagedThreadId,
				AppDomain.CurrentDomain.FriendlyName,
				logObj.Prefix,
				logObj.Source.FullName,
				logObj.Message,
				SystemInfo.NewLine
				);

			// Assert:
			Assert.AreEqual(expectedOutput, evaluatedPattern, $"Evaluated pattern expected to be identical to value: {expectedOutput}");
		}

		[Test]
		public void TestPatternStringCreateForLogLogWithExceptionAsParam()
		{
			// Arrange:
			string pattern = "%date{yyyy-MM-dd} [%7processid][%3thread][%appdomain] %level - %logger - %message%newline";
			PatternString patternString = PatternString.CreateForLogLog(pattern);

			Exception testException = new Exception("Test Exception");
			LogLog logObj = new LogLog(GetType(), "myInfoPrefix", "Hello world!", testException);

			// Act:
			string evaluatedPattern = patternString.FormatWithState(logObj);

			//NOTE: current PatternString.CreateForLogLog() does not support exception formatting. In LogLog the whole exception is output on a next line after the LogLog instance traced content.
			//      For now I do not see the necessity fot this. If really needed, it could be made analogous to ExceptionPatternConverter.
			string expectedOutput = string.Format(
				CultureInfo.InvariantCulture,
				//"{0:yyyy-MM-dd} [{1,7}][{2,3}][{3}] {4} - {5} - {6}{7}{8}{9}",
				"{0:yyyy-MM-dd} [{1,7}][{2,3}][{3}] {4} - {5} - {6}{7}",
				DateTime.Now,
				Process.GetCurrentProcess().Id,
				System.Threading.Thread.CurrentThread.ManagedThreadId,
				AppDomain.CurrentDomain.FriendlyName,
				logObj.Prefix,
				logObj.Source?.FullName,
				logObj.Message,
				//SystemInfo.NewLine,
				//testException,
				SystemInfo.NewLine
				);

			// Assert:
			Assert.AreEqual(expectedOutput, evaluatedPattern, $"Evaluated pattern expected to be identical to value: {expectedOutput}");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestPatternStringCreateForLogLogWithNullFormatStateParameter()
		{
			// Arrange:
			string pattern = "%date{yyyy-MM-dd} [%7processid][%3thread][%appdomain] %level - %logger - %message%newline";
			PatternString patternString = PatternString.CreateForLogLog(pattern);

			// Act:
			string evaluatedPattern = patternString.FormatWithState(null);

			// Assert: - expect that an Exception was thrown
			Assert.Fail("Expected Exception was not thrown"); // If it gets to this line, no exception was thrown
		}

		[Test]
		public void TestPatternStringCreateForLogLogWithNullLogLogObjectContent()
		{
			// Arrange:
			string pattern = "%date{yyyy-MM-dd} [%7processid][%3thread][%appdomain] %level - %logger - %message%newline";
			PatternString patternString = PatternString.CreateForLogLog(pattern);

			LogLog logObj = new LogLog(null, null, null, null);

			// Act:
			string evaluatedPattern = patternString.FormatWithState(logObj);

			string expectedOutput = string.Format(
				CultureInfo.InvariantCulture,
				"{0:yyyy-MM-dd} [{1,7}][{2,3}][{3}]  -  - {4}",
				DateTime.Now,
				Process.GetCurrentProcess().Id,
				System.Threading.Thread.CurrentThread.ManagedThreadId,
				AppDomain.CurrentDomain.FriendlyName,
				SystemInfo.NewLine
				);

			// Assert:
			Assert.AreEqual(expectedOutput, evaluatedPattern, $"Evaluated pattern expected to be identical to value: {expectedOutput}");
		}

		[Test]
		public void TestPatternStringCreateForLogLogWithWrongFormatStateParameter()
		{
			// Arrange:
			string pattern = "%date{yyyy-MM-dd} [%7processid][%3thread][%appdomain] %level - %logger - %message%newline";
			PatternString patternString = PatternString.CreateForLogLog(pattern);

			// Act:
			try
			{
				string evaluatedPattern = patternString.FormatWithState("Providing wrong (string) object here as state param, instead of LogLog instance for test sake.");

				// Assert: - expect that an Exception was thrown
				Assert.Fail("Expected Exception was not thrown"); // If it gets to this line, no exception was thrown
			}
			catch (ArgumentException ex)
			{
				// Assert:
				Assert.AreEqual("state must be of type [log4net.Util.LogLog]\r\nParametername: state", ex.Message);
			}
		}
		#endregion Test ThreadIdPatternConverter
	}
}
