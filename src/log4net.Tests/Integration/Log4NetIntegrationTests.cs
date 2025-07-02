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

    [Test]
    public void Log4Net_WritesLogFile_WithMaxSizeRoll_Config_Works()
    {
        var logDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "integrationTestLogDir_maxsizeroll");
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

    [Test]
    public void Log4Net_WritesLogFile_WithDateAndSizeRoll_Config_Works()
    {
        var logDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "integrationTestLogDir_maxsizerolldate");
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
            TestContext.WriteLine($"Date group: {group.Key}, file count: {group.Value}");
        }
    }

    [Test]
    public void Log4Net_ConfigWithoutFileName_CreatesOneFile()
    {
        var logDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "integrationTestLogDir_no_file_name");
        if (Directory.Exists(logDir)) Directory.Delete(logDir, true);
        Directory.CreateDirectory(logDir);
        var (log, repo) = ArrangeLogger("log4net.no_file_name.config");
        log.Info("Test entry with no file name");
        repo.Shutdown();
        // Assert: exactly one log file was created in the directory
        var files = Directory.GetFiles(logDir, "*", SearchOption.AllDirectories);
        Assert.That(files.Length, Is.EqualTo(1), "Should create exactly one log file");
        var fileName = Path.GetFileName(files[0]);
        TestContext.WriteLine($"Created file: {fileName}");
        // Assert the file name matches the date pattern yyyy-MM-dd.log
        var todayPattern = DateTime.Now.ToString("yyyy-MM-dd") + ".log";
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
