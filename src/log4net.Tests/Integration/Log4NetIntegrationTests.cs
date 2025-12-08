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
using System.Linq;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using NUnit.Framework;
using LoggerHierarchy = log4net.Repository.Hierarchy.Hierarchy;

namespace log4net.Tests.Integration
{
  /// <summary>
  /// Integration tests for log4net logging scenarios, including file and directory creation, log rolling, and configuration behaviors.
  /// <para>
  /// Expectations for test log files and directories:
  /// <list type="bullet">
  /// <item>Test log files may be placed directly in the test directory with names starting with <see cref="TestLogFilePrefix"/>,
  /// or inside directories whose names start with <see cref="TestLogDirectoryPrefix"/> and may have arbitrary file names.</item>
  /// <item>Each test run starts with a clean environment: any files or directories from previous runs matching these prefixes are deleted in <see cref="SetUp"/>.</item>
  /// <item>Tests assert the existence, count, and content of log files and directories as part of their validation.</item>
  /// </list>
  /// </para>
  /// </summary>
  [TestFixture]
  public class Log4NetIntegrationTests
  {
    /// <summary>
    /// Runs before each test to remove log files and directories from previous test runs.
    /// Ensures a clean environment for integration tests.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
      RemoveFilesFromPreviousRuns();
      RemoveDirsFromPreviousRuns();
    }

    /// <summary>
    /// Tests basic log4net functionality - writing log entries to a file.
    /// This is the most fundamental integration test verifying that:
    /// 1. Log4net can be configured from XML config
    /// 2. Log entries are written to the specified file
    /// 3. Content is preserved exactly as logged
    /// Catch: Validates end-to-end logging pipeline works correctly.
    /// </summary>
    [Test]
    public void Log4Net_WritesLogFile_AndContentIsCorrect()
    {
      (ILog log, ILoggerRepository repo) = ArrangeLogger("log4net.integration.basic.config");
      log.Info("Hello integration test");
      log.Error("This is an error");
      repo.Shutdown();

      // Assert: log file exists and contains expected content
      string[] logLines = File.ReadAllLines("integrationTestLogFile_integration.log");
      Assert.That(logLines, Has.Length.EqualTo(2));
      Assert.That(logLines[0], Does.Contain("Hello integration test"));
      Assert.That(logLines[1], Does.Contain("This is an error"));
    }

    /// <summary>
    /// Tests log4net's append behavior across multiple logger instances with repository shutdown/restart cycles.
    /// Verifies that:
    /// 1. Log entries accumulate correctly when repeatedly creating and shutting down logger repositories
    /// 2. File append mode works properly across repository restarts
    /// 3. No log entries are lost during the restart process
    /// Catch: This tests persistence and append behavior - critical for applications that restart logging frequently.
    /// </summary>
    [Test]
    public void Log4Net_WritesLogFile_AndContentIsCorrectAfterRestart()
    {
      for (int i = 0; i < 10; i++)
      {
        (ILog log, ILoggerRepository repo) = ArrangeLogger("log4net.integration.basic.config");
        log.Info("Hello integration test");
        log.Error("This is an error");
        repo.Shutdown();
      }
      // Assert: log file exists and contains expected content
      string[] logLines = File.ReadAllLines("integrationTestLogFile_integration.log");
      Assert.That(logLines, Has.Length.EqualTo(20));
      for (int i = 0; i < 10; i++)
      {
        Assert.That(logLines[i * 2], Does.Contain("Hello integration test"));
        Assert.That(logLines[i * 2 + 1], Does.Contain("This is an error"));
      }
    }

    /// <summary>
    /// Tests log4net's file rolling behavior with no append mode enabled.
    /// This test verifies that:
    /// 1. Multiple logger instances generate multiple rolled log files
    /// 2. Rolling configuration works correctly across repository restarts
    /// 3. Files are rolled according to the configuration policy
    /// Catch: Tests rolling without append - ensuring each restart creates new files rather than appending to existing ones.
    /// The expected file count (13) includes the current log file plus rolled files from previous iterations.
    /// </summary>
    [Test]
    public void Log4Net_WritesLogFile_WithRollAndNoAppend_AndContentIsCorrectAfterRestart()
    {
      for (int i = 0; i < 20; i++)
      {
        (ILog log, ILoggerRepository repo) = ArrangeLogger("log4net.roll.config");
        for (int j = 0; j < 10; ++j)
        {
          log.Info($"Hello, log4net! {i} {j}");
        }
        repo.Shutdown();
      }
      // Assert: log file exists and contains expected content
      string[] logFiles = Directory.GetFiles("integrationTestLogDir_roll");
      Assert.That(logFiles, Has.Length.EqualTo(12 + 1));
    }

