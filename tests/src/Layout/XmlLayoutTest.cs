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

using System;
using System.IO;
using System.Xml;

using log4net.Config;
using log4net.Util;
using log4net.Layout;
using log4net.Core;
using log4net.Repository;
using log4net.Tests.Appender;

using NUnit.Framework;

namespace log4net.Tests.Layout
{
	[TestFixture] public class XmlLayoutTest
	{
		/// <summary>
		/// Build a basic <see cref="LoggingEventData"/> object with some default values.
		/// </summary>
		/// <returns>A useful LoggingEventData object</returns>
		private LoggingEventData createBaseEvent()
		{
			LoggingEventData ed=new LoggingEventData();
			ed.Domain="Tests";
			ed.ExceptionString="";
			ed.Identity="TestRunner";
			ed.Level=Level.Info;
			ed.LocationInfo=new LocationInfo(this.GetType());
			ed.LoggerName="TestLogger";
			ed.Message="Test message";
			ed.ThreadName="TestThread";
			ed.TimeStamp=DateTime.Today;
			ed.UserName="TestRunner";
			ed.Properties=new PropertiesDictionary();

			return ed;
		}

		private string createEventNode(string message)
		{
			return String.Format("<event logger=\"TestLogger\" timestamp=\"{0}\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message>{1}</message></event>\r\n", 
#if NET_2_0
			    XmlConvert.ToString(DateTime.Today, XmlDateTimeSerializationMode.Local),
#else
			    XmlConvert.ToString(DateTime.Today),
#endif
				message);
		}

		private string createEventNode(string key, string value)
		{
			return String.Format("<event logger=\"TestLogger\" timestamp=\"{0:s}\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message>Test message</message><properties><data name=\"{1}\" value=\"{2}\" /></properties></event>\r\n",
#if NET_2_0
			    XmlConvert.ToString(DateTime.Today, XmlDateTimeSerializationMode.Local),
#else
			    XmlConvert.ToString(DateTime.Today),
#endif
				key,
				value);
		}

		[Test] public void TestBasicEventLogging()
		{
			TextWriter writer=new StringWriter();
			XmlLayout layout=new XmlLayout();
			LoggingEventData evt=createBaseEvent();

			layout.Format(writer,new LoggingEvent(evt));

			string expected = createEventNode("Test message");
			
			Assert.AreEqual	(expected, writer.ToString());
		}

		[Test] public void TestIllegalCharacterMasking()
		{
			TextWriter writer=new StringWriter();
			XmlLayout layout=new XmlLayout();
			LoggingEventData evt=createBaseEvent();

			evt.Message="This is a masked char->\uFFFF";

			layout.Format(writer,new LoggingEvent(evt));

			string expected = createEventNode("This is a masked char-&gt;?");
			
			Assert.AreEqual	(expected, writer.ToString());
		}

		[Test] public void TestCDATAEscaping1()
		{
			TextWriter writer=new StringWriter();
			XmlLayout layout=new XmlLayout();
			LoggingEventData evt=createBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message="&&&&&&&Escape this ]]>. End here.";

			layout.Format(writer,new LoggingEvent(evt));

			string expected = createEventNode("<![CDATA[&&&&&&&Escape this ]]>]]<![CDATA[>. End here.]]>");
			
			Assert.AreEqual	(expected, writer.ToString());
		}

		[Test] public void TestCDATAEscaping2()
		{
			TextWriter writer=new StringWriter();
			XmlLayout layout=new XmlLayout();
			LoggingEventData evt=createBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message="&&&&&&&Escape the end ]]>";

			layout.Format(writer,new LoggingEvent(evt));

			string expected = createEventNode("<![CDATA[&&&&&&&Escape the end ]]>]]&gt;");
			
			Assert.AreEqual	(expected, writer.ToString());
		}

		[Test] public void TestCDATAEscaping3()
		{
			TextWriter writer=new StringWriter();
			XmlLayout layout=new XmlLayout();
			LoggingEventData evt=createBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message="]]>&&&&&&&Escape the begining";

			layout.Format(writer,new LoggingEvent(evt));

			string expected = createEventNode("<![CDATA[]]>]]<![CDATA[>&&&&&&&Escape the begining]]>");
			
			Assert.AreEqual	(expected, writer.ToString());
		}

		[Test] public void TestBase64EventLogging()
		{
			TextWriter writer=new StringWriter();
			XmlLayout layout=new XmlLayout();
			LoggingEventData evt=createBaseEvent();

			layout.Base64EncodeMessage=true;
			layout.Format(writer,new LoggingEvent(evt));

			string expected = createEventNode("VGVzdCBtZXNzYWdl");
			
			Assert.AreEqual	(expected, writer.ToString());
		}

		[Test] public void TestPropertyEventLogging()
		{
			LoggingEventData evt=createBaseEvent();
			evt.Properties["Property1"]="prop1";

			XmlLayout layout=new XmlLayout();
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = createEventNode("Property1",  "prop1");
			
			Assert.AreEqual	(expected, stringAppender.GetString());
		}

		[Test] public void TestBase64PropertyEventLogging()
		{
			LoggingEventData evt=createBaseEvent();
			evt.Properties["Property1"]="prop1";

			XmlLayout layout=new XmlLayout();
			layout.Base64EncodeProperties=true;
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = createEventNode("Property1", "cHJvcDE=");
			
			Assert.AreEqual	(expected, stringAppender.GetString());
		}

		[Test] public void TestPropertyCharacterEscaping()
		{
			LoggingEventData evt=createBaseEvent();
			evt.Properties["Property1"]="prop1 \"quoted\"";

			XmlLayout layout=new XmlLayout();
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = createEventNode("Property1", "prop1 &quot;quoted&quot;"); 
			
			Assert.AreEqual	(expected, stringAppender.GetString());
		}

		[Test] public void TestPropertyIllegalCharacterMasking()
		{
			LoggingEventData evt=createBaseEvent();
			evt.Properties["Property1"]="mask this ->\uFFFF";

			XmlLayout layout=new XmlLayout();
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = createEventNode("Property1", "mask this -&gt;?");
			
			Assert.AreEqual	(expected, stringAppender.GetString());
		}

		[Test] public void TestPropertyIllegalCharacterMaskingInName()
		{
			LoggingEventData evt=createBaseEvent();
			evt.Properties["Property\uFFFF"]="mask this ->\uFFFF";

			XmlLayout layout=new XmlLayout();
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = createEventNode("Property?", "mask this -&gt;?");
			
			Assert.AreEqual	(expected, stringAppender.GetString());
		}
	}
}