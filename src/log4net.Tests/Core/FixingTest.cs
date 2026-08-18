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
using System.Linq;
using System.Threading;

using log4net.Core;
using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Core;

/// <summary>
/// Tests for <see cref="LoggingEvent.Fix"/> and the fields it captures.
/// </summary>
[TestFixture]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types")]
public class FixingTest
{
  /// <summary>
  /// The name of the repository the events under test belong to.
  /// </summary>
  private const string TestRepository = "Test Repository";

  /// <summary>
  /// Creates the repository the events under test belong to, and names the thread, so that
  /// <see cref="LoggingEvent.ThreadName"/> has something stable to capture.
  /// </summary>
  [OneTimeSetUp]
  public void CreateRepository()
  {
    bool exists = false;
    Repository.ILoggerRepository[] repositories = LogManager.GetAllRepositories();
    if (repositories is not null)
    {
      foreach (Repository.ILoggerRepository r in repositories)
      {
        if (r.Name == TestRepository)
        {
          exists = true;
          break;
        }
      }
    }
    if (!exists)
    {
      LogManager.CreateRepository(TestRepository);
    }

    // write-once
    if (Thread.CurrentThread.Name is null)
    {
      Thread.CurrentThread.Name = "Log4Net Test thread";
    }
  }

  /// <summary>
  /// <see cref="FixFlags.All"/> has to contain every other flag, so that fixing everything does not
  /// quietly leave a field out when a new flag is added.
  /// </summary>
  [Test]
  public void AllContainsEveryFlag()
  {
    // Arrange
    // Act
    var allFlags = Enum.GetValues(typeof(FixFlags)).Cast<FixFlags>()
      .Except([FixFlags.None])
      .ToArray();
    // Assert
    foreach (var flag in allFlags)
    {
      Assert.That(FixFlags.All & flag, Is.EqualTo(flag), $"FixFlags.All does not contain {flag}");
    }
  }

  /// <summary>
  /// A newly created event has nothing fixed yet.
  /// </summary>
  [Test]
  public void TestUnfixedValues()
  {
    LoggingEventData loggingEventData = BuildStandardEventData();

    // LoggingEvents occur at distinct points in time
    LoggingEvent loggingEvent = new(
      loggingEventData.LocationInfo?.GetType(),
      LogManager.GetRepository(TestRepository),
      loggingEventData.LoggerName,
      loggingEventData.Level,
      loggingEventData.Message,
      new("This is the exception"));

    AssertExpectedLoggingEvent(loggingEvent, loggingEventData);

    Assert.That(loggingEvent.Fix, Is.EqualTo(FixFlags.None), "Fixed Fields is incorrect");
  }

  /// <summary>
  /// Fixing with <see cref="FixFlags.All"/> reports every field as fixed.
  /// </summary>
  [Test]
  public void TestAllFixedValues()
  {
    LoggingEventData loggingEventData = BuildStandardEventData();

    // LoggingEvents occur at distinct points in time
    LoggingEvent loggingEvent = new(
      loggingEventData.LocationInfo?.GetType(),
      LogManager.GetRepository(TestRepository),
      loggingEventData.LoggerName,
      loggingEventData.Level,
      loggingEventData.Message,
      new("This is the exception"));

    AssertExpectedLoggingEvent(loggingEvent, loggingEventData);

    loggingEvent.Fix = FixFlags.All;

    Assert.That(loggingEvent.Fix, Is.EqualTo(FixFlags.LocationInfo | FixFlags.UserName | FixFlags.Identity | FixFlags.Partial | FixFlags.Message | FixFlags.ThreadName | FixFlags.Exception | FixFlags.Domain | FixFlags.Properties), "Fixed Fields is incorrect");
  }

  /// <summary>
  /// Fixing with <see cref="FixFlags.None"/> leaves the event unfixed.
  /// </summary>
  [Test]
  public void TestNoFixedValues()
  {
    LoggingEventData loggingEventData = BuildStandardEventData();

    // LoggingEvents occur at distinct points in time
    LoggingEvent loggingEvent = new(
      loggingEventData.LocationInfo?.GetType(),
      LogManager.GetRepository(TestRepository),
      loggingEventData.LoggerName,
      loggingEventData.Level,
      loggingEventData.Message,
      new("This is the exception"));

    AssertExpectedLoggingEvent(loggingEvent, loggingEventData);

    loggingEvent.Fix = FixFlags.None;

    Assert.That(loggingEvent.Fix, Is.EqualTo(FixFlags.None), "Fixed Fields is incorrect");
  }

