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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
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
    _currentCulture = Thread.CurrentThread.CurrentCulture;
    _currentUiCulture = Thread.CurrentThread.CurrentUICulture;
    Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
  }
  [TearDown]
  public void TearDown()
  {
    Utils.RemovePropertyFromAllContexts();
    // restore previous culture
    Thread.CurrentThread.CurrentCulture = _currentCulture!;
    Thread.CurrentThread.CurrentUICulture = _currentUiCulture!;
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

    ILog log1 = LogManager.GetLogger(rep.Name, nameof(TestThreadPropertiesPattern));

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

    ILog log1 = LogManager.GetLogger(rep.Name, nameof(TestGlobalPropertiesPattern));

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

    layout.AddConverter(nameof(TestAddingCustomPattern), typeof(TestMessagePatternConverter));
    layout.ConversionPattern = "%" + nameof(TestAddingCustomPattern);
    layout.ActivateOptions();

    stringAppender.Layout = layout;

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, nameof(TestAddingCustomPattern));

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
    ILog log1 = LogManager.GetLogger(rep.Name, nameof(NamedPatternConverterWithoutPrecisionShouldReturnFullName));

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
    ILog log1 = LogManager.GetLogger(rep.Name, nameof(NamedPatternConverterWithPrecision1ShouldStripLeadingStuffIfPresent));

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
    ILog log1 = LogManager.GetLogger(rep.Name, nameof(NamedPatternConverterWithPrecision2ShouldStripLessLeadingStuffIfPresent));

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
  [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Reflection")]
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

    ILog log1 = LogManager.GetLogger(rep.Name, nameof(TestExceptionPattern));

    InvalidOperationException exception = new("Oh no!");
    log1.Info("TestMessage", exception);

    Assert.That(stringAppender.GetString(), Is.EqualTo(SystemInfo.NullText));

    stringAppender.Reset();
  }

  [Test]
  public void ConvertMultipleDatePatternsTest()
  {
    StringAppender stringAppender = new()
    {
      Layout = NewPatternLayout("%utcdate{ABSOLUTE} - %utcdate{ISO8601}")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog logger = LogManager.GetLogger(rep.Name, nameof(ConvertMultipleDatePatternsTest));

    logger.Logger.Log(new(new() { TimeStampUtc = new(2025, 02, 10, 13, 01, 02, 123, DateTimeKind.Utc), Message = "test", Level = Level.Info }));
    Assert.That(stringAppender.GetString(), Is.EqualTo("13:01:02,123 - 2025-02-10 13:01:02,123"));
    stringAppender.Reset();
    logger.Logger.Log(new(new() { TimeStampUtc = new(2025, 02, 10, 13, 01, 03, 123, DateTimeKind.Utc), Message = "test", Level = Level.Info }));
    Assert.That(stringAppender.GetString(), Is.EqualTo("13:01:03,123 - 2025-02-10 13:01:03,123"));
  }

#if NET8_0_OR_GREATER
  [Test]
  public void ConvertMicrosecondsPatternTest()
  {
    StringAppender stringAppender = new()
    {
      Layout = NewPatternLayout("%utcdate{yyyyMMdd HH:mm:ss.ffffff}")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog logger = LogManager.GetLogger(rep.Name, nameof(ConvertMicrosecondsPatternTest));

    logger.Logger.Log(new(new() { TimeStampUtc = new(2025, 02, 10, 13, 01, 02, 123, 456, DateTimeKind.Utc), Message = "test", Level = Level.Info }));
    Assert.That(stringAppender.GetString(), Is.EqualTo("20250210 13:01:02.123456"));
  }

  [Test]
  public void ConvertMultipleMicrosecondsPatternTest()
  {
    StringAppender stringAppender = new()
    {
      Layout = NewPatternLayout("[%date{yyyyMMdd HH:mm:ss.ffffff}] [%-5level] [%thread] - %message%newline")
    };

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog logger = LogManager.GetLogger(rep.Name, nameof(ConvertMultipleMicrosecondsPatternTest));

    for (int i = 0; i < 100; i++)
    {
      logger.Info(null);
      Thread.Sleep(1);
    }
    string[] lines = stringAppender.GetString().Split('\n');
    Assert.That(lines, Has.Length.EqualTo(lines.Distinct().Count()), "no duplicate timestamps allowed");
  }
#endif

  [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Reflection")]
  private sealed class MessageAsNamePatternConverter : NamedPatternConverter
  {
    protected override string GetFullyQualifiedName(LoggingEvent loggingEvent) => loggingEvent.MessageObject?.ToString() ?? string.Empty;
  }
}