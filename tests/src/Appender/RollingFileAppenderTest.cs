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
using System.Collections;
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

namespace log4net.Tests.Appender
{
	/// <summary>
	/// Used for internal unit testing the <see cref="RollingFileAppender"/> class.
	/// </summary>
	[TestFixture]
	public class RollingFileAppenderTest
	{
		private const string c_fileName = "test_41d3d834_4320f4da.log";
		private const string c_testMessage98Chars = "01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567";
		private const string c_testMessage99Chars = "012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678";
		private const int c_iMaximumFileSize = 450; // in bytes
		private int _iMessagesLoggedThisFile = 0;
		private int _iMessagesLogged = 0;
		private int _iCountDirection = 0;
		private int _MaxSizeRollBackups = 3;
		private CountingAppender _caRoot;
		private Logger _root;
		private CultureInfo _currentCulture;
		private CultureInfo _currentUICulture;

		private class SilentErrorHandler : IErrorHandler
		{
			private StringBuilder m_buffer = new StringBuilder();

			public string Message
			{
				get { return m_buffer.ToString(); }
			}

			public void Error(string message)
			{
				m_buffer.Append(message + "\n");
			}

			public void Error(string message, Exception e)
			{
				m_buffer.Append(message + "\n" + e.Message + "\n");
			}

			public void Error(string message, Exception e, ErrorCode errorCode)
			{
				m_buffer.Append(message + "\n" + e.Message + "\n");
			}
		}

		/// <summary>
		/// Sets up variables used for the tests
		/// </summary>
		private void InitializeVariables()
		{
			_iMessagesLoggedThisFile = 0;
			_iMessagesLogged = 0;
			_iCountDirection = +1; // Up
			_MaxSizeRollBackups = 3;
		}

		/// <summary>
		/// Shuts down any loggers in the hierarchy, along
		/// with all appenders, and deletes any test files used
		/// for logging.
		/// </summary>
		private static void ResetAndDeleteTestFiles()
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
			_currentUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
		}

		/// <summary>
		/// Any steps that happen after each test go here
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			ResetAndDeleteTestFiles();
			
