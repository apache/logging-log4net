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
using System.IO;
using System.Xml;

using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;

using NUnit.Framework;
using System.Globalization;

namespace log4net.Tests.Layout;

[TestFixture]
public sealed class XmlLayoutTest
{
  private CultureInfo? _currentCulture;
  private CultureInfo? _currentUiCulture;

  [SetUp]
  public void SetUp()
  {
    // set correct thread culture
    _currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
    _currentUiCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
    System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
  }

  [TearDown]
  public void TearDown()
  {
    // restore previous culture
    System.Threading.Thread.CurrentThread.CurrentCulture = _currentCulture!;
    System.Threading.Thread.CurrentThread.CurrentUICulture = _currentUiCulture!;
  }

  /// <summary>
  /// Build a basic <see cref="LoggingEventData"/> object with some default values.
  /// </summary>
  /// <returns>A useful LoggingEventData object</returns>
  private LoggingEventData CreateBaseEvent()
  {
    return new LoggingEventData
    {
      Domain = "Tests",
      ExceptionString = "",
      Identity = "TestRunner",
      Level = Level.Info,
      LocationInfo = new LocationInfo(GetType()),
      LoggerName = "TestLogger",
      Message = "Test message",
      ThreadName = "TestThread",
      TimeStampUtc = DateTime.Today.ToUniversalTime(),
      UserName = "TestRunner",
      Properties = new PropertiesDictionary()
    };
  }

  private static string CreateEventNode(string message)
  {
    return string.Format("<{0}event logger=\"TestLogger\" timestamp=\"{2}\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"{1}><{0}message>{3}</{0}message></{0}event>" +
      Environment.NewLine,
#if NETCOREAPP
      "log4net:", @" xmlns:log4net=""log4net""",
#else
      string.Empty, string.Empty,
#endif
      XmlConvert.ToString(DateTime.Today, XmlDateTimeSerializationMode.Local),
      message);
  }

  private static string CreateEventNode(string key, string value)
  {
    return string.Format("<{0}event logger=\"TestLogger\" timestamp=\"{2}\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"{1}><{0}message>Test message</{0}message><{0}properties><{0}data name=\"{3}\" value=\"{4}\" /></{0}properties></{0}event>" + Environment.NewLine,
#if NETCOREAPP
      "log4net:", @" xmlns:log4net=""log4net""",
#else
      string.Empty, string.Empty,
#endif
     XmlConvert.ToString(DateTime.Today, XmlDateTimeSerializationMode.Local), key, value);
  }

  [Test]
  public void TestBasicEventLogging()
  {
    using TextWriter writer = new StringWriter();
    XmlLayout layout = new();
    LoggingEventData evt = CreateBaseEvent();

    layout.Format(writer, new LoggingEvent(evt));

    string expected = CreateEventNode("Test message");

    Assert.That(writer.ToString(), Is.EqualTo(expected));
  }

  [Test]
  public void TestIllegalCharacterMasking()
  {
    using StringWriter writer = new();
    XmlLayout layout = new();
    LoggingEventData evt = CreateBaseEvent();

    evt.Message = "This is a masked char->\uFFFF";

    layout.Format(writer, new LoggingEvent(evt));

    string expected = CreateEventNode("This is a masked char-&gt;?");

    Assert.That(writer.ToString(), Is.EqualTo(expected));
  }

  [Test]
  public void TestCdataEscaping1()
  {
    using StringWriter writer = new();
    XmlLayout layout = new();
    LoggingEventData evt = CreateBaseEvent();

    //The &'s trigger the use of a cdata block
    evt.Message = "&&&&&&&Escape this ]]>. End here.";

    layout.Format(writer, new LoggingEvent(evt));

    string expected = CreateEventNode("<![CDATA[&&&&&&&Escape this ]]>]]<![CDATA[>. End here.]]>");

    Assert.That(writer.ToString(), Is.EqualTo(expected));
  }

  [Test]
  public void TestCdataEscaping2()
  {
    using StringWriter writer = new();
    XmlLayout layout = new();
    LoggingEventData evt = CreateBaseEvent();

    //The &'s trigger the use of a cdata block
    evt.Message = "&&&&&&&Escape the end ]]>";

    layout.Format(writer, new LoggingEvent(evt));

    string expected = CreateEventNode("<![CDATA[&&&&&&&Escape the end ]]>]]&gt;");

    Assert.That(writer.ToString(), Is.EqualTo(expected));
  }

  [Test]
  public void TestCdataEscaping3()
  {
    using StringWriter writer = new();
    XmlLayout layout = new();
    LoggingEventData evt = CreateBaseEvent();

    //The &'s trigger the use of a cdata block
    evt.Message = "]]>&&&&&&&Escape the begining";

    layout.Format(writer, new LoggingEvent(evt));

    string expected = CreateEventNode("<![CDATA[]]>]]<![CDATA[>&&&&&&&Escape the begining]]>");

    Assert.That(writer.ToString(), Is.EqualTo(expected));
  }

