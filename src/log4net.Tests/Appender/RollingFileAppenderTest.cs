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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using log4net.Util;
using NUnit.Framework;
using System.Globalization;
using System.Linq;

namespace log4net.Tests.Appender;

/// <summary>
/// Used for internal unit testing the <see cref="RollingFileAppender"/> class.
/// </summary>
[TestFixture]
public class RollingFileAppenderTest
{
  protected string FileName = "test_41d3d834_4320f4da.log";

  private const string TestMessage98Chars =
    "01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567";

  private const string TestMessage99Chars =
    "012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678";

  private const int MaximumFileSize = 450; // in bytes
  private int _messagesLogged;
  private int _countDirection;
  private int _maxSizeRollBackups = 3;
  private CountingAppender? _caRoot;
  private Logger? _root;
  private CultureInfo? _currentCulture;
  private CultureInfo? _currentUiCulture;

  private sealed class SilentErrorHandler : IErrorHandler
  {
    private readonly StringBuilder _buffer = new();

    public string Message => _buffer.ToString();

    public void Error(string message)
      => _buffer.Append(message + '\n');

    public void Error(string message, Exception e)
      => _buffer.Append(message + '\n' + e.Message + '\n');

    public void Error(string message, Exception? e, ErrorCode errorCode)
      => _buffer.Append(message + '\n' + e?.Message + '\n');
  }

  private sealed class RollingFileAppenderForTest : RollingFileAppender
  {
    /// <summary>
    /// Builds a list of filenames for all files matching the base filename plus a file pattern.
    /// </summary>
    internal new List<string> GetExistingFiles(string baseFilePath)
      => base.GetExistingFiles(baseFilePath);
  }

  /// <summary>
  /// Sets up variables used for the tests
  /// </summary>
  private void InitializeVariables()
  {
    _messagesLogged = 0;
    _countDirection = +1; // Up
    _maxSizeRollBackups = 3;
  }

  /// <summary>
  /// Shuts down any loggers in the hierarchy, along
  /// with all appenders, and deletes any test files used
  /// for logging.
  /// </summary>
  private void ResetAndDeleteTestFiles()
  {
    // Regular users should not use the clear method lightly!
    LogManager.GetRepository().ResetConfiguration();
    LogManager.GetRepository().Shutdown();
    ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Clear();

    DeleteTestFiles();
  }

  /// <summary>
  /// Any initialization that happens before each test can
  /// go here
  /// </summary>
  [SetUp]
  public void SetUp()
  {
    ResetAndDeleteTestFiles();
    InitializeVariables();

    // set correct thread culture
    _currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
    _currentUiCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
    System.Threading.Thread.CurrentThread.CurrentCulture =
        System.Threading.Thread.CurrentThread.CurrentUICulture =
            CultureInfo.InvariantCulture;
  }

  /// <summary>
  /// Any steps that happen after each test go here
  /// </summary>
  [TearDown]
  public void TearDown()
  {
    ResetAndDeleteTestFiles();

    // restore previous culture
    System.Threading.Thread.CurrentThread.CurrentCulture = _currentCulture!;
    System.Threading.Thread.CurrentThread.CurrentUICulture = _currentUiCulture!;
  }

  /// <summary>
  /// Finds the number of files that match the base file name,
  /// and matches the result against an expected count
  /// </summary>
  private void VerifyFileCount(int expectedCount, bool preserveLogFileNameExtension = false)
  {
    List<string> files = GetExistingFiles(FileName, preserveLogFileNameExtension);
    Assert.That(files, Is.Not.Null);
    Assert.That(files, Has.Count.EqualTo(expectedCount));
  }

  /// <summary>
  /// Creates a file with the given number, and the shared base file name
  /// </summary>
  private void CreateFile(int fileNumber)
  {
    FileInfo fileInfo = new(MakeFileName(FileName, fileNumber));

    FileStream? fileStream = null;
    try
    {
      fileStream = fileInfo.Create();
    }
    finally
    {
      if (null != fileStream)
      {
        try
        {
          fileStream.Close();
        }
        catch (ObjectDisposedException)
        {
          // Ignore
        }
      }
    }
  }

  /// <summary>
  /// Verifies that the code correctly loads all filenames
  /// </summary>
  [Test]
  public void TestGetExistingFiles()
  {
    VerifyFileCount(0);
    CreateFile(0);
    VerifyFileCount(1);
    CreateFile(1);
    VerifyFileCount(2);
  }

  [Test]
  public void RollingCombinedWithPreserveExtension()
  {
    _root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;
    _root.Level = Level.All;
    PatternLayout patternLayout = new();
    patternLayout.ActivateOptions();

    RollingFileAppender roller = new()
    {
      StaticLogFileName = false,
      Layout = patternLayout,
      AppendToFile = true,
      RollingStyle = RollingFileAppender.RollingMode.Composite,
      DatePattern = "dd_MM_yyyy",
      MaxSizeRollBackups = 1,
      CountDirection = 1,
      PreserveLogFileNameExtension = true,
      MaximumFileSize = "10KB",
      File = FileName
    };
    roller.ActivateOptions();
    _root.AddAppender(roller);

    _root.Repository!.Configured = true;

    for (int i = 0; i < 1000; i++)
    {
      StringBuilder s = new();
      for (int j = 50; j < 100; j++)
      {
        if (j > 50)
        {
          s.Append(' ');
        }

        s.Append(j);
      }

      _root.Log(Level.Debug, s.ToString(), null);
    }

    VerifyFileCount(2, true);
  }

  /// <summary>
  /// Removes all test files that exist
  /// </summary>
  private void DeleteTestFiles()
  {
    List<string> files = GetExistingFiles(FileName);
    files.AddRange(GetExistingFiles(FileName, true));
    foreach (string file in files)
    {
      try
      {
        Debug.WriteLine("Deleting test file " + file);
        File.Delete(file);
      }
      catch (Exception e) when (e is not null)
      {
        Debug.WriteLine("Exception while deleting test file " + e);
      }
    }
  }

  /// <summary>
  /// Generates a file name associated with the count, using
  /// the base file name.
  /// </summary>
  /// <param name="baseFile"></param>
  /// <param name="fileCount"></param>
  /// <returns></returns>
  private static string MakeFileName(string baseFile, int fileCount)
  {
    if (0 == fileCount)
    {
      return baseFile;
    }

    return baseFile + "." + fileCount;
  }

  /// <summary>
  /// Returns a RollingFileAppender using all the internal settings for maximum
  /// file size and number of backups
  /// </summary>
  /// <returns></returns>
  private RollingFileAppender CreateAppender() => CreateAppender(new FileAppender.ExclusiveLock());

  /// <summary>
  /// Returns a RollingFileAppender using all the internal settings for maximum
  /// file size and number of backups
  /// </summary>
  /// <param name="lockModel">The locking model to test</param>
  /// <returns></returns>
  private RollingFileAppender CreateAppender(FileAppender.LockingModelBase lockModel)
  {
    //
    // Use a basic pattern that
    // includes just the message and a CR/LF.
    //
    PatternLayout layout = new("%m%n");

    //
    // Create the new appender
    //
    RollingFileAppender appender = new()
    {
      Layout = layout,
      File = FileName,
      Encoding = Encoding.ASCII,
      MaximumFileSize = MaximumFileSize.ToString(),
      MaxSizeRollBackups = _maxSizeRollBackups,
      CountDirection = _countDirection,
      RollingStyle = RollingFileAppender.RollingMode.Size,
      LockingModel = lockModel
    };

    appender.ActivateOptions();

    return appender;
  }

