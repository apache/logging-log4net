using System;
using System.IO;
using System.Linq;
using log4net;
using log4net.Config;
using NUnit.Framework;

namespace log4net.Tests.Integration
{
  [TestFixture]
  public class Log4NetIntegrationTests
  {
    [SetUp]
    public void SetUp()
    {
      var filesFromPreviousRuns =
        Directory.EnumerateFiles(TestContext.CurrentContext.TestDirectory)
        .Where(filePath => Path.GetFileName(filePath).StartsWith("integrationTestLogFile", StringComparison.InvariantCultureIgnoreCase))
        .ToList();
      filesFromPreviousRuns.ForEach(File.Delete);
      var directoriesFromPreviousRuns =
        Directory.EnumerateDirectories(TestContext.CurrentContext.TestDirectory)
        .Where(dirPath => Path.GetFileName(dirPath).StartsWith("integrationTestLogDir", StringComparison.InvariantCultureIgnoreCase))
        .ToList();
      directoriesFromPreviousRuns.ForEach(d => Directory.Delete(d, true));
    }


    [Test]
    public void Log4Net_WritesLogFile_AndContentIsCorrect()
    {
      // Arrange: configure log4net from config file
      var config = "log4net.integration.basic.config";
      string configPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Integration", config);
      var repo = LogManager.CreateRepository(Guid.NewGuid().ToString());
      XmlConfigurator.Configure(repo, new FileInfo(configPath));

      // Act: log
      var log = LogManager.GetLogger(repo.Name, "IntegrationTestLogger");
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
        // Arrange: configure log4net from config file
        var config = "log4net.integration.basic.config";
        string configPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Integration", config);
        var repo = LogManager.CreateRepository(Guid.NewGuid().ToString());
        XmlConfigurator.Configure(repo, new FileInfo(configPath));

        // Act: log
        var log = LogManager.GetLogger(repo.Name, "IntegrationTestLogger");
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
        // Arrange: configure log4net from config file
        var config = "log4net.roll.config";
        string configPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Integration", config);
        var repo = LogManager.CreateRepository(Guid.NewGuid().ToString());
        XmlConfigurator.Configure(repo, new FileInfo(configPath));

        // Act: log
        var log = LogManager.GetLogger(repo.Name, "log");
        for (int j = 0; j < 10; ++j)
        {
          log.Info($"Hello, log4net! {i} {j}");
        }
        repo.Shutdown();
      }

      // Assert: log file exists and contains expected content
      string[] logFiles = Directory.GetFiles("integrationTestLogDir_1");
      Assert.That(logFiles.Length, Is.EqualTo(12+1));
    }

    [Test]
    public void Log4Net_WritesLogFile_WithMaxSizeRoll_Config_Works()
    {
      var logDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "integrationTestLogDir_maxsizeroll");
      if (Directory.Exists(logDir)) Directory.Delete(logDir, true);
      Directory.CreateDirectory(logDir);
      var config = "log4net.maxsizeroll.config";
      string configPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Integration", config);
      var repo = LogManager.CreateRepository(Guid.NewGuid().ToString());
      log4net.Config.XmlConfigurator.Configure(repo, new FileInfo(configPath));
      var log = LogManager.GetLogger(repo.Name, "log");
      // Write enough lines to trigger rolling
      for (int i = 0; i < 1000; ++i)
      {
        log.Info($"Log entry {i}");
      }
      repo.Shutdown();
      // Assert: rolled files exist
      string[] logFiles = Directory.GetFiles(logDir, "*.log");
      Assert.That(logFiles.Length, Is.EqualTo(4)); // 1 current + 3 backups
      // Optionally, check that each file is <= 10KB
      foreach (var file in logFiles) Assert.That(new FileInfo(file).Length, Is.LessThanOrEqualTo(10*1024 +100));
    }

    [Test]
    public void Log4Net_WritesLogFile_WithDateAndSizeRoll_Config_Works()
    {
      var logDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "integrationTestLogDir_maxsizerolldate");
      if (Directory.Exists(logDir)) Directory.Delete(logDir, true);
      Directory.CreateDirectory(logDir);
      var config = "log4net.maxsizeroll_date.config";
      string configPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Integration", config);
      var repo = LogManager.CreateRepository(Guid.NewGuid().ToString());
      log4net.Config.XmlConfigurator.Configure(repo, new FileInfo(configPath));
      var log = LogManager.GetLogger(repo.Name, "log");
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
  }
}
