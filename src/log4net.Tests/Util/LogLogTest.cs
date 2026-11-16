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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using log4net.Config;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Util;

[TestFixture]
public class LogLogTest
{
  /// <summary>
  /// Tests the <see cref="TraceListener"/> functionality
  /// </summary>
  [Test]
  public void TraceListenerCounterTest()
  {
    TraceListenerCounter listTraceListener = new();

    Trace.Listeners.Clear();
    Trace.Listeners.Add(listTraceListener);

    Trace.Write("Hello");
    Trace.Write("World");

    Assert.That(listTraceListener.Count, Is.EqualTo(2));
  }

  /// <summary>
  /// Tests the <see cref="LogLog.EmitInternalMessages"/> property
  /// </summary>
  [Test]
  public void EmitInternalMessages()
  {
    TraceListenerCounter listTraceListener = new();
    Trace.Listeners.Clear();
    Trace.Listeners.Add(listTraceListener);
    // Emitting is the subject here, so these two reach the console by design.
    LogLog.Error(GetType(), "Hello");
    LogLog.Error(GetType(), "World");
    Trace.Flush();
    Assert.That(listTraceListener.Count, Is.EqualTo(2));

    try
    {
      LogLog.EmitInternalMessages = false;

      LogLog.Error(GetType(), "Hello");
      LogLog.Error(GetType(), "World");
      Assert.That(listTraceListener.Count, Is.EqualTo(2));
    }
    finally
    {
      LogLog.EmitInternalMessages = true;
    }
  }

  /// <summary>
  /// An exception whose own <c>ToString()</c> throws is written as a fallback text, not rethrown.
  /// </summary>
  [TestCase(nameof(LogLog.Warn))]
  [TestCase(nameof(LogLog.Debug))]
  [TestCase(nameof(LogLog.Error))]
  [NonParallelizable]
  public void UnrenderableExceptionIsNotRethrown(string method)
  {
    TraceListenerRecorder recorder = new();
    Trace.Listeners.Add(recorder);
    bool internalDebugging = LogLog.InternalDebugging;
    try
    {
      // The exception text is only rendered when internal messages are emitted, so these reach the console.
      LogLog.InternalDebugging = true;
      Action<Type, string, Exception?> log = method switch
      {
        nameof(LogLog.Debug) => LogLog.Debug,
        nameof(LogLog.Warn) => LogLog.Warn,
        _ => LogLog.Error
      };
      Assert.That(() => log(GetType(), "message", new UnrenderableException()), Throws.Nothing);
    }
    finally
    {
      LogLog.InternalDebugging = internalDebugging;
      Trace.Listeners.Remove(recorder);
    }

    Assert.That(recorder.Lines, Has.Some.EqualTo(
      "log4net.Tests.Util.UnrenderableException: <Exception while rendering exception: System.InvalidOperationException>"));
  }

  /// <summary>
  /// A format argument whose <c>ToString()</c> throws an exception that cannot render itself used to
  /// escape <see cref="ILog.InfoFormat(string, object?)"/>, twice because the error handler reports once.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void FormatArgumentThrowingAnUnrenderableExceptionDoesNotThrow()
  {
    StringAppender appender = new() { Layout = new PatternLayout("%message%newline") };
    ILoggerRepository repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(repository, appender);
    ILog log = LogManager.GetLogger(repository.Name, "test");

    // The defect needs internal messages emitted, so the warnings reach the console.
    Assert.That(() => log.InfoFormat("{0}", new ThrowsUnrenderableException()), Throws.Nothing);
    Assert.That(() => log.InfoFormat("{0}", new ThrowsUnrenderableException()), Throws.Nothing);
    Assert.That(appender.GetString().Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries),
      Has.Exactly(2).StartsWith("<log4net.Error>"));
  }

  /// <summary>
  /// Tests the <see cref="LogLog.LogReceivedAdapter"/> class.
  /// </summary>
  [Test]
  public void LogReceivedAdapter()
  {
    List<LogLog> messages = [];

    using LogLog.LogReceivedAdapter _ = new(messages);
    // The adapter is fed either way, so this only keeps it off the console.
    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      LogLog.Debug(GetType(), "Won't be recorded");
      LogLog.Error(GetType(), "This will be recorded.");
      LogLog.Error(GetType(), "This will be recorded.");
    });

    Assert.That(messages, Has.Count.EqualTo(2));
  }
  
  /// <summary>
  /// Tests multi threaded calls to <see cref="LogLog.OnLogReceived"/>
  /// </summary>
  [Test]
  public void LogReceivedAdapterThreading()
  {
    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      for (int i = 0; i < 1000; i++)
      {
        LogReceivedAdapterThreadingCore(i);
      }
    });
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", 
    Justification = "no cryptography")]
  private void LogReceivedAdapterThreadingCore(int seed)
  {
    List<LogLog> messages = new(1);
    object syncRoot = ((ICollection)messages).SyncRoot;
    Random random = new(seed);
    using LogLog.LogReceivedAdapter _ = new(messages);
    Parallel.For(0, 10, i =>
    {
      if (random.Next(10) > 8)
      {
        lock (syncRoot)
        {
          messages.Clear();
          messages.Capacity = 1;
        }
      }
      LogLog.Warn(GetType(), messages.Capacity.ToString() + ' ' + messages.Count);
    });
  }
}

/// <summary>
/// Mock for <see cref="TraceListener"/> that counts the number of calls to <see cref="Write(string?)"/>
/// </summary>
internal sealed class TraceListenerCounter : TraceListener
{
  /// <inheritdoc/>
  public override void Write(string? message) => Count++;

  /// <inheritdoc/>
  public override void WriteLine(string? message) => Write(message);

  /// <inheritdoc/>
  public void Reset() => Count = 0;

  /// <inheritdoc/>
  public int Count { get; private set; }
}

/// <summary>
/// Records the lines written to a <see cref="TraceListener"/>.
/// </summary>
internal sealed class TraceListenerRecorder : TraceListener
{
  /// <inheritdoc/>
  public override void Write(string? message) => Lines.Add(message ?? string.Empty);

  /// <inheritdoc/>
  public override void WriteLine(string? message) => Write(message);

  /// <summary>
  /// The lines written so far.
  /// </summary>
  public List<string> Lines { get; } = [];
}

/// <summary>
/// An exception whose <c>ToString()</c> throws.
/// </summary>
public sealed class UnrenderableException : Exception
{
  /// <inheritdoc/>
  public override string ToString() => throw new InvalidOperationException("ToString failed");
}

/// <summary>
/// An object whose <c>ToString()</c> throws an exception naming the object itself, so that exception's
/// <c>Message</c>, and with it its own <c>ToString()</c>, throws again.
/// </summary>
internal sealed class ThrowsUnrenderableException
{
  /// <inheritdoc/>
  public override string ToString() => throw new ArgumentOutOfRangeException("value", this, "unsupported");
}