    /// <summary>
    /// Tests log4net's file rolling behavior based on maximum file size.
    /// This test verifies that:
    /// 1. Log files are rolled when they exceed the configured maximum size
    /// 2. The correct number of backup files are maintained
    /// 3. Each file remains within the size limit (10KB + small buffer for overhead)
    /// Catch: Tests size-based rolling policy - critical for preventing log files from growing indefinitely.
    /// Expected: 1 current file + 3 backup files = 4 total files.
    /// </summary>
    [Test]
    public void Log4Net_WritesLogFile_WithMaxSizeRoll_Config_Works()
    {
      DirectoryInfo logDir = CreateLogDirectory("integrationTestLogDir_maxsizeroll");
      (ILog log, ILoggerRepository repo) = ArrangeLogger("log4net.maxsizeroll.config");
      for (int i = 0; i < 1000; ++i)
      {
        log.Info($"Log entry {i}");
      }
      repo.Shutdown();
      // Assert: rolled files exist
      FileInfo[] logFiles = logDir.GetFiles("*.log");
      Assert.That(logFiles, Has.Length.EqualTo(4)); // 1 current + 3 backups
      // Check that each file is <= 10KB
      foreach (FileInfo file in logFiles)
        Assert.That(file.Length, Is.LessThanOrEqualTo(10 * 1024 + 100));
    }

    /// <summary>
    /// Tests log4net's composite rolling behavior based on both date and file size.
    /// This test verifies that:
    /// 1. Log files are rolled when they exceed the configured maximum size
    /// 2. Log files are also rolled based on date/time patterns
    /// 3. The combination of date and size rolling creates multiple files grouped by date
    /// 4. Each date group maintains its own rolling sequence
    /// Catch: Tests composite rolling policy - ensures both date and size triggers work together correctly.
    /// Expected: Multiple files grouped by date, with size-based rolling within each date group.
    /// </summary>
    [Test]
    public void Log4Net_WritesLogFile_WithDateAndSizeRoll_Config_Works()
    {
      DirectoryInfo logDir = CreateLogDirectory("integrationTestLogDir_maxsizerolldate");
      DateTime startDate = new(2025, 12, 08, 15, 55, 50);
      MockDateTime mockDateTime = new(startDate); // start at the end of a minute
      (ILog log, ILoggerRepository repo) = ArrangeCompositeLogger(mockDateTime);
      // distribute 10.000 log entries over 60 seconds
      TimeSpan stepIncrement = new(TimeSpan.FromSeconds(60).Ticks / 10000);
      // 1000 entries (each 100 bytes) -> ~100KB total - 10 rolls expected - 4 will survive
      for (int i = 1; i < 1000; ++i)
      {
        log.Debug($"DateRoll entry {i:D5}");
        mockDateTime.Now += stepIncrement;
      }
      // switch to next minute to force date roll
      mockDateTime.Now = startDate.AddSeconds(10);
      // 1000 entries (each 100 bytes) -> ~100KB total - 10 rolls expected - 4 will survive
      for (int i = 1; i < 1000; ++i)
      {
        log.Debug($"DateRoll entry {i:D5}");
        mockDateTime.Now += stepIncrement;
      }
      repo.Shutdown();
      // Assert: rolled files exist (date+size pattern)
      FileInfo[] logFiles = logDir.GetFiles("*.log");
      Assert.That(logFiles, Has.Length.EqualTo(8));
      // Group files by date part in the filename (yyyy-MM-dd-mm)
      Dictionary<string, int> dateGroups = logFiles
        .Select(f => Path.GetFileNameWithoutExtension(f.Name))
        .Select(name => name.Split('.').First())
        .GroupBy(date => date)
        .ToDictionary(g => g.Key, g => g.Count());
      // Assert that at least one group exists and print group counts
      Assert.That(dateGroups, Has.Count.EqualTo(2));
      foreach (KeyValuePair<string, int> group in dateGroups)
      {
        TestContext.Out.WriteLine($"Date group: {group.Key}, file count: {group.Value}");
      }
    }

