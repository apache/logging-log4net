#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
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

using System;
using System.Diagnostics;
using System.Globalization;

using log4net.Config;
using log4net.Util;
using log4net.Layout;
using log4net.Core;
using log4net.Appender;
using log4net.Repository;

using log4net.Tests.Appender;

using NUnit.Framework;

namespace log4net.Tests.Layout
{
	/// <summary>
	/// Used for internal unit testing the <see cref="PatternLayoutTest"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="PatternLayoutTest"/> class.
	/// </remarks>
	[TestFixture] public class PatternLayoutTest
	{
		[Test] public void TestMdcPattern()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%mdc{prop1}");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestMdcPattern");

			log1.Info("TestMessage");
			Assertion.AssertEquals("Test no mdc value set", "(null)", stringAppender.GetString());
			stringAppender.Reset();

			MDC.Set("prop1", "val1");

			log1.Info("TestMessage");
			Assertion.AssertEquals("Test thread mdc value set", "val1", stringAppender.GetString());
			stringAppender.Reset();

			MDC.Remove("prop1");

			log1.Info("TestMessage");
			Assertion.AssertEquals("Test mdc value removed", "(null)", stringAppender.GetString());
			stringAppender.Reset();
		}

		[Test] public void TestAddingCustomPattern()
		{
			StringAppender stringAppender = new StringAppender();
			PatternLayout layout = new PatternLayout();

			layout.AddConverter("TestAddingCustomPattern", typeof(TestMessagePatternConverter));
			layout.ConversionPattern = "%TestAddingCustomPattern";
			layout.ActivateOptions();

			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestAddingCustomPattern");

			log1.Info("TestMessage");
			Assertion.AssertEquals("%TestAddingCustomPattern not registered", "TestMessage", stringAppender.GetString());
			stringAppender.Reset();	
		}

		/// <summary>
		/// Converter to include event message
		/// </summary>
		private class TestMessagePatternConverter : log4net.Layout.Pattern.PatternLayoutConverter 
		{
			/// <summary>
			/// Convert the pattern to the rendered message
			/// </summary>
			/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
			/// <param name="loggingEvent">the event being logged</param>
			/// <returns>the relevant location information</returns>
			override protected void Convert(System.IO.TextWriter writer, LoggingEvent loggingEvent)
			{
				loggingEvent.WriteRenderedMessage(writer);
			}
		}
	}
}
