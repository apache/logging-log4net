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

using System.Xml;
using System;
using log4net.Appender;
using NUnit.Framework;
using log4net.Repository;
using log4net.Config;
using log4net.Util;
using log4net.Core;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace log4net.Tests.Appender;

/// <summary>
/// Used for internal unit testing the <see cref="FileAppender"/> class.
/// </summary>
[TestFixture]
public sealed class FileAppenderTest
{
  /// <summary>
  /// Shuts down any loggers in the hierarchy, along with all appenders
  /// </summary>
  private static void Reset()
  {
    // Regular users should not use the clear method lightly!
    LogManager.GetRepository().ResetConfiguration();
    LogManager.GetRepository().Shutdown();
    ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Clear();
  }

  /// <summary>
  /// Any initialization that happens before each test can
  /// go here
  /// </summary>
  [SetUp]
  public void SetUp() => Reset();

  /// <summary>
  /// Any steps that happen after each test go here
  /// </summary>
  [TearDown]
  public void TearDown() => Reset();

  /// <summary>
  /// Verifies that the <see cref="FileAppender.File"/> property accepts a <see cref="PatternString"/>
  /// </summary>
  [Test]
  public void FilenameWithPatternStringTest()
  {
    LogLog.LogReceived += LogReceived;
    try
    {
      XmlDocument log4NetConfig = new();
      log4NetConfig.LoadXml(
        """
        <log4net>
          <appender name="ConsoleAppender" type="log4net.Appender.ConsoleAppender">
            <layout type="log4net.Layout.PatternLayout">
              <conversionPattern value="%date{ABSOLUTE} [%logger] %level - %message%newline%exception"/>
            </layout>
          </appender>
          <appender name="GeneralFileAppender" type="log4net.Appender.FileAppender">
            <file type="log4net.Util.PatternString" value="Logs\file_%property{LogName}_%date{yyyyMMddHHmmss}.Log"/>
            <appendToFile value="true"/>
            <lockingModel type="log4net.Appender.FileAppender+MinimalLock"/>
            <layout type="log4net.Layout.PatternLayout">
              <conversionPattern value="%date{ABSOLUTE} [%logger] %level - %message%newline%exception"/>
            </layout>
          </appender>
          <root>
            <level value="INFO"/>
            <appender-ref ref="GeneralFileAppender"/>
          </root>
        </log4net>
        """);
      ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
      XmlConfigurator.Configure(rep, log4NetConfig["log4net"]!);
    }
    finally
    {
      LogLog.LogReceived -= LogReceived;
    }

    static void LogReceived(object? source, LogReceivedEventArgs e) => Assert.Fail(e.LogLog.Message);
  }

  /// <summary>
  /// Verifies that the <see cref="FileAppender.File"/> property accepts a <see cref="PatternString"/> with a <see cref="GlobalContext"/>
  /// </summary>
  /// <remarks>see https://github.com/apache/logging-log4net/issues/193</remarks>
  [Test]
  [Platform(Include = "Win")]
  public void FilenameWithGlobalContextPatternStringTest()
  {
    DirectoryInfo logs = new("./Logs");
    if (logs.Exists)
    {
      logs.Delete(true);
    }

    XmlDocument log4NetConfig = new();
    log4NetConfig.LoadXml(
      """
      <log4net>
        <appender name="ConsoleAppender" type="log4net.Appender.ConsoleAppender">
          <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%date{ABSOLUTE} [%logger] %level - %message%newline%exception"/>
          </layout>
        </appender>
        <appender name="GeneralFileAppender" type="log4net.Appender.FileAppender">
          <file type="log4net.Util.PatternString" value="Logs\file_%property{LogName}_%date{yyyyMMddHHmmss}.Log"/>
          <appendToFile value="true"/>
          <lockingModel type="log4net.Appender.FileAppender+MinimalLock"/>
          <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%date{ABSOLUTE} [%logger] %level - %message%newline%exception"/>
          </layout>
        </appender>
        <root>
          <level value="INFO"/>
          <appender-ref ref="GeneralFileAppender"/>
        </root>
      </log4net>
      """);
    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    // latest possible moment to set GlobalContext property used in filename
    GlobalContext.Properties["LogName"] = "custom_log_issue_193";
    XmlConfigurator.Configure(rep, log4NetConfig["log4net"]!);
    ILogger logger = rep.GetLogger(nameof(FilenameWithGlobalContextPatternStringTest));
    logger.Log(GetType(), Level.Info, nameof(FilenameWithGlobalContextPatternStringTest), null);
    logs.Refresh();
    Assert.That(logs.GetFiles().Any(file => file.Name.StartsWith("file_custom_log_issue_193")));
  }

