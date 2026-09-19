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
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Tests.Integration;
using log4net.Util;

using NUnit.Framework;

using PeanutButter.Utils;

namespace log4net.Tests.Appender;

/// <summary>
/// What <see cref="RollingFileAppender"/> does when a roll cannot complete. Every test here blocks
/// the base rename with <see cref="BlockRename"/>, which leaves the archive shift working, so the
/// state under test is the reported one: a reader holding the log file and nothing else.
/// </summary>
[TestFixture]
public sealed class RollingFileAppenderRollFailureTest
{
  private const string Marker = "must survive the failed roll";

  /// <summary>
  /// A failed rename leaves the file in place, and reopening it without appending destroyed it.
  /// The archive it shifted on the way must not be shifted a second time.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void AFailedRollKeepsTheEventsItCouldNotMove()
  {
    using AutoTempFolder folder = new();
    Internal.RecordingErrorHandler errors = new();
    string file = Path.Combine(folder.Path, "roll-failure.log");
    RollingFileAppender appender = new()
    {
      File = file,
      Layout = new PatternLayout("%message%newline"),
      RollingStyle = RollingFileAppender.RollingMode.Size,
      MaxSizeRollBackups = 3,
      MaximumFileSize = "200",
      AppendToFile = true,
      LockingModel = new FileAppender.MinimalLock(),
      ErrorHandler = errors
    };
    appender.ActivateOptions();

    try
    {
      // Two ordinary rolls, so there is an archive to rotate.
      appender.DoAppend(CreateEvent(new string('a', 200)));
      appender.DoAppend(CreateEvent(new string('a', 200)));
      appender.DoAppend(CreateEvent(new string('a', 200)));

      BlockRename(file + ".1");

      // The file is already over the limit, so this event rolls first and the roll is the one that
      // fails. What it writes afterwards is the content the failed roll used to destroy.
      LogLog.ExecuteWithoutEmittingInternalMessages(() => appender.DoAppend(CreateEvent(Marker)));
      Assert.That(errors.Messages, Is.Not.Empty, "the roll never failed, so nothing was exercised");

      // That first attempt shifts the archive before failing on the base file, which is
      // unavoidable. What must not happen is a second shift.
      string[] afterFirstFailure = Backups(file);
      Assert.That(afterFirstFailure, Is.Not.Empty, "the fixture needs an archive for the roll to shift");
      string[] contentAfterFirstFailure = Array.ConvertAll(afterFirstFailure, File.ReadAllText);
      int attemptsBeforeGrowth = BaseRenameAttempts(errors, file);

      // Events well under a tenth of MaxFileSize, which is the retry cadence, so the attempts are
      // measurably rarer than the events. At 20 bytes each the two would be the same thing.
      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        for (int i = 0; i < 20; i++)
        {
          appender.DoAppend(CreateEvent("b"));
        }
      });

