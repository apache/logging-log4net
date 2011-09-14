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

using System.Diagnostics;

using log4net.Appender;
using log4net.Core;

using NUnit.Framework;

namespace log4net.Tests.Appender
{
	/// <summary>
	/// Used for internal unit testing the <see cref="EventLogAppender"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="EventLogAppender"/> class.
	/// </remarks>
	[TestFixture]
	public class EventLogAppenderTest
	{
		/// <summary>
		/// Verifies that for each event log level, the correct system
		/// event log enumeration is returned
		/// </summary>
		[Test]
		public void TestGetEntryTypeForExistingApplicationName()
		{
			EventLogAppender eventAppender = new EventLogAppender();
            eventAppender.ApplicationName = "Winlogon";
			eventAppender.ActivateOptions();

			Assert.AreEqual(
				EventLogEntryType.Information,
				GetEntryType(eventAppender, Level.All));

			Assert.AreEqual(
				EventLogEntryType.Information,
				GetEntryType(eventAppender, Level.Debug));

			Assert.AreEqual(
				EventLogEntryType.Information,
				GetEntryType(eventAppender, Level.Info));

			Assert.AreEqual(
				EventLogEntryType.Warning,
				GetEntryType(eventAppender, Level.Warn));

			Assert.AreEqual(
				EventLogEntryType.Error,
				GetEntryType(eventAppender, Level.Error));

			Assert.AreEqual(
				EventLogEntryType.Error,
				GetEntryType(eventAppender, Level.Fatal));

			Assert.AreEqual(
				EventLogEntryType.Error,
				GetEntryType(eventAppender, Level.Off));
		}

        /// <summary>
        /// ActivateOption tries to create an event source if it doesn't exist but this is going to fail on more modern Windows versions unless the code is run with local administrator privileges.
        /// </summary>
        [Test]
        [Platform(Exclude = "Win2K,WinXP", Include="Win")]
        public void ActivateOptionsDisablesAppenderIfSourceDoesntExist()
        {
            EventLogAppender eventAppender = new EventLogAppender();
            eventAppender.ActivateOptions();
            Assert.AreEqual(Level.Off, eventAppender.Threshold);
        }

		//
		// Helper functions to dig into the appender
		//

		private static EventLogEntryType GetEntryType(EventLogAppender appender, Level level)
		{
			return (EventLogEntryType)Utils.InvokeMethod(appender, "GetEntryType", level);
		}

	}
}