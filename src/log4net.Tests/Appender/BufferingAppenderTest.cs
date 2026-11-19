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
using System.Collections.Generic;
using System.Linq;

using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Tests.Appender.Internal;

using NUnit.Framework;

using static log4net.Tests.TestThreads;

namespace log4net.Tests.Appender;

/// <summary>
/// Used for internal unit testing the <see cref="BufferingAppenderSkeleton"/> class.
/// </summary>
[TestFixture]
public class BufferingAppenderTest
{
  private BufferingForwardingAppender _bufferingForwardingAppender = new();
  private CountingAppender _countingAppender = new();
  private Repository.Hierarchy.Hierarchy _hierarchy = new();

  private void SetupRepository()
  {
    _hierarchy = new Repository.Hierarchy.Hierarchy();

    _countingAppender = new CountingAppender();
    _countingAppender.ActivateOptions();

    _bufferingForwardingAppender = new BufferingForwardingAppender();
    _bufferingForwardingAppender.AddAppender(_countingAppender);

    _bufferingForwardingAppender.BufferSize = 0;
    _bufferingForwardingAppender.ClearFilters();
    _bufferingForwardingAppender.Evaluator = null;
    _bufferingForwardingAppender.Fix = FixFlags.Partial;
    _bufferingForwardingAppender.Lossy = false;
    _bufferingForwardingAppender.LossyEvaluator = null;
    _bufferingForwardingAppender.Threshold = Level.All;

    _bufferingForwardingAppender.ActivateOptions();

    BasicConfigurator.Configure(_hierarchy, _bufferingForwardingAppender);
  }

  [Test]
  public void TestSetupAppender()
  {
    SetupRepository();

    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test empty appender");

    ILogger logger = _hierarchy.GetLogger("test");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message logged", null);

    Assert.That(_countingAppender.Counter, Is.EqualTo(1), "Test 1 event logged");
  }

