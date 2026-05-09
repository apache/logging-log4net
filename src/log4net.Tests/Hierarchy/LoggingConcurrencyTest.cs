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
using System.Threading;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using NUnit.Framework;

namespace log4net.Tests.Hierarchy;

/// <summary>
/// Concurrency tests for <see cref="LogManager.GetLogger(string, string)"/>.
/// Regression test for LOG4NET-292.
/// </summary>
[TestFixture]
public class LoggingConcurrencyTest
{
  private MemoryAppender _memoryAppender = null!;
  private ILoggerRepository _repository = null!;

  [SetUp]
  public void SetUp()
  {
    _repository = LogManager.CreateRepository(Guid.NewGuid().ToString());

    _memoryAppender = new MemoryAppender();
    _memoryAppender.ActivateOptions();

    var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)_repository;
    hierarchy.Root.AddAppender(_memoryAppender);
    hierarchy.Root.Level = Level.All;
    hierarchy.Configured = true;
  }

  [TearDown]
  public void TearDown()
  {
    _memoryAppender.Clear();
    _repository.Shutdown();
  }

  /// <summary>
  /// Verifies that no messages are lost when multiple threads call
  /// <see cref="LogManager.GetLogger(string, string)"/> with the same logger
  /// name concurrently (LOG4NET-292).
  /// </summary>
  [Test]
  public void GetLogger_UnderHighConcurrency_ShouldNotLoseMessages()
  {
    const int messageCount = 400;
    const int loggerCount = 20;
    const int repetitions = 20;

    for (int run = 0; run < repetitions; run++)
    {
      _memoryAppender.Clear();

      Parallel.For(0, messageCount, i =>
      {
        var logger = LogManager.GetLogger(_repository.Name, "ConcurrencyTest-" + run + "-" + (i % loggerCount));
        logger.Info("High concurrency message " + i);
      });

      var events = _memoryAppender.GetEvents();
      Assert.That(events, Has.Length.EqualTo(messageCount), $"Run {run}: expected {messageCount} messages");

      for (var i = 0; i < messageCount; i++)
      {
        var expectedMessage = "High concurrency message " + i;
        Assert.That(events, Has.Some.Matches<LoggingEvent>(e => e.RenderedMessage == expectedMessage),
          $"Run {run}: Missing message: {expectedMessage}");
      }
    }
  }

  [Test]
  public void GetLogger_WithDottedNamesUnderHighConcurrency_ShouldNotLoseMessages()
  {
    const int messageCount = 400;
    const int loggerCount = 40;
    const int repetitions = 20;

    for (var run = 0; run < repetitions; run++)
    {
      _memoryAppender.Clear();
      string loggerPrefix = "DottedConcurrencyTest-" + run;

      Parallel.For(0, messageCount, i =>
      {
        var logger = LogManager.GetLogger(_repository.Name, GetDottedLoggerName(loggerPrefix, i % loggerCount));
        logger.Info("Dotted concurrency message " + i);
      });

      var events = _memoryAppender.GetEvents();
      Assert.That(events, Has.Length.EqualTo(messageCount), $"Run {run}: expected {messageCount} messages");

      for (var i = 0; i < messageCount; i++)
      {
        var expectedMessage = "Dotted concurrency message " + i;
        Assert.That(events, Has.Some.Matches<LoggingEvent>(e => e.RenderedMessage == expectedMessage),
          $"Run {run}: Missing message: {expectedMessage}");
      }
    }
  }

  [Test]
  public void GetLogger_WhenCreationEventThrows_ShouldNotBlockSubsequentCalls()
  {
    var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)_repository;
    var expectedException = new InvalidOperationException("Logger creation event failed");
    string loggerName = "ThrowingEventLogger-" + Guid.NewGuid();

    void ThrowingHandler(object sender, LoggerCreationEventArgs args) => throw expectedException;

    hierarchy.LoggerCreatedEvent += ThrowingHandler;
    try
    {
      var actualException = Assert.Throws<InvalidOperationException>(() => hierarchy.GetLogger(loggerName));
      Assert.That(actualException, Is.SameAs(expectedException));
    }
    finally
    {
      hierarchy.LoggerCreatedEvent -= ThrowingHandler;
    }

    Task<Logger> getLoggerTask = Task.Run(() => (Logger)hierarchy.GetLogger(loggerName));
    Assert.That(getLoggerTask.Wait(TimeSpan.FromSeconds(5)), Is.True,
      "A failed logger creation event left the logger permanently not ready.");

    Logger logger = getLoggerTask.GetAwaiter().GetResult();
    logger.Log(Level.Info, "Message after failed creation event", null);

    Assert.That(_memoryAppender.GetEvents(), Has.Some.Matches<LoggingEvent>(
      e => e.RenderedMessage == "Message after failed creation event"));
  }

  [Test]
  public void GetLogger_WhenParentLoggerIsStillRegistering_ShouldWaitBeforeReturningChild()
  {
    string testId = Guid.NewGuid().ToString("N");
    string parentName = "ConcurrentParent-" + testId;
    string firstChildName = parentName + ".FirstChild";
    string secondChildName = parentName + ".SecondChild";

    using ManualResetEventSlim parentAssignmentStarted = new();
    using ManualResetEventSlim allowParentAssignment = new();

    using BlockingLoggerFactory factory = new(parentName, firstChildName, secondChildName,
      parentAssignmentStarted, allowParentAssignment);
    log4net.Repository.Hierarchy.Hierarchy hierarchy = new(factory) { Name = "Hierarchy-" + testId };
    MemoryAppender memoryAppender = new();
    memoryAppender.ActivateOptions();
    hierarchy.Root.AddAppender(memoryAppender);
    hierarchy.Root.Level = Level.All;
    hierarchy.Configured = true;

    try
    {
      hierarchy.GetLogger(firstChildName);

      Task<Logger> parentTask = Task.Run(() => (Logger)hierarchy.GetLogger(parentName));
      Assert.That(parentAssignmentStarted.Wait(TimeSpan.FromSeconds(5)), Is.True,
        "The parent logger did not reach child relinking.");

      Task childLogTask = Task.Run(() =>
      {
        Logger childLogger = (Logger)hierarchy.GetLogger(secondChildName);
        childLogger.Log(Level.Info, "Message from child while parent registers", null);
      });

      Assert.That(childLogTask.Wait(TimeSpan.FromMilliseconds(100)), Is.False,
        "The child logger was returned before its parent logger was ready.");

      allowParentAssignment.Set();

      Assert.That(parentTask.Wait(TimeSpan.FromSeconds(5)), Is.True);
      Assert.That(factory.SecondChildCreated.Wait(TimeSpan.FromSeconds(5)), Is.True,
        "The second child logger was not created.");
      Assert.That(childLogTask.Wait(TimeSpan.FromSeconds(5)), Is.True);
      parentTask.GetAwaiter().GetResult();
      childLogTask.GetAwaiter().GetResult();

      Assert.That(memoryAppender.GetEvents(), Has.Some.Matches<LoggingEvent>(
        e => e.RenderedMessage == "Message from child while parent registers"));
    }
    finally
    {
      allowParentAssignment.Set();
      memoryAppender.Clear();
      hierarchy.Shutdown();
    }
  }

  private static string GetDottedLoggerName(string prefix, int index)
  {
    int group = index / 4;
    string groupName = prefix + ".Group" + group;

    return (index % 4) switch
    {
      0 => groupName,
      1 => groupName + ".Child",
      2 => groupName + ".Child.Grandchild",
      _ => groupName + ".Sibling"
    };
  }

  private sealed class BlockingLoggerFactory(
    string parentName,
    string firstChildName,
    string secondChildName,
    ManualResetEventSlim parentAssignmentStarted,
    ManualResetEventSlim allowParentAssignment) : ILoggerFactory, IDisposable
  {
    public ManualResetEventSlim SecondChildCreated { get; } = new();

    public Logger CreateLogger(ILoggerRepository repository, string? name)
    {
      if (name is null)
      {
        return new RootLogger(repository.LevelMap.LookupWithDefault(Level.Debug));
      }

      if (name == secondChildName)
      {
        SecondChildCreated.Set();
      }

      return new BlockingLogger(name, parentName, firstChildName, parentAssignmentStarted, allowParentAssignment);
    }

    public void Dispose() => SecondChildCreated.Dispose();
  }

  private sealed class BlockingLogger(
    string name,
    string parentName,
    string blockedChildName,
    ManualResetEventSlim parentAssignmentStarted,
    ManualResetEventSlim allowParentAssignment) : Logger(name)
  {
    public override Logger? Parent
    {
      get => base.Parent;
      set
      {
        if (Name == blockedChildName && value?.Name == parentName)
        {
          parentAssignmentStarted.Set();
          allowParentAssignment.Wait(TimeSpan.FromSeconds(5));
        }

        base.Parent = value;
      }
    }
  }
}
