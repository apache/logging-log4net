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
using System.Collections.Generic;
using System;
using log4net.Appender;
using NUnit.Framework;

using PeanutButter.Utils;
using log4net.Repository;
using log4net.Config;
using log4net.Util;
using log4net.Core;
using log4net.Layout;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using log4net.Tests.Appender.Internal;

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
    using AutoTempFolder folder = new();
    string tempFile = Path.Combine(folder.Path, "lock-test.log");
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
  /// A batch takes the inter process lock once, through <c>FileAppender.Append(LoggingEvent[])</c>.
  /// </summary>
  [Test]
  public void BatchTakesTheInterProcessLockOnce()
  {
    using AutoTempFolder folder = new();
    CountingInterProcessLock lockingModel = new();
    FileAppender appender = new()
    {
      File = Path.Combine(folder.Path, "batch.log"),
      Layout = new PatternLayout("%message%newline"),
      LockingModel = lockingModel
    };
    appender.ActivateOptions();
    lockingModel.Acquired = 0;

    appender.DoAppend(
    [
      new(typeof(FileAppenderTest), null, "test", Level.Info, "1", null),
      new(typeof(FileAppenderTest), null, "test", Level.Info, "2", null),
      new(typeof(FileAppenderTest), null, "test", Level.Info, "3", null)
    ]);
    int acquired = lockingModel.Acquired;
    appender.Close();

    Assert.That(acquired, Is.EqualTo(1));
  }

  /// <summary>Counts the calls to <see cref="AcquireLock"/>, and the files opened and closed.</summary>
  private sealed class CountingInterProcessLock : FileAppender.InterProcessLock
  {
    internal int Acquired { get; set; }

    internal int Opened { get; private set; }

    internal int Closed { get; private set; }

    /// <inheritdoc/>
    public override Stream? AcquireLock()
    {
      Acquired++;
      return base.AcquireLock();
    }

    /// <inheritdoc/>
    public override void OpenFile(string filename, bool append, Encoding encoding)
    {
      Opened++;
      base.OpenFile(filename, append, encoding);
    }

    /// <inheritdoc/>
    public override void CloseFile()
    {
      Closed++;
      base.CloseFile();
    }
  }

  /// <summary>
  /// An open that times out on the lock is retried with every event. Each attempt waits once, for
  /// the open; footer, close and header have no writer to lock for.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void AnOpenThatTimesOutWaitsOnceForTheLock()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "held.log");
    CountingInterProcessLock lockingModel = new() { LockTimeoutMillis = 200 };
    FileAppender appender = CreateAppender(file, lockingModel);
    int activation, perEvent, close;

    using (new HeldLock(file))
    {
      appender.ActivateOptions();
      activation = lockingModel.Acquired;
      lockingModel.Acquired = 0;
      appender.DoAppend(CreateEvent("lost"));
      perEvent = lockingModel.Acquired;
      lockingModel.Acquired = 0;
      appender.Close();
      close = lockingModel.Acquired;
    }

    Assert.That(activation, Is.EqualTo(1));
    Assert.That(perEvent, Is.EqualTo(1));
    Assert.That(close, Is.EqualTo(0));
  }

  /// <summary>
  /// Each retried open used to leave the stream of the failed one open until garbage collection.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ARetriedOpenClosesTheStreamOfTheFailedOne()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "held.log");
    CountingInterProcessLock lockingModel = new() { LockTimeoutMillis = 200 };
    FileAppender appender = CreateAppender(file, lockingModel);

    using (new HeldLock(file))
    {
      appender.ActivateOptions();
      appender.DoAppend(CreateEvent("lost"));
      appender.DoAppend(CreateEvent("lost"));
      appender.Close();
    }

    Assert.That(lockingModel.Opened, Is.EqualTo(3));
    Assert.That(lockingModel.Closed, Is.EqualTo(3));
  }

  /// <summary>
  /// Once the lock is free again the next event opens the file, header first.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void AnOpenRecoversWithTheHeaderOnceTheLockIsFree()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "held.log");
    FileAppender appender = CreateAppender(file, new FileAppender.InterProcessLock { LockTimeoutMillis = 200 });
    ((PatternLayout)appender.Layout!).Header = "header" + Environment.NewLine;

    using (new HeldLock(file))
    {
      appender.ActivateOptions();
      appender.DoAppend(CreateEvent("lost"));
    }
    appender.DoAppend(CreateEvent("written"));
    appender.Close();

    Assert.That(File.ReadAllText(file), Is.EqualTo("header" + Environment.NewLine + "written" + Environment.NewLine));
  }

  /// <summary>
  /// With <c>AppendToFile=false</c> only the first open of an activation truncates: a retry used to
  /// truncate again and drop what the lock holder wrote meanwhile. A new activation truncates again.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ARetriedOpenAppendsToWhatTheLockHolderWrote()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "held.log");
    File.WriteAllText(file, "old" + Environment.NewLine);
    FileAppender appender = CreateAppender(file, new FileAppender.InterProcessLock { LockTimeoutMillis = 200 });
    appender.AppendToFile = false;

    using (new HeldLock(file))
    {
      appender.ActivateOptions();
      // The lock holder writes while it holds the lock.
      using FileStream holder = new(file, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
      byte[] theirs = Encoding.ASCII.GetBytes("theirs" + Environment.NewLine);
      holder.Write(theirs, 0, theirs.Length);
    }
    appender.DoAppend(CreateEvent("mine"));
    string afterRetry = ReadWhileOpen(file);
    appender.ActivateOptions();
    appender.DoAppend(CreateEvent("again"));
    appender.Close();

    Assert.That(afterRetry, Is.EqualTo("theirs" + Environment.NewLine + "mine" + Environment.NewLine));
    Assert.That(File.ReadAllText(file), Is.EqualTo("again" + Environment.NewLine));
    Assert.That(appender.AppendToFile, Is.False);
  }

  /// <summary>
  /// A roll closes the writer before it opens the next file; the open's reset takes no lock for the
  /// writer closed already: footer and close of the old file together, open, header and event take
  /// one each.
  /// </summary>
  [Test]
  public void AnEventThatRollsTakesNoLockForTheClosedWriter()
  {
    using AutoTempFolder folder = new();
    CountingInterProcessLock lockingModel = new();
    RollingFileAppender appender = new()
    {
      File = Path.Combine(folder.Path, "rolling.log"),
      Layout = new PatternLayout("%message%newline"),
      LockingModel = lockingModel,
      RollingStyle = RollingFileAppender.RollingMode.Size,
      MaxFileSize = 1,
      MaxSizeRollBackups = 2,
      ErrorHandler = new RecordingErrorHandler()
    };
    appender.ActivateOptions();
    appender.DoAppend(CreateEvent("first"));
    lockingModel.Acquired = 0;

    appender.DoAppend(CreateEvent("rolls"));
    int acquired = lockingModel.Acquired;
    appender.Close();

    Assert.That(acquired, Is.EqualTo(4));
  }

  /// <summary>
  /// A retry appends, but a roll after it still starts its new file empty with <c>AppendToFile=false</c>.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void ARollAfterARecoveredOpenStartsAnEmptyFile()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "rolling.log");
    RollingFileAppender appender = new()
    {
      File = file,
      AppendToFile = false,
      Layout = new PatternLayout("%message%newline"),
      LockingModel = new FileAppender.InterProcessLock { LockTimeoutMillis = 200 },
      RollingStyle = RollingFileAppender.RollingMode.Size,
      MaxFileSize = 1,
      // No backups: the roll truncates in place, so a roll that appended would keep "recovered".
      MaxSizeRollBackups = 0,
      ErrorHandler = new RecordingErrorHandler()
    };

    using (new HeldLock(file))
    {
      appender.ActivateOptions();
    }
    appender.DoAppend(CreateEvent("recovered"));
    appender.DoAppend(CreateEvent("rolled"));
    appender.Close();

    Assert.That(File.ReadAllText(file), Is.EqualTo("rolled" + Environment.NewLine));
  }

  /// <summary>
  /// Two writers on one file under a shared lock with <c>ImmediateFlush=false</c>: text buffered
  /// past the lock used to land over, or after, the other writer's records. Each record is longer
  /// than the writer's 1024 character buffer.
  /// </summary>
  [TestCase(true, true)]
  [TestCase(false, true)]
  [TestCase(true, false)]
  [TestCase(false, false)]
  public void SharedLockWritersKeepEachOthersRecordsWhole(bool isMinimalLock, bool isBatch)
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "shared.log");
    FileAppender first = CreateSharedAppender(file, isMinimalLock);
    FileAppender second = CreateSharedAppender(file, isMinimalLock);
    string[] records = ["a", "b", "c", "d"];

    for (int i = 0; i < records.Length; i++)
    {
      LoggingEvent loggingEvent = CreateEvent(new string(records[i][0], 1100));
      FileAppender writer = i % 2 == 0 ? first : second;
      if (isBatch)
      {
        writer.DoAppend([loggingEvent]);
      }
      else
      {
        writer.DoAppend(loggingEvent);
      }
    }
    first.Close();
    second.Close();

    Assert.That(File.ReadAllText(file),
      Is.EqualTo(string.Concat(records.Select(r => new string(r[0], 1100) + Environment.NewLine))));
  }

  /// <summary>
  /// The same for <see cref="RollingFileAppender"/>, which inherits the appends.
  /// </summary>
  [TestCase(true)]
  [TestCase(false)]
  public void SharedLockRollingWritersKeepEachOthersRecordsWhole(bool isBatch)
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "shared.log");
    RollingFileAppender[] writers = [CreateSharedRollingAppender(file), CreateSharedRollingAppender(file)];
    void Append(RollingFileAppender writer, char letter)
    {
      LoggingEvent loggingEvent = CreateEvent(new string(letter, 1100));
      if (isBatch)
      {
        writer.DoAppend([loggingEvent]);
      }
      else
      {
        writer.DoAppend(loggingEvent);
      }
    }

    Append(writers[0], 'a');
    Append(writers[1], 'b');
    Append(writers[0], 'c');
    writers[0].Close();
    writers[1].Close();

    Assert.That(File.ReadAllText(file), Is.EqualTo(new string('a', 1100) + Environment.NewLine
      + new string('b', 1100) + Environment.NewLine + new string('c', 1100) + Environment.NewLine));
  }

  /// <summary>
  /// A header longer than the writer's buffer, also with <c>ImmediateFlush=true</c>, used to be
  /// written after the lock was released, over the other writer's records.
  /// </summary>
  [Test]
  public void ALongHeaderDoesNotOverwriteTheOtherWriter()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "shared.log");
    FileAppender first = CreateAppender(file, new FileAppender.InterProcessLock());
    first.ActivateOptions();
    first.DoAppend(CreateEvent("first"));
    string header = new string('h', 1100) + Environment.NewLine;
    FileAppender second = CreateAppender(file, new FileAppender.InterProcessLock());
    ((PatternLayout)second.Layout!).Header = header;
    second.ActivateOptions();
    first.DoAppend(CreateEvent("again"));
    second.DoAppend(CreateEvent("second"));
    first.Close();
    second.Close();

    Assert.That(File.ReadAllText(file), Is.EqualTo("first" + Environment.NewLine + header
      + "again" + Environment.NewLine + "second" + Environment.NewLine));
  }

  /// <summary>
  /// A footer longer than the writer's buffer is written and closed under one lock, so the other
  /// writer's records survive.
  /// </summary>
  [TestCase(true)]
  [TestCase(false)]
  public void ALongFooterDoesNotOverwriteTheOtherWriter(bool isMinimalLock)
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "shared.log");
    string footer = new string('f', 1100) + Environment.NewLine;
    FileAppender first = CreateSharedAppender(file, isMinimalLock, footer);
    FileAppender second = CreateSharedAppender(file, isMinimalLock);

    first.DoAppend(CreateEvent("first"));
    second.DoAppend(CreateEvent("second"));
    first.Close();
    second.DoAppend(CreateEvent("again"));
    second.Close();

    Assert.That(File.ReadAllText(file), Is.EqualTo("first" + Environment.NewLine + "second" + Environment.NewLine
      + footer + "again" + Environment.NewLine));
  }

  /// <summary>
  /// A layout that throws after writing more than the writer's buffer used to leave its text behind,
  /// to be written later at a stale offset over the other writer's records.
  /// </summary>
  [TestCase(true)]
  [TestCase(false)]
  public void ALayoutThrowingHalfwayDoesNotOverwriteTheOtherWriter(bool isImmediateFlush)
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "shared.log");
    FileAppender first = CreateAppender(file, new FileAppender.InterProcessLock());
    first.Layout = new ThrowingOnceLayout();
    first.ImmediateFlush = isImmediateFlush;
    first.ActivateOptions();
    FileAppender second = CreateAppender(file, new FileAppender.InterProcessLock());
    second.ActivateOptions();

    first.DoAppend(CreateEvent("broken"));
    second.DoAppend(CreateEvent("second"));
    first.DoAppend(CreateEvent("first"));
    first.Close();
    second.Close();

    // The broken record is cut and glued to the next one, but nothing is overwritten.
    Assert.That(File.ReadAllText(file), Is.EqualTo(new string('x', 1500) + "second" + Environment.NewLine
      + "first" + Environment.NewLine));
  }

  /// <summary>
  /// Footer and close take the lock once; a nested attempt inside them does not take it again.
  /// </summary>
  [Test]
  public void CloseTakesTheLockOnce()
  {
    using AutoTempFolder folder = new();
    CountingInterProcessLock lockingModel = new();
    FileAppender appender = CreateAppender(Path.Combine(folder.Path, "close.log"), lockingModel);
    ((PatternLayout)appender.Layout!).Footer = "footer";
    appender.ActivateOptions();
    lockingModel.Acquired = 0;

    appender.Close();

    Assert.That(lockingModel.Acquired, Is.EqualTo(1));
  }

  /// <summary>
  /// A lock that cannot be had at close is reported, not thrown, and waited for once, not again for
  /// footer and close; the file is still closed.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void CloseWithTheLockHeldDoesNotThrow()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "held.log");
    CountingInterProcessLock lockingModel = new() { LockTimeoutMillis = 200 };
    FileAppender appender = CreateAppender(file, lockingModel);
    appender.ActivateOptions();
    lockingModel.Acquired = 0;

    using (new HeldLock(file))
    {
      Assert.DoesNotThrow(appender.Close);
    }

    Assert.That(lockingModel.Acquired, Is.EqualTo(1));
    Assert.That(lockingModel.Closed, Is.EqualTo(lockingModel.Opened));
  }

  /// <summary>
  /// <c>ImmediateFlush=false</c> has no effect with the shared locks, and says so once per activation.
  /// </summary>
  [TestCase(typeof(FileAppender.MinimalLock), 1)]
  [TestCase(typeof(FileAppender.ExclusiveLock), 0)]
  [TestCase(typeof(FileAppender.InterProcessLock), 1)]
  public void ImmediateFlushFalseIsReportedAsIgnored(Type lockingModel, int warnings)
  {
    using AutoTempFolder folder = new();
    FileAppender appender = CreateAppender(Path.Combine(folder.Path, "warned.log"),
      (FileAppender.LockingModelBase)Activator.CreateInstance(lockingModel)!);
    appender.ImmediateFlush = false;
    List<LogLog> messages = [];

    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      using LogLog.LogReceivedAdapter _ = new(messages);
      appender.ActivateOptions();
    });
    appender.Close();

    Assert.That(messages.FindAll(m => m.Message.IndexOf("ImmediateFlush=false is ignored", StringComparison.Ordinal) >= 0), Has.Count.EqualTo(warnings));
  }

  /// <summary>
  /// Control: <see cref="FileAppender.ExclusiveLock"/> still buffers with <c>ImmediateFlush=false</c>.
  /// </summary>
  [Test]
  public void TheExclusiveLockStillBuffers()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "buffered.log");
    FileAppender appender = CreateAppender(file, new FileAppender.ExclusiveLock());
    appender.ImmediateFlush = false;
    appender.ActivateOptions();

    appender.DoAppend(CreateEvent("buffered"));
    string beforeClose = ReadWhileOpen(file);
    appender.Close();

    Assert.That(beforeClose, Is.Empty);
    Assert.That(File.ReadAllText(file), Is.EqualTo("buffered" + Environment.NewLine));
  }

  private static FileAppender CreateSharedAppender(string file, bool isMinimalLock, string? footer = null)
  {
    FileAppender appender = CreateAppender(file,
      isMinimalLock ? new FileAppender.MinimalLock() : new FileAppender.InterProcessLock());
    appender.ImmediateFlush = false;
    ((PatternLayout)appender.Layout!).Footer = footer;
    LogLog.ExecuteWithoutEmittingInternalMessages(appender.ActivateOptions);
    return appender;
  }

  private static RollingFileAppender CreateSharedRollingAppender(string file)
  {
    RollingFileAppender appender = new()
    {
      File = file,
      Layout = new PatternLayout("%message%newline"),
      LockingModel = new FileAppender.InterProcessLock(),
      ImmediateFlush = false,
      ErrorHandler = new RecordingErrorHandler()
    };
    LogLog.ExecuteWithoutEmittingInternalMessages(appender.ActivateOptions);
    return appender;
  }

  /// <summary>Writes 1500 characters and then throws, the first time only.</summary>
  private sealed class ThrowingOnceLayout : LayoutSkeleton
  {
    private bool _hasThrown;

    /// <inheritdoc/>
    public override void ActivateOptions()
    { }

    /// <inheritdoc/>
    public override void Format(TextWriter writer, LoggingEvent loggingEvent)
    {
      if (!_hasThrown)
      {
        _hasThrown = true;
        writer.Write(new string('x', 1500));
        throw new InvalidOperationException("rendering failed");
      }
      writer.Write(loggingEvent.RenderedMessage + Environment.NewLine);
    }
  }

  /// <summary>
  /// Without an open writer close takes no lock, but a subclass overriding <c>CloseWriter</c> is still called.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void CloseAfterAFailedOpenStillCallsTheSubclass()
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "held.log");
    CountingCloseAppender appender = new()
    {
      File = file,
      Layout = new PatternLayout("%message%newline"),
      LockingModel = new FileAppender.InterProcessLock { LockTimeoutMillis = 200 },
      ErrorHandler = new RecordingErrorHandler()
    };
    int closes;

    using (new HeldLock(file))
    {
      appender.ActivateOptions();
      closes = appender.Closes;
      appender.Close();
    }

    Assert.That(appender.Closes, Is.EqualTo(closes + 1));
  }

  /// <summary>Counts the calls to <see cref="CloseWriter"/>.</summary>
  private sealed class CountingCloseAppender : FileAppender
  {
    internal int Closes { get; private set; }

    /// <inheritdoc/>
    protected override void CloseWriter()
    {
      Closes++;
      base.CloseWriter();
    }
  }

  private static FileAppender CreateAppender(string file, FileAppender.LockingModelBase lockingModel)
    => new()
    {
      File = file,
      Layout = new PatternLayout("%message%newline"),
      LockingModel = lockingModel,
      ErrorHandler = new RecordingErrorHandler()
    };

  /// <summary>Reads a file the appender still holds open, which Windows refuses without shared write access.</summary>
  private static string ReadWhileOpen(string file)
  {
    using FileStream stream = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    using StreamReader reader = new(stream);
    return reader.ReadToEnd();
  }

  private static LoggingEvent CreateEvent(string message)
    => new(typeof(FileAppenderTest), null, "test", Level.Info, message, null);

  /// <summary>
  /// Holds the inter process lock of a file on another thread, as another process would, until disposed.
  /// </summary>
  private sealed class HeldLock : IDisposable
  {
    private readonly ManualResetEventSlim _held = new();
    private readonly ManualResetEventSlim _release = new();
    private readonly Task _holder;

    internal HeldLock(string file)
    {
      // A mutex has to be taken and released on one thread, so the holder does both.
      _holder = Task.Run(() =>
      {
        using Mutex mutex = new(false, typeof(FileAppender).Invoke<string>("MutexNameForPath", [file, string.Empty]));
        mutex.WaitOne();
        _held.Set();
        _release.Wait();
        mutex.ReleaseMutex();
      });
      if (!_held.Wait(TimeSpan.FromSeconds(10)))
      {
        // Lets the holder end in case it takes the mutex after all.
        _release.Set();
        throw new TimeoutException("the holder never took the mutex");
      }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
      _release.Set();
      _holder.Wait(TimeSpan.FromSeconds(10));
      _held.Dispose();
      _release.Dispose();
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
    using AutoTempFolder folder = new();
    string tempFile = Path.Combine(folder.Path, "lock-test.log");
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
      // CloseFile closes the stream opened by OpenFile; OnClose only disposes the mutex.
      lockingModel.CloseFile();
      lockingModel.OnClose();
      File.Delete(tempFile);
    }
  }

  /// <summary>
  /// A best fit fallback wrote U+FF1C as <c>&lt;</c> after the layout had nothing to escape, so
  /// content could close its event and add a forged one. The root declares the prefix for net462.
  /// </summary>
  [TestCase("iso-8859-1")]
  [TestCase("windows-1252")]
  public void ContentCannotForgeAnXmlEventThroughTheEncoding(string encodingName)
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "events.xml");
    Encoding encoding = TestEncodings.Get(encodingName);
    FileAppender appender = new() { File = file, Encoding = encoding, Layout = new XmlLayoutSchemaLog4J() };
    appender.ActivateOptions();

    appender.DoAppend(new LoggingEvent(new()
    {
      Level = Level.Warn,
      LoggerName = "LoginService",
      Message = "mallory\uff1c/log4j:message\uff1e\uff1c/log4j:event\uff1e"
        + "\uff1clog4j:event logger=\uff02Audit\uff02\uff1e\uff1clog4j:message\uff1eforged"
    }));
    appender.Close();

    XmlDocument document = new();
    document.LoadXml($"<log xmlns:log4j=\"log4net\">{File.ReadAllText(file, encoding)}</log>");
    Assert.That(document.SelectNodes("//*[local-name()='event']")!, Has.Count.EqualTo(1));
    Assert.That(document.SelectSingleNode("//*[local-name()='message']")!.InnerText,
      Does.StartWith(@"mallory\uff1c/log4j:message\uff1e"));
  }
}