  /// <summary>
  /// Used for test purposes, a table of these objects can be used to identify
  /// any existing files and their expected length.
  /// </summary>
  private sealed record RollFileEntry(string FileName, long FileLength);

  /// <summary>
  /// Used for table-driven testing.  This class holds information that can be used
  /// for testing of file rolling.
  /// </summary>
  /// <param name="PreLogFileEntries">
  /// A table of entries showing files that should exist and their expected sizes
  /// before logging is called
  /// </param>
  /// <param name="PostLogFileEntries">
  /// A table of entries showing files that should exist and their expected sizes
  /// after a message is logged
  /// </param>
  private sealed record RollConditions(IList<RollFileEntry> PreLogFileEntries, IList<RollFileEntry> PostLogFileEntries);

  private static void VerifyExistenceAndRemoveFromList(List<string> existing,
    string fileName, FileInfo file, RollFileEntry entry)
  {
    Assert.That(existing, Does.Contain(fileName), $"filename {fileName} not found in test directory");
    Assert.That(file.Length, Is.EqualTo(entry.FileLength), "file length mismatch");
    // Remove this file from the list
    existing.Remove(fileName);
  }

  /// <summary>
  /// Checks that all the expected files exist, and only the expected files.  Also
  /// verifies the length of all files against the expected length
  /// </summary>
  /// <param name="baseFileName"></param>
  /// <param name="fileEntries"></param>
  private static void VerifyFileConditions(string baseFileName, IList<RollFileEntry> fileEntries)
  {
    List<string> existingFiles = GetExistingFiles(baseFileName);

    foreach (RollFileEntry rollFile in fileEntries)
    {
      string fileName = rollFile.FileName;
      Assert.That(fileName, Is.Not.Null);
      FileInfo file = new(fileName);

      if (rollFile.FileLength > 0)
      {
        Assert.That(file.Exists, $"file {fileName} does not exist");
        VerifyExistenceAndRemoveFromList(existingFiles, fileName, file, rollFile);
      }
      else
      {
        // If length is 0, file may not exist yet.  If file exists, make sure length
        // is zero.  If file doesn't exist, this is OK

        if (file.Exists)
        {
          VerifyExistenceAndRemoveFromList(existingFiles, fileName, file, rollFile);
        }
      }
    }

    // This check ensures no extra files matching the wildcard pattern exist.
    // We only want the files we expect, and no others
    Assert.That(existingFiles, Is.Empty);
  }

  /// <summary>
  /// Called before logging a message to check that all the expected files exist, 
  /// and only the expected files.  Also verifies the length of all files against 
  /// the expected length
  /// </summary>
  /// <param name="baseFileName"></param>
  /// <param name="entry"></param>
  private static void VerifyPreConditions(string baseFileName, RollConditions entry)
    => VerifyFileConditions(baseFileName, entry.PreLogFileEntries);

  /// <summary>
  /// Called after logging a message to check that all the expected files exist, 
  /// and only the expected files.  Also verifies the length of all files against 
  /// the expected length
  /// </summary>
  /// <param name="baseFileName"></param>
  /// <param name="entry"></param>
  private static void VerifyPostConditions(string baseFileName, RollConditions entry)
    => VerifyFileConditions(baseFileName, entry.PostLogFileEntries);

  /// <summary>
  /// Logs a message, verifying the expected message counts against the 
  /// current running totals.
  /// </summary>
  private void LogMessage(string messageToLog)
  {
    Assert.That(_caRoot, Is.Not.Null);
    Assert.That(_messagesLogged++, Is.EqualTo(_caRoot.Counter));
    Assert.That(_root, Is.Not.Null);
    _root.Log(Level.Debug, messageToLog, null);
    Assert.That(_messagesLogged, Is.EqualTo(_caRoot.Counter));
  }

  /// <summary>
  /// Runs through all table entries, logging messages.  Before each message is logged,
  /// pre-conditions are checked to ensure the expected files exists, and they are the
  /// expected size.  After logging, verifies the same.
  /// </summary>
  /// <param name="baseFileName"></param>
  /// <param name="entries"></param>
  /// <param name="messageToLog"></param>
  private void RollFromTableEntries(string baseFileName, RollConditions[] entries, string messageToLog)
  {
    foreach (RollConditions entry in entries)
    {
      VerifyPreConditions(baseFileName, entry);
      LogMessage(messageToLog);
      VerifyPostConditions(baseFileName, entry);
    }
  }

  private static readonly int _sNewlineLength = Environment.NewLine.Length;

  /// <summary>
  /// Returns the number of bytes logged per message, including
  /// any newline characters in addition to the message length.
  /// </summary>
  /// <returns></returns>
  private static int TotalMessageLength(string message) => message.Length + _sNewlineLength;

  /// <summary>
  /// Determines how many messages of a fixed length can be logged
  /// to a single file before the file rolls.
  /// </summary>
  /// <returns></returns>
  private static int MessagesPerFile(int messageLength)
  {
    int messagesPerFile = MaximumFileSize / messageLength;

    //
    // RollingFileAppender checks for wrap BEFORE logging,
    // so we will actually get one more message per file than
    // we would otherwise.
    //
    if (messagesPerFile * messageLength < MaximumFileSize)
    {
      messagesPerFile++;
    }

    return messagesPerFile;
  }

  /// <summary>
  /// Current file name is always the base file name when counting. Dates will need a different approach.
  /// </summary>
  /// <returns></returns>
  private string GetCurrentFile() => FileName;

  /// <summary>
  /// Turns a group of file names into an array of file entries that include the name
  /// and a size.  This is useful for assigning the properties of backup files, when
  /// the length of the files are all the same size due to a fixed message length.
  /// </summary>
  /// <returns></returns>
  private static List<RollFileEntry> MakeBackupFileEntriesFromBackupGroup(
    string backupGroup, 
    int backupFileLength)
  {
    // ReSharper disable once ArrangeMethodOrOperatorBody
    return backupGroup.Split(' ')
      .Where(file => file.Trim().Length > 0)
      .Select(file => new RollFileEntry(file, backupFileLength))
      .ToList();
  }

  /// <summary>
  /// Finds the iGroup group in the string (comma separated groups)
  /// </summary>
  /// <returns></returns>
  private static string GetBackupGroup(string backupGroups, int group) 
    => backupGroups.Split(',')[group];

  /// <summary>
  /// Builds a collection of file entries based on the file names
  /// specified in a groups string and the max file size from the
  /// stats object
  /// </summary>
  /// <returns></returns>
  private static List<RollFileEntry>? MakeBackupFileEntriesForPostCondition(string backupGroups, RollingStats stats)
  {
    if (0 == stats.NumberOfFileRolls)
    {
      return null; // first round has no previous backups
    }

    string group = GetBackupGroup(backupGroups, stats.NumberOfFileRolls - 1);
    return MakeBackupFileEntriesFromBackupGroup(group, stats.MaximumFileSize);
  }

