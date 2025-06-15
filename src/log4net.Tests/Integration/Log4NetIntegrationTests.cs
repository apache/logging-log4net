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
        .Where(dirPath => Path.GetDirectoryName(dirPath)?.StartsWith("integrationTestLogDir", StringComparison.InvariantCultureIgnoreCase) == true)
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
      for (int i = 0; i < 10; i++)
      {
        // Arrange: configure log4net from config file
        var config = "log4net.roll.config.xml";
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
      Assert.That(logFiles.Length, Is.EqualTo(10));
    }
  }
}
