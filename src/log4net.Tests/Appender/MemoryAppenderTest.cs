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
using System.Linq;
using System.Threading;
using NUnit.Framework;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using log4net.Repository;

namespace log4net.Tests.Appender;

[TestFixture]
public class MemoryAppenderTest
{
  // ReSharper disable once InconsistentNaming
  private static int s_ThreadsRunning;
  private const int MaxThreads = 10;
  private const int LogEntriesPerThread = 100;
  private const long EventsExpected = LogEntriesPerThread * MaxThreads;

  [Test]
  public void TestThreadSafety()
  {
    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    var memoryAppender = new MemoryAppender();
    var patternLayout = new PatternLayout();
    memoryAppender.Layout = patternLayout;
    memoryAppender.ActivateOptions();
    BasicConfigurator.Configure(rep, memoryAppender);

    s_ThreadsRunning = MaxThreads;
    var threads = Enumerable.Range(0, MaxThreads)
        .Select(i => new Thread(LogMessages(rep.Name)))
        .ToList();

    foreach (var thread in threads)
    {
      thread.Start();
    }

    long cEventsRead = 0;
    while (s_ThreadsRunning > 0)
    {
      var events = memoryAppender.PopAllEvents();
      cEventsRead += events.Length;
    }
    foreach (var thread in threads)
    {
      thread.Join();
    }
    cEventsRead += memoryAppender.PopAllEvents().Length;
    Assert.That(cEventsRead, Is.EqualTo(EventsExpected), "Log events were lost.");
  }

  private static ThreadStart LogMessages(string repository)
  {
    return () =>
    {
      var logger = LogManager.GetLogger(repository, "LoggerThread");
      for (var i = 0; i < LogEntriesPerThread; i++)
      {
        logger.InfoFormat("Logging message {0}", i);
      }
      Interlocked.Decrement(ref s_ThreadsRunning);
    };
  }
}