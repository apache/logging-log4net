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
  /// Tests the <see cref="LogLog.LogReceivedAdapter"/> class.
  /// </summary>
  [Test]
  public void LogReceivedAdapter()
  {
    List<LogLog> messages = [];

    using LogLog.LogReceivedAdapter _ = new(messages);
    LogLog.Debug(GetType(), "Won't be recorded");
    LogLog.Error(GetType(), "This will be recorded.");
    LogLog.Error(GetType(), "This will be recorded.");

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