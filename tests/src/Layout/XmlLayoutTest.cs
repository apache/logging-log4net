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
			ed.TimeStamp=new DateTime(2005,8,24,12,0,0);
			ed.UserName="TestRunner";
			ed.Properties=new PropertiesDictionary();

			return ed;
		}

		[Test] public void TestBasicEventLogging()
		{
			TextWriter writer=new StringWriter();
			XmlLayout layout=new XmlLayout();
			LoggingEventData evt=createBaseEvent();

			layout.Format(writer,new LoggingEvent(evt));
			
			Assertion.AssertEquals	(
				"<event logger=\"TestLogger\" timestamp=\"2005-08-24T12:00:00.0000000+01:00\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message>Test message</message></event>\r\n",
				writer.ToString()
				);
		}

		[Test] public void TestIllegalCharacterMasking()
		{
			TextWriter writer=new StringWriter();
			XmlLayout layout=new XmlLayout();
			LoggingEventData evt=createBaseEvent();

			evt.Message="This is a masked char->\uFFFF";

			layout.Format(writer,new LoggingEvent(evt));
			
			Assertion.AssertEquals	(
									"<event logger=\"TestLogger\" timestamp=\"2005-08-24T12:00:00.0000000+01:00\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message>This is a masked char-&gt;?</message></event>\r\n",
									writer.ToString()
									);
		}

		[Test] public void TestCDATAEscaping1()
		{
			TextWriter writer=new StringWriter();
			XmlLayout layout=new XmlLayout();
			LoggingEventData evt=createBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message="&&&&&&&Escape this ]]>. End here.";

			layout.Format(writer,new LoggingEvent(evt));
			
			Assertion.AssertEquals	(
				"<event logger=\"TestLogger\" timestamp=\"2005-08-24T12:00:00.0000000+01:00\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message><![CDATA[&&&&&&&Escape this ]]>]]<![CDATA[>. End here.]]></message></event>\r\n",
				writer.ToString()
				);
		}

		[Test] public void TestCDATAEscaping2()
		{
			TextWriter writer=new StringWriter();
			XmlLayout layout=new XmlLayout();
			LoggingEventData evt=createBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message="&&&&&&&Escape the end ]]>";

			layout.Format(writer,new LoggingEvent(evt));
			
			Assertion.AssertEquals	(
				"<event logger=\"TestLogger\" timestamp=\"2005-08-24T12:00:00.0000000+01:00\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message><![CDATA[&&&&&&&Escape the end ]]>]]&gt;</message></event>\r\n",
				writer.ToString()
				);
		}

		[Test] public void TestCDATAEscaping3()
		{
			TextWriter writer=new StringWriter();
			XmlLayout layout=new XmlLayout();
			LoggingEventData evt=createBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message="]]>&&&&&&&Escape the begining";

			layout.Format(writer,new LoggingEvent(evt));
			
			Assertion.AssertEquals	(
				"<event logger=\"TestLogger\" timestamp=\"2005-08-24T12:00:00.0000000+01:00\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message><![CDATA[]]>]]<![CDATA[>&&&&&&&Escape the begining]]></message></event>\r\n",
				writer.ToString()
				);
		}

		[Test] public void TestBase64EventLogging()
		{
			TextWriter writer=new StringWriter();
			XmlLayout layout=new XmlLayout();
			LoggingEventData evt=createBaseEvent();

			layout.Base64EncodeMessage=true;
			layout.Format(writer,new LoggingEvent(evt));
			
			Assertion.AssertEquals	(
				"<event logger=\"TestLogger\" timestamp=\"2005-08-24T12:00:00.0000000+01:00\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message>VGVzdCBtZXNzYWdl</message></event>\r\n",
				writer.ToString()
				);
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
			
			Assertion.AssertEquals	(
				"<event logger=\"TestLogger\" timestamp=\"2005-08-24T12:00:00.0000000+01:00\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message>Test message</message><properties><data name=\"Property1\" value=\"prop1\" /></properties></event>\r\n",
				stringAppender.GetString()
				);
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
			
			Assertion.AssertEquals	(
				"<event logger=\"TestLogger\" timestamp=\"2005-08-24T12:00:00.0000000+01:00\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message>Test message</message><properties><data name=\"Property1\" value=\"cHJvcDE=\" /></properties></event>\r\n",
				stringAppender.GetString()
				);
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
			
			Assertion.AssertEquals	(
				"<event logger=\"TestLogger\" timestamp=\"2005-08-24T12:00:00.0000000+01:00\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message>Test message</message><properties><data name=\"Property1\" value=\"prop1 &quot;quoted&quot;\" /></properties></event>\r\n",
				stringAppender.GetString()
				);
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
			
			Assertion.AssertEquals	(
				"<event logger=\"TestLogger\" timestamp=\"2005-08-24T12:00:00.0000000+01:00\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message>Test message</message><properties><data name=\"Property1\" value=\"mask this -&gt;?\" /></properties></event>\r\n",
				stringAppender.GetString()
				);
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
			
			Assertion.AssertEquals	(
				"<event logger=\"TestLogger\" timestamp=\"2005-08-24T12:00:00.0000000+01:00\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message>Test message</message><properties><data name=\"Property?\" value=\"mask this -&gt;?\" /></properties></event>\r\n",
				stringAppender.GetString()
				);
		}
	}
}