			// restore previous culture
			System.Threading.Thread.CurrentThread.CurrentCulture = _currentCulture;
			System.Threading.Thread.CurrentThread.CurrentUICulture = _currentUICulture;
		}

		/// <summary>
		/// Finds the number of files that match the base file name,
		/// and matches the result against an expected count
		/// </summary>
		/// <param name="iExpectedCount"></param>
		private static void VerifyFileCount(int iExpectedCount)
		{
			ArrayList alFiles = GetExistingFiles(c_fileName);
			Assert.IsNotNull(alFiles);
			Assert.AreEqual(iExpectedCount, alFiles.Count);
		}

		/// <summary>
		/// Creates a file with the given number, and the shared base file name
		/// </summary>
		/// <param name="iFileNumber"></param>
		private static void CreateFile(int iFileNumber)
		{
			FileInfo fileInfo = new FileInfo(MakeFileName(c_fileName, iFileNumber));

			FileStream fileStream = null;
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
					catch
					{
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

		/// <summary>
		/// Removes all test files that exist
		/// </summary>
		private static void DeleteTestFiles()
		{
			ArrayList alFiles = GetExistingFiles(c_fileName);
			foreach(string sFile in alFiles)
			{
				try
				{
					Debug.WriteLine("Deleting test file " + sFile);
					File.Delete(sFile);
				}
				catch(Exception ex)
				{
					Debug.WriteLine("Exception while deleting test file " + ex);
				}
			}
		}

		///// <summary>
		///// Generates a file name associated with the count.
		///// </summary>
		///// <param name="iFileCount"></param>
		///// <returns></returns>
		//private string MakeFileName(int iFileCount)
		//{
		//    return MakeFileName(_fileName, iFileCount);
		//}

		/// <summary>
		/// Generates a file name associated with the count, using
		/// the base file name.
		/// </summary>
		/// <param name="sBaseFile"></param>
		/// <param name="iFileCount"></param>
		/// <returns></returns>
		private static string MakeFileName(string sBaseFile, int iFileCount)
		{
			if (0 == iFileCount)
			{
				return sBaseFile;
			}
			return sBaseFile + "." + iFileCount;
		}

		/// <summary>
		/// Returns a RollingFileAppender using all the internal settings for maximum
		/// file size and number of backups
		/// </summary>
		/// <returns></returns>
		private RollingFileAppender CreateAppender()
		{
			return CreateAppender(new FileAppender.ExclusiveLock());
		}

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
			PatternLayout layout = new PatternLayout("%m%n");

			//
			// Create the new appender
			//
			RollingFileAppender appender = new RollingFileAppender();
			appender.Layout = layout;
			appender.File = c_fileName;
                        appender.Encoding = Encoding.ASCII;
			appender.MaximumFileSize = c_iMaximumFileSize.ToString();
			appender.MaxSizeRollBackups = _MaxSizeRollBackups;
			appender.CountDirection = _iCountDirection;
			appender.RollingStyle = RollingFileAppender.RollingMode.Size;
			appender.LockingModel = lockModel;

			appender.ActivateOptions();

			return appender;
		}

		/// <summary>
		/// Used for test purposes, a table of these objects can be used to identify
		/// any existing files and their expected length.
		/// </summary>
		public class RollFileEntry
		{
			/// <summary>
			/// Stores the name of the file
			/// </summary>
			private string m_fileName;

			/// <summary>
			/// The expected length of the file
			/// </summary>
			private long m_fileLength;

			/// <summary>
			/// Default constructor
			/// </summary>
			public RollFileEntry()
			{
			}

			/// <summary>
			/// Constructor used when the fileInfo and expected length are known
			/// </summary>
			/// <param name="fileName"></param>
			/// <param name="fileLength"></param>
			public RollFileEntry(string fileName, long fileLength)
			{
				m_fileName = fileName;
				m_fileLength = fileLength;
			}

			/// <summary>
			/// Stores the name of the file
			/// </summary>
			public string FileName
			{
				get { return m_fileName; }
			}

			/// <summary>
			/// The expected length of the file
			/// </summary>
			public long FileLength
			{
				get { return m_fileLength; }
			}
		}

		/// <summary>
		/// Used for table-driven testing.  This class holds information that can be used
		/// for testing of file rolling.
		/// </summary>
		public class RollConditions
		{
			/// <summary>
			/// A table of entries showing files that should exist and their expected sizes
			/// before logging is called
			/// </summary>
			private RollFileEntry[] m_preLogFileEntries;

			/// <summary>
			/// A table of entries showing files that should exist and their expected sizes
			/// after a message is logged
			/// </summary>
			private RollFileEntry[] m_postLogFileEntries;

			/// <summary>
			/// Constructor, taking all required parameters
			/// </summary>
			/// <param name="preLogFileEntries"></param>
			/// <param name="postLogFileEntries"></param>
			public RollConditions(RollFileEntry[] preLogFileEntries, RollFileEntry[] postLogFileEntries)
			{
				m_preLogFileEntries = preLogFileEntries;
				m_postLogFileEntries = postLogFileEntries;
			}

			/// <summary>
			/// A table of entries showing files that should exist and their expected sizes
			/// before logging is called
			/// </summary>
			public RollFileEntry[] GetPreLogFileEntries()
			{
				return m_preLogFileEntries;
			}

			/// <summary>
			/// A table of entries showing files that should exist and their expected sizes
			/// after a message is logged
			/// </summary>
			public RollFileEntry[] GetPostLogFileEntries()
			{
				return m_postLogFileEntries;
			}
		}

		private static void VerifyExistenceAndRemoveFromList(ArrayList alExisting, string sFileName, FileInfo file, RollFileEntry entry)
		{
			Assert.IsTrue(alExisting.Contains(sFileName), "filename {0} not found in test directory", sFileName);
			Assert.AreEqual(entry.FileLength, file.Length, "file length mismatch");
			// Remove this file from the list
			alExisting.Remove(sFileName);
		}

		/// <summary>
		/// Checks that all the expected files exist, and only the expected files.  Also
		/// verifies the length of all files against the expected length
		/// </summary>
		/// <param name="sBaseFileName"></param>
		/// <param name="fileEntries"></param>
		private static void VerifyFileConditions(string sBaseFileName, RollFileEntry[] fileEntries)
		{
			ArrayList alExisting = GetExistingFiles(sBaseFileName);
			if (null != fileEntries)
			{
				//					AssertEquals( "File count mismatch", alExisting.Count, fileEntries.Length );
				foreach(RollFileEntry rollFile in fileEntries)
				{
					string sFileName = rollFile.FileName;
					FileInfo file = new FileInfo(sFileName);

					if (rollFile.FileLength > 0)
					{
						Assert.IsTrue(file.Exists, "filename {0} does not exist", sFileName);
						VerifyExistenceAndRemoveFromList(alExisting, sFileName, file, rollFile);
					}
					else
					{
						// If length is 0, file may not exist yet.  If file exists, make sure length
						// is zero.  If file doesn't exist, this is OK

						if (file.Exists)
						{
							VerifyExistenceAndRemoveFromList(alExisting, sFileName, file, rollFile);
						}
					}
				}
			}
			else
			{
				Assert.AreEqual(0, alExisting.Count);
			}

			// This check ensures no extra files matching the wildcard pattern exist.
			// We only want the files we expect, and no others
			Assert.AreEqual(0, alExisting.Count);
		}

		/// <summary>
		/// Called before logging a message to check that all the expected files exist, 
		/// and only the expected files.  Also verifies the length of all files against 
		/// the expected length
		/// </summary>
		/// <param name="sBaseFileName"></param>
		/// <param name="entry"></param>
		private static void VerifyPreConditions(string sBaseFileName, RollConditions entry)
		{
			VerifyFileConditions(sBaseFileName, entry.GetPreLogFileEntries());
		}

		/// <summary>
		/// Called after logging a message to check that all the expected files exist, 
		/// and only the expected files.  Also verifies the length of all files against 
		/// the expected length
		/// </summary>
		/// <param name="sBaseFileName"></param>
		/// <param name="entry"></param>
		private static void VerifyPostConditions(string sBaseFileName, RollConditions entry)
		{
			VerifyFileConditions(sBaseFileName, entry.GetPostLogFileEntries());
		}

		/// <summary>
		/// Logs a message, verifying the expected message counts against the 
		/// current running totals.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="sMessageToLog"></param>
		private void LogMessage(RollConditions entry, string sMessageToLog)
		{
			Assert.AreEqual(_caRoot.Counter, _iMessagesLogged++);
			_root.Log(Level.Debug, sMessageToLog, null);
			Assert.AreEqual(_caRoot.Counter, _iMessagesLogged);
			_iMessagesLoggedThisFile++;
		}

		//private void DumpFileEntry( RollFileEntry entry )
		//{
		//    System.Diagnostics.Debug.WriteLine( "\tfile   name: " + entry.FileName );
		//    System.Diagnostics.Debug.WriteLine( "\tfile length: " + entry.FileLength );
		//}

		//private void DumpTableEntry( RollConditions entry )
		//{
		//    System.Diagnostics.Debug.WriteLine( "Pre-Conditions" );
		//    foreach( RollFileEntry file in entry.GetPreLogFileEntries() )
		//    {
		//        DumpFileEntry( file );
		//    }
		//    System.Diagnostics.Debug.WriteLine( "Post-Conditions" );
		//    foreach( RollFileEntry file in entry.GetPostLogFileEntries() )
		//    {
		//        DumpFileEntry( file );
		//    }
		//    //				System.Diagnostics.Debug.WriteLine("");
		//}

		/// <summary>
		/// Runs through all table entries, logging messages.  Before each message is logged,
		/// pre-conditions are checked to ensure the expected files exist and they are the
		/// expected size.  After logging, verifies the same.
		/// </summary>
		/// <param name="sBaseFileName"></param>
		/// <param name="entries"></param>
		/// <param name="sMessageToLog"></param>
		private void RollFromTableEntries(string sBaseFileName, RollConditions[] entries, string sMessageToLog)
		{
			for(int i = 0; i < entries.Length; i++)
			{
				RollConditions entry = entries[i];

				//					System.Diagnostics.Debug.WriteLine( i + ": Entry " + i + " pre/post conditions");
				//					DumpTableEntry( entry );
				//					System.Diagnostics.Debug.WriteLine( i + ": Testing entry pre-conditions");
				VerifyPreConditions(sBaseFileName, entry);
				//					System.Diagnostics.Debug.WriteLine( i + ": Logging message");
				LogMessage(entry, sMessageToLog);
				//					System.Diagnostics.Debug.WriteLine( i + ": Testing entry post-conditions");
				VerifyPostConditions(sBaseFileName, entry);
				//					System.Diagnostics.Debug.WriteLine( i + ": Finished validating entry\n");
			}
		}

        private static readonly int s_Newline_Length = Environment.NewLine.Length;

		/// <summary>
		/// Returns the number of bytes logged per message, including
		/// any newline characters in addition to the message length.
		/// </summary>
		/// <param name="sMessage"></param>
		/// <returns></returns>
		private static int TotalMessageLength(string sMessage)
		{
            return sMessage.Length + s_Newline_Length;
		}

		/// <summary>
		/// Determines how many messages of a fixed length can be logged
		/// to a single file before the file rolls.
		/// </summary>
		/// <param name="iMessageLength"></param>
		/// <returns></returns>
		private static int MessagesPerFile(int iMessageLength)
		{
			int iMessagesPerFile = c_iMaximumFileSize / iMessageLength;

			//
			// RollingFileAppender checks for wrap BEFORE logging,
			// so we will actually get one more message per file than
			// we would otherwise.
			//
			if (iMessagesPerFile * iMessageLength < c_iMaximumFileSize)
			{
				iMessagesPerFile++;
			}

			return iMessagesPerFile;
		}

		/// <summary>
		/// Determines the name of the current file
		/// </summary>
		/// <returns></returns>
		private static string GetCurrentFile()
		{
			// Current file name is always the base file name when
			// counting.  Dates will need a different approach
			return c_fileName;
		}

		/// <summary>
		/// Turns a group of file names into an array of file entries that include the name
		/// and a size.  This is useful for assigning the properties of backup files, when
		/// the length of the files are all the same size due to a fixed message length.
		/// </summary>
		/// <param name="sBackupGroup"></param>
		/// <param name="iBackupFileLength"></param>
		/// <returns></returns>
		private static RollFileEntry[] MakeBackupFileEntriesFromBackupGroup(string sBackupGroup, int iBackupFileLength)
		{
			string[] sFiles = sBackupGroup.Split(' ');

			ArrayList alEntries = new ArrayList();

			for(int i = 0; i < sFiles.Length; i++)
			{
				// Weed out any whitespace entries from the array
				if (sFiles[i].Trim().Length > 0)
				{
					alEntries.Add(new RollFileEntry(sFiles[i], iBackupFileLength));
				}
			}

			return (RollFileEntry[])alEntries.ToArray(typeof(RollFileEntry));
		}

		/// <summary>
		/// Finds the iGroup group in the string (comma separated groups)
		/// </summary>
		/// <param name="sBackupGroups"></param>
		/// <param name="iGroup"></param>
		/// <returns></returns>
		private static string GetBackupGroup(string sBackupGroups, int iGroup)
		{
			string[] sGroups = sBackupGroups.Split(',');
			return sGroups[iGroup];
		}

		///// <summary>
		///// Builds a collection of file entries based on the file names
		///// specified in a groups string and the max file size from the
		///// stats object
		///// </summary>
		///// <param name="sBackupGroups"></param>
		///// <param name="stats"></param>
		///// <returns></returns>
		//private RollFileEntry[] MakeBackupFileEntriesForPreCondition( string sBackupGroups, RollingStats stats )
		//{
		//    if (0 == stats.NumberOfFileRolls )
		//    {
		//        return null;	// first round has no previous backups
		//    }
		//    string sGroup;
		//    if (0 == stats.MessagesThisFile )
		//    {
		//        // first file has special pattern...since rolling doesn't occur when message
		//        // is logged, rather before next message is logged.
		//        if (stats.NumberOfFileRolls <= 1 )
		//        {
		//            return null;   
		//        }
		//        // Use backup files from previous round.  The minus 2 is because we have already
		//        // rolled, and the first round uses null instead of the string
		//        sGroup = GetBackupGroup( sBackupGroups, stats.NumberOfFileRolls-2 );
		//    }
		//    else
		//    {
		//        sGroup = GetBackupGroup( sBackupGroups, stats.NumberOfFileRolls-1 );
		//    }
		//    return MakeBackupFileEntriesFromBackupGroup( sGroup, stats.MaximumFileSize );
		//}

		/// <summary>
		/// Builds a collection of file entries based on the file names
		/// specified in a groups string and the max file size from the
		/// stats object
		/// </summary>
		/// <param name="sBackupGroups"></param>
		/// <param name="stats"></param>
		/// <returns></returns>
		private static RollFileEntry[] MakeBackupFileEntriesForPostCondition(string sBackupGroups, RollingStats stats)
		{
			if (0 == stats.NumberOfFileRolls)
			{
				return null; // first round has no previous backups
			}
			string sGroup = GetBackupGroup(sBackupGroups, stats.NumberOfFileRolls - 1);
			return MakeBackupFileEntriesFromBackupGroup(sGroup, stats.MaximumFileSize);
		}


		/// <summary>
		/// This class holds information that is used while we are generating
		/// test data sets
		/// </summary>
		public class RollingStats
		{
			private int iTotalMessageLength;
			private int iMessagesPerFile;
			private int iMessagesThisFile;
			private int iNumberOfFileRolls;

			/// <summary>
			/// Number of total bytes a log file can reach.
			/// </summary>
			public int MaximumFileSize
			{
				get { return TotalMessageLength * MessagesPerFile; }
			}

			/// <summary>
			/// The length of a message, including any CR/LF characters.
			/// This length assumes all messages are a fixed length for
			/// test purposes.
			/// </summary>
			public int TotalMessageLength
			{
				get { return iTotalMessageLength; }
				set { iTotalMessageLength = value; }
			}

			/// <summary>
			/// A count of the number of messages that are logged to each
			/// file.
			/// </summary>
			public int MessagesPerFile
			{
				get { return iMessagesPerFile; }
				set { iMessagesPerFile = value; }
			}

			/// <summary>
			/// Counts how many messages have been logged to the current file
			/// </summary>
			public int MessagesThisFile
			{
				get { return iMessagesThisFile; }
				set { iMessagesThisFile = value; }
			}

			/// <summary>
			/// Counts how many times a file roll has occurred
			/// </summary>
			public int NumberOfFileRolls
			{
				get { return iNumberOfFileRolls; }
				set { iNumberOfFileRolls = value; }
			}
		}

		/// <summary>
		/// The stats are used to keep track of progress while we are algorithmically
		/// generating a table of pre/post condition tests for file rolling.
		/// </summary>
		/// <param name="sTestMessage"></param>
		/// <returns></returns>
		private static RollingStats InitializeStats(string sTestMessage)
		{
			RollingStats rollingStats = new RollingStats();

			rollingStats.TotalMessageLength = TotalMessageLength(sTestMessage);
			rollingStats.MessagesPerFile = MessagesPerFile(rollingStats.TotalMessageLength);
			rollingStats.MessagesThisFile = 0;
			rollingStats.NumberOfFileRolls = 0;

			return rollingStats;
		}

		/// <summary>
		/// Takes an existing array of RollFileEntry objects, creates a new array one element
		/// bigger, and appends the final element to the end.  If the existing entries are
		/// null (no entries), then a one-element array is returned with the final element
		/// as the only entry.
		/// </summary>
		/// <param name="existing"></param>
		/// <param name="final"></param>
		/// <returns></returns>
		private static RollFileEntry[] AddFinalElement(RollFileEntry[] existing, RollFileEntry final)
		{
			int iLength = 1;
			if (null != existing)
			{
				iLength += existing.Length;
			}
			RollFileEntry[] combined = new RollFileEntry[iLength];
			if (null != existing)
			{
				Array.Copy(existing, 0, combined, 0, existing.Length);
			}
			combined[iLength - 1] = final;
			return combined;
		}

		/// <summary>
		/// Generates the pre and post condition arrays from an array of backup files and the
		/// current file / next file.
		/// </summary>
		/// <param name="sBackupFiles"></param>
		/// <param name="preCondition"></param>
		/// <param name="current"></param>
		/// <param name="currentNext"></param>
		/// <param name="rollingStats"></param>
		/// <returns></returns>
		private static RollConditions BuildTableEntry(string sBackupFiles, RollConditions preCondition, RollFileEntry current, RollFileEntry currentNext, RollingStats rollingStats)
		{
			RollFileEntry[] backupsPost = MakeBackupFileEntriesForPostCondition(sBackupFiles, rollingStats);
			RollFileEntry[] post = AddFinalElement(backupsPost, currentNext);
			if (null == preCondition)
			{
				return new RollConditions(AddFinalElement(null, current), post);
			}
			return new RollConditions(preCondition.GetPostLogFileEntries(), post);
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
		private static RollFileEntry MoveNextEntry(RollingStats rollingStats, RollFileEntry currentNext)
		{
			rollingStats.MessagesThisFile = rollingStats.MessagesThisFile + 1;
			if (rollingStats.MessagesThisFile >= rollingStats.MessagesPerFile)
			{
				rollingStats.MessagesThisFile = 0;
				rollingStats.NumberOfFileRolls = rollingStats.NumberOfFileRolls + 1;

				return new RollFileEntry(GetCurrentFile(), 0);
			}
			else
			{
				return currentNext;
			}
		}

		/// <summary>
		/// Callback point for the regular expression parser.  Turns
		/// the number into a file name.
		/// </summary>
		/// <param name="match"></param>
		/// <returns></returns>
		private static string NumberedNameMaker(Match match)
		{
			Int32 iValue = Int32.Parse(match.Value);
			return MakeFileName(c_fileName, iValue);
		}

		/// <summary>
		/// Parses a numeric list of files, turning them into file names.
		/// Calls back to a method that does the actual replacement, turning
		/// the numeric value into a filename.
		/// </summary>
		/// <param name="sBackupInfo"></param>
		/// <param name="evaluator"></param>
		/// <returns></returns>
		private static string ConvertToFiles(string sBackupInfo, MatchEvaluator evaluator)
		{
			Regex regex = new Regex(@"\d+");
			return regex.Replace(sBackupInfo, evaluator);
		}

		/// <summary>
		/// Makes test entries used for verifying counted file names
		/// </summary>
		/// <param name="sTestMessage">A message to log repeatedly</param>
		/// <param name="sBackupInfo">Filename groups used to indicate backup file name progression
		/// that results after each message is logged</param>
		/// <param name="iMessagesToLog">How many times the test message will be repeatedly logged</param>
		/// <returns></returns>
		private static RollConditions[] MakeNumericTestEntries(string sTestMessage, string sBackupInfo, int iMessagesToLog)
		{
			return MakeTestEntries(
				sTestMessage,
				sBackupInfo,
				iMessagesToLog,
				new MatchEvaluator(NumberedNameMaker));
		}

		/// <summary>
		/// This routine takes a list of backup file names and a message that will be logged
		/// repeatedly, and generates a collection of objects containing pre-condition and 
		/// post-condition information.  This pre/post information shows the names and expected 
		/// file sizes for all files just before and just after a message is logged.
		/// </summary>
		/// <param name="sTestMessage">A message to log repeatedly</param>
		/// <param name="sBackupInfo">Filename groups used to indicate backup file name progression
		/// that results after each message is logged</param>
		/// <param name="iMessagesToLog">How many times the test message will be repeatedly logged</param>
		/// <param name="evaluator">Function that can turn a number into a filename</param>
		/// <returns></returns>
		private static RollConditions[] MakeTestEntries(string sTestMessage, string sBackupInfo, int iMessagesToLog, MatchEvaluator evaluator)
		{
			string sBackupFiles = ConvertToFiles(sBackupInfo, evaluator);

			RollConditions[] table = new RollConditions[iMessagesToLog];

			RollingStats rollingStats = InitializeStats(sTestMessage);

			RollConditions preCondition = null;
			rollingStats.MessagesThisFile = 0;

			RollFileEntry currentFile = new RollFileEntry(GetCurrentFile(), 0);
			for(int i = 0; i < iMessagesToLog; i++)
			{
				RollFileEntry currentNext = new RollFileEntry(
					GetCurrentFile(),
					(1 + rollingStats.MessagesThisFile) * rollingStats.TotalMessageLength);

				table[i] = BuildTableEntry(sBackupFiles, preCondition, currentFile, currentNext, rollingStats);
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
			RollFromTableEntries(c_fileName, table, GetTestMessage());
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
			string sBackupInfo = "1, 1 2, 1 2 3, 2 3 4, 3 4 5";

			//
			// Count Up
			//
			_iCountDirection = +1;

			//
			// Log 30 messages.  This is 5 groups, 6 checks per group ( 0, 100, 200, 300, 400, 500 
			// bytes for current file as messages are logged.
			//
			int iMessagesToLog = 30;

			VerifyRolling(MakeNumericTestEntries(GetTestMessage(), sBackupInfo, iMessagesToLog));
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
			string sBackupInfo = "1, 1 2, 1 2 3, 1 2 3 4, 1 2 3 4 5";

			//
			// Count Up
			//
			_iCountDirection = +1;

			//
			// Infinite backups
			//
			_MaxSizeRollBackups = -1;

			//
			// Log 30 messages.  This is 5 groups, 6 checks per group ( 0, 100, 200, 300, 400, 500 
			// bytes for current file as messages are logged.
			//
			int iMessagesToLog = 30;

			VerifyRolling(MakeNumericTestEntries(GetTestMessage(), sBackupInfo, iMessagesToLog));
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
			string sBackupInfo = ", , , , ";

			//
			// Count Up
			//
			_iCountDirection = +1;

			//
			// No backups
			//
			_MaxSizeRollBackups = 0;

			//
			// Log 30 messages.  This is 5 groups, 6 checks per group ( 0, 100, 200, 300, 400, 500 
			// bytes for current file as messages are logged.
			//
			int iMessagesToLog = 30;

			VerifyRolling(MakeNumericTestEntries(GetTestMessage(), sBackupInfo, iMessagesToLog));
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
			string sBackupInfo = "1, 1 2, 1 2 3, 1 2 3, 1 2 3";

			//
			// Count Up
			//
			_iCountDirection = -1;

			//
			// Log 30 messages.  This is 5 groups, 6 checks per group ( 0, 100, 200, 300, 400, 500 
			// bytes for current file as messages are logged.
			//
			int iMessagesToLog = 30;

			VerifyRolling(MakeNumericTestEntries(GetTestMessage(), sBackupInfo, iMessagesToLog));
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
			string sBackupInfo = "1, 1 2, 1 2 3, 1 2 3 4, 1 2 3 4 5";

			//
			// Count Down
			//
			_iCountDirection = -1;

			//
			// Infinite backups
			//
			_MaxSizeRollBackups = -1;

			//
			// Log 30 messages.  This is 5 groups, 6 checks per group ( 0, 100, 200, 300, 400, 500 
			// bytes for current file as messages are logged.
			//
			int iMessagesToLog = 30;

			VerifyRolling(MakeNumericTestEntries(GetTestMessage(), sBackupInfo, iMessagesToLog));
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
			string sBackupInfo = ", , , , ";

			//
			// Count Up
			//
			_iCountDirection = -1;

			//
			// No backups
			//
			_MaxSizeRollBackups = 0;

			//
			// Log 30 messages.  This is 5 groups, 6 checks per group ( 0, 100, 200, 300, 400, 500 
			// bytes for current file as messages are logged.
			//
			int iMessagesToLog = 30;

			VerifyRolling(MakeNumericTestEntries(GetTestMessage(), sBackupInfo, iMessagesToLog));
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
			Assert.AreEqual(_caRoot.Counter, 0);

			//
			// Set the root appender with a RollingFileAppender
			//
			_root.AddAppender(CreateAppender());

			_root.Repository.Configured = true;
		}

		/// <summary>
		/// Verifies that the current backup index is detected correctly when initializing
		/// </summary>
		/// <param name="sBaseFile"></param>
		/// <param name="alFiles"></param>
		/// <param name="iExpectedCurSizeRollBackups"></param>
		private static void VerifyInitializeRollBackupsFromBaseFile(string sBaseFile, ArrayList alFiles, int iExpectedCurSizeRollBackups)
		{
			InitializeAndVerifyExpectedValue(alFiles, sBaseFile, CreateRollingFileAppender("5,0,1"), iExpectedCurSizeRollBackups);
		}

		/// <summary>
		/// Tests that the current backup index is 0 when no
		/// existing files are seen
		/// </summary>
		[Test]
		public void TestInitializeRollBackups1()
		{
			string sBaseFile = "LogFile.log";
			ArrayList arrFiles = new ArrayList();
			arrFiles.Add("junk1");
			arrFiles.Add("junk1.log");
			arrFiles.Add("junk2.log");
			arrFiles.Add("junk.log.1");
			arrFiles.Add("junk.log.2");

			int iExpectedCurSizeRollBackups = 0;
			VerifyInitializeRollBackupsFromBaseFile(sBaseFile, arrFiles, iExpectedCurSizeRollBackups);
		}

		/// <summary>
		/// Verifies that files are detected when the base file is specified
		/// </summary>
		/// <param name="sBaseFile"></param>
		private static void VerifyInitializeRollBackupsFromBaseFile(string sBaseFile)
		{
			ArrayList alFiles = MakeTestDataFromString(sBaseFile, "0,1,2");

			int iExpectedCurSizeRollBackups = 2;
			VerifyInitializeRollBackupsFromBaseFile(sBaseFile, alFiles, iExpectedCurSizeRollBackups);
		}

		/// <summary>
		/// Verifies that count goes to the highest when counting up
		/// </summary>
		[Test]
		public void TestInitializeCountUpFixed()
		{
			ArrayList alFiles = MakeTestDataFromString("3,4,5");
			int iExpectedValue = 5;
			InitializeAndVerifyExpectedValue(alFiles, c_fileName, CreateRollingFileAppender("3,0,1"), iExpectedValue);
		}

		/// <summary>
		/// Verifies that count goes to the highest when counting up
		/// </summary>
		[Test]
		public void TestInitializeCountUpFixed2()
		{
			ArrayList alFiles = MakeTestDataFromString("0,3");
			int iExpectedValue = 3;
			InitializeAndVerifyExpectedValue(alFiles, c_fileName, CreateRollingFileAppender("3,0,1"), iExpectedValue);
		}

		/// <summary>
		/// Verifies that count stays at 0 for the zero backups case
		/// when counting up
		/// </summary>
		[Test]
		public void TestInitializeCountUpZeroBackups()
		{
			ArrayList alFiles = MakeTestDataFromString("0,3");
			int iExpectedValue = 0;
			InitializeAndVerifyExpectedValue(alFiles, c_fileName, CreateRollingFileAppender("0,0,1"), iExpectedValue);
		}

		/// <summary>
		/// Verifies that count stays at 0 for the zero backups case
		/// when counting down
		/// </summary>
		[Test]
		public void TestInitializeCountDownZeroBackups()
		{
			ArrayList alFiles = MakeTestDataFromString("0,3");
			int iExpectedValue = 0;
			InitializeAndVerifyExpectedValue(alFiles, c_fileName, CreateRollingFileAppender("0,0,-1"), iExpectedValue);
		}


		/// <summary>
		/// Verifies that count goes to the highest when counting up
		/// </summary>
		[Test]
		public void TestInitializeCountDownFixed()
		{
			ArrayList alFiles = MakeTestDataFromString("4,5,6");
			VerifyInitializeDownFixedExpectedValue(alFiles, c_fileName, 0);
		}

		/// <summary>
		/// Verifies that count goes to the highest when counting up
		/// </summary>
		[Test]
		public void TestInitializeCountDownFixed2()
		{
			ArrayList alFiles = MakeTestDataFromString("1,5,6");
			VerifyInitializeDownFixedExpectedValue(alFiles, c_fileName, 1);
		}

		/// <summary>
		/// Verifies that count goes to the highest when counting up
		/// </summary>
		[Test]
		public void TestInitializeCountDownFixed3()
		{
			ArrayList alFiles = MakeTestDataFromString("2,5,6");
			VerifyInitializeDownFixedExpectedValue(alFiles, c_fileName, 2);
		}

		/// <summary>
		/// Verifies that count goes to the highest when counting up
		/// </summary>
		[Test]
		public void TestInitializeCountDownFixed4()
		{
			ArrayList alFiles = MakeTestDataFromString("3,5,6");
			VerifyInitializeDownFixedExpectedValue(alFiles, c_fileName, 3);
		}

		/// <summary>
		/// Verifies that count goes to the highest when counting up
		/// </summary>
		[Test]
		public void TestInitializeCountDownFixed5()
		{
			ArrayList alFiles = MakeTestDataFromString("1,2,3");
			VerifyInitializeDownFixedExpectedValue(alFiles, c_fileName, 3);
		}

		/// <summary>
		/// Verifies that count goes to the highest when counting up
		/// </summary>
		[Test]
		public void TestInitializeCountDownFixed6()
		{
			ArrayList alFiles = MakeTestDataFromString("1,2");
			VerifyInitializeDownFixedExpectedValue(alFiles, c_fileName, 2);
		}

		/// <summary>
		/// Verifies that count goes to the highest when counting up
		/// </summary>
		[Test]
		public void TestInitializeCountDownFixed7()
		{
			ArrayList alFiles = MakeTestDataFromString("2,3");
			VerifyInitializeDownFixedExpectedValue(alFiles, c_fileName, 3);
		}

		private static void InitializeAndVerifyExpectedValue(ArrayList alFiles, string sBaseFile, RollingFileAppender rfa, int iExpectedValue)
		{
			InitializeRollBackups(rfa, sBaseFile, alFiles);
			Assert.AreEqual(iExpectedValue, GetFieldCurSizeRollBackups(rfa));
		}

		/// <summary>
		/// Tests the count down case, with infinite max backups, to see that
		/// initialization of the rolling file appender results in the expected value
		/// </summary>
		/// <param name="alFiles"></param>
		/// <param name="sBaseFile"></param>
		/// <param name="iExpectedValue"></param>
		private static void VerifyInitializeDownInfiniteExpectedValue(ArrayList alFiles, string sBaseFile, int iExpectedValue)
		{
			InitializeAndVerifyExpectedValue(alFiles, sBaseFile, CreateRollingFileAppender("-1,0,-1"), iExpectedValue);
		}

		/// <summary>
		/// Creates a RollingFileAppender with the desired values, where the
		/// values are passed as a comma separated string, with 3 parameters,
		/// m_maxSizeRollBackups, m_curSizeRollBackups, CountDirection
		/// </summary>
		/// <param name="sParams"></param>
		/// <returns></returns>
		private static RollingFileAppender CreateRollingFileAppender(string sParams)
		{
			string[] asParams = sParams.Split(',');
			if (null == asParams || asParams.Length != 3)
			{
				throw new ArgumentOutOfRangeException(sParams, sParams, "Must have 3 comma separated params: MaxSizeRollBackups, CurSizeRollBackups, CountDirection");
			}

			RollingFileAppender rfa = new RollingFileAppender();
			rfa.RollingStyle = RollingFileAppender.RollingMode.Size;
			SetFieldMaxSizeRollBackups(rfa, Int32.Parse(asParams[0].Trim()));
			SetFieldCurSizeRollBackups(rfa, Int32.Parse(asParams[1].Trim()));
			rfa.CountDirection = Int32.Parse(asParams[2].Trim());

			return rfa;
		}

		/// <summary>
		/// Verifies that count goes to the highest when counting down
		/// and infinite backups are selected
		/// </summary>
		[Test]
		public void TestInitializeCountDownInfinite()
		{
			ArrayList alFiles = MakeTestDataFromString("2,3");
			VerifyInitializeDownInfiniteExpectedValue(alFiles, c_fileName, 3);
		}

		/// <summary>
		/// Verifies that count goes to the highest when counting down
		/// and infinite backups are selected
		/// </summary>
		[Test]
		public void TestInitializeCountDownInfinite2()
		{
			ArrayList alFiles = MakeTestDataFromString("2,3,4,5,6,7,8,9,10");
			VerifyInitializeDownInfiniteExpectedValue(alFiles, c_fileName, 10);
		}

		/// <summary>
		/// Verifies that count goes to the highest when counting down
		/// and infinite backups are selected
		/// </summary>
		[Test]
		public void TestInitializeCountDownInfinite3()
		{
			ArrayList alFiles = MakeTestDataFromString("9,10,3,4,5,7,9,6,1,2,8");
			VerifyInitializeDownInfiniteExpectedValue(alFiles, c_fileName, 10);
		}

		/// <summary>
		/// Verifies that count goes to the highest when counting up
		/// and infinite backups are selected
		/// </summary>
		[Test]
		public void TestInitializeCountUpInfinite()
		{
			ArrayList alFiles = MakeTestDataFromString("2,3");
			VerifyInitializeUpInfiniteExpectedValue(alFiles, c_fileName, 3);
		}

		/// <summary>
		/// Verifies that count goes to the highest when counting up
		/// and infinite backups are selected
		/// </summary>
		[Test]
		public void TestInitializeCountUpInfinite2()
		{
			ArrayList alFiles = MakeTestDataFromString("2,3,4,5,6,7,8,9,10");
			VerifyInitializeUpInfiniteExpectedValue(alFiles, c_fileName, 10);
		}

		/// <summary>
		/// Verifies that count goes to the highest when counting up
		/// and infinite backups are selected
		/// </summary>
		[Test]
		public void TestInitializeCountUpInfinite3()
		{
			ArrayList alFiles = MakeTestDataFromString("9,10,3,4,5,7,9,6,1,2,8");
			VerifyInitializeUpInfiniteExpectedValue(alFiles, c_fileName, 10);
		}

		/// <summary>
		/// Creates a logger hierarchy, configures a rolling file appender and returns an ILogger
		/// </summary>
		/// <param name="filename">The filename to log to</param>
		/// <param name="lockModel">The locking model to use.</param>
		/// <param name="handler">The error handler to use.</param>
		/// <returns>A configured ILogger</returns>
		private static ILogger CreateLogger(string filename, FileAppender.LockingModelBase lockModel, IErrorHandler handler)
		{
			Repository.Hierarchy.Hierarchy h = (Repository.Hierarchy.Hierarchy)LogManager.CreateRepository("TestRepository");

			RollingFileAppender appender = new RollingFileAppender();
			appender.File = filename;
			appender.AppendToFile = false;
			appender.CountDirection = 0;
			appender.RollingStyle = RollingFileAppender.RollingMode.Size;
			appender.MaxFileSize = 100000;
			appender.Encoding = Encoding.ASCII;
			appender.ErrorHandler = handler;
			if (lockModel != null)
			{
				appender.LockingModel = lockModel;
			}

			PatternLayout layout = new PatternLayout();
			layout.ConversionPattern = "%m%n";
			layout.ActivateOptions();

			appender.Layout = layout;
			appender.ActivateOptions();

			h.Root.AddAppender(appender);
			h.Configured = true;

			ILogger log = h.GetLogger("Logger");
			return log;
		}

		/// <summary>
		/// Destroys the logger hierarchy created by <see cref="RollingFileAppenderTest.CreateLogger"/>
		/// </summary>
		private static void DestroyLogger()
		{
			Repository.Hierarchy.Hierarchy h = (Repository.Hierarchy.Hierarchy)LogManager.GetRepository("TestRepository");
			h.ResetConfiguration();
			//Replace the repository selector so that we can recreate the hierarchy with the same name if necessary
			LoggerManager.RepositorySelector = new DefaultRepositorySelector(SystemInfo.GetTypeFromString("log4net.Repository.Hierarchy.Hierarchy", true, true));
		}

		private static void AssertFileEquals(string filename, string contents)
		{
			StreamReader sr = new StreamReader(filename);
			string logcont = sr.ReadToEnd();
			sr.Close();

			Assert.AreEqual(contents, logcont, "Log contents is not what is expected");

			File.Delete(filename);
		}

		/// <summary>
		/// Verifies that logging a message actually produces output
		/// </summary>
		[Test]
		public void TestLogOutput()
		{
			String filename = "test.log";
			SilentErrorHandler sh = new SilentErrorHandler();
			ILogger log = CreateLogger(filename, new FileAppender.ExclusiveLock(), sh);
			log.Log(GetType(), Level.Info, "This is a message", null);
			log.Log(GetType(), Level.Info, "This is a message 2", null);
			DestroyLogger();

			AssertFileEquals(filename, "This is a message" + Environment.NewLine + "This is a message 2" + Environment.NewLine);
			Assert.AreEqual("", sh.Message, "Unexpected error message");
		}

		/// <summary>
		/// Verifies that attempting to log to a locked file fails gracefully
		/// </summary>
		[Test]
		public void TestExclusiveLockFails()
		{
			String filename = "test.log";

			FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
			fs.Write(Encoding.ASCII.GetBytes("Test"), 0, 4);

			SilentErrorHandler sh = new SilentErrorHandler();
			ILogger log = CreateLogger(filename, new FileAppender.ExclusiveLock(), sh);
			log.Log(GetType(), Level.Info, "This is a message", null);
			log.Log(GetType(), Level.Info, "This is a message 2", null);
			DestroyLogger();
			fs.Close();

			AssertFileEquals(filename, "Test");
			Assert.AreEqual(sh.Message.Substring(0, 30), "Unable to acquire lock on file", "Expecting an error message");
		}

		/// <summary>
		/// Verifies that attempting to log to a locked file recovers if the lock is released
		/// </summary>
		[Test]
		public void TestExclusiveLockRecovers()
		{
			String filename = "test.log";

			FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
			fs.Write(Encoding.ASCII.GetBytes("Test"), 0, 4);

			SilentErrorHandler sh = new SilentErrorHandler();
			ILogger log = CreateLogger(filename, new FileAppender.ExclusiveLock(), sh);
			log.Log(GetType(), Level.Info, "This is a message", null);
			fs.Close();
			log.Log(GetType(), Level.Info, "This is a message 2", null);
			DestroyLogger();

			AssertFileEquals(filename, "This is a message 2" + Environment.NewLine);
			Assert.AreEqual("Unable to acquire lock on file", sh.Message.Substring(0, 30), "Expecting an error message");
		}

		/// <summary>
		/// Verifies that attempting to log to a file with ExclusiveLock really locks the file
		/// </summary>
		[Test]
		public void TestExclusiveLockLocks()
		{
			String filename = "test.log";
			bool locked = false;

			SilentErrorHandler sh = new SilentErrorHandler();
			ILogger log = CreateLogger(filename, new FileAppender.ExclusiveLock(), sh);
			log.Log(GetType(), Level.Info, "This is a message", null);

			try
			{
				FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
				fs.Write(Encoding.ASCII.GetBytes("Test"), 0, 4);
				fs.Close();
			}
			catch(IOException e1)
			{
#if MONO
				Assert.AreEqual("Sharing violation on path ", e1.Message.Substring(0, 26), "Unexpected exception");
#else
				Assert.AreEqual("The process cannot access the file ", e1.Message.Substring(0, 35), "Unexpected exception");
#endif
				locked = true;
			}

			log.Log(GetType(), Level.Info, "This is a message 2", null);
			DestroyLogger();

			Assert.IsTrue(locked, "File was not locked");
#if !MONO // at least on Linux with Mono 2.4 exclusive locking doesn't work as one would expect
			AssertFileEquals(filename, "This is a message" + Environment.NewLine + "This is a message 2" + Environment.NewLine);
#endif
			Assert.AreEqual("", sh.Message, "Unexpected error message");
		}


		/// <summary>
		/// Verifies that attempting to log to a locked file fails gracefully
		/// </summary>
		[Test]
		public void TestMinimalLockFails()
		{
			String filename = "test.log";

			FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
			fs.Write(Encoding.ASCII.GetBytes("Test"), 0, 4);

			SilentErrorHandler sh = new SilentErrorHandler();
			ILogger log = CreateLogger(filename, new FileAppender.MinimalLock(), sh);
			log.Log(GetType(), Level.Info, "This is a message", null);
			log.Log(GetType(), Level.Info, "This is a message 2", null);
			DestroyLogger();
			fs.Close();

			AssertFileEquals(filename, "Test");
			Assert.AreEqual("Unable to acquire lock on file", sh.Message.Substring(0, 30), "Expecting an error message");
		}

		/// <summary>
		/// Verifies that attempting to log to a locked file recovers if the lock is released
		/// </summary>
		[Test]
		public void TestMinimalLockRecovers()
		{
			String filename = "test.log";

			FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
			fs.Write(Encoding.ASCII.GetBytes("Test"), 0, 4);

			SilentErrorHandler sh = new SilentErrorHandler();
			ILogger log = CreateLogger(filename, new FileAppender.MinimalLock(), sh);
			log.Log(GetType(), Level.Info, "This is a message", null);
			fs.Close();
			log.Log(GetType(), Level.Info, "This is a message 2", null);
			DestroyLogger();

			AssertFileEquals(filename, "This is a message 2" + Environment.NewLine);
			Assert.AreEqual("Unable to acquire lock on file", sh.Message.Substring(0, 30), "Expecting an error message");
		}

		/// <summary>
		/// Verifies that attempting to log to a file with MinimalLock doesn't lock the file
		/// </summary>
		[Test]
		public void TestMinimalLockUnlocks()
		{
			String filename = "test.log";
			bool locked;

			SilentErrorHandler sh = new SilentErrorHandler();
			ILogger log = CreateLogger(filename, new FileAppender.MinimalLock(), sh);
			log.Log(GetType(), Level.Info, "This is a message", null);

			locked = true;
			FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.None);
			fs.Write(Encoding.ASCII.GetBytes("Test" + Environment.NewLine), 0, 4 + Environment.NewLine.Length);
			fs.Close();

			log.Log(GetType(), Level.Info, "This is a message 2", null);
			DestroyLogger();

			Assert.IsTrue(locked, "File was not locked");
			AssertFileEquals(filename, "This is a message" + Environment.NewLine + "Test" + Environment.NewLine + "This is a message 2" + Environment.NewLine);
			Assert.AreEqual("", sh.Message, "Unexpected error message");
		}

#if !NETCF
        /// <summary>
        /// Verifies that attempting to log to a locked file fails gracefully
        /// </summary>
        [Test]
        public void TestInterProcessLockFails() {
            String filename = "test.log";

            FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            fs.Write(Encoding.ASCII.GetBytes("Test"), 0, 4);

            SilentErrorHandler sh = new SilentErrorHandler();
            ILogger log = CreateLogger(filename, new FileAppender.InterProcessLock(), sh);
            log.Log(GetType(), Level.Info, "This is a message", null);
            log.Log(GetType(), Level.Info, "This is a message 2", null);
            DestroyLogger();
            fs.Close();

            AssertFileEquals(filename, "Test");
            Assert.AreEqual("Unable to acquire lock on file", sh.Message.Substring(0, 30), "Expecting an error message");
        }

        /// <summary>
        /// Verifies that attempting to log to a locked file recovers if the lock is released
        /// </summary>
        [Test]
        public void TestInterProcessLockRecovers() {
            String filename = "test.log";

            FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            fs.Write(Encoding.ASCII.GetBytes("Test"), 0, 4);

            SilentErrorHandler sh = new SilentErrorHandler();
            ILogger log = CreateLogger(filename, new FileAppender.InterProcessLock(), sh);
            log.Log(GetType(), Level.Info, "This is a message", null);
            fs.Close();
            log.Log(GetType(), Level.Info, "This is a message 2", null);
            DestroyLogger();

            AssertFileEquals(filename, "This is a message 2" + Environment.NewLine);
            Assert.AreEqual("Unable to acquire lock on file", sh.Message.Substring(0, 30), "Expecting an error message");
        }

        /// <summary>
        /// Verifies that attempting to log to a file with InterProcessLock really locks the file
        /// </summary>
        [Test]
        public void TestInterProcessLockUnlocks() {
            String filename = "test.log";
            bool locked;

            SilentErrorHandler sh = new SilentErrorHandler();
            ILogger log = CreateLogger(filename, new FileAppender.InterProcessLock(), sh);
            log.Log(GetType(), Level.Info, "This is a message", null);

            locked = true;
            FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            fs.Write(Encoding.ASCII.GetBytes("Test" + Environment.NewLine), 0, 4 + Environment.NewLine.Length);
            fs.Close();

            log.Log(GetType(), Level.Info, "This is a message 2", null);
            DestroyLogger();

            Assert.IsTrue(locked, "File was not locked");
            AssertFileEquals(filename, "This is a message" + Environment.NewLine + "Test" + Environment.NewLine + "This is a message 2" + Environment.NewLine);
            Assert.AreEqual("", sh.Message, "Unexpected error message");
        }
#endif

        /// <summary>
		/// Verify that the default LockModel is ExclusiveLock, to maintain backwards compatibility with previous behaviour
		/// </summary>
		[Test]
		public void TestDefaultLockingModel()
		{
			String filename = "test.log";
			SilentErrorHandler sh = new SilentErrorHandler();
			ILogger log = CreateLogger(filename, null, sh);

			IAppender[] appenders = log.Repository.GetAppenders();
			Assert.AreEqual(1, appenders.Length, "The wrong number of appenders are configured");

			RollingFileAppender rfa = (RollingFileAppender)(appenders[0]);
			Assert.AreEqual(SystemInfo.GetTypeFromString("log4net.Appender.FileAppender+ExclusiveLock", true, true), rfa.LockingModel.GetType(), "The LockingModel is of an unexpected type");

			DestroyLogger();
		}

		/// <summary>
		/// Tests the count up case, with infinite max backups , to see that
		/// initialization of the rolling file appender results in the expected value
		/// </summary>
		/// <param name="alFiles"></param>
		/// <param name="sBaseFile"></param>
		/// <param name="iExpectedValue"></param>
		private static void VerifyInitializeUpInfiniteExpectedValue(ArrayList alFiles, string sBaseFile, int iExpectedValue)
		{
			InitializeAndVerifyExpectedValue(alFiles, sBaseFile, CreateRollingFileAppender("-1,0,1"), iExpectedValue);
		}


		/// <summary>
		/// Tests the count down case, with max backups limited to 3, to see that
		/// initialization of the rolling file appender results in the expected value
		/// </summary>
		/// <param name="alFiles"></param>
		/// <param name="sBaseFile"></param>
		/// <param name="iExpectedValue"></param>
		private static void VerifyInitializeDownFixedExpectedValue(ArrayList alFiles, string sBaseFile, int iExpectedValue)
		{
			InitializeAndVerifyExpectedValue(alFiles, sBaseFile, CreateRollingFileAppender("3,0,-1"), iExpectedValue);
		}

		/// <summary>
		/// Turns a string of comma separated numbers into a collection of filenames
		/// generated from the numbers.  
		/// 
		/// Defaults to filename in _fileName variable.
		/// 
		/// </summary>
		/// <param name="sFileNumbers">Comma separated list of numbers for counted file names</param>
		/// <returns></returns>
		private static ArrayList MakeTestDataFromString(string sFileNumbers)
		{
			return MakeTestDataFromString(c_fileName, sFileNumbers);
		}

		/// <summary>
		/// Turns a string of comma separated numbers into a collection of filenames
		/// generated from the numbers
		/// 
		/// Uses the input filename.
		/// </summary>
		/// <param name="sFileName">Name of file to combine with numbers when generating counted file names</param>
		/// <param name="sFileNumbers">Comma separated list of numbers for counted file names</param>
		/// <returns></returns>
		private static ArrayList MakeTestDataFromString(string sFileName, string sFileNumbers)
		{
			ArrayList alFiles = new ArrayList();

			string[] sNumbers = sFileNumbers.Split(',');
			foreach(string sNumber in sNumbers)
			{
				Int32 iValue = Int32.Parse(sNumber.Trim());
				alFiles.Add(MakeFileName(sFileName, iValue));
			}

			return alFiles;
		}

		/// <summary>
		/// Tests that the current backup index is correctly detected
		/// for a file with no extension
		/// </summary>
		[Test]
		public void TestInitializeRollBackups2()
		{
			VerifyInitializeRollBackupsFromBaseFile("LogFile");
		}

		/// <summary>
		/// Tests that the current backup index is correctly detected
		/// for a file with a .log extension
		/// </summary>
		[Test]
		public void TestInitializeRollBackups3()
		{
			VerifyInitializeRollBackupsFromBaseFile("LogFile.log");
		}

		/// <summary>
		/// Makes sure that the initialization can detect the backup
		/// number correctly.
		/// </summary>
		/// <param name="iBackups"></param>
		/// <param name="iMaxSizeRollBackups"></param>
		public void VerifyInitializeRollBackups(int iBackups, int iMaxSizeRollBackups)
		{
			string sBaseFile = "LogFile.log";
			ArrayList arrFiles = new ArrayList();
			arrFiles.Add("junk1");
			for(int i = 0; i < iBackups; i++)
			{
				arrFiles.Add(MakeFileName(sBaseFile, i));
			}
			RollingFileAppender rfa = new RollingFileAppender();
			rfa.RollingStyle = RollingFileAppender.RollingMode.Size;
			SetFieldMaxSizeRollBackups(rfa, iMaxSizeRollBackups);
			SetFieldCurSizeRollBackups(rfa, 0);
			InitializeRollBackups(rfa, sBaseFile, arrFiles);

			// iBackups	/ Meaning
			// 0 = none
			// 1 = file.log
			// 2 = file.log.1
			// 3 = file.log.2
			if (0 == iBackups ||
			    1 == iBackups)
			{
				Assert.AreEqual(0, GetFieldCurSizeRollBackups(rfa));
			}
			else
			{
				Assert.AreEqual(Math.Min(iBackups - 1, iMaxSizeRollBackups), GetFieldCurSizeRollBackups(rfa));
			}
		}

		/// <summary>
		/// Tests that the current backup index is correctly detected,
		/// and gets no bigger than the max backups setting
		/// </summary>
		[Test]
		public void TestInitializeRollBackups4()
		{
			const int iMaxRollBackups = 5;
			VerifyInitializeRollBackups(0, iMaxRollBackups);
			VerifyInitializeRollBackups(1, iMaxRollBackups);
			VerifyInitializeRollBackups(2, iMaxRollBackups);
			VerifyInitializeRollBackups(3, iMaxRollBackups);
			VerifyInitializeRollBackups(4, iMaxRollBackups);
			VerifyInitializeRollBackups(5, iMaxRollBackups);
			VerifyInitializeRollBackups(6, iMaxRollBackups);
			// Final we cap out at the max value
			VerifyInitializeRollBackups(7, iMaxRollBackups);
			VerifyInitializeRollBackups(8, iMaxRollBackups);
		}

		/// <summary>
		/// 
		/// </summary>
		[Test, Ignore("Not Implemented: Want to test counted files limited up, to see that others are ?? ignored? deleted?")]
		public void TestInitialization3()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		[Test, Ignore("Not Implemented: Want to test counted files limited down, to see that others are ?? ignored? deleted?")]
		public void TestInitialization4()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		[Test, Ignore("Not Implemented: Want to test dated files with a limit, to see that others are ?? ignored? deleted?")]
		public void TestInitialization5()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		[Test, Ignore("Not Implemented: Want to test dated files with no limit, to see that others are ?? ignored? deleted?")]
		public void TestInitialization6()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		[Test, Ignore("Not Implemented: Want to test dated files with mixed dates existing, to see that other dates do not matter")]
		public void TestInitialization7()
		{
		}


		//
		// Helper functions to dig into the appender
		//

		private static ArrayList GetExistingFiles(string baseFilePath)
		{
			RollingFileAppender appender = new RollingFileAppender();
			appender.SecurityContext = NullSecurityContext.Instance;

			return (ArrayList)Utils.InvokeMethod(appender, "GetExistingFiles", baseFilePath);
		}

		private static void InitializeRollBackups(RollingFileAppender appender, string baseFile, ArrayList arrayFiles)
		{
			Utils.InvokeMethod(appender, "InitializeRollBackups", baseFile, arrayFiles);
		}

		private static int GetFieldCurSizeRollBackups(RollingFileAppender appender)
		{
			return (int)Utils.GetField(appender, "m_curSizeRollBackups");
		}

		private static void SetFieldCurSizeRollBackups(RollingFileAppender appender, int val)
		{
			Utils.SetField(appender, "m_curSizeRollBackups", val);
		}

		private static void SetFieldMaxSizeRollBackups(RollingFileAppender appender, int val)
		{
			Utils.SetField(appender, "m_maxSizeRollBackups", val);
		}

		private static string GetTestMessage()
		{
			switch (Environment.NewLine.Length)
			{
				case 2:
					return c_testMessage98Chars;

				case 1:
					return c_testMessage99Chars;

				default:
					throw new Exception("Unexpected Environment.NewLine.Length");
			}
		}
	}

	[TestFixture]
	public class RollingFileAppenderSubClassTest : RollingFileAppender
	{
		[Test]
		public void TestComputeCheckPeriod()
		{
			RollingFileAppender rfa = new RollingFileAppender();

			Assert.AreEqual(RollPoint.TopOfMinute, InvokeComputeCheckPeriod(rfa, ".yyyy-MM-dd HH:mm"), "TopOfMinute pattern");
			Assert.AreEqual(RollPoint.TopOfHour, InvokeComputeCheckPeriod(rfa, ".yyyy-MM-dd HH"), "TopOfHour pattern");
			Assert.AreEqual(RollPoint.HalfDay, InvokeComputeCheckPeriod(rfa, ".yyyy-MM-dd tt"), "HalfDay pattern");
			Assert.AreEqual(RollPoint.TopOfDay, InvokeComputeCheckPeriod(rfa, ".yyyy-MM-dd"), "TopOfDay pattern");
			Assert.AreEqual(RollPoint.TopOfMonth, InvokeComputeCheckPeriod(rfa, ".yyyy-MM"), "TopOfMonth pattern");

			// Test invalid roll point
			Assert.AreEqual(RollPoint.InvalidRollPoint, InvokeComputeCheckPeriod(rfa, "..."), "TopOfMonth pattern");
		}

		private static RollPoint InvokeComputeCheckPeriod(RollingFileAppender rollingFileAppender, string datePattern)
		{
			return (RollPoint)Utils.InvokeMethod(rollingFileAppender, "ComputeCheckPeriod", datePattern);
		}
	}
}