  [Test]
  public void TestBufferSize5()
  {
    SetupRepository();

    _bufferingForwardingAppender.BufferSize = 5;
    _bufferingForwardingAppender.ActivateOptions();

    Assert.That(_countingAppender.Counter, Is.EqualTo(0));

    ILogger logger = _hierarchy.GetLogger("test");

    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 1", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test 1 event in buffer");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 2", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test 2 event in buffer");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 3", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test 3 event in buffer");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 4", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test 4 event in buffer");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 5", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test 5 event in buffer");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 6", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(6), "Test 0 event in buffer. 6 event sent");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 7", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(6), "Test 1 event in buffer. 6 event sent");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 8", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(6), "Test 2 event in buffer. 6 event sent");
  }

  /// <summary>The buffer is emptied, so the answer is yes.</summary>
  [Test]
  public void FlushReportsSuccessWhenTheBufferIsEmptied()
  {
    SetupRepository();
    _bufferingForwardingAppender.BufferSize = 5;
    _bufferingForwardingAppender.ActivateOptions();

    ILogger logger = _hierarchy.GetLogger("test");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message", null);

    Assert.That(_bufferingForwardingAppender.Flush(0), Is.True);
    Assert.That(_countingAppender.Counter, Is.EqualTo(1));
  }

  /// <summary>
  /// A lossy appender keeps its events until something triggers them, so reporting that they
  /// were flushed would be untrue.
  /// </summary>
  [Test]
  public void FlushReportsFailureWhileALossyBufferKeepsItsEvents()
  {
    SetupRepository();
    _bufferingForwardingAppender.BufferSize = 5;
    _bufferingForwardingAppender.Lossy = true;
    _bufferingForwardingAppender.Evaluator = new LevelEvaluator(Level.Off);
    _bufferingForwardingAppender.ActivateOptions();

    ILogger logger = _hierarchy.GetLogger("test");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message", null);

    Assert.That(_bufferingForwardingAppender.Flush(0), Is.False);
    Assert.That(_countingAppender.Counter, Is.EqualTo(0));
  }

  /// <summary>
  /// A second <see cref="BufferingAppenderSkeleton.ActivateOptions"/> used to drop the buffered events.
  /// With the same size the buffer is kept until the next flush.
  /// </summary>
  [Test]
  public void AReactivationWithTheSameSizeKeepsTheBuffer()
  {
    BatchRecordingAppender appender = CreateBufferWithThreeEvents();

    appender.ActivateOptions();

    Assert.That(appender.Batches, Is.Empty);
    appender.Flush();
    Assert.That(appender.Batches, Is.EqualTo(new[] { new[] { "1", "2", "3" } }));
  }

  /// <summary>
  /// The first activation, or one without a buffer, has nothing to send, so it does not flush.
  /// </summary>
  [Test]
  public void AnActivationWithoutAnOldBufferDoesNotFlush()
  {
    BatchRecordingAppender appender = new() { BufferSize = 5 };
    appender.ActivateOptions();
    BatchRecordingAppender unbuffered = new() { BufferSize = 1 };
    unbuffered.ActivateOptions();
    unbuffered.ActivateOptions();

    Assert.That(appender.FlushCalls, Is.Zero);
    Assert.That(unbuffered.FlushCalls, Is.Zero);
  }

  /// <summary>
  /// Any other size sends the old buffer as one batch before the new one takes over, as a close would.
  /// </summary>
  [TestCase(1)]
  [TestCase(2)]
  [TestCase(3)]
  [TestCase(10)]
  public void AReactivationWithANewSizeSendsTheOldBufferFirst(int newSize)
  {
    BatchRecordingAppender appender = CreateBufferWithThreeEvents();

    appender.BufferSize = newSize;
    appender.ActivateOptions();
    appender.DoAppend(CreateEvent("4"));
    appender.Flush();

    Assert.That(appender.Batches, Is.EqualTo(new[] { new[] { "1", "2", "3" }, new[] { "4" } }));
  }

  /// <summary>
  /// A lossy buffer sends only what its <see cref="BufferingAppenderSkeleton.LossyEvaluator"/> accepts.
  /// </summary>
  [Test]
  public void ALossyReactivationSendsOnlyTheLossyEvaluatorHits()
  {
    BatchRecordingAppender appender = new()
    {
      BufferSize = 5,
      Lossy = true,
      Evaluator = new LevelEvaluator(Level.Error),
      LossyEvaluator = new LevelEvaluator(Level.Warn)
    };
    appender.ActivateOptions();
    appender.DoAppend(CreateEvent("1"));
    appender.DoAppend(CreateEvent("2", Level.Warn));
    appender.DoAppend(CreateEvent("3"));

    appender.BufferSize = 2;
    appender.ActivateOptions();

    Assert.That(appender.Batches, Is.EqualTo(new[] { new[] { "2" } }));
  }

  /// <summary>
  /// A <c>Fix</c> widened with the resize does not touch the old events: they keep their fix, and
  /// so get no identity from the activating thread.
  /// </summary>
  [Test]
  public void AReactivationLeavesTheOldEventsAsTheyWereFixed()
  {
    BatchRecordingAppender appender = new() { BufferSize = 5, Fix = FixFlags.Partial };
    appender.ActivateOptions();
    RunAs("alice", () =>
    {
      foreach (string message in new[] { "1", "2", "3" })
      {
        appender.DoAppend(CreateEvent(message));
      }
    });

    RunAs("bob", () =>
    {
      appender.Fix = FixFlags.All;
      appender.BufferSize = 2;
      appender.ActivateOptions();
    });

    Assert.That(appender.SentEvents, Has.Count.EqualTo(3));
    Assert.That(appender.SentEvents.Select(e => e.Fix), Is.All.EqualTo(FixFlags.Partial));
    Assert.That(appender.SentEvents.Select(e => e.Identity), Is.All.Null);
  }

  /// <summary>
  /// A send that throws during the reactivation is reported and does not keep the new buffer out.
  /// </summary>
  [Test]
  public void AReactivationSendThatThrowsIsReportedAndTheNewBufferWorks()
  {
    RecordingErrorHandler errorHandler = new();
    BatchRecordingAppender appender = CreateBufferWithThreeEvents(throwFor: "2");
    appender.ErrorHandler = errorHandler;

    appender.BufferSize = 2;
    Assert.That(appender.ActivateOptions, Throws.Nothing);
    appender.DoAppend(CreateEvent("4"));
    appender.Flush();

    Assert.That(errorHandler.Messages, Has.Some.Contains("ActivateOptions"));
    Assert.That(appender.Batches, Is.EqualTo(new[] { new[] { "4" } }));
  }

  /// <summary>
  /// A buffer of five holding the events 1, 2 and 3.
  /// </summary>
  private static BatchRecordingAppender CreateBufferWithThreeEvents(string? throwFor = null)
  {
    BatchRecordingAppender appender = new() { BufferSize = 5, ThrowFor = throwFor };
    appender.ActivateOptions();
    foreach (string message in new[] { "1", "2", "3" })
    {
      appender.DoAppend(CreateEvent(message));
    }
    return appender;
  }

  private static LoggingEvent CreateEvent(string message, Level? level = null)
    => new(typeof(BufferingAppenderTest), null, "test", level ?? Level.Info, message, null);

  /// <summary>Records each batch it sends and throws for <see cref="ThrowFor"/>.</summary>
  private sealed class BatchRecordingAppender : BufferingAppenderSkeleton
  {
    internal List<string[]> Batches { get; } = [];

    internal List<LoggingEvent> SentEvents { get; } = [];

    internal string? ThrowFor { get; init; }

    internal int FlushCalls { get; private set; }

    /// <inheritdoc/>
    public override void Flush(bool flushLossyBuffer)
    {
      FlushCalls++;
      base.Flush(flushLossyBuffer);
    }

    /// <inheritdoc/>
    protected override void SendBuffer(LoggingEvent[] events)
    {
      if (events.Any(e => e.RenderedMessage == ThrowFor))
      {
        throw new InvalidOperationException("send failed");
      }
      Batches.Add([.. events.Select(e => e.RenderedMessage!)]);
      SentEvents.AddRange(events);
    }
  }
}
