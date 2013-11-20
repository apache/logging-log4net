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

using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Tests.Layout;

using NUnit.Framework;

namespace log4net.Tests.Core
{
	/// <summary>
	/// Used for internal unit testing the <see cref="PatternLayoutTest"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="PatternLayoutTest"/> class.
	/// </remarks>
	[TestFixture]
	public class StringFormatTest
	{
		private CultureInfo _currentCulture;
		private CultureInfo _currentUICulture;

		[SetUp]
		public void SetUp()
		{
			// set correct thread culture
			_currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
			_currentUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
		}

		[TearDown]
		public void TearDown()
		{
			// restore previous culture
			System.Threading.Thread.CurrentThread.CurrentCulture = _currentCulture;
			System.Threading.Thread.CurrentThread.CurrentUICulture = _currentUICulture;
		}

		[Test]
		public void TestFormatString()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%message");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestFormatString");

			// ***
			log1.Info("TestMessage");
			Assert.AreEqual("TestMessage", stringAppender.GetString(), "Test simple INFO event");
			stringAppender.Reset();


			// ***
			log1.DebugFormat("Before {0} After", "Middle");
			Assert.AreEqual("Before Middle After", stringAppender.GetString(), "Test simple formatted DEBUG event");
			stringAppender.Reset();

			// ***
			log1.InfoFormat("Before {0} After", "Middle");
			Assert.AreEqual("Before Middle After", stringAppender.GetString(), "Test simple formatted INFO event");
			stringAppender.Reset();

			// ***
			log1.WarnFormat("Before {0} After", "Middle");
			Assert.AreEqual("Before Middle After", stringAppender.GetString(), "Test simple formatted WARN event");
			stringAppender.Reset();

			// ***
			log1.ErrorFormat("Before {0} After", "Middle");
			Assert.AreEqual("Before Middle After", stringAppender.GetString(), "Test simple formatted ERROR event");
			stringAppender.Reset();

			// ***
			log1.FatalFormat("Before {0} After", "Middle");
			Assert.AreEqual("Before Middle After", stringAppender.GetString(), "Test simple formatted FATAL event");
			stringAppender.Reset();


			// ***
			log1.InfoFormat("Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("Before Middle After End", stringAppender.GetString(), "Test simple formatted INFO event 2");
			stringAppender.Reset();

			// ***
			log1.InfoFormat("IGNORE THIS WARNING - EXCEPTION EXPECTED Before {0} After {1} {2}", "Middle", "End");
			Assert.AreEqual(STRING_FORMAT_ERROR, stringAppender.GetString(), "Test formatting error");
			stringAppender.Reset();
		}

		private const string STRING_FORMAT_ERROR = "<log4net.Error>Exception during StringFormat: Index (zero based) must be greater than or equal to zero and less than the size of the argument list. <format>IGNORE THIS WARNING - EXCEPTION EXPECTED Before {0} After {1} {2}</format><args>{Middle, End}</args></log4net.Error>";


