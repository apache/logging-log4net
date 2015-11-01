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

using log4net.Appender;
using log4net.Core;
using log4net.Layout;

using NUnit.Framework;

namespace log4net.Tests.Appender
{
	/// <summary>
	/// Used for internal unit testing the <see cref="SmtpPickupDirAppender"/> class.
	/// </summary>
	[TestFixture]
	public class SmtpPickupDirAppenderTest
	{
		private readonly string _testPickupDir;

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

		public SmtpPickupDirAppenderTest()
		{
			_testPickupDir = Path.Combine(Directory.GetCurrentDirectory(), "SmtpPickupDirAppenderTest_PickupDir");
		}
		/// <summary>
		/// Sets up variables used for the tests
		/// </summary>
		private void InitializePickupDir()
		{
			Directory.CreateDirectory(_testPickupDir);
		}

		/// <summary>
		/// Shuts down any loggers in the hierarchy, along
		/// with all appenders, and deletes any test files used
		/// for logging.
		/// </summary>
		private void ResetLogger()
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
		private ILogger CreateLogger(SmtpPickupDirAppender appender)
		{
			Repository.Hierarchy.Hierarchy h = (Repository.Hierarchy.Hierarchy)LogManager.CreateRepository("TestRepository");

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
		/// Create an appender to use for the logger
		/// </summary>
		/// <param name="handler">The error handler to use.</param>
		/// <returns></returns>
		private SmtpPickupDirAppender CreateSmtpPickupDirAppender(IErrorHandler handler)
		{
			SmtpPickupDirAppender appender = new SmtpPickupDirAppender();
			appender.PickupDir = _testPickupDir;
			appender.ErrorHandler = handler;
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
		/// Verifies that logging a message actually produces output
		/// </summary>
		[Test]
		public void TestDefaultFileNameIsAGuid()
		{
			SilentErrorHandler sh = new SilentErrorHandler();
			SmtpPickupDirAppender appender = CreateSmtpPickupDirAppender(sh);
			ILogger log = CreateLogger(appender);
			log.Log(GetType(), Level.Info, "This is a message", null);
			log.Log(GetType(), Level.Info, "This is a message 2", null);
			DestroyLogger();

			Assert.AreEqual(1, Directory.GetFiles(_testPickupDir).Length);
			var fileInfo = new FileInfo(Directory.GetFiles(_testPickupDir)[0]);
			Assert.IsEmpty(fileInfo.Extension);
			Guid tmpGuid;
			Assert.IsTrue(Guid.TryParse(fileInfo.Name, out tmpGuid)); // Assert that filename is a guid

			Assert.AreEqual("", sh.Message, "Unexpected error message");
		}
	}
}