  /// <summary>
  /// This class holds information that is used while we are generating
  /// test data sets
  /// </summary>
  /// <param name="TotalMessageLength">
  /// The length of a message, including any CR/LF characters.
  /// This length assumes all messages are a fixed length for test purposes.
  /// </param>
  /// <param name="MessagesPerFile">A count of the number of messages that are logged to each file.</param>
  private sealed record RollingStats(
    int TotalMessageLength,
    int MessagesPerFile)
  {
    /// <summary>
    /// Number of total bytes a log file can reach.
    /// </summary>
    // ReSharper disable once MemberHidesStaticFromOuterClass
    public int MaximumFileSize => TotalMessageLength * MessagesPerFile;

    /// <summary>
    /// Counts how many messages have been logged to the current file
    /// </summary>
    public int MessagesThisFile { get; set; }

    /// <summary>
    /// Counts how many times a file roll has occurred
    /// </summary>
    public int NumberOfFileRolls { get; set; }
  }

  /// <summary>
  /// The stats are used to keep track of progress while we are algorithmically
  /// generating a table of pre- / post-condition tests for file rolling.
  /// </summary>
  /// <param name="testMessage"></param>
  /// <returns></returns>
  private static RollingStats InitializeStats(string testMessage) 
    => new(TotalMessageLength(testMessage), MessagesPerFile(TotalMessageLength(testMessage)));

  /// <summary>
  /// Takes an existing array of RollFileEntry objects, creates a new array one element
  /// bigger, and appends the final element to the end.  If the existing entries are
  /// null (no entries), then a one-element array is returned with the final element
  /// as the only entry.
  /// </summary>
  /// <param name="existing"></param>
  /// <param name="final"></param>
  /// <returns></returns>
  private static List<RollFileEntry> AddFinalElement(List<RollFileEntry>? existing, RollFileEntry final)
  {
    int length = 1;
    if (existing is not null)
    {
      length += existing.Count;
    }

    List<RollFileEntry> combined = new(length);
    if (existing is not null)
    {
      combined.AddRange(existing);
    }

    combined.Add(final);
    return combined;
  }

  /// <summary>
  /// Generates the pre- and post-condition arrays from an array of backup files and the
  /// current file / next file.
  /// </summary>
  private static RollConditions BuildTableEntry(string backupFiles,
    RollConditions? preCondition,
    RollFileEntry current,
    RollFileEntry currentNext,
    RollingStats rollingStats)
  {
    List<RollFileEntry>? backupsPost = MakeBackupFileEntriesForPostCondition(backupFiles, rollingStats);
    List<RollFileEntry> post = AddFinalElement(backupsPost, currentNext);
    return preCondition is null 
      ? new(AddFinalElement(null, current), post) 
      : new(preCondition.PostLogFileEntries, post);
  }

  /// <summary>
  /// Returns a RollFileEntry that represents the next state of the current file,
  /// based on the current state.  When the current state would roll, the next
  /// entry is the current file wrapped to 0 bytes.  Otherwise, the next state
  /// is the post-condition passed in as the currentNext parameter
  /// </summary>
  /// <param name="rollingStats"></param>
  /// <param name="currentNext"></param>
  /// <returns></returns>
  private RollFileEntry MoveNextEntry(RollingStats rollingStats, RollFileEntry currentNext)
  {
    rollingStats.MessagesThisFile++;
    if (rollingStats.MessagesThisFile >= rollingStats.MessagesPerFile)
    {
      rollingStats.MessagesThisFile = 0;
      rollingStats.NumberOfFileRolls++;

      return new RollFileEntry(GetCurrentFile(), 0);
    }
    return currentNext;
  }

  /// <summary>
  /// Callback point for the regular expression parser.  Turns
  /// the number into a file name.
  /// </summary>
  private string NumberedNameMaker(Match match) 
    => MakeFileName(FileName, int.Parse(match.Value));

