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
public class EvaluatorTest
{
  private BufferingForwardingAppender bufferingForwardingAppender = new();
  private CountingAppender countingAppender = new();
  private Repository.Hierarchy.Hierarchy hierarchy = new();

  [SetUp]
  public void SetupRepository()
  {
    hierarchy = new Repository.Hierarchy.Hierarchy();

    countingAppender = new CountingAppender();
    countingAppender.ActivateOptions();

    bufferingForwardingAppender = new BufferingForwardingAppender();
    bufferingForwardingAppender.AddAppender(countingAppender);

    bufferingForwardingAppender.BufferSize = 5;
    bufferingForwardingAppender.ClearFilters();
    bufferingForwardingAppender.Fix = FixFlags.Partial;
    bufferingForwardingAppender.Lossy = false;
    bufferingForwardingAppender.LossyEvaluator = null;
    bufferingForwardingAppender.Threshold = Level.All;
  }

  private ILogger GetLogger([CallerMemberName] string name = "") => hierarchy.GetLogger(name);

  [Test]
  public void TestLevelEvaluator()
  {
    bufferingForwardingAppender.Evaluator = new LevelEvaluator(Level.Info);
    bufferingForwardingAppender.ActivateOptions();
    log4net.Config.BasicConfigurator.Configure(hierarchy, bufferingForwardingAppender);

    ILogger logger = GetLogger();

    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    Assert.AreEqual(0, countingAppender.Counter, "Test 2 events buffered");

    logger.Log(typeof(EvaluatorTest), Level.Info, "Info message logged", null);
    Assert.AreEqual(3, countingAppender.Counter, "Test 3 events flushed on Info message.");
  }

  [Test]
  public void TestTimeEvaluatorWhenElapsed()
  {
    bufferingForwardingAppender.Evaluator = new TimeEvaluator(1);
    bufferingForwardingAppender.ActivateOptions();
    log4net.Config.BasicConfigurator.Configure(hierarchy, bufferingForwardingAppender);

    ILogger logger = GetLogger();

    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    Assert.AreEqual(0, countingAppender.Counter, "Test 2 events buffered");

    Thread.Sleep(1000);
    logger.Log(typeof(EvaluatorTest), Level.Debug, "Info message logged", null);
    Assert.AreEqual(3, countingAppender.Counter, "Test 3 events flushed on Info message.");
  }

  [Test]
  public void TestTimeEvaluatorWhenBufferFull()
  {
    bufferingForwardingAppender.Evaluator = new TimeEvaluator(10);
    bufferingForwardingAppender.ActivateOptions();
    log4net.Config.BasicConfigurator.Configure(hierarchy, bufferingForwardingAppender);

    ILogger logger = GetLogger();

    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Debug, "Info message logged", null);
    Assert.AreEqual(0, countingAppender.Counter, "Test 5 events buffered");

    logger.Log(typeof(EvaluatorTest), Level.Debug, "Info message logged", null);
    Assert.AreEqual(6, countingAppender.Counter, "Test 6 events flushed on Info message.");
  }

  [Test]
  public void TestExceptionEvaluator()
  {
    bufferingForwardingAppender.Evaluator = new ExceptionEvaluator(typeof(ApplicationException), true);
    bufferingForwardingAppender.ActivateOptions();
    log4net.Config.BasicConfigurator.Configure(hierarchy, bufferingForwardingAppender);

    ILogger logger = GetLogger();

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    Assert.AreEqual(0, countingAppender.Counter, "Test 2 events buffered");

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", new ApplicationException());
    Assert.AreEqual(3, countingAppender.Counter, "Test 3 events flushed on ApplicationException message.");
  }

  [Test]
  public void TestExceptionEvaluatorTriggerOnSubClass()
  {
    bufferingForwardingAppender.Evaluator = new ExceptionEvaluator(typeof(Exception), true);
    bufferingForwardingAppender.ActivateOptions();
    log4net.Config.BasicConfigurator.Configure(hierarchy, bufferingForwardingAppender);

    ILogger logger = GetLogger();

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    Assert.AreEqual(0, countingAppender.Counter, "Test 2 events buffered");

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", new ApplicationException());
    Assert.AreEqual(3, countingAppender.Counter, "Test 3 events flushed on ApplicationException message.");
  }

  [Test]
  public void TestExceptionEvaluatorNoTriggerOnSubClass()
  {
    bufferingForwardingAppender.Evaluator = new ExceptionEvaluator(typeof(Exception), false);
    bufferingForwardingAppender.ActivateOptions();
    log4net.Config.BasicConfigurator.Configure(hierarchy, bufferingForwardingAppender);

    ILogger logger = GetLogger();

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    Assert.AreEqual(0, countingAppender.Counter, "Test 2 events buffered");

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", new ApplicationException());
    Assert.AreEqual(0, countingAppender.Counter, "Test 3 events buffered");
  }

  [Test]
  public void TestInvalidExceptionEvaluator()
  {
    // warning: String is not a subclass of Exception
    bufferingForwardingAppender.Evaluator = new ExceptionEvaluator(typeof(String), false);
    bufferingForwardingAppender.ActivateOptions();
    log4net.Config.BasicConfigurator.Configure(hierarchy, bufferingForwardingAppender);

    ILogger logger = GetLogger();

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
    Assert.AreEqual(0, countingAppender.Counter, "Test 2 events buffered");

    logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", new ApplicationException());
    Assert.AreEqual(0, countingAppender.Counter, "Test 3 events buffered");
  }
}