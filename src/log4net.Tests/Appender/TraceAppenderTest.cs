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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender.Internal;
using NUnit.Framework;

namespace log4net.Tests.Appender;

[TestFixture]
[NonParallelizable]
public class TraceAppenderTest
{
  [Test]
  public void DefaultCategoryTest()
  {
    var categoryTraceListener = new CategoryTraceListener();
    Trace.Listeners.Clear();
    Trace.Listeners.Add(categoryTraceListener);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

    var traceAppender = new TraceAppender
    {
      Layout = new SimpleLayout()
    };
    traceAppender.ActivateOptions();

    BasicConfigurator.Configure(rep, traceAppender);

    ILog log = LogManager.GetLogger(rep.Name, GetType());
    log.Debug("Message");

    Assert.That(
        categoryTraceListener.Category,
        Is.EqualTo(GetType().ToString()));
  }

  [Test]
  public void MethodNameCategoryTest()
  {
    var categoryTraceListener = new CategoryTraceListener();
    Trace.Listeners.Clear();
    Trace.Listeners.Add(categoryTraceListener);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

    var traceAppender = new TraceAppender();
    var methodLayout = new PatternLayout("%method");
    methodLayout.ActivateOptions();
    traceAppender.Category = methodLayout;
    traceAppender.Layout = new SimpleLayout();
    traceAppender.ActivateOptions();

    BasicConfigurator.Configure(rep, traceAppender);

    ILog log = LogManager.GetLogger(rep.Name, GetType());
    log.Debug("Message");

    Assert.That(categoryTraceListener.Category,
      Is.EqualTo(MethodBase.GetCurrentMethod()!.Name));
  }

  /// <summary>
  /// The default listener hands the text to a native API that stops at a NUL, which would drop
  /// whatever the layout rendered after it.
  /// </summary>
  [Test]
  public void NulCharactersAreEscaped()
  {
    CategoryTraceListener listener = new();
    Trace.Listeners.Clear();
    Trace.Listeners.Add(listener);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

    TraceAppender traceAppender = new() { Layout = new PatternLayout("%message tail") };
    traceAppender.ActivateOptions();

    BasicConfigurator.Configure(rep, traceAppender);

    LogManager.GetLogger(rep.Name, GetType()).Debug("before\0after");

    Assert.That(listener.Message, Is.EqualTo(@"before\0after tail"));
  }

  /// <summary>A category that renders content reaches the same native call, ahead of the message.</summary>
  [Test]
  public void NulCharactersInTheCategoryAreEscaped()
  {
    CategoryTraceListener listener = new();
    Trace.Listeners.Clear();
    Trace.Listeners.Add(listener);

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

    PatternLayout category = new("%property{cat}");
    category.ActivateOptions();
    TraceAppender traceAppender = new() { Layout = new SimpleLayout(), Category = category };
    traceAppender.ActivateOptions();

    BasicConfigurator.Configure(rep, traceAppender);

    ThreadContext.Properties["cat"] = "a\0b";
    LogManager.GetLogger(rep.Name, GetType()).Debug("Message");
    ThreadContext.Properties.Remove("cat");

    Assert.That(listener.Category, Is.EqualTo(@"a\0b"));
  }

  /// <summary>A listener that cannot be flushed must not reach the caller of <c>Flush</c>.</summary>
  [Test]
  public void FlushReportsAFailingListener()
  {
    Trace.Listeners.Clear();
    Trace.Listeners.Add(new UnflushableTraceListener());
    try
    {
      RecordingErrorHandler errorHandler = new();
      TraceAppender traceAppender = new() { Layout = new SimpleLayout(), ImmediateFlush = false, ErrorHandler = errorHandler };
      traceAppender.ActivateOptions();

      Assert.That(traceAppender.Flush(1000), Is.False);
      Assert.That(errorHandler.Messages, Has.Count.EqualTo(1));
    }
    finally
    {
      Trace.Listeners.Clear();
    }
  }
}

/// <summary>Stands in for a listener whose stream has failed.</summary>
public sealed class UnflushableTraceListener : TraceListener
{
  /// <inheritdoc/>
  public override void Write(string? message)
  { }

  /// <inheritdoc/>
  public override void WriteLine(string? message)
  { }

  /// <inheritdoc/>
  public override void Flush() => throw new IOException("Simulated failure to flush");
}

/// <summary>Records what the trace and debug appenders hand to a listener.</summary>
public class CategoryTraceListener : TraceListener
{
  /// <inheritdoc/>
  public override void Write(string? message) => Message = message;

  /// <inheritdoc/>
  public override void WriteLine(string? message) => Write(message);

  /// <inheritdoc/>
  public override void Write(string? message, string? category)
  {
    // The base call lands in Write(string) with the category in front.
    base.Write(message, category);
    Category = category;
    Message = message;
  }

  /// <summary>The category of the last write that carried one.</summary>
  public string? Category { get; private set; }

  /// <summary>The text of the last write.</summary>
  public string? Message { get; private set; }
}
