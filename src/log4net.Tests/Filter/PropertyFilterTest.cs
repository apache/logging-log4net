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

using log4net.Core;
using log4net.Filter;
using log4net.Repository;
using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Filter;

/// <summary>
/// Tests for <see cref="PropertyFilter"/>, which decides a property value through the same
/// matching as <see cref="StringMatchFilter"/>.
/// </summary>
[TestFixture]
public class PropertyFilterTest
{
  private const string Key = "user";

  /// <summary>
  /// A pattern that backtracks, with a value that makes it do so.
  /// </summary>
  private const string CatastrophicPattern = "^(a+)+$";

  private const string CraftedValue = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!";

  /// <summary>
  /// A property value is content too, so an ignorable character must not force a match here
  /// either.
  /// </summary>
  [TestCase("\0", TestName = "ANulDoesNotForceAPropertyMatch")]
  [TestCase("­", TestName = "ASoftHyphenDoesNotForceAPropertyMatch")]
  public void AnIgnorableCharacterDoesNotForceAPropertyMatch(string ignorable)
  {
    PropertyFilter filter = new() { Key = Key, StringToMatch = "admin", AcceptOnMatch = true };
    filter.ActivateOptions();

    Assert.That(filter.Decide(CreateEvent($"adm{ignorable}in")), Is.EqualTo(FilterDecision.Neutral));
  }

  /// <summary>
  /// The exact value still decides, so the ordinal comparison has not broken the filter.
  /// </summary>
  [Test]
  public void AnExactPropertyValueStillMatches()
  {
    PropertyFilter filter = new() { Key = Key, StringToMatch = "admin", AcceptOnMatch = false };
    filter.ActivateOptions();

    Assert.That(filter.Decide(CreateEvent("admin")), Is.EqualTo(FilterDecision.Deny));
    Assert.That(filter.Decide(CreateEvent("guest")), Is.EqualTo(FilterDecision.Neutral));
  }

  /// <summary>
  /// An abandoned match is decided the same way as in the base filter.
  /// </summary>
  [Test]
  public void AnAbandonedPropertyMatchHonoursTimeoutDecision()
  {
    PropertyFilter filter = new()
    {
      Key = Key,
      RegexToMatch = CatastrophicPattern,
      MatchTimeoutMillis = 50,
      TimeoutDecision = FilterDecision.Accept
    };
    filter.ActivateOptions();

    FilterDecision decision = FilterDecision.Neutral;
    LogLog.ExecuteWithoutEmittingInternalMessages(() => decision = filter.Decide(CreateEvent(CraftedValue)));

    Assert.That(decision, Is.EqualTo(FilterDecision.Accept));
  }

  /// <summary>
  /// Without a key there is nothing to look up, so the chain continues.
  /// </summary>
  [Test]
  public void AFilterWithoutAKeyIsNeutral()
  {
    PropertyFilter filter = new() { StringToMatch = "admin" };
    filter.ActivateOptions();

    Assert.That(filter.Decide(CreateEvent("admin")), Is.EqualTo(FilterDecision.Neutral));
  }

  /// <summary>
  /// Builds an event carrying <paramref name="value"/> under <see cref="Key"/>. The filter renders
  /// the property through the repository, so the event needs one.
  /// </summary>
  private static LoggingEvent CreateEvent(string value)
  {
    ILoggerRepository repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
    LoggingEventData data = new() { Level = Level.Info, Message = "TestMessage", LoggerName = "TestLogger" };
    LoggingEvent loggingEvent = new(null, repository, data);
    loggingEvent.Properties[Key] = value;
    return loggingEvent;
  }
}