  /// <summary>
  /// Without impersonation the user name is the process identity, which is the same whichever
  /// thread asks, so resolving it after the event has been fixed still gives the right answer. An
  /// event fixed without <see cref="FixFlags.UserName"/> must therefore keep reporting it.
  /// </summary>
  [Test]
  public void UserNameIsStillResolvedAfterFixingWithoutImpersonation()
  {
    string expected = CreateEvent().UserName;
    LoggingEvent loggingEvent = CreateEvent();

    // Partial deliberately leaves UserName out, being the documented setting for avoiding its cost.
    loggingEvent.Fix = FixFlags.Partial;

    Assert.That(loggingEvent.Fix & FixFlags.UserName, Is.EqualTo(FixFlags.None));
    Assert.That(loggingEvent.UserName, Is.EqualTo(expected));
    Assert.That(loggingEvent.UserName, Is.Not.EqualTo(SystemInfo.NotAvailableText));
  }

  /// <summary>
  /// Fixing with <see cref="FixFlags.None"/> skips the whole of FixVolatileData but still locks the
  /// cache, so the user name has to survive that path too.
  /// </summary>
  [Test]
  public void UserNameIsStillResolvedAfterFixingNothing()
  {
    string expected = CreateEvent().UserName;
    LoggingEvent loggingEvent = CreateEvent();

    loggingEvent.Fix = FixFlags.None;

    Assert.That(loggingEvent.UserName, Is.EqualTo(expected));
  }

  /// <summary>
  /// Fixing an event with UserName still has to capture it, on the thread that logged the event.
  /// </summary>
  [Test]
  public void UserNameIsCapturedWhenItIsFixed()
  {
    LoggingEvent loggingEvent = CreateEvent();

    string expected = loggingEvent.UserName;
    loggingEvent.Fix = FixFlags.All;

    Assert.That(loggingEvent.UserName, Is.EqualTo(expected));
    Assert.That(loggingEvent.UserName, Is.Not.EqualTo(SystemInfo.NotAvailableText));
  }

  /// <summary>
  /// Creates an event in the test repository.
  /// </summary>
  /// <returns>A new, unfixed event.</returns>
  private static LoggingEvent CreateEvent()
    => new(typeof(FixingTest),
      LogManager.GetRepository(TestRepository),
      typeof(FixingTest).FullName,
      Level.Warn,
      "Logging event works",
      null);

  /// <summary>
  /// Builds the event data the tests compare against.
  /// </summary>
  /// <returns>Event data with every field set to a known value.</returns>
  private static LoggingEventData BuildStandardEventData()
  {
    LoggingEventData loggingEventData = new()
    {
      LoggerName = typeof(FixingTest).FullName,
      Level = Level.Warn,
      Message = "Logging event works",
      Domain = "ReallySimpleApp",
      LocationInfo = new(nameof(FixingTest), "Main", "Class1.cs", "29"), //Completely arbitary
      ThreadName = Thread.CurrentThread.Name,
      TimeStampUtc = DateTime.UtcNow.Date,
      ExceptionString = "Exception occured here",
      UserName = "TestUser"
    };
    return loggingEventData;
  }

  /// <summary>
  /// Asserts that <paramref name="loggingEvent"/> carries the values of
  /// <paramref name="loggingEventData"/>.
  /// </summary>
  /// <param name="loggingEvent">The event to check.</param>
  /// <param name="loggingEventData">The expected values.</param>
  private static void AssertExpectedLoggingEvent(LoggingEvent loggingEvent, LoggingEventData loggingEventData)
  {
    Assert.That(loggingEventData.Domain, Is.EqualTo("ReallySimpleApp"), "Domain is incorrect");
    Assert.That(loggingEvent.GetExceptionString(), Is.EqualTo("System.Exception: This is the exception"), "Exception is incorrect");
    Assert.That(loggingEventData.Identity, Is.Null, "Identity is incorrect");
    Assert.That(loggingEventData.Level, Is.EqualTo(Level.Warn), "Level is incorrect");
    Assert.That(loggingEvent.LocationInformation, Is.Not.Null);
    Assert.That(loggingEvent.LocationInformation.MethodName, Is.EqualTo("get_LocationInformation"), "Location Info is incorrect");
    Assert.That(loggingEventData.LoggerName, Is.EqualTo("log4net.Tests.Core.FixingTest"), "LoggerName is incorrect");
    Assert.That(loggingEvent.Repository, Is.EqualTo(LogManager.GetRepository(TestRepository)), "Repository is incorrect");
    Assert.That(loggingEventData.ThreadName, Is.EqualTo(Thread.CurrentThread.Name), "ThreadName is incorrect");
    Assert.That(loggingEventData.UserName, Is.EqualTo("TestUser"), "UserName is incorrect");
    Assert.That(loggingEvent.RenderedMessage, Is.EqualTo("Logging event works"), "Message is incorrect");
  }
}