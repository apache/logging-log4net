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
using System.Xml;

using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;

using NUnit.Framework;
using System.Globalization;

namespace log4net.Tests.Layout
{
	[TestFixture]
	public class XmlLayoutSchemaLog4jNSTest
	{

#if !NETSTANDARD1_3
		private CultureInfo _currentCulture;
		private CultureInfo _currentUICulture;

		[SetUp]
		public void SetUp()
		{
			// set correct thread culture
			_currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
			_currentUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
		}

		[TearDown]
		public void TearDown()
		{
			// restore previous culture
			System.Threading.Thread.CurrentThread.CurrentCulture = _currentCulture;
			System.Threading.Thread.CurrentThread.CurrentUICulture = _currentUICulture;
		}
#endif

		/// <summary>
		/// Build a basic <see cref="LoggingEventData"/> object with some default values.
		/// </summary>
		/// <returns>A useful LoggingEventData object</returns>
		private LoggingEventData CreateBaseEvent()
		{
			LoggingEventData ed = new LoggingEventData();
			ed.Domain = "Tests";
			ed.ExceptionString = "";
			ed.Identity = "TestRunner";
			ed.Level = Level.Info;
			ed.LocationInfo = new LocationInfo(GetType());
			ed.LoggerName = "TestLogger";
			ed.Message = "Test message";
			ed.ThreadName = "TestThread";
			ed.TimeStampUtc = DateTime.Today.ToUniversalTime();
			ed.UserName = "TestRunner";
			ed.Properties = new PropertiesDictionary();

			return ed;
		}

		private static string CreateEventNode(string message, string prefix, string ns)
		{
			TimeSpan timeSince1970 = DateTime.Today.ToUniversalTime() - new DateTime(1970, 1, 1);
			return String.Format("<{2}:event logger=\"TestLogger\" timestamp=\"{0}\" level=\"INFO\" thread=\"TestThread\" xmlns:{2}=\"{3}\"><{2}:message>{1}</{2}:message><{2}:properties><{2}:data name=\"log4japp\" value=\"Tests\" /><{2}:data name=\"log4net:Identity\" value=\"TestRunner\" /><{2}:data name=\"log4net:UserName\" value=\"TestRunner\" /></{2}:properties></{2}:event>" + Environment.NewLine,
								 XmlConvert.ToString((long)timeSince1970.TotalMilliseconds),
								 message, prefix, ns);
		}

		private static string CreateEventNode(string message)
		{
			return CreateEventNode(message, "log4j", "http://logging.apache.org/log4j");
		}

