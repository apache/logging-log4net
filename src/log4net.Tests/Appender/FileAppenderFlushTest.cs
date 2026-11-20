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
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;

using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Tests.Appender.Internal;
using log4net.Util;

using NUnit.Framework;

using PeanutButter.Utils;

using static log4net.Tests.Appender.Internal.TestFileAppenders;

namespace log4net.Tests.Appender;

/// <summary>
/// <see cref="FileAppender.Flush(int)"/> writes the buffered text under the file lock.
/// </summary>
[TestFixture]
public sealed class FileAppenderFlushTest
{
  /// <summary>
  /// <c>Flush(int)</c> wrote without the file lock: the locking stream refused, the flush returned
  /// <see langword="false"/> and the buffered text was lost.
  /// </summary>
  [TestCase(typeof(FileAppender.NoLock))]
  [TestCase(typeof(FileAppender.MinimalLock))]
  [TestCase(typeof(FileAppender.ExclusiveLock))]
  [TestCase(typeof(FileAppender.InterProcessLock))]
  public void FlushWritesTheBufferedText(Type lockingModel)
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "flushed.log");
    FileAppender appender = CreateAppender(file, (FileAppender.LockingModelBase)Activator.CreateInstance(lockingModel)!);
    appender.ImmediateFlush = false;
    LogLog.ExecuteWithoutEmittingInternalMessages(appender.ActivateOptions);

    appender.DoAppend(CreateEvent("buffered"));
    bool isFlushed = appender.Flush(0);
    string afterFlush = ReadWhileOpen(file);
    appender.Close();

    Assert.That(isFlushed, Is.True);
    Assert.That(afterFlush, Is.EqualTo("buffered" + Environment.NewLine));
    Assert.That(((RecordingErrorHandler)appender.ErrorHandler).Messages, Is.Empty);
  }

  /// <summary>
  /// Without an open file there is nothing to flush: <see langword="true"/>, nothing reported.
  /// </summary>
  [TestCase(true)]
  [TestCase(false)]
  public void FlushWithoutAnOpenFileHasNothingToDo(bool isClosed)
  {
    using AutoTempFolder folder = new();
    // Not a shared lock: those return before the writer is checked.
    FileAppender appender = CreateAppender(Path.Combine(folder.Path, "closed.log"), new FileAppender.ExclusiveLock());
    appender.ImmediateFlush = false;
    if (isClosed)
    {
      LogLog.ExecuteWithoutEmittingInternalMessages(appender.ActivateOptions);
      appender.Close();
    }
    else
    {
      appender.File = null;
    }

    Assert.That(appender.Flush(0), Is.True);
    Assert.That(((RecordingErrorHandler)appender.ErrorHandler).Messages, Is.Empty);
  }

  /// <summary>
  /// A flush during an append waits for it on the appender lock before it touches the file lock.
  /// The other order raised the lock count of the writer's acquisition and released it on the
  /// flushing thread.
  /// </summary>
  [Test]
  public void FlushTakesTheAppenderLockBeforeTheFileLock()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "ordered.log");
    using ManualResetEventSlim entered = new();
    using ManualResetEventSlim gate = new();
    ThreadCheckingLock lockingModel = new();
    FileAppender appender = CreateAppender(file, lockingModel);
    appender.Layout = new GateLayout(entered, gate);
    appender.ImmediateFlush = false;
    appender.ActivateOptions();
    // Warms up, so that JIT or type initialisation cannot pass for a flusher blocked on the lock.
    appender.Flush(0);
    lockingModel.Reset();
    ExceptionDispatchInfo? failure = null;
    bool isFlushed = false;

    Thread writer = StartWorker(() => appender.DoAppend(CreateEvent("record")), e => failure = e);
    Thread flusher;
    bool isParked;
    try
    {
      Assert.That(entered.Wait(TimeSpan.FromSeconds(10)), Is.True, "the writer never reached the layout");
      flusher = StartWorker(() => isFlushed = appender.Flush(0), e => failure = e);
      isParked = TryWaitUntilBlocked(flusher);
    }
    finally
    {
      gate.Set();
    }
    Assert.That(writer.Join(TimeSpan.FromSeconds(10)), Is.True, "the writer did not finish");
    Assert.That(flusher.Join(TimeSpan.FromSeconds(10)), Is.True, "the flusher did not finish");
    failure?.Throw();
    (int acquired, int released, int foreignReleases) = (lockingModel.Acquired, lockingModel.Released, lockingModel.ForeignReleases);
    string afterFlush = ReadWhileOpen(file);
    appender.Close();

    Assert.That(isParked, Is.True, "the flusher was never seen waiting for the writer");
    Assert.That(foreignReleases, Is.EqualTo(0));
    Assert.That(acquired, Is.EqualTo(2));
    Assert.That(released, Is.EqualTo(2));
    Assert.That(isFlushed, Is.True);
    Assert.That(afterFlush, Is.EqualTo("record" + Environment.NewLine));
    Assert.That(((RecordingErrorHandler)appender.ErrorHandler).Messages, Is.Empty);
  }

  /// <summary>
  /// A flush that cannot get the file lock reports <see langword="false"/>.
  /// </summary>
  [Test]
  public void FlushWithoutTheFileLockFails()
  {
    using AutoTempFolder folder = new();
    ThreadCheckingLock lockingModel = new();
    FileAppender appender = CreateAppender(Path.Combine(folder.Path, "refused.log"), lockingModel);
    appender.ImmediateFlush = false;
    appender.ActivateOptions();
    appender.DoAppend(CreateEvent("record"));

    lockingModel.IsRefusing = true;
    bool isFlushed = appender.Flush(0);
    lockingModel.IsRefusing = false;
    appender.Close();

    Assert.That(isFlushed, Is.False);
  }

  /// <summary>
  /// The shared locks leave nothing buffered, so a flush has nothing to wait for, even with the lock held elsewhere.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void FlushWithASharedLockDoesNotWaitForIt()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "held.log");
    CountingInterProcessLock lockingModel = new() { LockTimeoutMillis = 200 };
    FileAppender appender = CreateAppender(file, lockingModel);
    appender.ImmediateFlush = false;
    LogLog.ExecuteWithoutEmittingInternalMessages(appender.ActivateOptions);
    appender.DoAppend(CreateEvent("record"));
    lockingModel.Acquired = 0;
    bool isFlushed;

    using (new HeldLock(file))
    {
      isFlushed = appender.Flush(0);
    }
    int acquired = lockingModel.Acquired;
    appender.Close();

    Assert.That(isFlushed, Is.True);
    Assert.That(acquired, Is.EqualTo(0));
    Assert.That(((RecordingErrorHandler)appender.ErrorHandler).Messages, Is.Empty);
  }

  /// <summary>
  /// <see cref="FileAppender.MinimalLock"/> likewise: a flush does not take the lock, so it does not open the file.
  /// </summary>
  [Test]
  public void FlushWithTheMinimalLockDoesNotTakeIt()
  {
    using AutoTempFolder folder = new();
    CountingMinimalLock lockingModel = new();
    FileAppender appender = CreateAppender(Path.Combine(folder.Path, "minimal.log"), lockingModel);
    appender.ImmediateFlush = false;
    LogLog.ExecuteWithoutEmittingInternalMessages(appender.ActivateOptions);
    appender.DoAppend(CreateEvent("record"));
    lockingModel.Acquired = 0;

    bool isFlushed = appender.Flush(0);
    int acquired = lockingModel.Acquired;
    appender.Close();

    Assert.That(isFlushed, Is.True);
    Assert.That(acquired, Is.EqualTo(0));
  }

  /// <summary>Counts the calls to <see cref="FileAppender.MinimalLock.AcquireLock"/>.</summary>
  private sealed class CountingMinimalLock : FileAppender.MinimalLock
  {
    internal int Acquired { get; set; }

    /// <inheritdoc/>
    public override Stream? AcquireLock()
    {
      Acquired++;
      return base.AcquireLock();
    }
  }

  /// <summary>
  /// Waits up to 10 seconds until <paramref name="thread"/> blocks or ends; whether it blocked.
  /// </summary>
  private static bool TryWaitUntilBlocked(Thread thread)
  {
    const ThreadState blockedOrEnded = ThreadState.WaitSleepJoin | ThreadState.Stopped;
    SpinWait.SpinUntil(() => (thread.ThreadState & blockedOrEnded) != 0, TimeSpan.FromSeconds(10));
    return (thread.ThreadState & ThreadState.WaitSleepJoin) != 0;
  }

  /// <summary>Runs <paramref name="action"/> on a background thread, handing an exception to <paramref name="onFailure"/>.</summary>
  private static Thread StartWorker(Action action, Action<ExceptionDispatchInfo> onFailure)
  {
    Thread thread = new(() =>
    {
      try
      {
        action();
      }
      catch (Exception e) when (!e.IsFatal())
      {
        onFailure(ExceptionDispatchInfo.Capture(e));
      }
    })
    { IsBackground = true };
    thread.Start();
    return thread;
  }

  /// <summary>
  /// An exclusive lock that records the acquiring thread and counts releases on any other thread.
  /// </summary>
  private sealed class ThreadCheckingLock : FileAppender.ExclusiveLock
  {
    private int _acquiringThread;

    internal int Acquired { get; private set; }

    internal int Released { get; private set; }

    internal int ForeignReleases { get; private set; }

    internal bool IsRefusing { get; set; }

    internal void Reset() => (Acquired, Released, ForeignReleases) = (0, 0, 0);

    /// <inheritdoc/>
    public override Stream? AcquireLock()
    {
      if (IsRefusing)
      {
        return null;
      }
      Stream? stream = base.AcquireLock();
      _acquiringThread = Environment.CurrentManagedThreadId;
      Acquired++;
      return stream;
    }

    /// <inheritdoc/>
    public override void ReleaseLock()
    {
      if (Environment.CurrentManagedThreadId != _acquiringThread)
      {
        ForeignReleases++;
      }
      Released++;
      base.ReleaseLock();
    }
  }

  /// <summary>Parks the first event it formats until the gate opens.</summary>
  private sealed class GateLayout(ManualResetEventSlim entered, ManualResetEventSlim gate) : LayoutSkeleton
  {
    private bool _hasParked;

    /// <inheritdoc/>
    public override void ActivateOptions()
    { }

    /// <inheritdoc/>
    public override void Format(TextWriter writer, LoggingEvent loggingEvent)
    {
      if (!_hasParked)
      {
        _hasParked = true;
        entered.Set();
        if (!gate.Wait(TimeSpan.FromSeconds(10)))
        {
          throw new TimeoutException("the gate was never opened");
        }
      }
      writer.Write(loggingEvent.RenderedMessage + Environment.NewLine);
    }
  }
}