  /// <summary>
  /// Parses a numeric list of files, turning them into file names.
  /// Calls back to a method that does the actual replacement, turning
  /// the numeric value into a filename.
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "SYSLIB1045:Convert to 'GeneratedRegexAttribute'.", 
    Justification = "only .net8")]
  private static string ConvertToFiles(string backupInfo, MatchEvaluator evaluator) 
    => Regex.Replace(backupInfo, @"\d+", evaluator);

  /// <summary>
  /// Makes test entries used for verifying counted file names
  /// </summary>
  /// <param name="testMessage">A message to log repeatedly</param>
  /// <param name="backupInfo">Filename groups used to indicate backup file name progression
  /// that results after each message is logged</param>
  /// <param name="messagesToLog">How many times the test message will be repeatedly logged</param>
  /// <returns></returns>
  private RollConditions[] MakeNumericTestEntries(
    string testMessage, 
    string backupInfo,
    int messagesToLog)
    => MakeTestEntries(testMessage, backupInfo, messagesToLog, NumberedNameMaker);

  /// <summary>
  /// This routine takes a list of backup file names and a message that will be logged
  /// repeatedly, and generates a collection of objects containing pre-condition and 
  /// post-condition information.  This pre- / post-information shows the names and expected 
  /// file sizes for all files just before and just after a message is logged.
  /// </summary>
  /// <param name="testMessage">A message to log repeatedly</param>
  /// <param name="backupInfo">Filename groups used to indicate backup file name progression
  /// that results after each message is logged</param>
  /// <param name="messagesToLog">How many times the test message will be repeatedly logged</param>
  /// <param name="evaluator">Function that can turn a number into a filename</param>
  /// <returns></returns>
  private RollConditions[] MakeTestEntries(
    string testMessage,
    string backupInfo,
    int messagesToLog,
    MatchEvaluator evaluator)
  {
    string backupFiles = ConvertToFiles(backupInfo, evaluator);

    RollConditions[] table = new RollConditions[messagesToLog];

    RollingStats rollingStats = InitializeStats(testMessage);

    RollConditions? preCondition = null;
    rollingStats.MessagesThisFile = 0;

    RollFileEntry currentFile = new(GetCurrentFile(), 0);
    for (int i = 0; i < messagesToLog; i++)
    {
      RollFileEntry currentNext = new(
          GetCurrentFile(),
          (1 + rollingStats.MessagesThisFile) * rollingStats.TotalMessageLength);

      table[i] = BuildTableEntry(backupFiles, preCondition, currentFile, currentNext, rollingStats);
      preCondition = table[i];

      //System.Diagnostics.Debug.WriteLine( "Message " + i );
      //DumpTableEntry( table[i] );

      currentFile = MoveNextEntry(rollingStats, currentNext);
    }

    return table;
  }

  /// <summary>
  /// Uses the externally defined rolling table to verify rolling names/sizes
  /// </summary>
  /// <remarks>
  /// Pattern is:  check pre-conditions.  Log messages, checking size of current file.
  /// when size exceeds limit, check post conditions.  Can determine from message the
  /// number of messages N that will cause a roll to occur.  Challenge is to verify the
  /// expected files, their sizes, and the names.  For a message of length L, the backups
  /// will be of size (N * L), and the current file will be of size (K * L), where K is
  /// the number of messages that have been logged to this file.
  ///
  /// File sizes can be checked algorithmically.  
  /// 
  /// File names are generated using a table driven algorithm, where a number is turned into
  /// the actual filename.
  /// 
  /// The entries are comma-separated, with spaces between the names.  Each comma indicates
  /// a 'roll', and the group between commas indicates the numbers for all backup files that
  /// occur as a result of the roll.  It is assumed that no backup files exist before a roll 
  /// occurs
  /// </remarks>
  /// <param name="table"></param>
  private void VerifyRolling(RollConditions[] table)
  {
    ConfigureRootAppender();
    RollFromTableEntries(FileName, table, GetTestMessage());
  }

  /// <summary>
  /// Validates rolling using a fixed number of backup files, with
  /// count direction set to up, so that newer files have higher counts.
  /// Newest = N, Oldest = N-K, where K is the number of backups to allow
  /// and N is the number of times rolling has occurred.
  /// </summary>
  [Test]
  public void TestRollingCountUpFixedBackups()
  {
    //
    // Oldest to newest when reading in a group left-to-right, so 1 2 3 means 1 is the
    // oldest, and 3 is the newest
    //
    const string backupInfo = "1, 1 2, 1 2 3, 2 3 4, 3 4 5";

    //
    // Count Up
    //
    _countDirection = +1;

    //
    // Log 30 messages.  This is 5 groups, 6 checks per group (0, 100, 200, 300, 400, 500) 
    // bytes for current file as messages are logged.
    //
    const int messagesToLog = 30;

    VerifyRolling(MakeNumericTestEntries(GetTestMessage(), backupInfo, messagesToLog));
  }

  /// <summary>
  /// Validates rolling using an infinite number of backup files, with
  /// count direction set to up, so that newer files have higher counts.
  /// Newest = N, Oldest = 1, where N is the number of times rolling has 
  /// occurred.
  /// </summary>
  [Test]
  public void TestRollingCountUpInfiniteBackups()
  {
    //
    // Oldest to newest when reading in a group left-to-right, so 1 2 3 means 1 is the
    // oldest, and 3 is the newest
    //
    const string backupInfo = "1, 1 2, 1 2 3, 1 2 3 4, 1 2 3 4 5";

    //
    // Count Up
    //
    _countDirection = +1;

    //
    // Infinite backups
    //
    _maxSizeRollBackups = -1;

    //
    // Log 30 messages.  This is 5 groups, 6 checks per group (0, 100, 200, 300, 400, 500) 
    // bytes for current file as messages are logged.
    //
    const int messagesToLog = 30;

    VerifyRolling(MakeNumericTestEntries(GetTestMessage(), backupInfo, messagesToLog));
  }

  /// <summary>
  /// Validates rolling with no backup files, with count direction set to up.
  /// Only the current file should be present, wrapping to 0 bytes once the
  /// previous file fills up.
  /// </summary>
  [Test]
  public void TestRollingCountUpZeroBackups()
  {
    //
    // Oldest to newest when reading in a group left-to-right, so 1 2 3 means 1 is the
    // oldest, and 3 is the newest
    //
    const string backupInfo = ", , , , ";

    //
    // Count Up
    //
    _countDirection = +1;

    //
    // No backups
    //
    _maxSizeRollBackups = 0;

    //
    // Log 30 messages.  This is 5 groups, 6 checks per group (0, 100, 200, 300, 400, 500) 
    // bytes for current file as messages are logged.
    //
    const int messagesToLog = 30;

    VerifyRolling(MakeNumericTestEntries(GetTestMessage(), backupInfo, messagesToLog));
  }


  /// <summary>
  /// Validates rolling using a fixed number of backup files, with
  /// count direction set to down, so that older files have higher counts.
  /// Newest = 1, Oldest = N, where N is the number of backups to allow
  /// </summary>
  [Test]
  public void TestRollingCountDownFixedBackups()
  {
    //
    // Oldest to newest when reading in a group left-to-right, so 1 2 3 means 1 is the
    // oldest, and 3 is the newest
    //
    const string backupInfo = "1, 1 2, 1 2 3, 1 2 3, 1 2 3";

    //
    // Count Up
    //
    _countDirection = -1;

    //
    // Log 30 messages.  This is 5 groups, 6 checks per group (0, 100, 200, 300, 400, 500) 
    // bytes for current file as messages are logged.
    //
    const int messagesToLog = 30;

    VerifyRolling(MakeNumericTestEntries(GetTestMessage(), backupInfo, messagesToLog));
  }

  /// <summary>
  /// Validates rolling using an infinite number of backup files, with
  /// count direction set to down, so that older files have higher counts.
  /// Newest = 1, Oldest = N, where N is the number of times rolling has
  /// occurred
  /// </summary>
  [Test]
  public void TestRollingCountDownInfiniteBackups()
  {
    //
    // Oldest to newest when reading in a group left-to-right, so 1 2 3 means 1 is the
    // oldest, and 3 is the newest
    //
    const string backupInfo = "1, 1 2, 1 2 3, 1 2 3 4, 1 2 3 4 5";

    //
    // Count Down
    //
    _countDirection = -1;

    //
    // Infinite backups
    //
    _maxSizeRollBackups = -1;

    //
    // Log 30 messages.  This is 5 groups, 6 checks per group (0, 100, 200, 300, 400, 500) 
    // bytes for current file as messages are logged.
    //
    const int messagesToLog = 30;

    VerifyRolling(MakeNumericTestEntries(GetTestMessage(), backupInfo, messagesToLog));
  }

  /// <summary>
  /// Validates rolling with no backup files, with count direction set to down.
  /// Only the current file should be present, wrapping to 0 bytes once the
  /// previous file fills up.
  /// </summary>
  [Test]
  public void TestRollingCountDownZeroBackups()
  {
    //
    // Oldest to newest when reading in a group left-to-right, so 1 2 3 means 1 is the
    // oldest, and 3 is the newest
    //
    const string backupInfo = ", , , , ";

    //
    // Count Up
    //
    _countDirection = -1;

    //
    // No backups
    //
    _maxSizeRollBackups = 0;

    //
    // Log 30 messages.  This is 5 groups, 6 checks per group (0, 100, 200, 300, 400, 500) 
    // bytes for current file as messages are logged.
    //
    const int messagesToLog = 30;

    VerifyRolling(MakeNumericTestEntries(GetTestMessage(), backupInfo, messagesToLog));
  }

  /// <summary>
  /// Configures the root appender for counting and rolling
  /// </summary>
  private void ConfigureRootAppender()
  {
    _root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;
    _root.Level = Level.Debug;
    _caRoot = new CountingAppender();
    _root.AddAppender(_caRoot);
    Assert.That(_caRoot.Counter, Is.EqualTo(0));

    //
    // Set the root appender with a RollingFileAppender
    //
    _root.AddAppender(CreateAppender());

    if (_root.Repository is not null)
    {
      _root.Repository.Configured = true;
    }
  }

  /// <summary>
  /// Verifies that the current backup index is detected correctly when initializing
  /// </summary>
  /// <param name="baseFile"></param>
  /// <param name="files"></param>
  /// <param name="expectedCurSizeRollBackups"></param>
  private static void VerifyInitializeRollBackupsFromBaseFile(string baseFile,
    List<string> files, int expectedCurSizeRollBackups)
    => InitializeAndVerifyExpectedValue(files, baseFile, CreateRollingFileAppender("5,0,1"),
        expectedCurSizeRollBackups);

  /// <summary>
  /// Tests that the current backup index is 0 when no
  /// existing files are seen
  /// </summary>
  [Test]
  public void TestInitializeRollBackups1()
  {
    const string baseFile = "LogFile.log";
    List<string> files =
    [
      "junk1",
      "junk1.log",
      "junk2.log",
      "junk.log.1",
      "junk.log.2",
    ];

    const int expectedCurSizeRollBackups = 0;
    VerifyInitializeRollBackupsFromBaseFile(baseFile, files, expectedCurSizeRollBackups);
  }

  /// <summary>
  /// Verifies that files are detected when the base file is specified
  /// </summary>
  private static void VerifyInitializeRollBackupsFromBaseFile(string baseFile)
  {
    List<string> files = MakeTestDataFromString(baseFile, "0,1,2");

    const int expectedCurSizeRollBackups = 2;
    VerifyInitializeRollBackupsFromBaseFile(baseFile, files, expectedCurSizeRollBackups);
  }

  /// <summary>
  /// Verifies that count goes to the highest when counting up
  /// </summary>
  [Test]
  public void TestInitializeCountUpFixed()
  {
    List<string> files = MakeTestDataFromString("3,4,5");
    const int expectedValue = 5;
    InitializeAndVerifyExpectedValue(files, FileName, CreateRollingFileAppender("3,0,1"), expectedValue);
  }

  /// <summary>
  /// Verifies that count goes to the highest when counting up
  /// </summary>
  [Test]
  public void TestInitializeCountUpFixed2()
  {
    List<string> files = MakeTestDataFromString("0,3");
    const int expectedValue = 3;
    InitializeAndVerifyExpectedValue(files, FileName, CreateRollingFileAppender("3,0,1"), expectedValue);
  }

  /// <summary>
  /// Verifies that count stays at 0 for the zero backups case
  /// when counting up
  /// </summary>
  [Test]
  public void TestInitializeCountUpZeroBackups()
  {
    List<string> files = MakeTestDataFromString("0,3");
    const int expectedValue = 0;
    InitializeAndVerifyExpectedValue(files, FileName, CreateRollingFileAppender("0,0,1"), expectedValue);
  }

  /// <summary>
  /// Verifies that count stays at 0 for the zero backups case
  /// when counting down
  /// </summary>
  [Test]
  public void TestInitializeCountDownZeroBackups()
  {
    List<string> files = MakeTestDataFromString("0,3");
    const int expectedValue = 0;
    InitializeAndVerifyExpectedValue(files, FileName, CreateRollingFileAppender("0,0,-1"), expectedValue);
  }


  /// <summary>
  /// Verifies that count goes to the highest when counting up
  /// </summary>
  [Test]
  public void TestInitializeCountDownFixed()
  {
    List<string> files = MakeTestDataFromString("4,5,6");
    VerifyInitializeDownFixedExpectedValue(files, FileName, 0);
  }

  /// <summary>
  /// Verifies that count goes to the highest when counting up
  /// </summary>
  [Test]
  public void TestInitializeCountDownFixed2()
  {
    List<string> files = MakeTestDataFromString("1,5,6");
    VerifyInitializeDownFixedExpectedValue(files, FileName, 1);
  }

  /// <summary>
  /// Verifies that count goes to the highest when counting up
  /// </summary>
  [Test]
  public void TestInitializeCountDownFixed3()
  {
    List<string> files = MakeTestDataFromString("2,5,6");
    VerifyInitializeDownFixedExpectedValue(files, FileName, 2);
  }

  /// <summary>
  /// Verifies that count goes to the highest when counting up
  /// </summary>
  [Test]
  public void TestInitializeCountDownFixed4()
  {
    List<string> files = MakeTestDataFromString("3,5,6");
    VerifyInitializeDownFixedExpectedValue(files, FileName, 3);
  }

  /// <summary>
  /// Verifies that count goes to the highest when counting up
  /// </summary>
  [Test]
  public void TestInitializeCountDownFixed5()
  {
    List<string> files = MakeTestDataFromString("1,2,3");
    VerifyInitializeDownFixedExpectedValue(files, FileName, 3);
  }

  /// <summary>
  /// Verifies that count goes to the highest when counting up
  /// </summary>
  [Test]
  public void TestInitializeCountDownFixed6()
  {
    List<string> files = MakeTestDataFromString("1,2");
    VerifyInitializeDownFixedExpectedValue(files, FileName, 2);
  }

  /// <summary>
  /// Verifies that count goes to the highest when counting up
  /// </summary>
  [Test]
  public void TestInitializeCountDownFixed7()
  {
    List<string> files = MakeTestDataFromString("2,3");
    VerifyInitializeDownFixedExpectedValue(files, FileName, 3);
  }

  private static void InitializeAndVerifyExpectedValue(List<string> files, string baseFile,
    RollingFileAppender rfa, int expectedValue)
  {
    rfa.InitializeRollBackups(baseFile, files);
    Assert.That(rfa.CurrentSizeRollBackups, Is.EqualTo(expectedValue));
  }

  /// <summary>
  /// Tests the count-down case, with infinite max backups, to see that
  /// initialization of the rolling file appender results in the expected value
  /// </summary>
  /// <param name="files"></param>
  /// <param name="baseFile"></param>
  /// <param name="expectedValue"></param>
  private static void VerifyInitializeDownInfiniteExpectedValue(List<string> files,
    string baseFile, int expectedValue)
    => InitializeAndVerifyExpectedValue(files, baseFile, CreateRollingFileAppender("-1,0,-1"), expectedValue);

  /// <summary>
  /// Creates a RollingFileAppender with the desired values, where the
  /// values are passed as a comma separated string, with 3 parameters,
  /// maxSizeRollBackups, curSizeRollBackups, CountDirection
  /// </summary>
  /// <returns></returns>
  private static RollingFileAppender CreateRollingFileAppender(string parameters)
  {
    string[] asParams = parameters.Split(',');
    if (asParams.Length != 3)
    {
      throw new ArgumentOutOfRangeException(parameters, parameters,
        "Must have 3 comma separated params: MaxSizeRollBackups, CurSizeRollBackups, CountDirection");
    }

    RollingFileAppender rfa = new()
    {
      RollingStyle = RollingFileAppender.RollingMode.Size,
      MaxSizeRollBackups = int.Parse(asParams[0].Trim()),
      CurrentSizeRollBackups = int.Parse(asParams[1].Trim()),
      CountDirection = int.Parse(asParams[2].Trim())
    };

    return rfa;
  }

  /// <summary>
  /// Verifies that count goes to the highest when counting down
  /// and infinite backups are selected
  /// </summary>
  [Test]
  public void TestInitializeCountDownInfinite()
  {
    List<string> files = MakeTestDataFromString("2,3");
    VerifyInitializeDownInfiniteExpectedValue(files, FileName, 3);
  }

  /// <summary>
  /// Verifies that count goes to the highest when counting down
  /// and infinite backups are selected
  /// </summary>
  [Test]
  public void TestInitializeCountDownInfinite2()
  {
    List<string> files = MakeTestDataFromString("2,3,4,5,6,7,8,9,10");
    VerifyInitializeDownInfiniteExpectedValue(files, FileName, 10);
  }

  /// <summary>
  /// Verifies that count goes to the highest when counting down
  /// and infinite backups are selected
  /// </summary>
  [Test]
  public void TestInitializeCountDownInfinite3()
  {
    List<string> files = MakeTestDataFromString("9,10,3,4,5,7,9,6,1,2,8");
    VerifyInitializeDownInfiniteExpectedValue(files, FileName, 10);
  }

  /// <summary>
  /// Verifies that count goes to the highest when counting up
  /// and infinite backups are selected
  /// </summary>
  [Test]
  public void TestInitializeCountUpInfinite()
  {
    List<string> files = MakeTestDataFromString("2,3");
    VerifyInitializeUpInfiniteExpectedValue(files, FileName, 3);
  }

  /// <summary>
  /// Verifies that count goes to the highest when counting up
  /// and infinite backups are selected
  /// </summary>
  [Test]
  public void TestInitializeCountUpInfinite2()
  {
    List<string> files = MakeTestDataFromString("2,3,4,5,6,7,8,9,10");
    VerifyInitializeUpInfiniteExpectedValue(files, FileName, 10);
  }

  /// <summary>
  /// Verifies that count goes to the highest when counting up
  /// and infinite backups are selected
  /// </summary>
  [Test]
  public void TestInitializeCountUpInfinite3()
  {
    List<string> files = MakeTestDataFromString("9,10,3,4,5,7,9,6,1,2,8");
    VerifyInitializeUpInfiniteExpectedValue(files, FileName, 10);
  }

  /// <summary>
  /// Creates a logger hierarchy, configures a rolling file appender and returns an ILogger
  /// </summary>
  /// <param name="filename">The filename to log to</param>
  /// <param name="lockModel">The locking model to use.</param>
  /// <param name="handler">The error handler to use.</param>
  /// <param name="maxFileSize">Maximum file size for roll</param>
  /// <param name="maxSizeRollBackups">Maximum number of roll backups</param>
  /// <returns>A configured ILogger</returns>
  private static ILogger CreateLogger(string filename, FileAppender.LockingModelBase? lockModel,
    IErrorHandler handler, int maxFileSize = 100000, int maxSizeRollBackups = 0)
  {
    Repository.Hierarchy.Hierarchy h =
      (Repository.Hierarchy.Hierarchy)LogManager.CreateRepository("TestRepository");

    RollingFileAppender appender = new()
    {
      File = filename,
      AppendToFile = false,
      CountDirection = 0,
      RollingStyle = RollingFileAppender.RollingMode.Size,
      MaxFileSize = maxFileSize,
      Encoding = Encoding.ASCII,
      ErrorHandler = handler,
      MaxSizeRollBackups = maxSizeRollBackups
    };
    if (lockModel is not null)
    {
      appender.LockingModel = lockModel;
    }

    PatternLayout layout = new()
    {
      ConversionPattern = "%m%n"
    };
    layout.ActivateOptions();

    appender.Layout = layout;
    appender.ActivateOptions();

    h.Root.AddAppender(appender);
    h.Configured = true;

    ILogger log = h.GetLogger("Logger");
    return log;
  }

  /// <summary>
  /// Destroys the logger hierarchy created by <see cref="CreateLogger"/>
  /// </summary>
  private static void DestroyLogger()
  {
    Repository.Hierarchy.Hierarchy h =
        (Repository.Hierarchy.Hierarchy)LogManager.GetRepository("TestRepository");
    h.ResetConfiguration();
    //Replace the repository selector so that we can recreate the hierarchy with the same name if necessary
    LoggerManager.RepositorySelector =
      new DefaultRepositorySelector(typeof(Repository.Hierarchy.Hierarchy));
  }

  private static void AssertFileEquals(string filename, string contents)
  {
    string logContent = File.ReadAllText(filename);
    Assert.That(logContent, Is.EqualTo(contents), "Log contents is not what is expected");
    File.Delete(filename);
  }

  /// <summary>
  /// Verifies that logging a message actually produces output
  /// </summary>
  [Test]
  public void TestLogOutput()
  {
    Utils.InconclusiveOnMono();
    const string filename = "test_simple.log";
    SilentErrorHandler sh = new();
    ILogger log = CreateLogger(filename, new FileAppender.ExclusiveLock(), sh);
    log.Log(GetType(), Level.Info, "This is a message", null);
    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();

    AssertFileEquals(filename,
      "This is a message" + Environment.NewLine + "This is a message 2" + Environment.NewLine);
    Assert.That(sh.Message, Is.EqualTo(""), "Unexpected error message");
  }

  /// <summary>
  /// Verifies that attempting to log to a locked file fails gracefully
  /// </summary>
  [Test]
  public void TestExclusiveLockFails()
  {
    const string filename = "test_exclusive_lock_fails.log";

    FileStream fs = new(filename, FileMode.Create, FileAccess.Write, FileShare.None);
    fs.Write("Test"u8.ToArray(), 0, 4);

    SilentErrorHandler sh = new();
    ILogger log = CreateLogger(filename, new FileAppender.ExclusiveLock(), sh);
    log.Log(GetType(), Level.Info, "This is a message", null);
    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();
    fs.Close();

    AssertFileEquals(filename, "Test");
    Assert.That(sh.Message, Does.StartWith("Unable to acquire lock on file"), "Expecting an error message");
  }

  /// <summary>
  /// Verifies that attempting to log to a locked file recovers if the lock is released
  /// </summary>
  [Test]
  public void TestExclusiveLockRecovers()
  {
    Utils.InconclusiveOnMono();
    const string filename = "test_exclusive_lock_recovers.log";

    FileStream fs = new(filename, FileMode.Create, FileAccess.Write, FileShare.None);
    fs.Write("Test"u8.ToArray(), 0, 4);

    SilentErrorHandler sh = new();
    ILogger log = CreateLogger(filename, new FileAppender.ExclusiveLock(), sh);
    log.Log(GetType(), Level.Info, "This is a message", null);
    fs.Close();
    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();

    AssertFileEquals(filename, "This is a message 2" + Environment.NewLine);
    Assert.That(sh.Message, Does.StartWith("Unable to acquire lock on file"),
      "Expecting an error message");
  }

  /// <summary>
  /// Verifies that attempting to log to a file with ExclusiveLock really locks the file
  /// </summary>
  [Test]
  public void TestExclusiveLockLocks()
  {
    Utils.InconclusiveOnMono();
    const string filename = "test_exclusive_lock_locks.log";
    bool locked = false;

    SilentErrorHandler sh = new();
    ILogger log = CreateLogger(filename, new FileAppender.ExclusiveLock(), sh);
    log.Log(GetType(), Level.Info, "This is a message", null);

    try
    {
      FileStream fs = new(filename, FileMode.Create, FileAccess.Write, FileShare.None);
      fs.Write("Test"u8.ToArray(), 0, 4);
      fs.Close();
    }
    catch (IOException e1)
    {
      Assert.That(e1.Message.Substring(0, 35), Is.EqualTo("The process cannot access the file "),
      "Unexpected exception");
      locked = true;
    }

    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();

    Assert.That(locked, "File was not locked");
    AssertFileEquals(filename,
      "This is a message" + Environment.NewLine + "This is a message 2" + Environment.NewLine);
    Assert.That(sh.Message, Is.EqualTo(""), "Unexpected error message");
  }


  /// <summary>
  /// Verifies that attempting to log to a locked file fails gracefully
  /// </summary>
  [Test]
  public void TestMinimalLockFails()
  {
    Utils.InconclusiveOnMono();
    const string filename = "test_minimal_lock_fails.log";

    FileStream fs = new(filename, FileMode.Create, FileAccess.Write, FileShare.None);
    fs.Write("Test"u8.ToArray(), 0, 4);

    SilentErrorHandler sh = new();
    ILogger log = CreateLogger(filename, new FileAppender.MinimalLock(), sh);
    log.Log(GetType(), Level.Info, "This is a message", null);
    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();
    fs.Close();

    AssertFileEquals(filename, "Test");
    Assert.That(sh.Message.Substring(0, 30), Is.EqualTo("Unable to acquire lock on file"),
        "Expecting an error message");
  }

  /// <summary>
  /// Verifies that attempting to log to a locked file recovers if the lock is released
  /// </summary>
  [Test]
  public void TestMinimalLockRecovers()
  {
    Utils.InconclusiveOnMono();
    const string filename = "test_minimal_lock_recovers.log";

    FileStream fs = new(filename, FileMode.Create, FileAccess.Write, FileShare.None);
    fs.Write("Test"u8.ToArray(), 0, 4);

    SilentErrorHandler sh = new();
    ILogger log = CreateLogger(filename, new FileAppender.MinimalLock(), sh);
    log.Log(GetType(), Level.Info, "This is a message", null);
    fs.Close();
    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();

    AssertFileEquals(filename, "This is a message 2" + Environment.NewLine);
    Assert.That(sh.Message.Substring(0, 30), Is.EqualTo("Unable to acquire lock on file"),
      "Expecting an error message");
  }

  /// <summary>
  /// Verifies that attempting to log to a file with MinimalLock doesn't lock the file
  /// </summary>
  [Test]
  public void TestMinimalLockUnlocks()
  {
    Utils.InconclusiveOnMono();
    const string filename = "test_minimal_lock_unlocks.log";

    SilentErrorHandler sh = new();
    ILogger log = CreateLogger(filename, new FileAppender.MinimalLock(), sh);
    log.Log(GetType(), Level.Info, "This is a message", null);

    FileStream fs = new(filename, FileMode.Append, FileAccess.Write, FileShare.None);
    fs.Write(Encoding.ASCII.GetBytes("Test" + Environment.NewLine), 0, 4 + Environment.NewLine.Length);
    fs.Close();

    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();

    AssertFileEquals(filename,
      "This is a message" + Environment.NewLine + "Test" + Environment.NewLine + "This is a message 2" +
      Environment.NewLine);
    Assert.That(sh.Message, Is.EqualTo(""), "Unexpected error message");
  }

  /// <summary>
  /// Verifies that attempting to log to a locked file fails gracefully
  /// </summary>
  [Test]
  public void TestInterProcessLockFails()
  {
    Utils.InconclusiveOnMono();
    const string filename = "test_interprocess_lock_fails.log";

    FileStream fs = new(filename, FileMode.Create, FileAccess.Write, FileShare.None);
    fs.Write("Test"u8.ToArray(), 0, 4);

    SilentErrorHandler sh = new();
    ILogger log = CreateLogger(filename, new FileAppender.InterProcessLock(), sh);
    log.Log(GetType(), Level.Info, "This is a message", null);
    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();
    fs.Close();

    AssertFileEquals(filename, "Test");
    Assert.That(sh.Message.Substring(0, 30), Is.EqualTo("Unable to acquire lock on file"),
        "Expecting an error message");
  }

  /// <summary>
  /// Verifies that attempting to log to a locked file recovers if the lock is released
  /// </summary>
  [Test]
  public void TestInterProcessLockRecovers()
  {
    Utils.InconclusiveOnMono();
    const string filename = "test_interprocess_lock_recovers.log";

    FileStream fs = new(filename, FileMode.Create, FileAccess.Write, FileShare.None);
    fs.Write("Test"u8.ToArray(), 0, 4);

    SilentErrorHandler sh = new();
    ILogger log = CreateLogger(filename, new FileAppender.InterProcessLock(), sh);
    log.Log(GetType(), Level.Info, "This is a message", null);
    fs.Close();
    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();

    AssertFileEquals(filename, "This is a message 2" + Environment.NewLine);
    Assert.That(sh.Message.Substring(0, 30), Is.EqualTo("Unable to acquire lock on file"),
      "Expecting an error message");
  }

  /// <summary>
  /// Verifies that attempting to log to a file with InterProcessLock really locks the file
  /// </summary>
  [Test]
  public void TestInterProcessLockUnlocks()
  {
    Utils.InconclusiveOnMono();
    const string filename = "test_interprocess_lock_unlocks.log";

    SilentErrorHandler sh = new();
    ILogger log = CreateLogger(filename, new FileAppender.InterProcessLock(), sh);
    log.Log(GetType(), Level.Info, "This is a message", null);

    FileStream fs = new(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
    fs.Write(Encoding.ASCII.GetBytes("Test" + Environment.NewLine), 0, 4 + Environment.NewLine.Length);
    fs.Close();

    log.Log(GetType(), Level.Info, "This is a message 2", null);
    DestroyLogger();

    AssertFileEquals(filename,
      "This is a message" + Environment.NewLine + "Test" + Environment.NewLine + "This is a message 2" +
      Environment.NewLine);
    Assert.That(sh.Message, Is.EqualTo(""), "Unexpected error message");
  }

  /// <summary>
  /// Verifies that rolling file works
  /// </summary>
  [Test]
  public void TestInterProcessLockRoll()
  {
    Utils.InconclusiveOnMono();
    const string filename = "test_interprocess_lock_roll.log";

    SilentErrorHandler sh = new();
    ILogger log = CreateLogger(filename, new FileAppender.InterProcessLock(), sh, 1, 2);

    Assert.DoesNotThrow(() => log.Log(GetType(), Level.Info, "A", null));
    Assert.DoesNotThrow(() => log.Log(GetType(), Level.Info, "A", null));

    DestroyLogger();

    AssertFileEquals(filename, "A" + Environment.NewLine);
    AssertFileEquals(filename + ".1", "A" + Environment.NewLine);
    Assert.That(sh.Message, Is.Empty);
  }

  /// <summary>
  /// Verify that the default LockModel is ExclusiveLock, to maintain backwards compatibility with previous behaviour
  /// </summary>
  [Test]
  public void TestDefaultLockingModel()
  {
    const string filename = "test_default.log";

    SilentErrorHandler sh = new();
    ILogger log = CreateLogger(filename, null, sh);

    IAppender[]? appenders = log.Repository?.GetAppenders();
    Assert.That(appenders, Is.Not.Null);
    Assert.That(appenders!, Has.Length.EqualTo(1), "The wrong number of appenders are configured");

    RollingFileAppender rfa = (RollingFileAppender)(appenders[0]);
    Assert.That(rfa.LockingModel.GetType(), Is.EqualTo(typeof(FileAppender.ExclusiveLock)),
        "The LockingModel is of an unexpected type");

    DestroyLogger();
  }

  /// <summary>
  /// Tests the count up case, with infinite max backups , to see that
  /// initialization of the rolling file appender results in the expected value
  /// </summary>
  private static void VerifyInitializeUpInfiniteExpectedValue(List<string> files,
    string baseFile, int expectedValue)
    => InitializeAndVerifyExpectedValue(files, baseFile, CreateRollingFileAppender("-1,0,1"), expectedValue);


  /// <summary>
  /// Tests the countdown case, with max backups limited to 3, to see that
  /// initialization of the rolling file appender results in the expected value
  /// </summary>
  private static void VerifyInitializeDownFixedExpectedValue(List<string> files, string baseFile,
    int expectedValue)
    => InitializeAndVerifyExpectedValue(files, baseFile, CreateRollingFileAppender("3,0,-1"), expectedValue);

  /// <summary>
  /// Turns a string of comma separated numbers into a collection of filenames
  /// generated from the numbers.  
  /// 
  /// Defaults to filename in _fileName variable.
  /// 
  /// </summary>
  /// <param name="fileNumbers">Comma separated list of numbers for counted file names</param>
  private List<string> MakeTestDataFromString(string fileNumbers)
    => MakeTestDataFromString(FileName, fileNumbers);

  /// <summary>
  /// Turns a string of comma separated numbers into a collection of filenames
  /// generated from the numbers
  /// 
  /// Uses the input filename.
  /// </summary>
  /// <param name="sFileName">Name of file to combine with numbers when generating counted file names</param>
  /// <param name="fileNumbers">Comma separated list of numbers for counted file names</param>
  /// <returns></returns>
  private static List<string> MakeTestDataFromString(string sFileName, string fileNumbers) 
    => fileNumbers.Split(',').Select(number => int.Parse(number.Trim()))
      .Select(value => MakeFileName(sFileName, value)).ToList();

  /// <summary>
  /// Tests that the current backup index is correctly detected
  /// for a file with no extension
  /// </summary>
  [Test]
  public void TestInitializeRollBackups2() => VerifyInitializeRollBackupsFromBaseFile("LogFile");

  /// <summary>
  /// Tests that the current backup index is correctly detected
  /// for a file with a .log extension
  /// </summary>
  [Test]
  public void TestInitializeRollBackups3() => VerifyInitializeRollBackupsFromBaseFile("LogFile.log");

  /// <summary>
  /// Makes sure that the initialization can detect the backup
  /// number correctly.
  /// </summary>
  private static void VerifyInitializeRollBackups(int backups, int maxSizeRollBackups)
  {
    const string baseFile = "LogFile.log";
    List<string> arrFiles = ["junk1"];
    for (int i = 0; i < backups; i++)
    {
      arrFiles.Add(MakeFileName(baseFile, i));
    }

    RollingFileAppender rfa = new()
    {
      RollingStyle = RollingFileAppender.RollingMode.Size,
      MaxSizeRollBackups = maxSizeRollBackups,
      CurrentSizeRollBackups = 0
    };
    rfa.InitializeRollBackups(baseFile, arrFiles);

    // iBackups  / Meaning
    // 0 = none
    // 1 = file.log
    // 2 = file.log.1
    // 3 = file.log.2
    Assert.That(rfa.CurrentSizeRollBackups, backups is 0 or 1 
      ? Is.EqualTo(0) 
      : Is.EqualTo(Math.Min(backups - 1, maxSizeRollBackups)));
  }

  /// <summary>
  /// Tests that the current backup index is correctly detected,
  /// and gets no bigger than the max backups setting
  /// </summary>
  [Test]
  public void TestInitializeRollBackups4()
  {
    const int maxRollBackups = 5;
    VerifyInitializeRollBackups(0, maxRollBackups);
    VerifyInitializeRollBackups(1, maxRollBackups);
    VerifyInitializeRollBackups(2, maxRollBackups);
    VerifyInitializeRollBackups(3, maxRollBackups);
    VerifyInitializeRollBackups(4, maxRollBackups);
    VerifyInitializeRollBackups(5, maxRollBackups);
    VerifyInitializeRollBackups(6, maxRollBackups);
    // Final we cap out at the max value
    VerifyInitializeRollBackups(7, maxRollBackups);
    VerifyInitializeRollBackups(8, maxRollBackups);
  }

  /// <summary>
  /// Ensures that no problems result from creating and then closing the appender
  /// when it has not also been initialized with ActivateOptions().
  /// </summary>
  [Test]
  public void TestCreateCloseNoActivateOptions() => new RollingFileAppender().Close();

  //
  // Helper functions to dig into the appender
  //

  private static List<string> GetExistingFiles(string baseFilePath, bool preserveLogFileNameExtension = false)
  {
    RollingFileAppenderForTest appender = new()
    {
      PreserveLogFileNameExtension = preserveLogFileNameExtension,
      SecurityContext = NullSecurityContext.Instance
    };
    return appender.GetExistingFiles(baseFilePath);
  }

  private static string GetTestMessage() => Environment.NewLine.Length switch
  {
    2 => TestMessage98Chars,
    1 => TestMessage99Chars,
    _ => throw new InvalidOperationException("Unexpected Environment.NewLine.Length"),
  };
}

[TestFixture]
public sealed class RollingFileAppenderSubClassTest : RollingFileAppender
{
  [Test]
  public void TestComputeCheckPeriod()
  {
    Assert.That(ComputeCheckPeriod(".yyyy-MM-dd HH:mm"), Is.EqualTo(RollPoint.TopOfMinute), "TopOfMinute pattern");
    Assert.That(ComputeCheckPeriod(".yyyy-MM-dd HH"), Is.EqualTo(RollPoint.TopOfHour), "TopOfHour pattern");
    Assert.That(ComputeCheckPeriod(".yyyy-MM-dd tt"), Is.EqualTo(RollPoint.HalfDay), "HalfDay pattern");
    Assert.That(ComputeCheckPeriod(".yyyy-MM-dd"), Is.EqualTo(RollPoint.TopOfDay), "TopOfDay pattern");
    Assert.That(ComputeCheckPeriod(".yyyy-MM"), Is.EqualTo(RollPoint.TopOfMonth), "TopOfMonth pattern");

    // Test invalid roll point
    Assert.That(ComputeCheckPeriod("..."), Is.EqualTo(RollPoint.InvalidRollPoint), "TopOfMonth pattern");
  }
}