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

using NUnit.Framework;

namespace log4net.Tests.Core;

[TestFixture]
public class StringFormatTest
{
  private CultureInfo? _currentCulture;
  private CultureInfo? _currentUiCulture;

  [SetUp]
  public void SetUp()
  {
    // set correct thread culture
    _currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
    _currentUiCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
    System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
  }

  [TearDown]
  public void TearDown()
  {
    // restore previous culture
    System.Threading.Thread.CurrentThread.CurrentCulture = _currentCulture!;
    System.Threading.Thread.CurrentThread.CurrentUICulture = _currentUiCulture!;
  }

  [Test]
  public void TestFormatString()
  {
    var stringAppender = new StringAppender
    {
      Layout = new PatternLayout("%message")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestFormatString");

    // ***
    log1.Info("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo("TestMessage"), "Test simple INFO event");
    stringAppender.Reset();


    // ***
    log1.DebugFormat("Before {0} After", "Middle");
    Assert.That(stringAppender.GetString(), Is.EqualTo("Before Middle After"), "Test simple formatted DEBUG event");
    stringAppender.Reset();

    // ***
    log1.InfoFormat("Before {0} After", "Middle");
    Assert.That(stringAppender.GetString(), Is.EqualTo("Before Middle After"), "Test simple formatted INFO event");
    stringAppender.Reset();

    // ***
    log1.WarnFormat("Before {0} After", "Middle");
    Assert.That(stringAppender.GetString(), Is.EqualTo("Before Middle After"), "Test simple formatted WARN event");
    stringAppender.Reset();

    // ***
    log1.ErrorFormat("Before {0} After", "Middle");
    Assert.That(stringAppender.GetString(), Is.EqualTo("Before Middle After"), "Test simple formatted ERROR event");
    stringAppender.Reset();

    // ***
    log1.FatalFormat("Before {0} After", "Middle");
    Assert.That(stringAppender.GetString(), Is.EqualTo("Before Middle After"), "Test simple formatted FATAL event");
    stringAppender.Reset();


    // ***
    log1.InfoFormat("Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo("Before Middle After End"), "Test simple formatted INFO event 2");
    stringAppender.Reset();

    // ***
    log1.InfoFormat("IGNORE THIS WARNING - EXCEPTION EXPECTED Before {0} After {1} {2}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo(StringFormatError), "Test formatting error");
    stringAppender.Reset();

    // *** Nulls
    log1.Debug(null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.Debug(null, new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo("System.Exception: Exception message" + Environment.NewLine));
    stringAppender.Reset();

    log1.InfoFormat("One{0} null", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("One null"));
    stringAppender.Reset();

    log1.InfoFormat("Two{0} nulls{1}", null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("Two nulls"));
    stringAppender.Reset();

    log1.InfoFormat("Three{0} nulls{1} here{2}", null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("Three nulls here"));
    stringAppender.Reset();

    log1.InfoFormat("Four{0} nulls{1} this{2} time{3}", null, null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("Four nulls this time"));
    stringAppender.Reset();

    object[]? args = null;
    log1.InfoFormat("Null param array", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo("Null param array"));
    stringAppender.Reset();

    // Degenerate case - null param array internally converts to an array of one null.
    log1.InfoFormat("Null param array with {0}", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo("Null param array with "));
    stringAppender.Reset();
  }

  private const string StringFormatError = "<log4net.Error>Exception during StringFormat: Index (zero based) must be greater than or equal to zero and less than the size of the argument list. <format>IGNORE THIS WARNING - EXCEPTION EXPECTED Before {0} After {1} {2}</format><args>{Middle, End}</args></log4net.Error>";

  [Test]
  public void TestLogFormatApi_Debug()
  {
    var stringAppender = new StringAppender
    {
      Layout = new PatternLayout("%level:%message")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Debug");

    // ***
    log1.Debug("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:TestMessage"), "Test simple DEBUG event 1");
    stringAppender.Reset();

    // ***
    log1.Debug("TestMessage", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:TestMessage"), "Test simple DEBUG event 2");
    stringAppender.Reset();

    // ***
    log1.Debug("TestMessage", new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:TestMessageSystem.Exception: Exception message" + Environment.NewLine), "Test simple DEBUG event 3");
    stringAppender.Reset();

    // ***
    log1.DebugFormat("a{0}", "1");
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:a1"), "Test formatted DEBUG event with 1 parm");
    stringAppender.Reset();

    // ***
    log1.DebugFormat("a{0}b{1}", "1", "2");
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:a1b2"), "Test formatted DEBUG event with 2 parm");
    stringAppender.Reset();

    // ***
    log1.DebugFormat("a{0}b{1}c{2}", "1", "2", "3");
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:a1b2c3"), "Test formatted DEBUG event with 3 parm");
    stringAppender.Reset();


    // ***
    log1.DebugFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:aQbWcEdReTf"), "Test formatted DEBUG event with 5 parms (only 4 used)");
    stringAppender.Reset();

    // ***
    log1.DebugFormat(null, "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:Before Middle After End"), "Test formatting with null provider");
    stringAppender.Reset();

    // ***
    log1.DebugFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:Before Middle After End"), "Test formatting with 'en' provider");
    stringAppender.Reset();

    // *** Nulls
    log1.Debug(null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:"));
    stringAppender.Reset();

    log1.Debug(null, new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:System.Exception: Exception message" + Environment.NewLine));
    stringAppender.Reset();

    log1.DebugFormat("One{0} null", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:One null"));
    stringAppender.Reset();

    log1.DebugFormat("Two{0} nulls{1}", null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:Two nulls"));
    stringAppender.Reset();

    log1.DebugFormat("Three{0} nulls{1} here{2}", null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:Three nulls here"));
    stringAppender.Reset();

    log1.DebugFormat("Four{0} nulls{1} this{2} time{3}", null, null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:Four nulls this time"));
    stringAppender.Reset();

    object[]? args = null;
    log1.DebugFormat("Null param array", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:Null param array"));
    stringAppender.Reset();

    // Degenerate case - null param array internally converts to an array of one null.
    log1.DebugFormat("Null param array with {0}", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo("DEBUG:Null param array with "));
    stringAppender.Reset();
  }

  [Test]
  public void TestLogFormatApi_NoDebug()
  {
    var stringAppender = new StringAppender
    {
      Threshold = Level.Info,
      Layout = new PatternLayout("%level:%message")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Debug");

    // ***
    log1.Debug("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple DEBUG event 1");
    stringAppender.Reset();

    // ***
    log1.Debug("TestMessage", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple DEBUG event 2");
    stringAppender.Reset();

    // ***
    log1.Debug("TestMessage", new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple DEBUG event 3");
    stringAppender.Reset();

    // ***
    log1.DebugFormat("a{0}", "1");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted DEBUG event with 1 parm");
    stringAppender.Reset();

    // ***
    log1.DebugFormat("a{0}b{1}", "1", "2");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted DEBUG event with 2 parm");
    stringAppender.Reset();

    // ***
    log1.DebugFormat("a{0}b{1}c{2}", "1", "2", "3");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted DEBUG event with 3 parm");
    stringAppender.Reset();


    // ***
    log1.DebugFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted DEBUG event with 5 parms (only 4 used)");
    stringAppender.Reset();

    // ***
    log1.DebugFormat(null, "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatting with null provider");
    stringAppender.Reset();

    // ***
    log1.DebugFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatting with 'en' provider");
    stringAppender.Reset();

    // *** Nulls
    log1.Debug(null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.Debug(null, new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.DebugFormat("One{0} null", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.DebugFormat("Two{0} nulls{1}", null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.DebugFormat("Three{0} nulls{1} here{2}", null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.DebugFormat("Four{0} nulls{1} this{2} time{3}", null, null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    object[]? args = null;
    log1.DebugFormat("Null param array", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    // Degenerate case - null param array internally converts to an array of one null.
    log1.DebugFormat("Null param array with {0}", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();
  }

  [Test]
  public void TestLogFormatApi_Info()
  {
    var stringAppender = new StringAppender
    {
      Layout = new PatternLayout("%level:%message")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Info");

    // ***
    log1.Info("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:TestMessage"), "Test simple INFO event 1");
    stringAppender.Reset();

    // ***
    log1.Info("TestMessage", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:TestMessage"), "Test simple INFO event 2");
    stringAppender.Reset();

    // ***
    log1.Info("TestMessage", new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:TestMessageSystem.Exception: Exception message" + Environment.NewLine), "Test simple INFO event 3");
    stringAppender.Reset();

    // ***
    log1.InfoFormat("a{0}", "1");
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:a1"), "Test formatted INFO event with 1 parm");
    stringAppender.Reset();

    // ***
    log1.InfoFormat("a{0}b{1}", "1", "2");
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:a1b2"), "Test formatted INFO event with 2 parm");
    stringAppender.Reset();

    // ***
    log1.InfoFormat("a{0}b{1}c{2}", "1", "2", "3");
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:a1b2c3"), "Test formatted INFO event with 3 parm");
    stringAppender.Reset();


    // ***
    log1.InfoFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:aQbWcEdReTf"), "Test formatted INFO event with 5 parms (only 4 used)");
    stringAppender.Reset();

    // ***
    log1.InfoFormat(null, "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:Before Middle After End"), "Test formatting with null provider");
    stringAppender.Reset();

    // ***
    log1.InfoFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:Before Middle After End"), "Test formatting with 'en' provider");
    stringAppender.Reset();

    // *** Nulls
    log1.Info(null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:"));
    stringAppender.Reset();

    log1.Info(null, new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:System.Exception: Exception message" + Environment.NewLine));
    stringAppender.Reset();

    log1.InfoFormat("One{0} null", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:One null"));
    stringAppender.Reset();

    log1.InfoFormat("Two{0} nulls{1}", null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:Two nulls"));
    stringAppender.Reset();

    log1.InfoFormat("Three{0} nulls{1} here{2}", null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:Three nulls here"));
    stringAppender.Reset();

    log1.InfoFormat("Four{0} nulls{1} this{2} time{3}", null, null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:Four nulls this time"));
    stringAppender.Reset();

    object[]? args = null;
    log1.InfoFormat("Null param array", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:Null param array"));
    stringAppender.Reset();

    // Degenerate case - null param array internally converts to an array of one null.
    log1.InfoFormat("Null param array with {0}", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo("INFO:Null param array with "));
    stringAppender.Reset();
  }

  [Test]
  public void TestLogFormatApi_NoInfo()
  {
    var stringAppender = new StringAppender
    {
      Threshold = Level.Warn,
      Layout = new PatternLayout("%level:%message")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Info");

    // ***
    log1.Info("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple INFO event 1");
    stringAppender.Reset();

    // ***
    log1.Info("TestMessage", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple INFO event 2");
    stringAppender.Reset();

    // ***
    log1.Info("TestMessage", new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple INFO event 3");
    stringAppender.Reset();

    // ***
    log1.InfoFormat("a{0}", "1");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted INFO event with 1 parm");
    stringAppender.Reset();

    // ***
    log1.InfoFormat("a{0}b{1}", "1", "2");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted INFO event with 2 parm");
    stringAppender.Reset();

    // ***
    log1.InfoFormat("a{0}b{1}c{2}", "1", "2", "3");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted INFO event with 3 parm");
    stringAppender.Reset();


    // ***
    log1.InfoFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted INFO event with 5 parms (only 4 used)");
    stringAppender.Reset();

    // ***
    log1.InfoFormat(null, "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatting with null provider");
    stringAppender.Reset();

    // ***
    log1.InfoFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatting with 'en' provider");
    stringAppender.Reset();

    // *** Nulls
    log1.Info(null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.Info(null, new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.InfoFormat("One{0} null", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.InfoFormat("Two{0} nulls{1}", null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.InfoFormat("Three{0} nulls{1} here{2}", null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.InfoFormat("Four{0} nulls{1} this{2} time{3}", null, null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    object[]? args = null;
    log1.InfoFormat("Null param array", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    // Degenerate case - null param array internally converts to an array of one null.
    log1.InfoFormat("Null param array with {0}", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();
  }

  [Test]
  public void TestLogFormatApi_Warn()
  {
    var stringAppender = new StringAppender
    {
      Layout = new PatternLayout("%level:%message")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Warn");

    // ***
    log1.Warn("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:TestMessage"), "Test simple WARN event 1");
    stringAppender.Reset();

    // ***
    log1.Warn("TestMessage", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:TestMessage"), "Test simple WARN event 2");
    stringAppender.Reset();

    // ***
    log1.Warn("TestMessage", new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:TestMessageSystem.Exception: Exception message" + Environment.NewLine), "Test simple WARN event 3");
    stringAppender.Reset();

    // ***
    log1.WarnFormat("a{0}", "1");
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:a1"), "Test formatted WARN event with 1 parm");
    stringAppender.Reset();

    // ***
    log1.WarnFormat("a{0}b{1}", "1", "2");
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:a1b2"), "Test formatted WARN event with 2 parm");
    stringAppender.Reset();

    // ***
    log1.WarnFormat("a{0}b{1}c{2}", "1", "2", "3");
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:a1b2c3"), "Test formatted WARN event with 3 parm");
    stringAppender.Reset();


    // ***
    log1.WarnFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:aQbWcEdReTf"), "Test formatted WARN event with 5 parms (only 4 used)");
    stringAppender.Reset();

    // ***
    log1.WarnFormat(null, "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:Before Middle After End"), "Test formatting with null provider");
    stringAppender.Reset();

    // ***
    log1.WarnFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:Before Middle After End"), "Test formatting with 'en' provider");
    stringAppender.Reset();

    // *** Nulls
    log1.Warn(null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:"));
    stringAppender.Reset();

    log1.Warn(null, new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:System.Exception: Exception message" + Environment.NewLine));
    stringAppender.Reset();

    log1.WarnFormat("One{0} null", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:One null"));
    stringAppender.Reset();

    log1.WarnFormat("Two{0} nulls{1}", null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:Two nulls"));
    stringAppender.Reset();

    log1.WarnFormat("Three{0} nulls{1} here{2}", null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:Three nulls here"));
    stringAppender.Reset();

    log1.WarnFormat("Four{0} nulls{1} this{2} time{3}", null, null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:Four nulls this time"));
    stringAppender.Reset();

    object[]? args = null;
    log1.WarnFormat("Null param array", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:Null param array"));
    stringAppender.Reset();

    // Degenerate case - null param array internally converts to an array of one null.
    log1.WarnFormat("Null param array with {0}", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo("WARN:Null param array with "));
    stringAppender.Reset();
  }

  [Test]
  public void TestLogFormatApi_NoWarn()
  {
    var stringAppender = new StringAppender
    {
      Threshold = Level.Error,
      Layout = new PatternLayout("%level:%message")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Warn");

    // ***
    log1.Warn("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple WARN event 1");
    stringAppender.Reset();

    // ***
    log1.Warn("TestMessage", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple WARN event 2");
    stringAppender.Reset();

    // ***
    log1.Warn("TestMessage", new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple WARN event 3");
    stringAppender.Reset();

    // ***
    log1.WarnFormat("a{0}", "1");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted WARN event with 1 parm");
    stringAppender.Reset();

    // ***
    log1.WarnFormat("a{0}b{1}", "1", "2");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted WARN event with 2 parm");
    stringAppender.Reset();

    // ***
    log1.WarnFormat("a{0}b{1}c{2}", "1", "2", "3");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted WARN event with 3 parm");
    stringAppender.Reset();


    // ***
    log1.WarnFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted WARN event with 5 parms (only 4 used)");
    stringAppender.Reset();

    // ***
    log1.WarnFormat(null, "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatting with null provider");
    stringAppender.Reset();

    // ***
    log1.WarnFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatting with 'en' provider");
    stringAppender.Reset();

    // *** Nulls
    log1.Warn(null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.Warn(null, new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.WarnFormat("One{0} null", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.WarnFormat("Two{0} nulls{1}", null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.WarnFormat("Three{0} nulls{1} here{2}", null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.WarnFormat("Four{0} nulls{1} this{2} time{3}", null, null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    object[]? args = null;
    log1.WarnFormat("Null param array", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    // Degenerate case - null param array internally converts to an array of one null.
    log1.WarnFormat("Null param array with {0}", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();
  }

  [Test]
  public void TestLogFormatApi_Error()
  {
    var stringAppender = new StringAppender
    {
      Layout = new PatternLayout("%level:%message")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Error");

    // ***
    log1.Error("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:TestMessage"), "Test simple ERROR event 1");
    stringAppender.Reset();

    // ***
    log1.Error("TestMessage", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:TestMessage"), "Test simple ERROR event 2");
    stringAppender.Reset();

    // ***
    log1.Error("TestMessage", new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:TestMessageSystem.Exception: Exception message" + Environment.NewLine), "Test simple ERROR event 3");
    stringAppender.Reset();

    // ***
    log1.ErrorFormat("a{0}", "1");
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:a1"), "Test formatted ERROR event with 1 parm");
    stringAppender.Reset();

    // ***
    log1.ErrorFormat("a{0}b{1}", "1", "2");
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:a1b2"), "Test formatted ERROR event with 2 parm");
    stringAppender.Reset();

    // ***
    log1.ErrorFormat("a{0}b{1}c{2}", "1", "2", "3");
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:a1b2c3"), "Test formatted ERROR event with 3 parm");
    stringAppender.Reset();


    // ***
    log1.ErrorFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:aQbWcEdReTf"), "Test formatted ERROR event with 5 parms (only 4 used)");
    stringAppender.Reset();

    // ***
    log1.ErrorFormat(null, "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:Before Middle After End"), "Test formatting with null provider");
    stringAppender.Reset();

    // ***
    log1.ErrorFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:Before Middle After End"), "Test formatting with 'en' provider");
    stringAppender.Reset();

    // *** Nulls
    log1.Error(null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:"));
    stringAppender.Reset();

    log1.Error(null, new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:System.Exception: Exception message" + Environment.NewLine));
    stringAppender.Reset();

    log1.ErrorFormat("One{0} null", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:One null"));
    stringAppender.Reset();

    log1.ErrorFormat("Two{0} nulls{1}", null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:Two nulls"));
    stringAppender.Reset();

    log1.ErrorFormat("Three{0} nulls{1} here{2}", null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:Three nulls here"));
    stringAppender.Reset();

    log1.ErrorFormat("Four{0} nulls{1} this{2} time{3}", null, null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:Four nulls this time"));
    stringAppender.Reset();

    object[]? args = null;
    log1.ErrorFormat("Null param array", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:Null param array"));
    stringAppender.Reset();

    // Degenerate case - null param array internally converts to an array of one null.
    log1.ErrorFormat("Null param array with {0}", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo("ERROR:Null param array with "));
    stringAppender.Reset();
  }

  [Test]
  public void TestLogFormatApi_NoError()
  {
    var stringAppender = new StringAppender
    {
      Threshold = Level.Fatal,
      Layout = new PatternLayout("%level:%message")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Error");

    // ***
    log1.Error("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple ERROR event 1");
    stringAppender.Reset();

    // ***
    log1.Error("TestMessage", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple ERROR event 2");
    stringAppender.Reset();

    // ***
    log1.Error("TestMessage", new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple ERROR event 3");
    stringAppender.Reset();

    // ***
    log1.ErrorFormat("a{0}", "1");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted ERROR event with 1 parm");
    stringAppender.Reset();

    // ***
    log1.ErrorFormat("a{0}b{1}", "1", "2");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted ERROR event with 2 parm");
    stringAppender.Reset();

    // ***
    log1.ErrorFormat("a{0}b{1}c{2}", "1", "2", "3");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted ERROR event with 3 parm");
    stringAppender.Reset();


    // ***
    log1.ErrorFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted ERROR event with 5 parms (only 4 used)");
    stringAppender.Reset();

    // ***
    log1.ErrorFormat(null, "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatting with null provider");
    stringAppender.Reset();

    // ***
    log1.ErrorFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatting with 'en' provider");
    stringAppender.Reset();

    // *** Nulls
    log1.Error(null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.Error(null, new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.ErrorFormat("One{0} null", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.ErrorFormat("Two{0} nulls{1}", null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.ErrorFormat("Three{0} nulls{1} here{2}", null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.ErrorFormat("Four{0} nulls{1} this{2} time{3}", null, null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    object[]? args = null;
    log1.ErrorFormat("Null param array", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    // Degenerate case - null param array internally converts to an array of one null.
    log1.ErrorFormat("Null param array with {0}", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();
  }

  [Test]
  public void TestLogFormatApi_Fatal()
  {
    var stringAppender = new StringAppender
    {
      Layout = new PatternLayout("%level:%message")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Fatal");

    // ***
    log1.Fatal("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:TestMessage"), "Test simple FATAL event 1");
    stringAppender.Reset();

    // ***
    log1.Fatal("TestMessage", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:TestMessage"), "Test simple FATAL event 2");
    stringAppender.Reset();

    // ***
    log1.Fatal("TestMessage", new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:TestMessageSystem.Exception: Exception message" + Environment.NewLine), "Test simple FATAL event 3");
    stringAppender.Reset();

    // ***
    log1.FatalFormat("a{0}", "1");
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:a1"), "Test formatted FATAL event with 1 parm");
    stringAppender.Reset();

    // ***
    log1.FatalFormat("a{0}b{1}", "1", "2");
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:a1b2"), "Test formatted FATAL event with 2 parm");
    stringAppender.Reset();

    // ***
    log1.FatalFormat("a{0}b{1}c{2}", "1", "2", "3");
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:a1b2c3"), "Test formatted FATAL event with 3 parm");
    stringAppender.Reset();


    // ***
    log1.FatalFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:aQbWcEdReTf"), "Test formatted FATAL event with 5 parms (only 4 used)");
    stringAppender.Reset();

    // ***
    log1.FatalFormat(null, "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:Before Middle After End"), "Test formatting with null provider");
    stringAppender.Reset();

    // ***
    log1.FatalFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:Before Middle After End"), "Test formatting with 'en' provider");
    stringAppender.Reset();

    // *** Nulls
    log1.Fatal(null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:"));
    stringAppender.Reset();

    log1.Fatal(null, new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:System.Exception: Exception message" + Environment.NewLine));
    stringAppender.Reset();

    log1.FatalFormat("One{0} null", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:One null"));
    stringAppender.Reset();

    log1.FatalFormat("Two{0} nulls{1}", null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:Two nulls"));
    stringAppender.Reset();

    log1.FatalFormat("Three{0} nulls{1} here{2}", null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:Three nulls here"));
    stringAppender.Reset();

    log1.FatalFormat("Four{0} nulls{1} this{2} time{3}", null, null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:Four nulls this time"));
    stringAppender.Reset();

    object[]? args = null;
    log1.FatalFormat("Null param array", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:Null param array"));
    stringAppender.Reset();

    // Degenerate case - null param array internally converts to an array of one null.
    log1.FatalFormat("Null param array with {0}", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo("FATAL:Null param array with "));
    stringAppender.Reset();
  }

  [Test]
  public void TestLogFormatApi_NoFatal()
  {
    var stringAppender = new StringAppender
    {
      Threshold = Level.Off,
      Layout = new PatternLayout("%level:%message")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestLogFormatApi_Fatal");

    // ***
    log1.Fatal("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple FATAL event 1");
    stringAppender.Reset();

    // ***
    log1.Fatal("TestMessage", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple FATAL event 2");
    stringAppender.Reset();

    // ***
    log1.Fatal("TestMessage", new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test simple FATAL event 3");
    stringAppender.Reset();

    // ***
    log1.FatalFormat("a{0}", "1");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted FATAL event with 1 parm");
    stringAppender.Reset();

    // ***
    log1.FatalFormat("a{0}b{1}", "1", "2");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted FATAL event with 2 parm");
    stringAppender.Reset();

    // ***
    log1.FatalFormat("a{0}b{1}c{2}", "1", "2", "3");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted FATAL event with 3 parm");
    stringAppender.Reset();


    // ***
    log1.FatalFormat("a{0}b{1}c{2}d{3}e{4}f", "Q", "W", "E", "R", "T", "Y");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatted FATAL event with 5 parms (only 4 used)");
    stringAppender.Reset();

    // ***
    log1.FatalFormat(null, "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatting with null provider");
    stringAppender.Reset();

    // ***
    log1.FatalFormat(new CultureInfo("en"), "Before {0} After {1}", "Middle", "End");
    Assert.That(stringAppender.GetString(), Is.EqualTo(""), "Test formatting with 'en' provider");
    stringAppender.Reset();

    // *** Nulls
    log1.Fatal(null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.Fatal(null, new Exception("Exception message"));
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.FatalFormat("One{0} null", null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.FatalFormat("Two{0} nulls{1}", null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.FatalFormat("Three{0} nulls{1} here{2}", null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    log1.FatalFormat("Four{0} nulls{1} this{2} time{3}", null, null, null, null);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    object[]? args = null;
    log1.FatalFormat("Null param array", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();

    // Degenerate case - null param array internally converts to an array of one null.
    log1.FatalFormat("Null param array with {0}", args);
    Assert.That(stringAppender.GetString(), Is.EqualTo(""));
    stringAppender.Reset();
  }
}
