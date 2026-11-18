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
using System.Threading.Tasks;
using log4net.Filter;
using log4net.Appender;
using log4net.Core;
using log4net.Tests.Appender.Internal;
using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Unit tests for <see cref="AppenderSkeleton"/> filter chain management.
/// </summary>
[TestFixture]
public sealed class AppenderSkeletonTest
{
  private CountingAppender _appender = null!;

  [SetUp]
  public void SetUp() => _appender = new CountingAppender();

  [TearDown]
  public void TearDown() => _appender.Close();

  /// <summary>
  /// Verifies that <see cref="AppenderSkeleton.AddFilter"/> sets <see cref="AppenderSkeleton.FilterHead"/> when the chain is empty.
  /// </summary>
  [Test]
  public void AddFilter_FirstFilter_SetsFilterHead()
  {
    DenyAllFilter filter = new();
    _appender.AddFilter(filter);
    Assert.That(_appender.FilterHead, Is.SameAs(filter));
  }

  /// <summary>
  /// Verifies that a second <see cref="AppenderSkeleton.AddFilter"/> call appends the filter via <c>IFilter.Next</c>.
  /// </summary>
  [Test]
  public void AddFilter_SecondFilter_LinksToChain()
  {
    DenyAllFilter first = new();
    DenyAllFilter second = new();
    _appender.AddFilter(first);
    _appender.AddFilter(second);
    Assert.That(_appender.FilterHead, Is.SameAs(first));
    Assert.That(_appender.FilterHead!.Next, Is.SameAs(second));
  }

  /// <summary>
  /// Verifies that <see cref="AppenderSkeleton.ClearFilters"/> resets <see cref="AppenderSkeleton.FilterHead"/> to null.
  /// </summary>
  [Test]
  public void ClearFilters_ResetsFilterHead()
  {
    _appender.AddFilter(new DenyAllFilter());
    _appender.ClearFilters();
    Assert.That(_appender.FilterHead, Is.Null);
  }

  /// <summary>
  /// Verifies that filters can be added again after <see cref="AppenderSkeleton.ClearFilters"/>.
  /// </summary>
  [Test]
  public void ClearFilters_AllowsAddingFiltersAgain()
  {
    _appender.AddFilter(new DenyAllFilter());
    _appender.ClearFilters();
    DenyAllFilter filter = new();
    _appender.AddFilter(filter);
    Assert.That(_appender.FilterHead, Is.SameAs(filter));
  }

  /// <summary>
  /// One event of a batch that throws in <c>Append</c> costs only that event.
  /// </summary>
  [Test]
  public void BulkAppendKeepsTheEventsAfterOneThatThrows()
  {
    RecordingErrorHandler errorHandler = new();
    MessageRecordingAppender appender = new() { ErrorHandler = errorHandler, ThrowFor = "2" };

    appender.DoAppend(CreateEvents("1", "2", "3", "4", "5"));

    Assert.That(appender.Messages, Is.EqualTo(new[] { "1", "3", "4", "5" }));
    Assert.That(errorHandler.Messages, Has.Count.EqualTo(1));
  }

  /// <summary>
  /// One event of a batch that makes a filter throw costs only that event.
  /// </summary>
  [Test]
  public void BulkFilterKeepsTheEventsAfterOneThatThrows()
  {
    RecordingErrorHandler errorHandler = new();
    MessageRecordingAppender appender = new() { ErrorHandler = errorHandler };
    appender.AddFilter(new ThrowingFilter());

    appender.DoAppend(CreateEvents("1", "2", "3", "4", "5"));

    Assert.That(appender.Messages, Is.EqualTo(new[] { "1", "3", "4", "5" }));
    Assert.That(errorHandler.Messages, Has.Count.EqualTo(1));
  }

  /// <summary>
  /// A filter decision outside the enum used to keep the filter loop spinning on the same filter.
  /// </summary>
  [Test]
  public void UndefinedFilterDecisionMovesOnToTheNextFilter()
  {
    MessageRecordingAppender appender = new();
    appender.AddFilter(new UndefinedDecisionFilter());
    appender.AddFilter(new DenyAllFilter());

    // Unfixed, DoAppend never returns, so it runs on another thread with a deadline.
    Task append = Task.Run(() => appender.DoAppend(CreateEvents("1")[0]));

    Assert.That(append.Wait(TimeSpan.FromSeconds(10)), Is.True, "DoAppend did not return");
    Assert.That(appender.Messages, Is.Empty, "the next filter, DenyAllFilter, decides");
  }

  /// <summary>
  /// A subclass that overrides only <c>Append(IEnumerable)</c> still gets the batch in one call.
  /// </summary>
  [Test]
  public void EnumerableOnlyOverrideGetsTheBatch()
  {
    EnumerableOnlyAppender appender = new();

    appender.DoAppend(CreateEvents("1", "2", "3"));

    Assert.That(appender.Batches, Is.EqualTo(new[] { 3 }));
  }