    /// <summary>
    /// Tests log4net's automatic file naming behavior when no explicit filename is configured.
    /// This test verifies that:
    /// 1. Log4net can automatically generate file names based on date patterns
    /// 2. Only one file is created when no explicit file name is provided
    /// 3. The generated file name follows the expected date format (yyyy-MM-dd.log)
    /// Catch: Tests the framework's ability to handle missing file name configuration gracefully.
    /// Expected: A single log file with name matching today's date pattern.
    /// </summary>
    [Test]
    public void Log4Net_ConfigWithoutFileName_CreatesOneFile()
    {
      DirectoryInfo logDir = CreateLogDirectory("integrationTestLogDir_no_file_name");
      (ILog log, ILoggerRepository repo) = ArrangeLogger("log4net.no_file_name.config");
      log.Info("Test entry with no file name");
      repo.Shutdown();
      // Assert: exactly one log file was created in the directory
      FileInfo[] files = logDir.GetFiles("*", SearchOption.AllDirectories);
      Assert.That(files, Has.Length.EqualTo(1), "Should create exactly one log file");
      TestContext.Out.WriteLine($"Created file: {files[0].Name}");
      // Assert the file name matches the date pattern yyyy-MM-dd.log
      string todayPattern = DateTime.Now.ToString("yyyy-MM-dd") + ".log";
      Assert.That(files[0].Name, Is.EqualTo(todayPattern), $"File name should match pattern: {todayPattern}");
    }

    private static DirectoryInfo CreateLogDirectory(string name)
    {
      DirectoryInfo logDir = new(Path.Combine(TestContext.CurrentContext.TestDirectory, name));
      if (logDir.Exists)
      {
        logDir.Delete(true);
      }
      logDir.Create();
      return logDir;
    }

    private const string TestLogFilePrefix = "integrationTestLogFile";
    private const string TestLogDirectoryPrefix = "integrationTestLogDir";

    private static void RemoveDirsFromPreviousRuns()
    {
      List<DirectoryInfo> directoriesFromPreviousRuns = new DirectoryInfo(TestContext.CurrentContext.TestDirectory)
        .EnumerateDirectories()
        .Where(directory => directory.Name.StartsWith(TestLogDirectoryPrefix, StringComparison.InvariantCultureIgnoreCase))
        .ToList();
      directoriesFromPreviousRuns.ForEach(d => d.Delete(true));
    }

    private static void RemoveFilesFromPreviousRuns()
    {
      List<FileInfo> filesFromPreviousRuns = new DirectoryInfo(TestContext.CurrentContext.TestDirectory)
        .EnumerateFiles()
        .Where(filePath => filePath.Name.StartsWith(TestLogFilePrefix, StringComparison.InvariantCultureIgnoreCase))
        .ToList();
      filesFromPreviousRuns.ForEach(file => file.Delete());
    }

    private static string GetConfigPath(string configFile)
        => Path.Combine(TestContext.CurrentContext.TestDirectory, "Integration", configFile);

    private static (ILog log, ILoggerRepository repo) ArrangeLogger(string configFile)
    {
      string configPath = GetConfigPath(configFile);
      ILoggerRepository repo = LogManager.CreateRepository(Guid.NewGuid().ToString());
      XmlConfigurator.Configure(repo, new FileInfo(configPath));
      ILog log = LogManager.GetLogger(repo.Name, "IntegrationTestLogger");
      return (log, repo);
    }

    private static (ILog log, ILoggerRepository repo) ArrangeCompositeLogger(RollingFileAppender.IDateTime dateTime)
    {
      LoggerHierarchy repo = (LoggerHierarchy)LogManager.CreateRepository(Guid.NewGuid().ToString());
      PatternLayout layout = new() { ConversionPattern = "%d{yyyy/MM/dd HH:mm:ss.fff} %m-%M%n" };
      layout.ActivateOptions();
      RollingFileAppender rollingAppender = new()
      {
        Name = "LogFileAppender",
        File = "integrationTestLogDir_maxsizerolldate/.log",
        AppendToFile = true,
        RollingStyle = RollingFileAppender.RollingMode.Composite,
        DatePattern = "HH-mm",
        DateTimeStrategy = dateTime,
        MaximumFileSize = "10KB",
        MaxSizeRollBackups = 3,
        StaticLogFileName = false,
        CountDirection = 1,
        PreserveLogFileNameExtension = true,
        LockingModel = new FileAppender.NoLock(),
        Layout = layout
      };
      rollingAppender.ActivateOptions();
      repo.Configured = true;
      Logger logger = (Logger)repo.GetLogger("IntegrationTestLogger");
      logger.Level = Level.Debug;
      logger.AddAppender(rollingAppender);
      logger.Additivity = false;
      return (log: new LogImpl(logger), repo);
    }

  }
}