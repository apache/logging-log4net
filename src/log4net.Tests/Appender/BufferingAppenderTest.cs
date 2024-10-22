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

using log4net.Appender;
using log4net.Config;
using log4net.Core;

using NUnit.Framework;

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
}