  /// <summary>
  /// An <c>Append(IEnumerable)</c> override that calls back into <c>Append(LoggingEvent[])</c>,
  /// with or without an array override calling base, appends each event once instead of recursing.
  /// </summary>
  [TestCase(false)]
  [TestCase(true)]
  public void EnumerableOverrideCallingBackDoesNotRecurse(bool withArrayOverride)
  {
    CallingBackAppender appender = withArrayOverride ? new CallingBackWithArrayOverrideAppender() : new();

    appender.DoAppend(CreateEvents("1", "2", "3"));

    Assert.That(appender.Messages, Is.EqualTo(new[] { "1", "2", "3" }));
  }

  /// <summary>
  /// After an <c>Append(IEnumerable)</c> override threw once, the next batch still reaches it.
  /// </summary>
  [Test]
  public void EnumerableOverrideIsReachedAgainAfterItThrew()
  {
    ThrowsOnceEnumerableAppender appender = new() { ErrorHandler = new RecordingErrorHandler() };

    appender.DoAppend(CreateEvents("1"));
    appender.DoAppend(CreateEvents("2"));

    Assert.That(appender.Calls, Is.EqualTo(2));
  }

  private static LoggingEvent[] CreateEvents(params string[] messages)
    => [.. messages.Select(message => new LoggingEvent(typeof(AppenderSkeletonTest), null, "test", Level.Info, message, null))];

  /// <summary>Overrides only <c>Append(IEnumerable)</c> and records the batch sizes.</summary>
  private sealed class EnumerableOnlyAppender : AppenderSkeleton
  {
    internal List<int> Batches { get; } = [];

    /// <inheritdoc/>
    protected override void Append(LoggingEvent loggingEvent) => Batches.Add(-1);

    /// <inheritdoc/>
    protected override void Append(IEnumerable<LoggingEvent> loggingEvents) => Batches.Add(loggingEvents.Count());
  }

  /// <summary>Its <c>Append(IEnumerable)</c> calls <c>Append(LoggingEvent[])</c>; a depth guard stops a recursion.</summary>
  private class CallingBackAppender : AppenderSkeleton
  {
    private int _depth;

    internal List<string> Messages { get; } = [];

    /// <inheritdoc/>
    protected override void Append(LoggingEvent loggingEvent) => Messages.Add(loggingEvent.RenderedMessage!);

    /// <inheritdoc/>
    protected override void Append(IEnumerable<LoggingEvent> loggingEvents)
    {
      if (++_depth > 10)
      {
        throw new InvalidOperationException("recursion");
      }
      try
      {
        Append(loggingEvents.ToArray());
      }
      finally
      {
        _depth--;
      }
    }
  }

  /// <summary>Also overrides <c>Append(LoggingEvent[])</c>, calling base.</summary>
  private sealed class CallingBackWithArrayOverrideAppender : CallingBackAppender
  {
    /// <inheritdoc/>
    protected override void Append(LoggingEvent[] loggingEvents) => base.Append(loggingEvents);
  }

  /// <summary>Its <c>Append(IEnumerable)</c> throws on the first call.</summary>
  private sealed class ThrowsOnceEnumerableAppender : AppenderSkeleton
  {
    internal int Calls { get; private set; }

    /// <inheritdoc/>
    protected override void Append(LoggingEvent loggingEvent)
    { }

    /// <inheritdoc/>
    protected override void Append(IEnumerable<LoggingEvent> loggingEvents)
    {
      if (++Calls == 1)
      {
        throw new InvalidOperationException("first batch failed");
      }
    }
  }

  /// <summary>Records the messages it appends and throws for <see cref="ThrowFor"/>.</summary>
  private sealed class MessageRecordingAppender : AppenderSkeleton
  {
    internal List<string> Messages { get; } = [];

    internal string? ThrowFor { get; init; }

    /// <inheritdoc/>
    protected override void Append(LoggingEvent loggingEvent)
    {
      if (loggingEvent.RenderedMessage == ThrowFor)
      {
        throw new InvalidOperationException("append failed");
      }
      Messages.Add(loggingEvent.RenderedMessage!);
    }
  }

  /// <summary>Returns a decision outside <see cref="FilterDecision"/>.</summary>
  private sealed class UndefinedDecisionFilter : FilterSkeleton
  {
    /// <inheritdoc/>
    public override FilterDecision Decide(LoggingEvent loggingEvent) => (FilterDecision)5;
  }

  /// <summary>Throws for the message "2", accepts the rest.</summary>
  private sealed class ThrowingFilter : FilterSkeleton
  {
    /// <inheritdoc/>
    public override FilterDecision Decide(LoggingEvent loggingEvent)
      => loggingEvent.RenderedMessage == "2" ? throw new InvalidOperationException("filter failed") : FilterDecision.Neutral;
  }
}
