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
  private BufferingForwardingAppender bufferingForwardingAppender = new();
  private CountingAppender countingAppender = new();
  private Repository.Hierarchy.Hierarchy hierarchy = new();

  private void SetupRepository()
  {
    hierarchy = new Repository.Hierarchy.Hierarchy();

    countingAppender = new CountingAppender();
    countingAppender.ActivateOptions();

    bufferingForwardingAppender = new BufferingForwardingAppender();
    bufferingForwardingAppender.AddAppender(countingAppender);

    bufferingForwardingAppender.BufferSize = 0;
    bufferingForwardingAppender.ClearFilters();
    bufferingForwardingAppender.Evaluator = null;
    bufferingForwardingAppender.Fix = FixFlags.Partial;
    bufferingForwardingAppender.Lossy = false;
    bufferingForwardingAppender.LossyEvaluator = null;
    bufferingForwardingAppender.Threshold = Level.All;

    bufferingForwardingAppender.ActivateOptions();

    BasicConfigurator.Configure(hierarchy, bufferingForwardingAppender);
  }

  [Test]
  public void TestSetupAppender()
  {
    SetupRepository();

    Assert.AreEqual(0, countingAppender.Counter, "Test empty appender");

    ILogger logger = hierarchy.GetLogger("test");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message logged", null);

    Assert.AreEqual(1, countingAppender.Counter, "Test 1 event logged");
  }

  [Test]
  public void TestBufferSize5()
  {
    SetupRepository();

    bufferingForwardingAppender.BufferSize = 5;
    bufferingForwardingAppender.ActivateOptions();

    Assert.AreEqual(countingAppender.Counter, 0);

    ILogger logger = hierarchy.GetLogger("test");

    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 1", null);
    Assert.AreEqual(0, countingAppender.Counter, "Test 1 event in buffer");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 2", null);
    Assert.AreEqual(0, countingAppender.Counter, "Test 2 event in buffer");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 3", null);
    Assert.AreEqual(0, countingAppender.Counter, "Test 3 event in buffer");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 4", null);
    Assert.AreEqual(0, countingAppender.Counter, "Test 4 event in buffer");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 5", null);
    Assert.AreEqual(0, countingAppender.Counter, "Test 5 event in buffer");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 6", null);
    Assert.AreEqual(6, countingAppender.Counter, "Test 0 event in buffer. 6 event sent");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 7", null);
    Assert.AreEqual(6, countingAppender.Counter, "Test 1 event in buffer. 6 event sent");
    logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 8", null);
    Assert.AreEqual(6, countingAppender.Counter, "Test 2 event in buffer. 6 event sent");
  }
}