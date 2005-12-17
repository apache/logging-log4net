#region Copyright & License
//
// Copyright 2001-2005 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
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

using log4net.Core;
using log4net.Appender;

using NUnit.Framework;

namespace log4net.Tests.Appender
{
	/// <summary>
	/// Used for internal unit testing the <see cref="EventLogAppender"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="EventLogAppender"/> class.
	/// </remarks>
	[TestFixture] public class EventLogAppenderTest
	{
		/// <summary>
		/// Verifies that for each event log level, the correct system
		/// event log enumeration is returned
		/// </summary>
		[Test] public void TestGetEntryType()
		{
			EventLogAppender eventAppender = new EventLogAppender();
			eventAppender.ActivateOptions();

			Assert.AreEqual(
				System.Diagnostics.EventLogEntryType.Information,
				GetEntryType( eventAppender, Level.All ) );

			Assert.AreEqual(
				System.Diagnostics.EventLogEntryType.Information,
				GetEntryType( eventAppender, Level.Debug ) );

			Assert.AreEqual(
				System.Diagnostics.EventLogEntryType.Information,
				GetEntryType( eventAppender, Level.Info ) );

			Assert.AreEqual(
				System.Diagnostics.EventLogEntryType.Warning,
				GetEntryType( eventAppender, Level.Warn ) );

			Assert.AreEqual(
				System.Diagnostics.EventLogEntryType.Error,
				GetEntryType( eventAppender, Level.Error ) );

			Assert.AreEqual(
				System.Diagnostics.EventLogEntryType.Error,
				GetEntryType( eventAppender, Level.Fatal ) );

			Assert.AreEqual(
				System.Diagnostics.EventLogEntryType.Error,
				GetEntryType( eventAppender, Level.Off ) );

		}

		//
		// Helper functions to dig into the appender
		//

		private static System.Diagnostics.EventLogEntryType GetEntryType(EventLogAppender appender, Level level)
		{
			return (System.Diagnostics.EventLogEntryType)Utils.InvokeMethod(appender, "GetEntryType", level);
		}
	}
}
