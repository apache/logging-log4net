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
using log4net.Core;
using log4net.Layout;

using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Used for internal unit testing the <see cref="SmtpPickupDirAppender"/> class.
/// </summary>
[TestFixture]
public class SmtpPickupDirAppenderTest
{
  private readonly string _testPickupDir;

  private sealed class SilentErrorHandler : IErrorHandler
  {
    private readonly StringBuilder _buffer = new();

    public string Message => _buffer.ToString();

    public void Error(string message) => _buffer.Append(message + '\n');

    public void Error(string message, Exception e) => _buffer.Append(message + '\n' + e.Message + '\n');

    public void Error(string message, Exception? e, ErrorCode errorCode)
      => _buffer.Append(message + '\n' + e?.Message + '\n');
  }

  public SmtpPickupDirAppenderTest()
    => _testPickupDir = Path.Combine(Directory.GetCurrentDirectory(), "SmtpPickupDirAppenderTest_PickupDir");

  /// <summary>
  /// Sets up variables used for the tests
  /// </summary>
  private void InitializePickupDir() => Directory.CreateDirectory(_testPickupDir);

  /// <summary>
  /// Shuts down any loggers in the hierarchy, along
  /// with all appenders, and deletes any test files used
  /// for logging.
  /// </summary>
  private static void ResetLogger()
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
  public void SetUp()
  {
    ResetLogger();
    DeleteTestFiles();
    InitializePickupDir();
  }

  /// <summary>
  /// Any steps that happen after each test go here
  /// </summary>
  [TearDown]
  public void TearDown()
  {
    ResetLogger();
    DeleteTestFiles();
  }

  /// <summary>
  /// Removes all test files that exist
  /// </summary>
  private void DeleteTestFiles()
  {
    if (Directory.Exists(_testPickupDir))
    {
      Directory.Delete(_testPickupDir, true);
    }
  }

  /// <summary>
  /// Creates a logger hierarchy, configures a SMTP pickup dir appender and returns an ILogger
  /// </summary>
  /// <param name="appender">The appender to use</param>
  /// <returns>A configured ILogger</returns>
  private static ILogger CreateLogger(SmtpPickupDirAppender appender)
  {
    Repository.Hierarchy.Hierarchy h = (Repository.Hierarchy.Hierarchy)LogManager.CreateRepository("TestRepository");

    var layout = new PatternLayout { ConversionPattern = "%m%n" };
    layout.ActivateOptions();

    appender.Layout = layout;
    appender.ActivateOptions();

    h.Root.AddAppender(appender);
    h.Configured = true;

    ILogger log = h.GetLogger("Logger");
    return log;
  }

  /// <summary>
  /// Create an appender to use for the logger
  /// </summary>
  /// <param name="handler">The error handler to use.</param>
  /// <returns></returns>
  private SmtpPickupDirAppender CreateSmtpPickupDirAppender(IErrorHandler handler)
  {
    SmtpPickupDirAppender appender = new()
    {
      PickupDir = _testPickupDir,
      ErrorHandler = handler
    };
    return appender;
  }

  /// <summary>
  /// Destroys the logger hierarchy created by <see cref="SmtpPickupDirAppenderTest.CreateLogger"/>
  /// </summary>
  private static void DestroyLogger()
  {
    Repository.Hierarchy.Hierarchy h = (Repository.Hierarchy.Hierarchy)LogManager.GetRepository("TestRepository");
    h.ResetConfiguration();
    //Replace the repository selector so that we can recreate the hierarchy with the same name if necessary
    LoggerManager.RepositorySelector = new DefaultRepositorySelector(typeof(log4net.Repository.Hierarchy.Hierarchy));
  }

  /// <summary>
  /// Tests if the sent message contained the date header.
  /// </summary>
  [Test]
  public void TestOutputContainsSentDate()
  {
    Utils.InconclusiveOnMono();
    SilentErrorHandler sh = new();
    SmtpPickupDirAppender appender = CreateSmtpPickupDirAppender(sh);
    ILogger log = CreateLogger(appender);
    log.Log(GetType(), Level.Info, "This is a message", null);
    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();

    Assert.That(Directory.GetFiles(_testPickupDir), Has.Length.EqualTo(1));
    string[] fileContent = File.ReadAllLines((Directory.GetFiles(_testPickupDir)[0]));
    bool hasDateHeader = false;
    const string dateHeaderStart = "Date: ";
    foreach (string line in fileContent)
    {
      if (line.StartsWith(dateHeaderStart))
      {
        string datePart = line.Substring(dateHeaderStart.Length);
        DateTime date = DateTime.ParseExact(datePart, "r", System.Globalization.CultureInfo.InvariantCulture);
        double diff = Math.Abs((DateTime.UtcNow - date).TotalMilliseconds);
        Assert.That(diff, Is.LessThanOrEqualTo(1000),
          "Times should be equal, allowing a diff of one second to make test robust");
        hasDateHeader = true;
      }
    }
    Assert.That(hasDateHeader, "Output must contains a date header");

    Assert.That(sh.Message, Is.EqualTo(string.Empty), "Unexpected error message");
  }

  /// <summary>
  /// Verifies that file extension is applied to output file name.
  /// </summary>
  [Test]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1846:Prefer 'AsSpan' over 'Substring'",
    Justification = "only .net8")]
  public void TestConfigurableFileExtension()
  {
    Utils.InconclusiveOnMono();
    const string fileExtension = "eml";
    SilentErrorHandler sh = new();
    SmtpPickupDirAppender appender = CreateSmtpPickupDirAppender(sh);
    appender.FileExtension = fileExtension;
    ILogger log = CreateLogger(appender);
    log.Log(GetType(), Level.Info, "This is a message", null);
    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();

    Assert.That(Directory.GetFiles(_testPickupDir), Has.Length.EqualTo(1));
    FileInfo fileInfo = new(Directory.GetFiles(_testPickupDir)[0]);
    Assert.That(fileInfo.Extension, Is.EqualTo("." + fileExtension));
    Assert.That(Guid.TryParse(fileInfo.Name.Substring(0, fileInfo.Name.Length - fileInfo.Extension.Length), out _));

    Assert.That(sh.Message, Is.EqualTo(""), "Unexpected error message");
  }

  /// <summary>
  /// Verifies that logging a message actually produces output
  /// </summary>
  [Test]
  public void TestDefaultFileNameIsAGuid()
  {
    Utils.InconclusiveOnMono();
    SilentErrorHandler sh = new();
    SmtpPickupDirAppender appender = CreateSmtpPickupDirAppender(sh);
    ILogger log = CreateLogger(appender);
    log.Log(GetType(), Level.Info, "This is a message", null);
    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();

    Assert.That(Directory.GetFiles(_testPickupDir), Has.Length.EqualTo(1));
    FileInfo fileInfo = new(Directory.GetFiles(_testPickupDir)[0]);
    Assert.That(fileInfo.Extension, Is.Empty);
    Assert.That(Guid.TryParse(fileInfo.Name, out _));

    Assert.That(sh.Message, Is.EqualTo(string.Empty), "Unexpected error message");
  }
}