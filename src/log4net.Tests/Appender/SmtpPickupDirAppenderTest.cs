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

using PeanutButter.Utils;

namespace log4net.Tests.Appender;

/// <summary>
/// Used for internal unit testing the <see cref="SmtpPickupDirAppender"/> class.
/// </summary>
[TestFixture]
public class SmtpPickupDirAppenderTest
{

  private sealed class SilentErrorHandler : IErrorHandler
  {
    private readonly StringBuilder _buffer = new();

    public string Message => _buffer.ToString();

    public void Error(string message) => _buffer.Append(message + '\n');

    public void Error(string message, Exception e) => _buffer.Append(message + '\n' + e.Message + '\n');

    public void Error(string message, Exception? e, ErrorCode errorCode)
      => _buffer.Append(message + '\n' + e?.Message + '\n');
  }


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
  public void SetUp() => ResetLogger();

  /// <summary>
  /// Any steps that happen after each test go here
  /// </summary>
  [TearDown]
  public void TearDown() => ResetLogger();

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
  /// <param name="pickupDir">The directory the appender writes its mails to.</param>
  /// <param name="handler">The error handler to use.</param>
  /// <returns></returns>
  private static SmtpPickupDirAppender CreateSmtpPickupDirAppender(string pickupDir, IErrorHandler handler)
    => new()
    {
      PickupDir = pickupDir,
      ErrorHandler = handler
    };

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
  /// An unpaired surrogate used to abort the write, losing every buffered event with it and
  /// leaving a truncated mail behind for the pickup service to send.
  /// </summary>
  [Test]
  public void ContentThatCannotBeEncodedDoesNotDestroyTheBatch()
  {
    using AutoTempFolder pickupDir = new();
    SilentErrorHandler sh = new();
    SmtpPickupDirAppender appender = CreateSmtpPickupDirAppender(pickupDir.Path, sh);
    ILogger log = CreateLogger(appender);

    log.Log(GetType(), Level.Info, "poison" + (char)0xd800 + "event", null);
    log.Log(GetType(), Level.Info, "the event after it", null);
    DestroyLogger();

    Assert.That(Directory.GetFiles(pickupDir.Path), Has.Length.EqualTo(1));
    string content = File.ReadAllText(Directory.GetFiles(pickupDir.Path)[0]);

    Assert.That(content, Does.Contain(@"poison\ud800event"));
    Assert.That(content, Does.Contain("the event after it"));
    Assert.That(sh.Message, Is.EqualTo(string.Empty), "Unexpected error message");
  }

  /// <summary>
  /// The appender used to close the file with a lone dot, so a logged line that is only a dot was
  /// indistinguishable from it and ended the mail early for an agent honouring the terminator.
  /// </summary>
  [Test]
  public void ALoggedDotIsTheOnlyDotLineInTheFile()
  {
    using AutoTempFolder pickupDir = new();
    SilentErrorHandler sh = new();
    SmtpPickupDirAppender appender = CreateSmtpPickupDirAppender(pickupDir.Path, sh);
    ILogger log = CreateLogger(appender);

    log.Log(GetType(), Level.Info, ".", null);
    log.Log(GetType(), Level.Info, "the event after it", null);
    DestroyLogger();

    string[] lines = File.ReadAllLines(Directory.GetFiles(pickupDir.Path)[0]);

    Assert.That(Array.FindAll(lines, line => line == "."), Has.Length.EqualTo(1));
    Assert.That(lines, Does.Contain("the event after it"));
    Assert.That(sh.Message, Is.EqualTo(string.Empty), "Unexpected error message");
  }

  /// <summary>
  /// Tests if the sent message contained the date header.
  /// </summary>
  [Test]
  public void TestOutputContainsSentDate()
  {
    using AutoTempFolder pickupDir = new();
    SilentErrorHandler sh = new();
    SmtpPickupDirAppender appender = CreateSmtpPickupDirAppender(pickupDir.Path, sh);
    ILogger log = CreateLogger(appender);
    DateTime beforeLog = DateTime.UtcNow;
    log.Log(GetType(), Level.Info, "This is a message", null);
    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();

    Assert.That(Directory.GetFiles(pickupDir.Path), Has.Length.EqualTo(1));
    string[] fileContent = File.ReadAllLines((Directory.GetFiles(pickupDir.Path)[0]));
    bool hasDateHeader = false;
    const string dateHeaderStart = "Date: ";
    foreach (string line in fileContent)
    {
      if (line.StartsWith(dateHeaderStart))
      {
        string datePart = line.Substring(dateHeaderStart.Length);
        DateTime date = DateTime.ParseExact(datePart, "r", System.Globalization.CultureInfo.InvariantCulture);
        double diff = Math.Abs((date - beforeLog).TotalMilliseconds);
        Assert.That(diff, Is.LessThanOrEqualTo(5000),
          "Times should be equal, allowing a diff of five seconds to make test robust");
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
    const string fileExtension = "eml";
    using AutoTempFolder pickupDir = new();
    SilentErrorHandler sh = new();
    SmtpPickupDirAppender appender = CreateSmtpPickupDirAppender(pickupDir.Path, sh);
    appender.FileExtension = fileExtension;
    ILogger log = CreateLogger(appender);
    log.Log(GetType(), Level.Info, "This is a message", null);
    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();

    Assert.That(Directory.GetFiles(pickupDir.Path), Has.Length.EqualTo(1));
    FileInfo fileInfo = new(Directory.GetFiles(pickupDir.Path)[0]);
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
    using AutoTempFolder pickupDir = new();
    SilentErrorHandler sh = new();
    SmtpPickupDirAppender appender = CreateSmtpPickupDirAppender(pickupDir.Path, sh);
    ILogger log = CreateLogger(appender);
    log.Log(GetType(), Level.Info, "This is a message", null);
    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();

    Assert.That(Directory.GetFiles(pickupDir.Path), Has.Length.EqualTo(1));
    FileInfo fileInfo = new(Directory.GetFiles(pickupDir.Path)[0]);
    Assert.That(fileInfo.Extension, Is.Empty);
    Assert.That(Guid.TryParse(fileInfo.Name, out _));

    Assert.That(sh.Message, Is.EqualTo(string.Empty), "Unexpected error message");
  }
}