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
using System.Globalization;

using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Layout.Pattern;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Layout;

/// <summary>
/// Used for internal unit testing the <see cref="PatternLayout"/> class.
/// </summary>
/// <remarks>
/// Used for internal unit testing the <see cref="PatternLayout"/> class.
/// </remarks>
[TestFixture]
public class PatternLayoutTest
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
    Utils.RemovePropertyFromAllContexts();
    // restore previous culture
    System.Threading.Thread.CurrentThread.CurrentCulture = _currentCulture!;
    System.Threading.Thread.CurrentThread.CurrentUICulture = _currentUiCulture!;
  }

  protected virtual PatternLayout NewPatternLayout() => new();

  protected virtual PatternLayout NewPatternLayout(string pattern) => new(pattern);

  [Test]
  public void TestThreadPropertiesPattern()
  {
    StringAppender stringAppender = new()
    {
      Layout = NewPatternLayout("%property{" + Utils.PropertyKey + "}")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

    log1.Info("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo(SystemInfo.NullText), "Test no thread properties value set");
    stringAppender.Reset();

    ThreadContext.Properties[Utils.PropertyKey] = "val1";

    log1.Info("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo("val1"), "Test thread properties value set");
    stringAppender.Reset();

    ThreadContext.Properties.Remove(Utils.PropertyKey);

    log1.Info("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo(SystemInfo.NullText), "Test thread properties value removed");
    stringAppender.Reset();
  }

  [Test]
  public void TestStackTracePattern()
  {
    StringAppender stringAppender = new()
    {
      Layout = NewPatternLayout("%stacktrace{2}")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestStackTracePattern");

    log1.Info("TestMessage");
    Assert.That(stringAppender.GetString(), Does.EndWith("PatternLayoutTest.TestStackTracePattern"), "stack trace value set");
    stringAppender.Reset();
  }

  [Test]
  public void TestGlobalPropertiesPattern()
  {
    StringAppender stringAppender = new()
    {
      Layout = NewPatternLayout("%property{" + Utils.PropertyKey + "}")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestGlobalProperiesPattern");

    log1.Info("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo(SystemInfo.NullText), "Test no global properties value set");
    stringAppender.Reset();

    GlobalContext.Properties[Utils.PropertyKey] = "val1";

    log1.Info("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo("val1"), "Test global properties value set");
    stringAppender.Reset();

    GlobalContext.Properties.Remove(Utils.PropertyKey);

    log1.Info("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo(SystemInfo.NullText), "Test global properties value removed");
    stringAppender.Reset();
  }

  [Test]
  public void TestAddingCustomPattern()
  {
    StringAppender stringAppender = new();
    PatternLayout layout = NewPatternLayout();

    layout.AddConverter("TestAddingCustomPattern", typeof(TestMessagePatternConverter));
    layout.ConversionPattern = "%TestAddingCustomPattern";
    layout.ActivateOptions();

    stringAppender.Layout = layout;

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestAddingCustomPattern");

    log1.Info("TestMessage");
    Assert.That(stringAppender.GetString(), Is.EqualTo("TestMessage"), "%TestAddingCustomPattern not registered");
    stringAppender.Reset();
  }

  [Test]
  public void NamedPatternConverterWithoutPrecisionShouldReturnFullName()
  {
    StringAppender stringAppender = new();
    PatternLayout layout = NewPatternLayout();
    layout.AddConverter("message-as-name", typeof(MessageAsNamePatternConverter));
    layout.ConversionPattern = "%message-as-name";
    layout.ActivateOptions();
    stringAppender.Layout = layout;
    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);
    ILog log1 = LogManager.GetLogger(rep.Name, "TestAddingCustomPattern");

    log1.Info("NoDots");
    Assert.That(stringAppender.GetString(), Is.EqualTo("NoDots"), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info("One.Dot");
    Assert.That(stringAppender.GetString(), Is.EqualTo("One.Dot"), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info("Tw.o.Dots");
    Assert.That(stringAppender.GetString(), Is.EqualTo("Tw.o.Dots"), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info("TrailingDot.");
    Assert.That(stringAppender.GetString(), Is.EqualTo("TrailingDot."), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info(".LeadingDot");
    Assert.That(stringAppender.GetString(), Is.EqualTo(".LeadingDot"), "%message-as-name not registered");
    stringAppender.Reset();

    // empty string and other evil combinations as tests for of-by-one mistakes in index calculations
    log1.Info(string.Empty);
    Assert.That(stringAppender.GetString(), Is.EqualTo(string.Empty), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info(".");
    Assert.That(stringAppender.GetString(), Is.EqualTo("."), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info("x");
    Assert.That(stringAppender.GetString(), Is.EqualTo("x"), "%message-as-name not registered");
    stringAppender.Reset();
  }

  [Test]
  public void NamedPatternConverterWithPrecision1ShouldStripLeadingStuffIfPresent()
  {
    StringAppender stringAppender = new();
    PatternLayout layout = NewPatternLayout();
    layout.AddConverter("message-as-name", typeof(MessageAsNamePatternConverter));
    layout.ConversionPattern = "%message-as-name{1}";
    layout.ActivateOptions();
    stringAppender.Layout = layout;
    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);
    ILog log1 = LogManager.GetLogger(rep.Name, "TestAddingCustomPattern");

    log1.Info("NoDots");
    Assert.That(stringAppender.GetString(), Is.EqualTo("NoDots"), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info("One.Dot");
    Assert.That(stringAppender.GetString(), Is.EqualTo("Dot"), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info("Tw.o.Dots");
    Assert.That(stringAppender.GetString(), Is.EqualTo("Dots"), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info("TrailingDot.");
    Assert.That(stringAppender.GetString(), Is.EqualTo("TrailingDot."), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info(".LeadingDot");
    Assert.That(stringAppender.GetString(), Is.EqualTo("LeadingDot"), "%message-as-name not registered");
    stringAppender.Reset();

    // empty string and other evil combinations as tests for of-by-one mistakes in index calculations
    log1.Info(string.Empty);
    Assert.That(stringAppender.GetString(), Is.EqualTo(string.Empty), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info("x");
    Assert.That(stringAppender.GetString(), Is.EqualTo("x"), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info(".");
    Assert.That(stringAppender.GetString(), Is.EqualTo("."), "%message-as-name not registered");
    stringAppender.Reset();
  }

  [Test]
  public void NamedPatternConverterWithPrecision2ShouldStripLessLeadingStuffIfPresent()
  {
    StringAppender stringAppender = new();
    PatternLayout layout = NewPatternLayout();
    layout.AddConverter("message-as-name", typeof(MessageAsNamePatternConverter));
    layout.ConversionPattern = "%message-as-name{2}";
    layout.ActivateOptions();
    stringAppender.Layout = layout;
    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);
    ILog log1 = LogManager.GetLogger(rep.Name, "TestAddingCustomPattern");

    log1.Info("NoDots");
    Assert.That(stringAppender.GetString(), Is.EqualTo("NoDots"), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info("One.Dot");
    Assert.That(stringAppender.GetString(), Is.EqualTo("One.Dot"), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info("Tw.o.Dots");
    Assert.That(stringAppender.GetString(), Is.EqualTo("o.Dots"), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info("TrailingDot.");
    Assert.That(stringAppender.GetString(), Is.EqualTo("TrailingDot."), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info(".LeadingDot");
    Assert.That(stringAppender.GetString(), Is.EqualTo("LeadingDot"), "%message-as-name not registered");
    stringAppender.Reset();

    // empty string and other evil combinations as tests for of-by-one mistakes in index calculations
    log1.Info(string.Empty);
    Assert.That(stringAppender.GetString(), Is.EqualTo(string.Empty), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info("x");
    Assert.That(stringAppender.GetString(), Is.EqualTo("x"), "%message-as-name not registered");
    stringAppender.Reset();

    log1.Info(".");
    Assert.That(stringAppender.GetString(), Is.EqualTo("."), "%message-as-name not registered");
    stringAppender.Reset();
  }

  /// <summary>
  /// Converter to include event message
  /// </summary>
  private sealed class TestMessagePatternConverter : PatternLayoutConverter
  {
    /// <summary>
    /// Convert the pattern to the rendered message
    /// </summary>
    /// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
    /// <param name="loggingEvent">the event being logged</param>
    /// <returns>the relevant location information</returns>
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent) => loggingEvent.WriteRenderedMessage(writer);
  }

  [Test]
  public void TestExceptionPattern()
  {
    StringAppender stringAppender = new();
    PatternLayout layout = NewPatternLayout("%exception{stacktrace}");
    stringAppender.Layout = layout;

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestExceptionPattern");

    InvalidOperationException exception = new("Oh no!");
    log1.Info("TestMessage", exception);

    Assert.That(stringAppender.GetString(), Is.EqualTo(SystemInfo.NullText));

    stringAppender.Reset();
  }

  private sealed class MessageAsNamePatternConverter : NamedPatternConverter
  {
    protected override string GetFullyQualifiedName(LoggingEvent loggingEvent) => loggingEvent.MessageObject?.ToString() ?? string.Empty;
  }
}