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
using System.Runtime.CompilerServices;
using System.Threading;
using log4net.Appender;
using log4net.Core;
using log4net.Tests.Appender;
using NUnit.Framework;

namespace log4net.Tests.Core;

[TestFixture]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types")]
public class EvaluatorTest
{
  private BufferingForwardingAppender _bufferingForwardingAppender = new();
  private CountingAppender _countingAppender = new();
  private Repository.Hierarchy.Hierarchy _hierarchy = new();

  [SetUp]
  public void SetupRepository()
  {
    _hierarchy = new Repository.Hierarchy.Hierarchy();

    _countingAppender = new CountingAppender();
    _countingAppender.ActivateOptions();

    _bufferingForwardingAppender = new BufferingForwardingAppender();
    _bufferingForwardingAppender.AddAppender(_countingAppender);

    _bufferingForwardingAppender.BufferSize = 5;
    _bufferingForwardingAppender.ClearFilters();
    _bufferingForwardingAppender.Fix = FixFlags.Partial;
    _bufferingForwardingAppender.Lossy = false;
    _bufferingForwardingAppender.LossyEvaluator = null;
    _bufferingForwardingAppender.Threshold = Level.All;
  }

  private ILogger GetLogger([CallerMemberName] string name = "") => _hierarchy.GetLogger(name);

  [Test]
  public void TestLevelEvaluator()
  {
    _bufferingForwardingAppender.Evaluator = new LevelEvaluator(Level.Info);
    _bufferingForwardingAppender.ActivateOptions();
    log4net.Config.BasicConfigurator.Configure(_hierarchy, _bufferingForwardingAppender);

    ILogger logger = GetLogger();

    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test 2 events buffered");

    logger.Log(typeof(EvaluatorTest), Level.Info, "Info message logged", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(3), "Test 3 events flushed on Info message.");
  }

  [Test]
  public void TestTimeEvaluatorWhenElapsed()
  {
    _bufferingForwardingAppender.Evaluator = new TimeEvaluator(1);
    _bufferingForwardingAppender.ActivateOptions();
    log4net.Config.BasicConfigurator.Configure(_hierarchy, _bufferingForwardingAppender);

    ILogger logger = GetLogger();

    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test 2 events buffered");

    Thread.Sleep(1000);
    logger.Log(typeof(EvaluatorTest), Level.Debug, "Info message logged", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(3), "Test 3 events flushed on Info message.");
  }

  [Test]
  public void TestTimeEvaluatorWhenBufferFull()
  {
    _bufferingForwardingAppender.Evaluator = new TimeEvaluator(10);
    _bufferingForwardingAppender.ActivateOptions();
    log4net.Config.BasicConfigurator.Configure(_hierarchy, _bufferingForwardingAppender);

    ILogger logger = GetLogger();

    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Debug, "Info message logged", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test 5 events buffered");

    logger.Log(typeof(EvaluatorTest), Level.Debug, "Info message logged", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(6), "Test 6 events flushed on Info message.");
  }

  [Test]
  public void TestExceptionEvaluator()
  {
    _bufferingForwardingAppender.Evaluator = new ExceptionEvaluator(typeof(ApplicationException), true);
    _bufferingForwardingAppender.ActivateOptions();
    log4net.Config.BasicConfigurator.Configure(_hierarchy, _bufferingForwardingAppender);

    ILogger logger = GetLogger();

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test 2 events buffered");

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", new ApplicationException());
    Assert.That(_countingAppender.Counter, Is.EqualTo(3), "Test 3 events flushed on ApplicationException message.");
  }

  [Test]
  public void TestExceptionEvaluatorTriggerOnSubClass()
  {
    _bufferingForwardingAppender.Evaluator = new ExceptionEvaluator(typeof(Exception), true);
    _bufferingForwardingAppender.ActivateOptions();
    log4net.Config.BasicConfigurator.Configure(_hierarchy, _bufferingForwardingAppender);

    ILogger logger = GetLogger();

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test 2 events buffered");

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", new ApplicationException());
    Assert.That(_countingAppender.Counter, Is.EqualTo(3), "Test 3 events flushed on ApplicationException message.");
  }

  [Test]
  public void TestExceptionEvaluatorNoTriggerOnSubClass()
  {
    _bufferingForwardingAppender.Evaluator = new ExceptionEvaluator(typeof(Exception), false);
    _bufferingForwardingAppender.ActivateOptions();
    log4net.Config.BasicConfigurator.Configure(_hierarchy, _bufferingForwardingAppender);

    ILogger logger = GetLogger();

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test 2 events buffered");

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", new ApplicationException());
    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test 3 events buffered");
  }

  [Test]
  public void TestInvalidExceptionEvaluator()
  {
    // warning: String is not a subclass of Exception
    _bufferingForwardingAppender.Evaluator = new ExceptionEvaluator(typeof(String), false);
    _bufferingForwardingAppender.ActivateOptions();
    log4net.Config.BasicConfigurator.Configure(_hierarchy, _bufferingForwardingAppender);

    ILogger logger = GetLogger();

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test 2 events buffered");

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", new ApplicationException());
    Assert.That(_countingAppender.Counter, Is.EqualTo(0), "Test 3 events buffered");
  }
}