  /// <summary>
  /// Verifies that <see cref="FileAppender.InterProcessLock"/> releases the mutex
  /// when the underlying file stream is null.
  /// </summary>
  [Test]
  public void InterProcessLock_AcquireLock_ReleasesMutexWhenStreamIsNull()
  {
    string tempFile = Path.GetTempFileName();
    FileAppender appender = new() { File = "log4net_ipl_test" };
    FileAppender.InterProcessLock lockingModel = new() { CurrentAppender = appender };
    lockingModel.ActivateOptions();
    lockingModel.OpenFile(tempFile, false, Encoding.UTF8);
    lockingModel.CloseFile(); // sets _stream to null; mutex remains alive
    try
    {
      Stream? stream = lockingModel.AcquireLock();
      Assert.That(stream, Is.Null);

      // if the mutex was released, a second thread can acquire the lock without blocking
      Task task = Task.Run(lockingModel.AcquireLock);
      Assert.That(task.Wait(TimeSpan.FromSeconds(2)), Is.True,
        "Mutex was not released by AcquireLock when stream is null");
    }
    finally
    {
      lockingModel.OnClose();
      File.Delete(tempFile);
    }
  }

  /// <summary>
  /// The wait for the inter process lock happens while the appender lock is held, so it has to be
  /// bounded by default: a lock nobody releases would otherwise suspend all logging for good.
  /// </summary>
  [Test]
  public void LockTimeoutMillisDefaultsToAFiniteValue()
    => Assert.That(new FileAppender.InterProcessLock().LockTimeoutMillis, Is.EqualTo(10000));

  /// <summary>
  /// Timeout.Infinite restores waiting indefinitely and 0 gives up at once; any other negative
  /// value has no meaning and is rejected rather than reinterpreted.
  /// </summary>
  [Test]
  public void LockTimeoutMillisRejectsNegativeValuesExceptInfinite()
  {
    FileAppender.InterProcessLock lockingModel = new();

    Assert.That(() => lockingModel.LockTimeoutMillis = -2, Throws.TypeOf<ArgumentOutOfRangeException>());

    lockingModel.LockTimeoutMillis = Timeout.Infinite;
    Assert.That(lockingModel.LockTimeoutMillis, Is.EqualTo(Timeout.Infinite));

    lockingModel.LockTimeoutMillis = 0;
    Assert.That(lockingModel.LockTimeoutMillis, Is.EqualTo(0));
  }

  /// <summary>
  /// When something else holds the lock and does not let go, the event is dropped rather than the
  /// logging thread being blocked forever.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void AcquireLockGivesUpWhenTheLockIsHeldTooLong()
  {
    const string appenderFile = "log4net_lock_timeout_test";
    string tempFile = Path.GetTempFileName();
    FileAppender appender = new() { File = appenderFile };
    FileAppender.InterProcessLock lockingModel = new()
    {
      CurrentAppender = appender,
      LockTimeoutMillis = 200
    };
    lockingModel.ActivateOptions();
    lockingModel.OpenFile(tempFile, false, Encoding.UTF8);

    using ManualResetEventSlim held = new();
    using ManualResetEventSlim release = new();
    // A mutex has to be taken and released on one thread, so the holder does both.
    Task holder = Task.Run(() =>
    {
      using Mutex contender = new(false, appenderFile);
      contender.WaitOne();
      held.Set();
      release.Wait();
      contender.ReleaseMutex();
    });

    try
    {
      Assert.That(held.Wait(TimeSpan.FromSeconds(10)), Is.True, "the contending thread never took the mutex");

      Stream? stream = null;
      Stopwatch stopwatch = Stopwatch.StartNew();
      LogLog.ExecuteWithoutEmittingInternalMessages(() => stream = lockingModel.AcquireLock());
      stopwatch.Stop();

      Assert.That(stream, Is.Null, "the lock was reported as acquired while another thread held it");
      Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(10)));
    }
    finally
    {
      release.Set();
      holder.Wait(TimeSpan.FromSeconds(10));
      lockingModel.OnClose();
      File.Delete(tempFile);
    }
  }
}