      Assert.That(Backups(file), Is.EqualTo(afterFirstFailure),
        "the archive was rotated again while the rename kept failing");
      Assert.That(Array.ConvertAll(afterFirstFailure, File.ReadAllText), Is.EqualTo(contentAfterFirstFailure),
        "the backup contents were rewritten while the rename kept failing");
      // Only the base rename is retried, once per tenth of MaxFileSize of growth: often enough
      // that 40 bytes of events bring two attempts, rarely enough that 20 events do not bring 20.
      Assert.That(BaseRenameAttempts(errors, file) - attemptsBeforeGrowth, Is.InRange(2, 19),
        "the retry cadence is neither a tenth of MaxFileSize nor anything close to it");
    }
    finally
    {
      LogLog.ExecuteWithoutEmittingInternalMessages(appender.Close);
    }

    Assert.That(File.ReadAllText(file), Does.Contain(Marker),
      "the roll could not rename the file, so reopening it must not have truncated it");
  }

  /// <summary>
  /// The same failure with a dated name, where the file being rolled is not the configured one.
  /// Testing the base file's existence instead of the one the rename could not move passes here
  /// and truncates anyway.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void AFailedRollKeepsTheEventsWhenTheFileNameIsDated()
  {
    using AutoTempFolder folder = new();
    Internal.RecordingErrorHandler errors = new();
    string file = Path.Combine(folder.Path, "roll-failure.log");
    RollingFileAppender appender = new()
    {
      File = file,
      Layout = new PatternLayout("%message%newline"),
      RollingStyle = RollingFileAppender.RollingMode.Composite,
      DatePattern = "'.'yyyy-MM-dd",
      StaticLogFileName = false,
      MaxSizeRollBackups = 3,
      MaximumFileSize = "200",
      AppendToFile = true,
      LockingModel = new FileAppender.MinimalLock(),
      ErrorHandler = errors
    };
    appender.ActivateOptions();

    // The file that is written and rolled, which is not the configured name.
    string dated = appender.File!;
    Assert.That(dated, Is.Not.EqualTo(file), "the fixture needs a name the configured one does not match");

    try
    {
      appender.DoAppend(CreateEvent(new string('a', 200)));

      BlockRename(dated + ".1");

      // Rolls first, fails on the dated name, and keeps what the rename could not move.
      LogLog.ExecuteWithoutEmittingInternalMessages(
        () => appender.DoAppend(CreateEvent(new string('b', 200))));
      Assert.That(errors.Messages, Is.Not.Empty, "the roll never failed, so nothing was exercised");

      // Written into the file the failed rename left behind, so a truncating reopen destroys it.
      appender.DoAppend(CreateEvent(Marker));

      // Grow past the retry threshold, so the base rename is attempted and fails again.
      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        for (int i = 0; i < 20; i++)
        {
          appender.DoAppend(CreateEvent(new string('b', 200)));
        }
      });
    }
    finally
    {
      LogLog.ExecuteWithoutEmittingInternalMessages(appender.Close);
    }

    Assert.That(File.ReadAllText(dated), Does.Contain(Marker),
      "the roll could not rename the dated file, so reopening it must not have truncated it");
  }

  /// <summary>
  /// A retry that succeeds moves the kept file into the archive, and the next roll has to shift
  /// that generation rather than overwrite it.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ASuccessfulRetryKeepsTheBackupItRecovered()
  {
    using AutoTempFolder folder = new();
    Internal.RecordingErrorHandler errors = new();
    string file = Path.Combine(folder.Path, "roll-failure.log");
    RollingFileAppender appender = new()
    {
      File = file,
      Layout = new PatternLayout("%message%newline"),
      RollingStyle = RollingFileAppender.RollingMode.Size,
      // Far more than the run needs, so nothing may be discarded as too old.
      MaxSizeRollBackups = 10,
      MaximumFileSize = "200",
      AppendToFile = true,
      LockingModel = new FileAppender.MinimalLock(),
      ErrorHandler = errors
    };
    appender.ActivateOptions();

    try
    {
      appender.DoAppend(CreateEvent(new string('a', 200)));

      BlockRename(file + ".1");

      // Rolls first, fails, and is then written into the file the rename could not move.
      LogLog.ExecuteWithoutEmittingInternalMessages(() => appender.DoAppend(CreateEvent(Marker)));
      Assert.That(errors.Messages, Is.Not.Empty, "the roll never failed, so nothing was exercised");

      // The obstruction is gone, so the next retry succeeds.
      UnblockRename(file + ".1");

      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        for (int i = 0; i < 20 && !ArchiveHolds(file, Marker); i++)
        {
          appender.DoAppend(CreateEvent(new string('b', 200)));
        }
      });
      Assert.That(ArchiveHolds(file, Marker), Is.True,
        "the retry never moved the kept file into the archive, so nothing was recovered");

      // One ordinary roll on top of the recovered generation.
      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        for (int i = 0; i < 2; i++)
        {
          appender.DoAppend(CreateEvent(new string('c', 200)));
        }
      });
    }
    finally
    {
      LogLog.ExecuteWithoutEmittingInternalMessages(appender.Close);
    }

    Assert.That(ArchiveHolds(file, Marker), Is.True,
      "the roll after the retry overwrote the backup the retry had just recovered");
  }

  /// <summary>
  /// A failed base rename leaves the numbered files one slot higher than the backup count says,
  /// and the time roll has to take that top one with it.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ATimeRollAfterAFailedRenameTakesEveryBackupWithIt()
  {
    using AutoTempFolder folder = new();
    Internal.RecordingErrorHandler errors = new();
    string file = Path.Combine(folder.Path, "roll-failure.log");
    MockDateTime clock = new(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local));
    RollingFileAppender appender = new()
    {
      File = file,
      Layout = new PatternLayout("%message%newline"),
      RollingStyle = RollingFileAppender.RollingMode.Composite,
      DatePattern = "'.'yyyy-MM-dd",
      StaticLogFileName = true,
      MaxSizeRollBackups = 5,
      MaximumFileSize = "200",
      AppendToFile = true,
      LockingModel = new FileAppender.MinimalLock(),
      DateTimeStrategy = clock,
      ErrorHandler = errors
    };
    appender.ActivateOptions();

    try
    {
      // Two ordinary rolls, so the marker ends up in the second backup.
      appender.DoAppend(CreateEvent(Marker + new string('a', 200)));
      appender.DoAppend(CreateEvent(new string('a', 200)));
      appender.DoAppend(CreateEvent(new string('a', 200)));

      BlockRename(file + ".1");

      LogLog.ExecuteWithoutEmittingInternalMessages(
        () => appender.DoAppend(CreateEvent(new string('b', 200))));
      Assert.That(errors.Messages, Is.Not.Empty, "the base rename never failed, so nothing was exercised");
      Assert.That(File.ReadAllText(file + ".3"), Does.Contain(Marker),
        "the fixture needs the archive shifted a slot beyond the backup count");

      // A day later, so the time roll moves the whole group under the dated name.
      clock.Now = clock.Now.AddDays(1);
      LogLog.ExecuteWithoutEmittingInternalMessages(() => appender.DoAppend(CreateEvent("after midnight")));
    }
    finally
    {
      LogLog.ExecuteWithoutEmittingInternalMessages(appender.Close);
    }

    Assert.That(File.Exists(file + ".3"), Is.False,
      "the backup stayed under the old base name instead of moving with the group");
  }

  /// <summary>
  /// A failed time rename is retried too, where size rolling is switched off entirely. Without
  /// that, the pending rename waits for a size roll that never comes, and the next boundary
  /// archives the accumulated file under the later period.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void AFailedTimeRenameIsRetriedWithoutSizeRolling()
  {
    using AutoTempFolder folder = new();
    Internal.RecordingErrorHandler errors = new();
    string file = Path.Combine(folder.Path, "roll-failure.log");
    MockDateTime clock = new(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local));
    RollingFileAppender appender = new()
    {
      File = file,
      Layout = new PatternLayout("%message%newline"),
      RollingStyle = RollingFileAppender.RollingMode.Date,
      DatePattern = "'.'yyyy-MM-dd",
      StaticLogFileName = true,
      // Size rolling stays off; this only paces the retry, which is what the defect ignored.
      MaximumFileSize = "200",
      AppendToFile = true,
      LockingModel = new FileAppender.MinimalLock(),
      DateTimeStrategy = clock,
      ErrorHandler = errors
    };
    appender.ActivateOptions();
    string dated = file + ".2026-01-01";

    try
    {
      appender.DoAppend(CreateEvent(Marker));

      BlockRename(dated);

      // A day on, so the time roll runs and cannot move the file.
      clock.Now = clock.Now.AddDays(1);
      LogLog.ExecuteWithoutEmittingInternalMessages(
        () => appender.DoAppend(CreateEvent(new string('a', 200))));
      Assert.That(errors.Messages, Is.Not.Empty, "the time rename never failed, so nothing was exercised");
      Assert.That(File.ReadAllText(file), Does.Contain(Marker), "the failed rename must keep the file");

      UnblockRename(dated);

      // Grow past the retry threshold. With the retry nested under size rolling, nothing happens.
      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        for (int i = 0; i < 20 && !File.Exists(dated); i++)
        {
          appender.DoAppend(CreateEvent(new string('b', 200)));
        }
      });
    }
    finally
    {
      LogLog.ExecuteWithoutEmittingInternalMessages(appender.Close);
    }

    Assert.That(File.Exists(dated), Is.True,
      "the pending time rename was never retried, so the period was never archived");
    Assert.That(File.ReadAllText(dated), Does.Contain(Marker));
  }

  /// <summary>
  /// The roll ExistingInit performs at startup, before anything is open. Its failed rename used to
  /// fall through to the configured AppendToFile, so AppendToFile=false destroyed the period the
  /// rename could not archive.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void AFailedStartupRollKeepsTheFileItCouldNotMove()
  {
    using AutoTempFolder folder = new();
    Internal.RecordingErrorHandler errors = new();
    string file = Path.Combine(folder.Path, "roll-failure.log");
    MockDateTime clock = new(new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Local));

    // Yesterday's log, left behind by the previous run, and its archive name is taken.
    File.WriteAllText(file, Marker + Environment.NewLine);
    File.SetLastWriteTime(file, clock.Now.AddDays(-1));
    string dated = file + ".2026-01-01";
    BlockRename(dated);

    RollingFileAppender appender = new()
    {
      File = file,
      Layout = new PatternLayout("%message%newline"),
      RollingStyle = RollingFileAppender.RollingMode.Date,
      DatePattern = "'.'yyyy-MM-dd",
      StaticLogFileName = true,
      // The setting that used to decide it: an explicit request for an empty file at startup.
      AppendToFile = false,
      LockingModel = new FileAppender.MinimalLock(),
      DateTimeStrategy = clock,
      ErrorHandler = errors
    };

    try
    {
      LogLog.ExecuteWithoutEmittingInternalMessages(appender.ActivateOptions);
      Assert.That(errors.Messages, Is.Not.Empty, "the startup rename never failed, so nothing was exercised");

      LogLog.ExecuteWithoutEmittingInternalMessages(() => appender.DoAppend(CreateEvent("after startup")));
    }
    finally
    {
      LogLog.ExecuteWithoutEmittingInternalMessages(appender.Close);
    }

    Assert.That(File.ReadAllText(file), Does.Contain(Marker),
      "the startup roll could not rename the file, so opening it must not have truncated it");
  }

  /// <summary>
  /// Blocks one rename by occupying its target with a directory. <see cref="File.Move(string,string)"/>
  /// throws when the destination exists, and the appender's own delete of the target skips it,
  /// because <see cref="File.Exists"/> is false for a directory. Only that rename fails, so the
  /// archive shift still goes through, which is the reported shape: a reader holding the log file
  /// without FILE_SHARE_DELETE.
  /// </summary>
  private static void BlockRename(string target)
  {
    if (File.Exists(target))
    {
      File.Delete(target);
    }

    Directory.CreateDirectory(target);
  }

  private static void UnblockRename(string target) => Directory.Delete(target, true);

  /// <summary>The numbered backups, excluding the log file itself: a `.*` pattern matches it too.</summary>
  private static string[] Backups(string file)
    => Array.FindAll(
      Directory.GetFiles(Path.GetDirectoryName(file)!, Path.GetFileName(file) + ".*"),
      f => !string.Equals(f, file, StringComparison.Ordinal));

  /// <summary>
  /// How often the base rename itself was attempted. A failed attempt can report twice, once for
  /// the delete of the target and once for the move, so counting messages counts the wrong thing.
  /// </summary>
  private static int BaseRenameAttempts(Internal.RecordingErrorHandler errors, string file)
    => errors.Messages.FindAll(m => m.IndexOf($"[{file}] ->", StringComparison.Ordinal) >= 0).Count;

  /// <summary>Whether any numbered backup holds <paramref name="content"/>.</summary>
  private static bool ArchiveHolds(string file, string content)
    => Array.Exists(Backups(file),
      f => File.ReadAllText(f).IndexOf(content, StringComparison.Ordinal) >= 0);

  private static LoggingEvent CreateEvent(string message)
    => new(new LoggingEventData { Level = Level.Info, Message = message, LoggerName = "RollFailure" });
}
