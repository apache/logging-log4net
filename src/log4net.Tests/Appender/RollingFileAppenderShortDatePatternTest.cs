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
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Util;

using NUnit.Framework;

using PeanutButter.Utils;

namespace log4net.Tests.Appender;

/// <summary>
/// What <see cref="RollingFileAppender"/> does when a static name and a short date pattern name
/// dated files like numbered backups, see #335.
/// </summary>
[TestFixture]
public sealed class RollingFileAppenderShortDatePatternTest
{
  /// <summary>
  /// With a static name, .dd names the file of the 10th like backup 10, so the appender is
  /// reported and uses the dated name instead, with its backups numbered after the date.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ShortDatePatternUsesDatedFileName()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "log.txt");
    RollingFileAppender appender = CreateShortDatePatternAppender(file, ".dd", 10, -1);
    appender.MaxFileSize = 100;
    List<string> messages = ActivateCapturingInternalMessages(appender);
    try
    {
      Assert.That(messages, Has.Exactly(1).Contains("DatePattern [.dd]"));
      for (int i = 0; i < 5; i++)
      {
        appender.DoAppend(CreateEvent(new string('a', 200)));
      }
    }
    finally
    {
      appender.Close();
    }

    string[] files = Directory.GetFiles(folder.Path);
    Assert.That(files, Does.Not.Contain(file));
    Assert.That(files, Does.Contain(file + ".04").And.Contain(file + ".04.1"));
  }

  /// <summary>
  /// Only a date that a backup name can reach is a collision: counting down to 5 never writes .10,
  /// a non-static name keeps its backups apart from the dated files, and five or more digits
  /// never count as a backup.
  /// </summary>
  [TestCase(".d", true, 5, -1, true)]
  [TestCase(".dd", true, 5, 1, true)]
  [TestCase(".dd", true, 5, -1, false)]
  [TestCase(".dd", true, 10, -1, true)]
  [TestCase(".dd", true, -1, -1, true)]
  [TestCase(".dd", false, 10, -1, false)]
  [TestCase(".yyyyMMdd", true, 10, 1, false)]
  [TestCase(".yyyy-MM-dd", true, 10, -1, false)]
  [NonParallelizable]
  public void ShortDatePatternIsReportedOnlyWhenBackupsCollide(string datePattern,
    bool staticLogFileName, int maxSizeRollBackups, int countDirection, bool reported)
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "log.txt");
    RollingFileAppender appender = CreateShortDatePatternAppender(file, datePattern,
      maxSizeRollBackups, countDirection, staticLogFileName);
    List<string> messages = ActivateCapturingInternalMessages(appender);
    appender.Close();

    Assert.That(messages,
      Has.Exactly(reported ? 1 : 0).Contains("names dated files like numbered backups"));
  }

  /// <summary>
  /// Date rolling that appends makes no numbered backups, so there is nothing to collide with.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void DateRollingWithAppendIsNotReported()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "log.txt");
    RollingFileAppender appender = CreateShortDatePatternAppender(file, ".d", 10, -1,
      rollingStyle: RollingFileAppender.RollingMode.Date);
    List<string> messages = ActivateCapturingInternalMessages(appender);
    appender.Close();

    Assert.That(messages, Is.Empty);
  }

  /// <summary>
  /// Date rolling without appending rolls the existing file to .1 at startup, which with .d is the
  /// name of the file of the 1st, so the appender leaves it alone and opens the dated file.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ShortDatePatternRestartWithoutAppendKeepsExistingFile()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "log.txt");
    File.WriteAllText(file, "previous run");
    // the same period as the mocked clock, so the start-up does not roll by date
    File.SetLastWriteTime(file, _mockedNow);
    RollingFileAppender appender = CreateShortDatePatternAppender(file, ".d", 3, -1,
      rollingStyle: RollingFileAppender.RollingMode.Date, appendToFile: false);
    List<string> messages = ActivateCapturingInternalMessages(appender);
    appender.Close();

    Assert.That(messages, Has.Exactly(1).Contains("DatePattern [.d]"));
    Assert.That(Directory.GetFiles(folder.Path), Is.EquivalentTo(new[] { file, file + ".4" }));
    Assert.That(File.ReadAllText(file), Is.EqualTo("previous run"));
  }

  /// <summary>
  /// With a dated name and .d, the files of the 11th start with the name of the file of the 1st
  /// and must not count as its backups.
  /// </summary>
  [Test]
  public void DatedNameCountsOnlyItsOwnBackups()
  {
    RollingFileAppender appender = CreateShortDatePatternAppender("log.txt", ".d", 5, 1,
      staticLogFileName: false);
    appender.DateTimeStrategy = new Integration.MockDateTime(new(2026, 10, 1));
    appender.InitializeRollBackups("log.txt",
      ["log.txt.1", "log.txt.1.2", "log.txt.11", "log.txt.11.7"]);

    Assert.That(appender.CurrentSizeRollBackups, Is.EqualTo(2));
  }

  private static readonly DateTime _mockedNow = new(2026, 10, 4, 9, 0, 0);

  private static RollingFileAppender CreateShortDatePatternAppender(string file,
    string datePattern, int maxSizeRollBackups, int countDirection, bool staticLogFileName = true,
    RollingFileAppender.RollingMode rollingStyle = RollingFileAppender.RollingMode.Composite,
    bool appendToFile = true) => new()
  {
    File = file,
    Layout = new PatternLayout("%message%newline"),
    RollingStyle = rollingStyle,
    DatePattern = datePattern,
    StaticLogFileName = staticLogFileName,
    MaxSizeRollBackups = maxSizeRollBackups,
    CountDirection = countDirection,
    AppendToFile = appendToFile,
    DateTimeStrategy = new Integration.MockDateTime(_mockedNow)
  };

  private static List<string> ActivateCapturingInternalMessages(RollingFileAppender appender)
  {
    List<LogLog> messages = [];
    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      using LogLog.LogReceivedAdapter _ = new(messages);
      appender.ActivateOptions();
    });
    return messages.ConvertAll(m => m.Message);
  }

  private static LoggingEvent CreateEvent(string message) => new(new LoggingEventData
  {
    Level = Level.Info,
    Message = message,
    LoggerName = "ShortDatePattern"
  });
}
