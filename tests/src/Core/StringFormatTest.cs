#region Copyright & License
//
// Copyright 2006 The Apache Software Foundation
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
using System.IO;

using log4net.Config;
using log4net.Layout;
using log4net.Core;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Core
{
	/// <summary>
	/// Used for internal unit testing the <see cref="PatternLayoutTest"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="PatternLayoutTest"/> class.
	/// </remarks>
	[TestFixture] public class StringFormatTest
	{
		[Test] public void TestThreadPropertiesPattern()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%message");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			// ***
			log1.Info("TestMessage");
			Assert.AreEqual("TestMessage", stringAppender.GetString(), "Test simple INFO event");
			stringAppender.Reset();


			// ***
			log1.DebugFormat("Before {0} After", "Middle");
			Assert.AreEqual("Before Middle After", stringAppender.GetString(), "Test simple formatted DEBUG event");
			stringAppender.Reset();

			// ***
			log1.InfoFormat("Before {0} After", "Middle");
			Assert.AreEqual("Before Middle After", stringAppender.GetString(), "Test simple formatted INFO event");
			stringAppender.Reset();

			// ***
			log1.WarnFormat("Before {0} After", "Middle");
			Assert.AreEqual("Before Middle After", stringAppender.GetString(), "Test simple formatted WARN event");
			stringAppender.Reset();

			// ***
			log1.ErrorFormat("Before {0} After", "Middle");
			Assert.AreEqual("Before Middle After", stringAppender.GetString(), "Test simple formatted ERROR event");
			stringAppender.Reset();

			// ***
			log1.FatalFormat("Before {0} After", "Middle");
			Assert.AreEqual("Before Middle After", stringAppender.GetString(), "Test simple formatted FATAL event");
			stringAppender.Reset();


			// ***
			log1.InfoFormat("Before {0} After {1}", "Middle", "End");
			Assert.AreEqual("Before Middle After End", stringAppender.GetString(), "Test simple formatted INFO event 2");
			stringAppender.Reset();

			// ***
			log1.InfoFormat("Before {0} After {1} {2}", "Middle", "End");
			Assert.AreEqual(STRING_FORMAT_ERROR, stringAppender.GetString(), "Test formatting error");
			stringAppender.Reset();
		}

		private const string STRING_FORMAT_ERROR = "<log4net.Error>Exception during StringFormat: Index (zero based) must be greater than or equal to zero and less than the size of the argument list. <format>Before {0} After {1} {2}</format><args>{Middle, End}</args></log4net.Error>";
	}
}