  [Test]
  public void TestBase64EventLogging()
  {
    using StringWriter writer = new();
    XmlLayout layout = new();
    LoggingEventData evt = CreateBaseEvent();

    layout.Base64EncodeMessage = true;
    layout.Format(writer, new LoggingEvent(evt));

    string expected = CreateEventNode("VGVzdCBtZXNzYWdl");

    Assert.That(writer.ToString(), Is.EqualTo(expected));
  }

  [Test]
  public void TestPropertyEventLogging()
  {
    LoggingEventData evt = CreateBaseEvent();
    evt.Properties!["Property1"] = "prop1";

    XmlLayout layout = new();
    StringAppender stringAppender = new() { Layout = layout };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);
    ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

    log1.Logger.Log(new LoggingEvent(evt));

    string expected = CreateEventNode("Property1", "prop1");

    Assert.That(stringAppender.GetString(), Is.EqualTo(expected));
  }

  [Test]
  public void TestBase64PropertyEventLogging()
  {
    LoggingEventData evt = CreateBaseEvent();
    evt.Properties!["Property1"] = "prop1";

    XmlLayout layout = new() { Base64EncodeProperties = true };
    StringAppender stringAppender = new() { Layout = layout };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);
    ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

    log1.Logger.Log(new LoggingEvent(evt));

    string expected = CreateEventNode("Property1", "cHJvcDE=");

    Assert.That(stringAppender.GetString(), Is.EqualTo(expected));
  }

  [Test]
  public void TestPropertyCharacterEscaping()
  {
    LoggingEventData evt = CreateBaseEvent();
    evt.Properties!["Property1"] = "prop1 \"quoted\"";

    XmlLayout layout = new();
    StringAppender stringAppender = new() { Layout = layout };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);
    ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

    log1.Logger.Log(new LoggingEvent(evt));

    string expected = CreateEventNode("Property1", "prop1 &quot;quoted&quot;");

    Assert.That(stringAppender.GetString(), Is.EqualTo(expected));
  }

  [Test]
  public void TestPropertyIllegalCharacterMasking()
  {
    LoggingEventData evt = CreateBaseEvent();
    evt.Properties!["Property1"] = "mask this ->\uFFFF";

    XmlLayout layout = new();
    StringAppender stringAppender = new() { Layout = layout };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);
    ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

    log1.Logger.Log(new LoggingEvent(evt));

    string expected = CreateEventNode("Property1", "mask this -&gt;?");

    Assert.That(stringAppender.GetString(), Is.EqualTo(expected));
  }

  [Test]
  public void TestPropertyIllegalCharacterMaskingInName()
  {
    LoggingEventData evt = CreateBaseEvent();
    evt.Properties!["Property\uFFFF"] = "mask this ->\uFFFF";

    XmlLayout layout = new();
    StringAppender stringAppender = new() { Layout = layout };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);
    ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

    log1.Logger.Log(new LoggingEvent(evt));

    string expected = CreateEventNode("Property?", "mask this -&gt;?");

    Assert.That(stringAppender.GetString(), Is.EqualTo(expected));
  }

  [Test]
  public void BracketsInStackTracesKeepLogWellFormed()
  {
    XmlLayout layout = new();
    StringAppender stringAppender = new() { Layout = layout };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);
    ILog log1 = LogManager.GetLogger(rep.Name, "TestLogger");

    void Bar(int foo)
    {
      try
      {
        throw null!;
      }
      catch (Exception ex) when (ex is not null)
      {
        log1.Error($"Error {foo}", ex);
      }
    }

    Bar(42);

    // really only asserts there is no exception
    XmlDocument loggedDoc = new();
    loggedDoc.LoadXml(stringAppender.GetString());
  }

  [Test]
  public void BracketsInStackTracesAreEscapedProperly()
  {
    XmlLayout layout = new();
    StringAppender stringAppender = new() { Layout = layout };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);
    ILog log1 = LogManager.GetLogger(rep.Name, "TestLogger");

    void Bar(int foo)
    {
      try
      {
        throw null!;
      }
      catch (Exception ex) when (ex is not null)
      {
        log1.Error($"Error {foo}", ex);
      }
    }

    Bar(42);

#if NETCOREAPP
    const string nodeName = "log4net:exception";
#else
    const string nodeName = "exception";
#endif

    string log = stringAppender.GetString();
    int startOfExceptionText = log.IndexOf("<" + nodeName + ">", StringComparison.InvariantCulture) + nodeName.Length + 2;
    int endOfExceptionText = log.IndexOf("</" + nodeName + ">", StringComparison.InvariantCulture);
    string sub = log.Substring(startOfExceptionText, endOfExceptionText - startOfExceptionText);
    if (sub.StartsWith("<![CDATA["))
    {
      Assert.That(sub, Does.EndWith("]]>"));
    }
    else
    {
      Assert.That(sub, Does.Not.Contain("<"));
      Assert.That(sub, Does.Not.Contain(">"));
    }
  }
}