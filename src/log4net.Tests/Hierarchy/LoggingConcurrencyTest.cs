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

    for (var run = 0; run < repetitions; run++)
    {
      _memoryAppender.Clear();

      Parallel.For(0, messageCount, i =>
      {
        var logger = LogManager.GetLogger(_repository.Name, "ConcurrencyTest-" + (i % loggerCount));
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
}
