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
using System.Reflection;
using System.Text;
using System.Threading;

using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Util;

using NUnit.Framework;

using PeanutButter.Utils;

namespace log4net.Tests.Appender;

/// <summary>The mutex name the file lock and the rolling lock derive from the log file path.</summary>
[TestFixture]
public sealed class FileAppenderMutexNameTest
{
  /// <summary>Records what the appender had resolved by the time the locking model was activated.</summary>
  private sealed class RecordingLock : FileAppender.LockingModelBase
  {
    internal string? FileAtActivation { get; private set; }

    /// <inheritdoc/>
    public override void ActivateOptions() => FileAtActivation = CurrentAppender?.File;

    /// <inheritdoc/>
    public override Stream? AcquireLock() => Stream.Null;

    /// <inheritdoc/>
    public override void ReleaseLock()
    { }

    /// <inheritdoc/>
    public override void OpenFile(string filename, bool append, Encoding encoding)
    { }

    /// <inheritdoc/>
    public override void CloseFile()
    { }

    /// <inheritdoc/>
    public override void OnClose()
    { }
  }

  /// <summary>
  /// The model was activated before the path was resolved, so a relative and an absolute spelling
  /// of one file never shared a mutex.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void TheLockingModelIsActivatedAfterThePathIsResolved()
  {
    RecordingLock model = new();
    FileAppender appender = new()
    {
      File = "mutex-name-test.log",
      Layout = new PatternLayout("%message%newline"),
      LockingModel = model,
      ErrorHandler = new Internal.RecordingErrorHandler()
    };

    try
    {
      LogLog.ExecuteWithoutEmittingInternalMessages(appender.ActivateOptions);

      Assert.That(model.FileAtActivation, Is.Not.Null, "the locking model was never activated");
      Assert.That(Path.IsPathRooted(model.FileAtActivation!), Is.True,
        "the locking model named its mutex after an unresolved path");
      Assert.That(model.FileAtActivation, Does.EndWith("mutex-name-test.log"));
    }
    finally
    {
      LogLog.ExecuteWithoutEmittingInternalMessages(appender.Close);
      string written = appender.File!;
      if (File.Exists(written))
      {
        File.Delete(written);
      }
    }
  }

  /// <summary>A name past the Unix limit took the appender down before anything was logged.</summary>
  [Test]
  [NonParallelizable]
  public void ADeepPathStillActivates()
  {
    using AutoTempFolder folder = new();

    // Over the 255 character mutex name limit once "_rolling" is added, under the 260 Windows
    // still enforces on net462.
    string leaf = new('d', 250 - folder.Path.Length - "roll.log".Length - 2);
    string directory = Path.Combine(folder.Path, leaf);
    Directory.CreateDirectory(directory);
    string file = Path.Combine(directory, "roll.log");
    Assert.That(file, Has.Length.EqualTo(250), "the fixture must exceed the mutex name limit");

    RollingFileAppender appender = new()
    {
      File = file,
      Layout = new PatternLayout("%message%newline"),
      RollingStyle = RollingFileAppender.RollingMode.Size,
      MaximumFileSize = "10KB",
      LockingModel = new FileAppender.MinimalLock(),
      ErrorHandler = new Internal.RecordingErrorHandler()
    };

    try
    {
      appender.ActivateOptions();
      appender.DoAppend(new LoggingEvent(new LoggingEventData
      {
        Level = Level.Info,
        Message = "deep",
        LoggerName = "MutexName"
      }));
    }
    finally
    {
      LogLog.ExecuteWithoutEmittingInternalMessages(appender.Close);
    }

    Assert.That(File.ReadAllText(file), Does.Contain("deep"));
  }

  /// <summary>A name that fits is left alone, so no existing deployment's mutex changes.</summary>
  [Test]
  public void AShortPathKeepsTheNameEarlierVersionsComputed()
    => Assert.That(MutexNameForPath("/var/log/app.log", "_rolling"), Is.EqualTo("_var_log_app.log_rolling"));

  /// <summary>Under a limit, a long name is hashed below it and stays distinct.</summary>
  [Test]
  public void ALongPathIsHashedWhereThePlatformHasALimit()
  {
    string deep = "/" + new string('d', 4000) + "/app.log";
    string name = MutexNameForPath(deep, "_rolling", 255);

    Assert.That(name, Has.Length.LessThanOrEqualTo(255));
    Assert.That(name, Does.EndWith("_rolling"));
    Assert.That(MutexNameForPath(deep + "x", "_rolling", 255), Is.Not.EqualTo(name),
      "two different paths collapsed onto one mutex");

    // The defect itself: this threw before the cap existed.
    using Mutex mutex = new(false, name);
    Assert.That(mutex.WaitOne(0), Is.True);
    mutex.ReleaseMutex();
  }

  /// <summary>
  /// The limit is on encoded bytes, not characters: a name of 104 characters and 304 bytes is
  /// already refused, measured, so counting characters lets one through that cannot be created.
  /// </summary>
  [Test]
  public void AMultibytePathIsMeasuredInBytes()
  {
    // Well under the limit as characters, well over it as UTF-8.
    string multibyte = "/" + new string('\u4e2d', 200) + "/app.log";
    Assert.That(multibyte, Has.Length.LessThan(255), "the point of the fixture is a short-looking name");

    string name = MutexNameForPath(multibyte, "_rolling", 255);

    Assert.That(Encoding.UTF8.GetByteCount(name), Is.LessThanOrEqualTo(255));
    using Mutex mutex = new(false, name);
    Assert.That(mutex.WaitOne(0), Is.True);
    mutex.ReleaseMutex();
  }

  /// <summary>With no limit, which is Windows, the name is left as earlier versions computed it.</summary>
  [Test]
  public void ALongPathIsLeftAloneWhereThePlatformHasNoLimit()
  {
    string deep = "/" + new string('d', 4000) + "/app.log";

    Assert.That(MutexNameForPath(deep, "_rolling", null),
      Is.EqualTo("_" + new string('d', 4000) + "_app.log_rolling"));
  }

  /// <summary>
  /// Which limit the platform gets, expected from this fixture's own check rather than the one
  /// under test. On Windows nothing else would notice a wrong gate.
  /// </summary>
  [Test]
  public void ThePlatformDecidesWhetherALongNameIsHashed()
  {
    string deep = "/" + new string('d', 4000) + "/app.log";
    string name = MutexNameForPath(deep, "_rolling");

    if (Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX)
    {
      Assert.That(name, Has.Length.LessThanOrEqualTo(255), "Unix rejects a longer name");
    }
    else
    {
      Assert.That(name, Has.Length.EqualTo(deep.Length + "_rolling".Length),
        "Windows has no limit, so the name must be left as earlier versions computed it");
    }
  }

  private static string MutexNameForPath(string path, string suffix)
    => (string)typeof(FileAppender)
      .GetMethod("MutexNameForPath", BindingFlags.NonPublic | BindingFlags.Static,
        null, [typeof(string), typeof(string)], null)!
      .Invoke(null, [path, suffix])!;

  private static string MutexNameForPath(string path, string suffix, int? maxLength)
    => (string)typeof(FileAppender)
      .GetMethod("MutexNameForPath", BindingFlags.NonPublic | BindingFlags.Static,
        null, [typeof(string), typeof(string), typeof(int?)], null)!
      .Invoke(null, [path, suffix, maxLength])!;
}
