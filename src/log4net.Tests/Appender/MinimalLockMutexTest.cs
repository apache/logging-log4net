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
/// Outside Windows <see cref="FileAppender.MinimalLock"/> writers take turns through the mutex
/// <see cref="FileAppender.InterProcessLock"/> uses: a shared open let a second writer in there,
/// and its write replaced the first one's.
/// </summary>
[TestFixture]
[NonParallelizable]
public sealed class MinimalLockMutexTest
{
  /// <summary>
  /// A writer that cannot get the mutex in time drops the event and reports it; once the mutex is
  /// free it writes, and it releases the mutex after each write.
  /// </summary>
  [Test]
  [Platform(Exclude = "Win")]
  public void AHeldMutexKeepsTheWriterOut()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "minimal.log");
    FileAppender appender = CreateMinimalLockAppender(file);

    using (new HeldLock(file))
    {
      appender.DoAppend(CreateEvent("lost"));
    }
    appender.DoAppend(CreateEvent("written"));
    // Throws when the writer kept the mutex.
    using (new HeldLock(file))
    { }
    appender.Close();

    Assert.That(File.ReadAllText(file), Is.EqualTo("written" + Environment.NewLine));
    Assert.That(((RecordingErrorHandler)appender.ErrorHandler).Messages, Has.Count.EqualTo(1));
  }

  /// <summary>
  /// Control: a reader holding the file open does not keep the writer out.
  /// </summary>
  [Test]
  [Platform(Exclude = "Win")]
  public void AReaderDoesNotHoldUpTheWriter()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "minimal.log");
    FileAppender appender = CreateMinimalLockAppender(file);
    File.WriteAllText(file, string.Empty);

    string afterAppend;
    using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
    {
      appender.DoAppend(CreateEvent("written"));
      afterAppend = ReadWhileOpen(file);
    }
    appender.Close();

    Assert.That(afterAppend, Is.EqualTo("written" + Environment.NewLine));
  }

  /// <summary>
  /// An open that fails after the mutex was taken releases it, as no release follows a failed acquire.
  /// </summary>
  [Test]
  [Platform(Exclude = "Win")]
  public void AFailedOpenReleasesTheMutex()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "minimal.log");
    FileAppender appender = CreateMinimalLockAppender(file);
    File.WriteAllText(file, string.Empty);

    using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
    {
      appender.DoAppend(CreateEvent("lost"));
    }
    // Throws when the failed open kept the mutex.
    Assert.DoesNotThrow(() => new HeldLock(file).Dispose());
    appender.Close();
  }

  /// <summary>
  /// Without a writable home the runtime cannot create the mutex: one warning, and the writer goes on without it.
  /// </summary>
  [Test]
  [Platform(Exclude = "Win")]
  public void AMutexThatCannotBeCreatedIsReportedOnce()
  {
    List<LogLog> messages = [];
    Mutex? mutex = null;

    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      using LogLog.LogReceivedAdapter _ = new(messages);
      // A name the runtime refuses with the same IOException.
      mutex = typeof(FileAppender).Invoke<Mutex?>("TryCreateMutex", ["Global\\no/such", "nothing"]);
    });

    Assert.That(mutex, Is.Null);
    Assert.That(messages, Has.Count.EqualTo(1));
  }

  /// <summary>
  /// A mutex whose owner ended without releasing it counts as taken: the event is written, nothing reported.
  /// </summary>
  [Test]
  [Platform(Exclude = "Win")]
  public void AnAbandonedMutexCountsAsTaken()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "minimal.log");
    FileAppender appender = CreateMinimalLockAppender(file);
    using Mutex abandoned = new(false, typeof(FileAppender).Invoke<string>("MutexName", [file, string.Empty]));
    Thread owner = new(() => abandoned.WaitOne());
    owner.Start();
    Assert.That(owner.Join(TimeSpan.FromSeconds(10)), Is.True, "the owner did not end");

    appender.DoAppend(CreateEvent("written"));
    appender.Close();

    Assert.That(File.ReadAllText(file), Is.EqualTo("written" + Environment.NewLine));
    Assert.That(((RecordingErrorHandler)appender.ErrorHandler).Messages, Is.Empty);
  }

  /// <summary>
  /// Windows refuses a second writer's open already, so there the mutex is not taken.
  /// </summary>
  [Test]
  [Platform("Win")]
  public void WindowsTakesNoMutex()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "minimal.log");
    FileAppender appender = CreateMinimalLockAppender(file);

    using (new HeldLock(file))
    {
      appender.DoAppend(CreateEvent("written"));
    }
    appender.Close();

    Assert.That(File.ReadAllText(file), Is.EqualTo("written" + Environment.NewLine));
  }

  /// <summary>
  /// A rolling appender with a changing file name takes the mutex of its base name, as <see cref="FileAppender.InterProcessLock"/> does.
  /// </summary>
  [Test]
  [Platform(Exclude = "Win")]
  public void ARollingAppenderTakesTheMutexOfItsBaseName()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "rolling.log");
    FileAppender.MinimalLock lockingModel = new();
    lockingModel.Invoke("set_MutexTimeoutMillis", 200);
    RollingFileAppender appender = new()
    {
      File = file,
      StaticLogFileName = false,
      RollingStyle = RollingFileAppender.RollingMode.Size,
      CountDirection = 1,
      LockingModel = lockingModel,
      Layout = new PatternLayout("%message%newline"),
      ErrorHandler = new RecordingErrorHandler()
    };
    appender.ActivateOptions();

    using (new HeldLock(file))
    {
      appender.DoAppend(CreateEvent("lost"));
    }
    appender.Close();

    Assert.That(((RecordingErrorHandler)appender.ErrorHandler).Messages, Has.Count.EqualTo(1));
  }

  /// <summary>
  /// Without a file name there is no mutex, and closing disposes the one there was.
  /// </summary>
  [Test]
  [Platform(Exclude = "Win")]
  public void TheMutexLivesFromActivationToClose()
  {
    using AutoTempFolder folder = new();
    FileAppender.MinimalLock unnamed = new() { CurrentAppender = new FileAppender() };
    unnamed.ActivateOptions();
    FileAppender appender = CreateMinimalLockAppender(Path.Combine(folder.Path, "minimal.log"));
    FileAppender.MinimalLock lockingModel = (FileAppender.MinimalLock)appender.LockingModel;
    Mutex? whileOpen = lockingModel.GetFieldValue<Mutex?>("_mutex");

    appender.Close();

    Assert.That(unnamed.GetFieldValue<Mutex?>("_mutex"), Is.Null);
    Assert.That(whileOpen, Is.Not.Null);
    Assert.That(lockingModel.GetFieldValue<Mutex?>("_mutex"), Is.Null);
  }

  private static FileAppender CreateMinimalLockAppender(string file)
  {
    FileAppender.MinimalLock lockingModel = new();
    lockingModel.Invoke("set_MutexTimeoutMillis", 200);
    FileAppender appender = CreateAppender(file, lockingModel);
    appender.ActivateOptions();
    return appender;
  }
}
