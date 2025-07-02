using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using log4net;
using log4net.Config;
using NUnit.Framework;

namespace log4net.Tests.Integration
{
  /// <summary>
  /// Integration tests for log4net logging scenarios, including file and directory creation, log rolling, and configuration behaviors.
  /// <para>
  /// Expectations for test log files and directories:
  /// <list type="bullet">
  /// <item>Test log files may be placed directly in the test directory with names starting with <c>TestLogFilePrefix</c>, or inside directories whose names start with <c>TestLogDirectoryPrefix</c> and may have arbitrary file names.</item>
  /// <item>Each test run starts with a clean environment: any files or directories from previous runs matching these prefixes are deleted in <c>SetUp</c>.</item>
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
        var (log, repo) = ArrangeLogger("log4net.integration.basic.config");
        log.Info("Hello integration test");
        log.Error("This is an error");
        repo.Shutdown();

        // Assert: log file exists and contains expected content
        string[] logLines = File.ReadAllLines("integrationTestLogFile_integration.log");
        Assert.That(logLines.Length, Is.EqualTo(2));
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
            var (log, repo) = ArrangeLogger("log4net.integration.basic.config");
            log.Info("Hello integration test");
            log.Error("This is an error");
            repo.Shutdown();
        }
        // Assert: log file exists and contains expected content
        string[] logLines = File.ReadAllLines("integrationTestLogFile_integration.log");
        Assert.That(logLines.Length, Is.EqualTo(20));
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
            var (log, repo) = ArrangeLogger("log4net.roll.config");
            for (int j = 0; j < 10; ++j)
            {
                log.Info($"Hello, log4net! {i} {j}");
            }
            repo.Shutdown();
        }
        // Assert: log file exists and contains expected content
        string[] logFiles = Directory.GetFiles("integrationTestLogDir_roll");
        Assert.That(logFiles.Length, Is.EqualTo(12 + 1));
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
        string logDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "integrationTestLogDir_maxsizeroll");
        if (Directory.Exists(logDir)) Directory.Delete(logDir, true);
        Directory.CreateDirectory(logDir);
        var (log, repo) = ArrangeLogger("log4net.maxsizeroll.config");
        for (int i = 0; i < 1000; ++i)
        {
            log.Info($"Log entry {i}");
        }
        repo.Shutdown();
        // Assert: rolled files exist
        string[] logFiles = Directory.GetFiles(logDir, "*.log");
        Assert.That(logFiles.Length, Is.EqualTo(4)); // 1 current + 3 backups
        // Optionally, check that each file is <= 10KB
        foreach (var file in logFiles)
            Assert.That(new FileInfo(file).Length, Is.LessThanOrEqualTo(10 * 1024 + 100));
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
        string logDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "integrationTestLogDir_maxsizerolldate");
        if (Directory.Exists(logDir)) Directory.Delete(logDir, true);
        Directory.CreateDirectory(logDir);
        var (log, repo) = ArrangeLogger("log4net.maxsizeroll_date.config");
        // Write enough lines to trigger rolling by size and date
        for (int i = 1; i < 10000; ++i)
        {
            log.Debug($"DateRoll entry {i}");
            if (i % 5000 == 0) System.Threading.Thread.Sleep(TimeSpan.FromMinutes(1)); // allow time for date to change if needed
        }
        repo.Shutdown();
        // Assert: rolled files exist (date+size pattern)
        string[] logFiles = Directory.GetFiles(logDir, "*.log");
        Assert.That(logFiles.Length, Is.EqualTo(8));
        // Group files by date part in the filename (yyyy-MM-dd-mm)
        var dateGroups = logFiles
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Select(name => name.Split('.').First())
            .GroupBy(date => date)
            .ToDictionary(g => g.Key, g => g.Count());
        // Assert that at least one group exists and print group counts
        Assert.That(dateGroups.Count, Is.EqualTo(2));
        foreach (var group in dateGroups)
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
        string logDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "integrationTestLogDir_no_file_name");
        if (Directory.Exists(logDir)) Directory.Delete(logDir, true);
        Directory.CreateDirectory(logDir);
        var (log, repo) = ArrangeLogger("log4net.no_file_name.config");
        log.Info("Test entry with no file name");
        repo.Shutdown();
        // Assert: exactly one log file was created in the directory
        var files = Directory.GetFiles(logDir, "*", SearchOption.AllDirectories);
        Assert.That(files.Length, Is.EqualTo(1), "Should create exactly one log file");
        var fileName = Path.GetFileName(files[0]);
        TestContext.Out.WriteLine($"Created file: {fileName}");
        // Assert the file name matches the date pattern yyyy-MM-dd.log
        string todayPattern = DateTime.Now.ToString("yyyy-MM-dd") + ".log";
        Assert.That(fileName, Is.EqualTo(todayPattern), $"File name should match pattern: {todayPattern}");
    }

    private const string TestLogFilePrefix = "integrationTestLogFile";
    private const string TestLogDirectoryPrefix = "integrationTestLogDir";

    private static void RemoveDirsFromPreviousRuns()
    {
      List<string> directoriesFromPreviousRuns =
              Directory.EnumerateDirectories(TestContext.CurrentContext.TestDirectory)
              .Where(dirPath => Path.GetFileName(dirPath).StartsWith(TestLogDirectoryPrefix, StringComparison.InvariantCultureIgnoreCase))
              .ToList();
      directoriesFromPreviousRuns.ForEach(d => Directory.Delete(d, true));
    }

    private static void RemoveFilesFromPreviousRuns()
    {
      List<string> filesFromPreviousRuns =
              Directory.EnumerateFiles(TestContext.CurrentContext.TestDirectory)
              .Where(filePath => Path.GetFileName(filePath).StartsWith(TestLogFilePrefix, StringComparison.InvariantCultureIgnoreCase))
              .ToList();
      filesFromPreviousRuns.ForEach(File.Delete);
    }

    private static string GetConfigPath(string configFile)
        => Path.Combine(TestContext.CurrentContext.TestDirectory, "Integration", configFile);

    private static (ILog log, log4net.Repository.ILoggerRepository repo) ArrangeLogger(string configFile)
    {
        string configPath = GetConfigPath(configFile);
        var repo = LogManager.CreateRepository(Guid.NewGuid().ToString());
        XmlConfigurator.Configure(repo, new FileInfo(configPath));
        var log = LogManager.GetLogger(repo.Name, "IntegrationTestLogger");
        return (log, repo);
    }
  }
}
