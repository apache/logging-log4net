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
using System.Globalization;
using log4net.Config;
using log4net.Core;
using NUnit.Framework;

namespace log4net.Tests.Appender;

[TestFixture]
public class RecursiveLoggingTest
{
  private readonly EventRaisingAppender _eventRaisingAppender = new();
  private readonly Repository.Hierarchy.Hierarchy _hierarchy = new();
  private int _eventCount;
  private ILogger? _logger;
  private const int MaxRecursion = 3;

  private void SetupRepository()
  {
    _eventRaisingAppender.LoggingEventAppended += eventRaisingAppender_LoggingEventAppended;

    _hierarchy.Root.Level = Level.All;
    _hierarchy.Root.AddAppender(_eventRaisingAppender);

    BasicConfigurator.Configure(_hierarchy, _eventRaisingAppender);

    _logger = _hierarchy.GetLogger("test");
  }

  void eventRaisingAppender_LoggingEventAppended(object? sender, LoggingEventEventArgs e)
  {
    if (_eventCount < MaxRecursion && _logger is not null)
    {
      _eventCount++;
      string message = String.Format(CultureInfo.CurrentCulture, "Log event {0} from EventRaisingAppender", _eventCount);
      Console.WriteLine("Logging message: " + message);
      _logger.Log(typeof(RecursiveLoggingTest), Level.Warn, message, null);
    }
  }

  [Test]
  public void TestAllowRecursiveLoggingFromAppender()
  {
    SetupRepository();

    _eventCount = 0;
    _logger!.Log(typeof(RecursiveLoggingTest), Level.Warn, "Message logged", null);

    Assert.That(_eventCount, Is.EqualTo(MaxRecursion), "Expected MaxRecursion recursive calls");
  }
}
