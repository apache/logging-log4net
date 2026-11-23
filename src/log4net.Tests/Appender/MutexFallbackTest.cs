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
using System.Collections.Generic;
using System.IO;
using System.Threading;

using log4net.Appender;
using log4net.Layout;
using log4net.Tests.Appender.Internal;
using log4net.Util;

using NUnit.Framework;

using PeanutButter.Utils;

using static log4net.Tests.Appender.Internal.TestFileAppenders;

namespace log4net.Tests.Appender;

/// <summary>
/// Appenders whose named mutex cannot be created, as without a writable home directory, log without it.
/// </summary>
[TestFixture]
[NonParallelizable]
public sealed class MutexFallbackTest
{
  /// <summary>
  /// A <see cref="RollingFileAppender"/> activates, logs and rolls, with one warning naming the roll.
  /// </summary>
  [Test]
  public void RollingWithoutANamedMutexLogsAndRolls()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "r.log");
    RecordingErrorHandler errors = new();
    RollingFileAppender appender = new()
    {
      File = file,
      Layout = new PatternLayout("%message%newline"),
      RollingStyle = RollingFileAppender.RollingMode.Size,
      MaxFileSize = 10,
      MaxSizeRollBackups = 2,
      ErrorHandler = errors
    };

    List<LogLog> messages = WithFailingMutexes(() =>
    {
      appender.ActivateOptions();
      appender.DoAppend(CreateEvent("first event"));
      appender.DoAppend(CreateEvent("second event"));
      appender.Close();
    });

    Assert.That(File.ReadAllText(file + ".1"), Does.Contain("first event"));
    Assert.That(File.ReadAllText(file), Does.Contain("second event"));
    Assert.That(messages, Has.Count.EqualTo(1));
    Assert.That(messages[0].Message, Does.Contain("other processes may roll the same files at the same time"));
    Assert.That(errors.Messages, Is.Empty);
  }

  /// <summary>
  /// <see cref="FileAppender.InterProcessLock"/> activates and logs, with one warning and no error report.
  /// </summary>
  [Test]
  public void InterProcessLockWithoutANamedMutexLogs()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "ipl.log");
    FileAppender appender = CreateAppender(file, new FileAppender.InterProcessLock());

    List<LogLog> messages = WithFailingMutexes(() =>
    {
      appender.ActivateOptions();
      appender.DoAppend(CreateEvent("written"));
      appender.Close();
    });

    Assert.That(File.ReadAllText(file), Does.Contain("written"));
    Assert.That(messages, Has.Count.EqualTo(1));
    Assert.That(messages[0].Message, Does.Contain("other processes may write the file at the same time"));
    Assert.That(((RecordingErrorHandler)appender.ErrorHandler).Messages, Is.Empty);
  }

  /// <summary>
  /// Runs <paramref name="action"/> with a mutex factory that fails as the runtime does without a
  /// writable home, and returns the internal messages.
  /// </summary>
  private static List<LogLog> WithFailingMutexes(Action action)
  {
    List<LogLog> messages = [];
    Func<string, Mutex> original = typeof(FileAppender).GetFieldValue<Func<string, Mutex>>("_mutexFactory");
    typeof(FileAppender).SetFieldValue("_mutexFactory",
      new Func<string, Mutex>(_ => throw new IOException("simulated: no writable home")));
    try
    {
      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        using LogLog.LogReceivedAdapter _ = new(messages);
        action();
      });
    }
    finally
    {
      typeof(FileAppender).SetFieldValue("_mutexFactory", original);
    }
    return messages;
  }
}