		[Test]
		public void TestLogFormatApi_Debug()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%level:%message");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Debug");

			// ***
			log1.Debug("TestMessage");
			Assert.AreEqual("DEBUG:TestMessage", stringAppender.GetString(), "Test simple DEBUG event 1");
			stringAppender.Reset();

			// ***
			log1.Debug("TestMessage", null);
			Assert.AreEqual("DEBUG:TestMessage", stringAppender.GetString(), "Test simple DEBUG event 2");
			stringAppender.Reset();

			// ***
			log1.Debug("TestMessage", new Exception("Exception message"));
			Assert.AreEqual("DEBUG:TestMessageSystem.Exception: Exception message" + Environment.NewLine, stringAppender.GetString(), "Test simple DEBUG event 3");
			stringAppender.Reset();

			// ***
			log1.DebugFormat("a{0}", "1");
			Assert.AreEqual("DEBUG:a1", stringAppender.GetString(), "Test formatted DEBUG event with 1 parm");
			stringAppender.Reset();

			// ***
			log1.DebugFormat("a{0}b{1}", "1", "2");
			Assert.AreEqual("DEBUG:a1b2", stringAppender.GetString(), "Test formatted DEBUG event with 2 parm");
			stringAppender.Reset();

			// ***
			log1.DebugFormat("a{0}b{1}c{2}", "1", "2", "3");
			Assert.AreEqual("DEBUG:a1b2c3", stringAppender.GetString(), "Test formatted DEBUG event with 3 parm");
			stringAppender.Reset();


			// ***
			log1.DebugFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
			Assert.AreEqual("DEBUG:aQbWcEdReTf", stringAppender.GetString(), "Test formatted DEBUG event with 5 parms (only 4 used)");
			stringAppender.Reset();

			// ***
			log1.DebugFormat(null, "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("DEBUG:Before Middle After End", stringAppender.GetString(), "Test formatting with null provider");
			stringAppender.Reset();

			// ***
			log1.DebugFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("DEBUG:Before Middle After End", stringAppender.GetString(), "Test formatting with 'en' provider");
			stringAppender.Reset();
		}

		[Test]
		public void TestLogFormatApi_NoDebug()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Threshold = Level.Info;
			stringAppender.Layout = new PatternLayout("%level:%message");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Debug");

			// ***
			log1.Debug("TestMessage");
			Assert.AreEqual("", stringAppender.GetString(), "Test simple DEBUG event 1");
			stringAppender.Reset();

			// ***
			log1.Debug("TestMessage", null);
			Assert.AreEqual("", stringAppender.GetString(), "Test simple DEBUG event 2");
			stringAppender.Reset();

			// ***
			log1.Debug("TestMessage", new Exception("Exception message"));
			Assert.AreEqual("", stringAppender.GetString(), "Test simple DEBUG event 3");
			stringAppender.Reset();

			// ***
			log1.DebugFormat("a{0}", "1");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted DEBUG event with 1 parm");
			stringAppender.Reset();

			// ***
			log1.DebugFormat("a{0}b{1}", "1", "2");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted DEBUG event with 2 parm");
			stringAppender.Reset();

			// ***
			log1.DebugFormat("a{0}b{1}c{2}", "1", "2", "3");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted DEBUG event with 3 parm");
			stringAppender.Reset();


			// ***
			log1.DebugFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted DEBUG event with 5 parms (only 4 used)");
			stringAppender.Reset();

			// ***
			log1.DebugFormat(null, "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatting with null provider");
			stringAppender.Reset();

			// ***
			log1.DebugFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatting with 'en' provider");
			stringAppender.Reset();
		}


		[Test]
		public void TestLogFormatApi_Info()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%level:%message");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Info");

			// ***
			log1.Info("TestMessage");
			Assert.AreEqual("INFO:TestMessage", stringAppender.GetString(), "Test simple INFO event 1");
			stringAppender.Reset();

			// ***
			log1.Info("TestMessage", null);
			Assert.AreEqual("INFO:TestMessage", stringAppender.GetString(), "Test simple INFO event 2");
			stringAppender.Reset();

			// ***
			log1.Info("TestMessage", new Exception("Exception message"));
			Assert.AreEqual("INFO:TestMessageSystem.Exception: Exception message" + Environment.NewLine, stringAppender.GetString(), "Test simple INFO event 3");
			stringAppender.Reset();

			// ***
			log1.InfoFormat("a{0}", "1");
			Assert.AreEqual("INFO:a1", stringAppender.GetString(), "Test formatted INFO event with 1 parm");
			stringAppender.Reset();

			// ***
			log1.InfoFormat("a{0}b{1}", "1", "2");
			Assert.AreEqual("INFO:a1b2", stringAppender.GetString(), "Test formatted INFO event with 2 parm");
			stringAppender.Reset();

			// ***
			log1.InfoFormat("a{0}b{1}c{2}", "1", "2", "3");
			Assert.AreEqual("INFO:a1b2c3", stringAppender.GetString(), "Test formatted INFO event with 3 parm");
			stringAppender.Reset();


			// ***
			log1.InfoFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
			Assert.AreEqual("INFO:aQbWcEdReTf", stringAppender.GetString(), "Test formatted INFO event with 5 parms (only 4 used)");
			stringAppender.Reset();

			// ***
			log1.InfoFormat(null, "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("INFO:Before Middle After End", stringAppender.GetString(), "Test formatting with null provider");
			stringAppender.Reset();

			// ***
			log1.InfoFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("INFO:Before Middle After End", stringAppender.GetString(), "Test formatting with 'en' provider");
			stringAppender.Reset();
		}

		[Test]
		public void TestLogFormatApi_NoInfo()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Threshold = Level.Warn;
			stringAppender.Layout = new PatternLayout("%level:%message");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Info");

			// ***
			log1.Info("TestMessage");
			Assert.AreEqual("", stringAppender.GetString(), "Test simple INFO event 1");
			stringAppender.Reset();

			// ***
			log1.Info("TestMessage", null);
			Assert.AreEqual("", stringAppender.GetString(), "Test simple INFO event 2");
			stringAppender.Reset();

			// ***
			log1.Info("TestMessage", new Exception("Exception message"));
			Assert.AreEqual("", stringAppender.GetString(), "Test simple INFO event 3");
			stringAppender.Reset();

			// ***
			log1.InfoFormat("a{0}", "1");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted INFO event with 1 parm");
			stringAppender.Reset();

			// ***
			log1.InfoFormat("a{0}b{1}", "1", "2");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted INFO event with 2 parm");
			stringAppender.Reset();

			// ***
			log1.InfoFormat("a{0}b{1}c{2}", "1", "2", "3");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted INFO event with 3 parm");
			stringAppender.Reset();


			// ***
			log1.InfoFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted INFO event with 5 parms (only 4 used)");
			stringAppender.Reset();

			// ***
			log1.InfoFormat(null, "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatting with null provider");
			stringAppender.Reset();

			// ***
			log1.InfoFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatting with 'en' provider");
			stringAppender.Reset();
		}


		[Test]
		public void TestLogFormatApi_Warn()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%level:%message");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Warn");

			// ***
			log1.Warn("TestMessage");
			Assert.AreEqual("WARN:TestMessage", stringAppender.GetString(), "Test simple WARN event 1");
			stringAppender.Reset();

			// ***
			log1.Warn("TestMessage", null);
			Assert.AreEqual("WARN:TestMessage", stringAppender.GetString(), "Test simple WARN event 2");
			stringAppender.Reset();

			// ***
			log1.Warn("TestMessage", new Exception("Exception message"));
			Assert.AreEqual("WARN:TestMessageSystem.Exception: Exception message" + Environment.NewLine, stringAppender.GetString(), "Test simple WARN event 3");
			stringAppender.Reset();

			// ***
			log1.WarnFormat("a{0}", "1");
			Assert.AreEqual("WARN:a1", stringAppender.GetString(), "Test formatted WARN event with 1 parm");
			stringAppender.Reset();

			// ***
			log1.WarnFormat("a{0}b{1}", "1", "2");
			Assert.AreEqual("WARN:a1b2", stringAppender.GetString(), "Test formatted WARN event with 2 parm");
			stringAppender.Reset();

			// ***
			log1.WarnFormat("a{0}b{1}c{2}", "1", "2", "3");
			Assert.AreEqual("WARN:a1b2c3", stringAppender.GetString(), "Test formatted WARN event with 3 parm");
			stringAppender.Reset();


			// ***
			log1.WarnFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
			Assert.AreEqual("WARN:aQbWcEdReTf", stringAppender.GetString(), "Test formatted WARN event with 5 parms (only 4 used)");
			stringAppender.Reset();

			// ***
			log1.WarnFormat(null, "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("WARN:Before Middle After End", stringAppender.GetString(), "Test formatting with null provider");
			stringAppender.Reset();

			// ***
			log1.WarnFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("WARN:Before Middle After End", stringAppender.GetString(), "Test formatting with 'en' provider");
			stringAppender.Reset();
		}

		[Test]
		public void TestLogFormatApi_NoWarn()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Threshold = Level.Error;
			stringAppender.Layout = new PatternLayout("%level:%message");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Warn");

			// ***
			log1.Warn("TestMessage");
			Assert.AreEqual("", stringAppender.GetString(), "Test simple WARN event 1");
			stringAppender.Reset();

			// ***
			log1.Warn("TestMessage", null);
			Assert.AreEqual("", stringAppender.GetString(), "Test simple WARN event 2");
			stringAppender.Reset();

			// ***
			log1.Warn("TestMessage", new Exception("Exception message"));
			Assert.AreEqual("", stringAppender.GetString(), "Test simple WARN event 3");
			stringAppender.Reset();

			// ***
			log1.WarnFormat("a{0}", "1");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted WARN event with 1 parm");
			stringAppender.Reset();

			// ***
			log1.WarnFormat("a{0}b{1}", "1", "2");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted WARN event with 2 parm");
			stringAppender.Reset();

			// ***
			log1.WarnFormat("a{0}b{1}c{2}", "1", "2", "3");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted WARN event with 3 parm");
			stringAppender.Reset();


			// ***
			log1.WarnFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted WARN event with 5 parms (only 4 used)");
			stringAppender.Reset();

			// ***
			log1.WarnFormat(null, "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatting with null provider");
			stringAppender.Reset();

			// ***
			log1.WarnFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatting with 'en' provider");
			stringAppender.Reset();
		}


		[Test]
		public void TestLogFormatApi_Error()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%level:%message");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Error");

			// ***
			log1.Error("TestMessage");
			Assert.AreEqual("ERROR:TestMessage", stringAppender.GetString(), "Test simple ERROR event 1");
			stringAppender.Reset();

			// ***
			log1.Error("TestMessage", null);
			Assert.AreEqual("ERROR:TestMessage", stringAppender.GetString(), "Test simple ERROR event 2");
			stringAppender.Reset();

			// ***
			log1.Error("TestMessage", new Exception("Exception message"));
			Assert.AreEqual("ERROR:TestMessageSystem.Exception: Exception message" + Environment.NewLine, stringAppender.GetString(), "Test simple ERROR event 3");
			stringAppender.Reset();

			// ***
			log1.ErrorFormat("a{0}", "1");
			Assert.AreEqual("ERROR:a1", stringAppender.GetString(), "Test formatted ERROR event with 1 parm");
			stringAppender.Reset();

			// ***
			log1.ErrorFormat("a{0}b{1}", "1", "2");
			Assert.AreEqual("ERROR:a1b2", stringAppender.GetString(), "Test formatted ERROR event with 2 parm");
			stringAppender.Reset();

			// ***
			log1.ErrorFormat("a{0}b{1}c{2}", "1", "2", "3");
			Assert.AreEqual("ERROR:a1b2c3", stringAppender.GetString(), "Test formatted ERROR event with 3 parm");
			stringAppender.Reset();


			// ***
			log1.ErrorFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
			Assert.AreEqual("ERROR:aQbWcEdReTf", stringAppender.GetString(), "Test formatted ERROR event with 5 parms (only 4 used)");
			stringAppender.Reset();

			// ***
			log1.ErrorFormat(null, "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("ERROR:Before Middle After End", stringAppender.GetString(), "Test formatting with null provider");
			stringAppender.Reset();

			// ***
			log1.ErrorFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("ERROR:Before Middle After End", stringAppender.GetString(), "Test formatting with 'en' provider");
			stringAppender.Reset();
		}

		[Test]
		public void TestLogFormatApi_NoError()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Threshold = Level.Fatal;
			stringAppender.Layout = new PatternLayout("%level:%message");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Error");

			// ***
			log1.Error("TestMessage");
			Assert.AreEqual("", stringAppender.GetString(), "Test simple ERROR event 1");
			stringAppender.Reset();

			// ***
			log1.Error("TestMessage", null);
			Assert.AreEqual("", stringAppender.GetString(), "Test simple ERROR event 2");
			stringAppender.Reset();

			// ***
			log1.Error("TestMessage", new Exception("Exception message"));
			Assert.AreEqual("", stringAppender.GetString(), "Test simple ERROR event 3");
			stringAppender.Reset();

			// ***
			log1.ErrorFormat("a{0}", "1");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted ERROR event with 1 parm");
			stringAppender.Reset();

			// ***
			log1.ErrorFormat("a{0}b{1}", "1", "2");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted ERROR event with 2 parm");
			stringAppender.Reset();

			// ***
			log1.ErrorFormat("a{0}b{1}c{2}", "1", "2", "3");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted ERROR event with 3 parm");
			stringAppender.Reset();


			// ***
			log1.ErrorFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted ERROR event with 5 parms (only 4 used)");
			stringAppender.Reset();

			// ***
			log1.ErrorFormat(null, "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatting with null provider");
			stringAppender.Reset();

			// ***
			log1.ErrorFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatting with 'en' provider");
			stringAppender.Reset();
		}


		[Test]
		public void TestLogFormatApi_Fatal()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%level:%message");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Fatal");

			// ***
			log1.Fatal("TestMessage");
			Assert.AreEqual("FATAL:TestMessage", stringAppender.GetString(), "Test simple FATAL event 1");
			stringAppender.Reset();

			// ***
			log1.Fatal("TestMessage", null);
			Assert.AreEqual("FATAL:TestMessage", stringAppender.GetString(), "Test simple FATAL event 2");
			stringAppender.Reset();

			// ***
			log1.Fatal("TestMessage", new Exception("Exception message"));
			Assert.AreEqual("FATAL:TestMessageSystem.Exception: Exception message" + Environment.NewLine, stringAppender.GetString(), "Test simple FATAL event 3");
			stringAppender.Reset();

			// ***
			log1.FatalFormat("a{0}", "1");
			Assert.AreEqual("FATAL:a1", stringAppender.GetString(), "Test formatted FATAL event with 1 parm");
			stringAppender.Reset();

			// ***
			log1.FatalFormat("a{0}b{1}", "1", "2");
			Assert.AreEqual("FATAL:a1b2", stringAppender.GetString(), "Test formatted FATAL event with 2 parm");
			stringAppender.Reset();

			// ***
			log1.FatalFormat("a{0}b{1}c{2}", "1", "2", "3");
			Assert.AreEqual("FATAL:a1b2c3", stringAppender.GetString(), "Test formatted FATAL event with 3 parm");
			stringAppender.Reset();


			// ***
			log1.FatalFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
			Assert.AreEqual("FATAL:aQbWcEdReTf", stringAppender.GetString(), "Test formatted FATAL event with 5 parms (only 4 used)");
			stringAppender.Reset();

			// ***
			log1.FatalFormat(null, "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("FATAL:Before Middle After End", stringAppender.GetString(), "Test formatting with null provider");
			stringAppender.Reset();

			// ***
			log1.FatalFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("FATAL:Before Middle After End", stringAppender.GetString(), "Test formatting with 'en' provider");
			stringAppender.Reset();
		}

		[Test]
		public void TestLogFormatApi_NoFatal()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Threshold = Level.Off;
			stringAppender.Layout = new PatternLayout("%level:%message");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Fatal");

			// ***
			log1.Fatal("TestMessage");
			Assert.AreEqual("", stringAppender.GetString(), "Test simple FATAL event 1");
			stringAppender.Reset();

			// ***
			log1.Fatal("TestMessage", null);
			Assert.AreEqual("", stringAppender.GetString(), "Test simple FATAL event 2");
			stringAppender.Reset();

			// ***
			log1.Fatal("TestMessage", new Exception("Exception message"));
			Assert.AreEqual("", stringAppender.GetString(), "Test simple FATAL event 3");
			stringAppender.Reset();

			// ***
			log1.FatalFormat("a{0}", "1");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted FATAL event with 1 parm");
			stringAppender.Reset();

			// ***
			log1.FatalFormat("a{0}b{1}", "1", "2");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted FATAL event with 2 parm");
			stringAppender.Reset();

			// ***
			log1.FatalFormat("a{0}b{1}c{2}", "1", "2", "3");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted FATAL event with 3 parm");
			stringAppender.Reset();


			// ***
			log1.FatalFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatted FATAL event with 5 parms (only 4 used)");
			stringAppender.Reset();

			// ***
			log1.FatalFormat(null, "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatting with null provider");
			stringAppender.Reset();

			// ***
			log1.FatalFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("", stringAppender.GetString(), "Test formatting with 'en' provider");
			stringAppender.Reset();
		}
	}
}