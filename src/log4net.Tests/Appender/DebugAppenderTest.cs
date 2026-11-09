/*
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
using System.Diagnostics;
using System.Reflection;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender.Internal;
using NUnit.Framework;

namespace log4net.Tests.Appender;

[TestFixture]
[NonParallelizable]
public class DebugAppenderTest
{
  [Test]
  public void NullCategoryTest()
  {
    CategoryTraceListener categoryTraceListener = new();
    Debug.Listeners.Add(categoryTraceListener);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

    DebugAppender debugAppender = new()
    {
      Layout = new SimpleLayout()
    };
    debugAppender.ActivateOptions();

    debugAppender.Category = null;

    TestErrorHandler testErrHandler = new();
    debugAppender.ErrorHandler = testErrHandler;

    BasicConfigurator.Configure(rep, debugAppender);

    ILog log = LogManager.GetLogger(rep.Name, GetType());
    log.Debug("Message");

    Assert.That(categoryTraceListener.Category, Is.Null);

    Assert.That(testErrHandler.ErrorOccured, Is.False);

    Debug.Listeners.Remove(categoryTraceListener);
  }

  [Test]
  public void EmptyStringCategoryTest()
  {
    CategoryTraceListener categoryTraceListener = new();
    Debug.Listeners.Add(categoryTraceListener);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

    DebugAppender debugAppender = new()
    {
      Layout = new SimpleLayout()
    };
    debugAppender.ActivateOptions();

    debugAppender.Category = new PatternLayout("");

    BasicConfigurator.Configure(rep, debugAppender);

    ILog log = LogManager.GetLogger(rep.Name, GetType());
    log.Debug("Message");

    Assert.That(categoryTraceListener.Category, Is.Null);

    Debug.Listeners.Remove(categoryTraceListener);
  }

  [Test]
  public void DefaultCategoryTest()
  {
    CategoryTraceListener categoryTraceListener = new();
    Debug.Listeners.Add(categoryTraceListener);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

    DebugAppender debugAppender = new()
    {
      Layout = new SimpleLayout()
    };
    debugAppender.ActivateOptions();

    BasicConfigurator.Configure(rep, debugAppender);

    ILog log = LogManager.GetLogger(rep.Name, GetType());
    log.Debug("Message");

    Assert.That(
      categoryTraceListener.Category,
      Is.EqualTo(GetType().ToString()));

    Debug.Listeners.Remove(categoryTraceListener);
  }

  [Test]
  public void MethodNameCategoryTest()
  {
    CategoryTraceListener categoryTraceListener = new();
    Debug.Listeners.Add(categoryTraceListener);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

    DebugAppender debugAppender = new();
    PatternLayout methodLayout = new("%method");
    methodLayout.ActivateOptions();
    debugAppender.Category = methodLayout;
    debugAppender.Layout = new SimpleLayout();
    debugAppender.ActivateOptions();

    BasicConfigurator.Configure(rep, debugAppender);

    ILog log = LogManager.GetLogger(rep.Name, GetType());
    log.Debug("Message");

    Assert.That(
        categoryTraceListener.Category,
        Is.EqualTo(MethodInfo.GetCurrentMethod().Name));

    Debug.Listeners.Remove(categoryTraceListener);
  }

  /// <summary>
  /// The default listener hands the text to a native API that stops at a NUL, which would drop
  /// whatever the layout rendered after it.
  /// </summary>
  [Test]
  public void NulCharactersAreEscaped()
  {
    CategoryTraceListener categoryTraceListener = new();
    Debug.Listeners.Add(categoryTraceListener);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

    DebugAppender debugAppender = new() { Layout = new PatternLayout("%message tail") };
    debugAppender.ActivateOptions();

    BasicConfigurator.Configure(rep, debugAppender);

    LogManager.GetLogger(rep.Name, GetType()).Debug("before\0after");

    Assert.That(categoryTraceListener.Message, Is.EqualTo(@"before\0after tail"));

    Debug.Listeners.Remove(categoryTraceListener);
  }

  /// <summary>Without a category the appender takes a different <c>Debug.Write</c> overload.</summary>
  /// <param name="categoryPattern">The category pattern, <see langword="null"/> for no category.</param>
  [TestCase(null, TestName = "NoCategory")]
  [TestCase("", TestName = "EmptyCategory")]
  public void NulCharactersAreEscapedWithoutACategory(string? categoryPattern)
  {
    CategoryTraceListener categoryTraceListener = new();
    Debug.Listeners.Add(categoryTraceListener);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

    PatternLayout? category = categoryPattern is null ? null : new(categoryPattern);
    category?.ActivateOptions();
    DebugAppender debugAppender = new() { Layout = new PatternLayout("%message tail"), Category = category };
    debugAppender.ActivateOptions();

    BasicConfigurator.Configure(rep, debugAppender);

    LogManager.GetLogger(rep.Name, GetType()).Debug("before\0after");

    Assert.That(categoryTraceListener.Message, Is.EqualTo(@"before\0after tail"));

    Debug.Listeners.Remove(categoryTraceListener);
  }

  /// <summary>A category that renders content reaches the same native call, ahead of the message.</summary>
  [Test]
  public void NulCharactersInTheCategoryAreEscaped()
  {
    CategoryTraceListener categoryTraceListener = new();
    Debug.Listeners.Add(categoryTraceListener);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

    PatternLayout category = new("%property{cat}");
    category.ActivateOptions();
    DebugAppender debugAppender = new() { Layout = new SimpleLayout(), Category = category };
    debugAppender.ActivateOptions();

    BasicConfigurator.Configure(rep, debugAppender);

    ThreadContext.Properties["cat"] = "a\0b";
    LogManager.GetLogger(rep.Name, GetType()).Debug("Message");
    ThreadContext.Properties.Remove("cat");

    Assert.That(categoryTraceListener.Category, Is.EqualTo(@"a\0b"));

    Debug.Listeners.Remove(categoryTraceListener);
  }

  /// <summary>A listener that cannot be flushed must not reach the caller of <c>Flush</c>.</summary>
  [Test]
  public void FlushReportsAFailingListener()
  {
    UnflushableTraceListener listener = new();
    Debug.Listeners.Add(listener);
    try
    {
      RecordingErrorHandler errorHandler = new();
      DebugAppender debugAppender = new() { Layout = new SimpleLayout(), ImmediateFlush = false, ErrorHandler = errorHandler };
      debugAppender.ActivateOptions();

      Assert.That(debugAppender.Flush(1000), Is.False);
      Assert.That(errorHandler.Messages, Has.Count.EqualTo(1));
    }
    finally
    {
      Debug.Listeners.Remove(listener);
    }
  }

  private sealed class TestErrorHandler : IErrorHandler
  {
    public bool ErrorOccured { get; private set; }

    public void Error(string message, Exception? e, ErrorCode errorCode)
      => ErrorOccured = true;

    public void Error(string message, Exception e)
      => Error(message, e, ErrorCode.GenericFailure);

    public void Error(string message)
      => Error(message, null, ErrorCode.GenericFailure);
  }
}
#endif