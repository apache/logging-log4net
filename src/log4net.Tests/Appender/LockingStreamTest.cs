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
using System.Text;

using log4net.Appender;

using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>The recursion counter inside the private <c>FileAppender.LockingStream</c>.</summary>
[TestFixture]
public sealed class LockingStreamTest
{
  /// <summary>Hands out a stream only when told to, so a failed acquisition can be staged.</summary>
  private sealed class SwitchableLock : FileAppender.LockingModelBase
  {
    internal bool CanAcquire { get; set; }

    internal int ReleaseCount { get; private set; }

    /// <inheritdoc/>
    public override Stream? AcquireLock() => CanAcquire ? Stream.Null : null;

    /// <inheritdoc/>
    public override void ReleaseLock() => ReleaseCount++;

    /// <inheritdoc/>
    public override void OpenFile(string filename, bool append, Encoding encoding)
    { }

    /// <inheritdoc/>
    public override void CloseFile()
    { }

    /// <inheritdoc/>
    public override void ActivateOptions()
    { }

    /// <inheritdoc/>
    public override void OnClose()
    { }
  }

  /// <summary>
  /// An unmatched release drove the counter below zero, after which every later acquisition failed
  /// and the model lock was never released.
  /// </summary>
  [Test]
  public void AnUnmatchedReleaseDoesNotBreakTheNextAcquisition()
  {
    SwitchableLock model = new() { CanAcquire = false };
    object stream = NewLockingStream(model);

    // What the footer, close and open paths used to do.
    Assert.That(stream.Invoke<bool>("AcquireLock"), Is.False, "the model was set up to refuse");
    stream.Invoke("ReleaseLock");

    model.CanAcquire = true;

    Assert.That(stream.Invoke<bool>("AcquireLock"), Is.True,
      "the counter went negative, so the stream could never be locked again");
    stream.Invoke("ReleaseLock");
    Assert.That(model.ReleaseCount, Is.EqualTo(1), "the model lock must be released exactly once");
  }

  /// <summary>Nesting still locks and releases the model once.</summary>
  [Test]
  public void NestedAcquisitionsReleaseTheModelOnce()
  {
    SwitchableLock model = new() { CanAcquire = true };
    object stream = NewLockingStream(model);

    Assert.That(stream.Invoke<bool>("AcquireLock"), Is.True);
    Assert.That(stream.Invoke<bool>("AcquireLock"), Is.True);
    stream.Invoke("ReleaseLock");
    Assert.That(model.ReleaseCount, Is.EqualTo(0), "still held by the outer acquisition");

    stream.Invoke("ReleaseLock");
    Assert.That(model.ReleaseCount, Is.EqualTo(1));
  }

  /// <summary>
  /// The footer, close and open paths run through one helper. The work has to happen either way,
  /// because closing is what releases the OS handle, but only a lock that was taken may be released.
  /// </summary>
  [Test]
  public void RunWithBestEffortLockRunsTheWorkButReleasesOnlyWhatItTook()
  {
    SwitchableLock model = new() { CanAcquire = false };
    FileAppender appender = new();
    SetStream(appender, NewLockingStream(model));

    bool ran = false;
    RunWithBestEffortLock(appender, () => ran = true);
    Assert.That(ran, Is.True, "the work must run even without the lock, or the file is never closed");
    Assert.That(model.ReleaseCount, Is.EqualTo(0), "released a lock it never took");

    model.CanAcquire = true;
    ran = false;

    RunWithBestEffortLock(appender, () => ran = true);
    Assert.That(ran, Is.True);
    Assert.That(model.ReleaseCount, Is.EqualTo(1), "the counter went negative, so nothing locked again");
  }


  private static void SetStream(FileAppender appender, object stream)
    => appender.SetFieldValue("_stream", stream);

  private static void RunWithBestEffortLock(FileAppender appender, Action action)
    => appender.Invoke(nameof(RunWithBestEffortLock), [action]);

  private static object NewLockingStream(FileAppender.LockingModelBase model)
    => typeof(FileAppender).NonPublicNestedType("LockingStream").Construct<object>([model]);
}