		[Test]
		public void TestBasicEventLogging()
		{
			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			TextWriter writer = new StringWriter();
			XmlLayoutSchemaLog4jNS layout = new XmlLayoutSchemaLog4jNS();
			LoggingEventData evt = CreateBaseEvent();

			layout.Format(writer, new LoggingEvent(null, rep, evt));

			string expected = CreateEventNode("Test message");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void NamespaceUriCanBeChanged()
		{
			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			TextWriter writer = new StringWriter();
			XmlLayoutSchemaLog4jNS layout = new XmlLayoutSchemaLog4jNS();
			layout.NamespaceUri = "urn:foo:bar";
			LoggingEventData evt = CreateBaseEvent();

			layout.Format(writer, new LoggingEvent(null, rep, evt));

			string expected = CreateEventNode("Test message", "log4j", "urn:foo:bar");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void PrefixCanBeChanged()
		{
			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			TextWriter writer = new StringWriter();
			XmlLayoutSchemaLog4jNS layout = new XmlLayoutSchemaLog4jNS();
			layout.Prefix = "foo";
			LoggingEventData evt = CreateBaseEvent();

			layout.Format(writer, new LoggingEvent(null, rep, evt));

			string expected = CreateEventNode("Test message", "foo", "http://logging.apache.org/log4j");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestIllegalCharacterMasking()
		{
			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			TextWriter writer = new StringWriter();
			XmlLayoutSchemaLog4jNS layout = new XmlLayoutSchemaLog4jNS();
			LoggingEventData evt = CreateBaseEvent();

			evt.Message = "This is a masked char->\uFFFF";

			layout.Format(writer, new LoggingEvent(null, rep, evt));

			string expected = CreateEventNode("This is a masked char-&gt;?");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestCDATAEscaping1()
		{
			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			TextWriter writer = new StringWriter();
			XmlLayoutSchemaLog4jNS layout = new XmlLayoutSchemaLog4jNS();
			LoggingEventData evt = CreateBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message = "&&&&&&&Escape this ]]>. End here.";

			layout.Format(writer, new LoggingEvent(null, rep, evt));

			string expected = CreateEventNode("<![CDATA[&&&&&&&Escape this ]]>]]<![CDATA[>. End here.]]>");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestCDATAEscaping2()
		{
			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			TextWriter writer = new StringWriter();
			XmlLayoutSchemaLog4jNS layout = new XmlLayoutSchemaLog4jNS();
			LoggingEventData evt = CreateBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message = "&&&&&&&Escape the end ]]>";

			layout.Format(writer, new LoggingEvent(null, rep, evt));

			string expected = CreateEventNode("<![CDATA[&&&&&&&Escape the end ]]>]]&gt;");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestCDATAEscaping3()
		{
			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			TextWriter writer = new StringWriter();
			XmlLayoutSchemaLog4jNS layout = new XmlLayoutSchemaLog4jNS();
			LoggingEventData evt = CreateBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message = "]]>&&&&&&&Escape the begining";

			layout.Format(writer, new LoggingEvent(null, rep, evt));

			string expected = CreateEventNode("<![CDATA[]]>]]<![CDATA[>&&&&&&&Escape the begining]]>");

			Assert.AreEqual(expected, writer.ToString());
		}

#if NET_4_0 || MONO_4_0 || NETSTANDARD1_3
		[Test]
		public void BracketsInStackTracesKeepLogWellFormed() {
			XmlLayoutSchemaLog4jNS layout = new XmlLayoutSchemaLog4jNS();
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestLogger");
			Action<int> bar = foo => {
				try {
					throw new NullReferenceException();
				} catch (Exception ex) {
					log1.Error(string.Format("Error {0}", foo), ex);
				}
			};
			bar(42);

			// really only asserts there is no exception
			var loggedDoc = new XmlDocument();
			loggedDoc.LoadXml(stringAppender.GetString());
		}

		[Test]
		public void BracketsInStackTracesAreEscapedProperly() {
			XmlLayoutSchemaLog4jNS layout = new XmlLayoutSchemaLog4jNS();
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestLogger");
			Action<int> bar = foo => {
				try {
					throw new NullReferenceException();
				}
				catch (Exception ex) {
					log1.Error(string.Format("Error {0}", foo), ex);
				}
			};
			bar(42);

			var log = stringAppender.GetString();
#if NETSTANDARD1_3
			var startOfExceptionText = log.IndexOf("<log4j:throwable>", StringComparison.Ordinal) + 17;
			var endOfExceptionText = log.IndexOf("</log4j:throwable>", StringComparison.Ordinal);
#else
			var startOfExceptionText = log.IndexOf("<log4j:throwable>", StringComparison.InvariantCulture) + 17;
			var endOfExceptionText = log.IndexOf("</log4j:throwable>", StringComparison.InvariantCulture);
#endif
			var sub = log.Substring(startOfExceptionText, endOfExceptionText - startOfExceptionText);
			if (sub.StartsWith("<![CDATA["))
			{
				StringAssert.EndsWith("]]>", sub);
			}
			else
			{
				StringAssert.DoesNotContain("<", sub);
				StringAssert.DoesNotContain(">", sub);
			}
		}
#endif
	}
}
