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
using System.Diagnostics;

using log4net.Core;
using log4net.Filter;
using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Filter;

/// <summary>
/// Tests for <see cref="StringMatchFilter"/>
/// </summary>
[TestFixture]
public class StringMatchFilterTest
{
  /// <summary>
  /// A pattern that backtracks, with an input that makes it do so. Matching this without a deadline
  /// runs for longer than any test would wait.
  /// </summary>
  private const string CatastrophicPattern = "^(a+)+$";

  private const string CraftedMessage = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!";

  /// <summary>
  /// The match runs while the appender lock is held, so one that backtracks has to be abandoned
  /// rather than holding the thread. The event is then left to the rest of the filter chain.
  /// </summary>
  [Test]
  public void AMatchThatBacktracksIsAbandoned()
  {
    StringMatchFilter filter = new() { RegexToMatch = CatastrophicPattern, MatchTimeoutMillis = 100 };
    filter.ActivateOptions();

    Stopwatch stopwatch = Stopwatch.StartNew();
    FilterDecision decision = FilterDecision.Accept;
    LogLog.ExecuteWithoutEmittingInternalMessages(() => decision = filter.Decide(CreateEvent(CraftedMessage)));
    stopwatch.Stop();

    Assert.That(decision, Is.EqualTo(FilterDecision.Neutral));
    Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(30)));
  }

  /// <summary>
  /// Abandoning the match must not be silent, but it must also not report once per event: the
  /// condition repeats for every event that reaches the filter.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void AnAbandonedMatchIsReportedOnce()
  {
    StringMatchFilter filter = new() { RegexToMatch = CatastrophicPattern, MatchTimeoutMillis = 100 };
    filter.ActivateOptions();

    List<LogLog> messages = [];
    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      using LogLog.LogReceivedAdapter _ = new(messages);
      filter.Decide(CreateEvent(CraftedMessage));
      filter.Decide(CreateEvent(CraftedMessage));
      filter.Decide(CreateEvent(CraftedMessage));
    });

    Assert.That(
      messages.ConvertAll(m => m.Message).FindAll(m => m.IndexOf("was abandoned", StringComparison.Ordinal) >= 0),
      Has.Count.EqualTo(1));
  }

  /// <summary>
  /// A pattern that does not backtrack has to keep working, with the decision unchanged.
  /// </summary>
  [Test]
  public void AMatchingPatternStillDecides()
  {
    StringMatchFilter filter = new() { RegexToMatch = "cat", AcceptOnMatch = true };
    filter.ActivateOptions();

    Assert.That(filter.Decide(CreateEvent("the cat sat")), Is.EqualTo(FilterDecision.Accept));
    Assert.That(filter.Decide(CreateEvent("the dog sat")), Is.EqualTo(FilterDecision.Neutral));
  }

  /// <summary>
  /// The deadline has to be finite by default, so that an expensive pattern cannot hold the
  /// appender lock indefinitely without the operator having opted into that.
  /// </summary>
  [Test]
  public void MatchTimeoutMillisDefaultsToAFiniteValue()
    => Assert.That(new StringMatchFilter().MatchTimeoutMillis, Is.EqualTo(1000));

  /// <summary>
  /// 0 is the documented opt-out that restores unbounded matching; a negative deadline has no
  /// meaning and is rejected rather than being reinterpreted.
  /// </summary>
  [Test]
  public void MatchTimeoutMillisRejectsNegativeValuesButAllowsZero()
  {
    StringMatchFilter filter = new();

    Assert.That(() => filter.MatchTimeoutMillis = -1, Throws.TypeOf<ArgumentOutOfRangeException>());

    filter.MatchTimeoutMillis = 0;
    Assert.That(filter.MatchTimeoutMillis, Is.EqualTo(0));
  }

  private static LoggingEvent CreateEvent(string message)
    => new(new LoggingEventData { Level = Level.Info, Message = message, LoggerName = "TestLogger" });
}
