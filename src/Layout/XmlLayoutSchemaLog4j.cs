#region Copyright
//
// This framework is based on log4j see http://jakarta.apache.org/log4j
// Copyright (C) The Apache Software Foundation. All rights reserved.
//
// This software is published under the terms of the Apache Software
// License version 1.1, a copy of which has been included with this
// distribution in the LICENSE.txt file.
// 
#endregion

using System;
using System.Text;
using System.Xml;
using System.IO;

using log4net.Core;
using log4net.Util;

namespace log4net.Layout
{
	/// <summary>
	/// Layout that formats the log events as XML elements compatible with the log4j schema
	/// </summary>
	/// <remarks>
	/// <para>
	/// Formats the log events acording to the http://jakarta.apache.org/log4j schema.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public class XmlLayoutSchemaLog4j : XmlLayoutBase
	{
		#region Static Members

		private static readonly DateTime s_date1970 = new DateTime(1970, 1, 1);

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs an XMLLayoutSchemaLog4j
		/// </summary>
		public XmlLayoutSchemaLog4j() : base()
		{
		}

		/// <summary>
		/// Constructs an XMLLayoutSchemaLog4j.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The <b>LocationInfo</b> option takes a boolean value. By
		/// default, it is set to false which means there will be no location
		/// information output by this layout. If the the option is set to
		/// true, then the file name and line number of the statement
		/// at the origin of the log statement will be output. 
		/// </para>
		/// <para>
		/// If you are embedding this layout within an SMTPAppender
		/// then make sure to set the <b>LocationInfo</b> option of that 
		/// appender as well.
		/// </para>
		/// </remarks>
		public XmlLayoutSchemaLog4j(bool locationInfo) :  base(locationInfo)
		{
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// The version of the log4j schema to use.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Only version 1.2 of the log4j schema is supported.
		/// </para>
		/// </remarks>
		public string Version
		{
			get { return "1.2"; }
			set 
			{ 
				if (value != "1.2")
				{
					throw new ArgumentException("Only version 1.2 of the log4j schema is currently supported");
				}
			}
		}

		#endregion

		/* Example log4j schema event

<log4j:event logger="first logger" level="ERROR" thread="Thread-3" timestamp="1051494121460">
  <log4j:message><![CDATA[errormsg 3]]></log4j:message>
  <log4j:NDC><![CDATA[third]]></log4j:NDC>
  <log4j:MDC>
    <log4j:data name="some string" value="some valuethird"/>
  </log4j:MDC>
  <log4j:throwable><![CDATA[java.lang.Exception: someexception-third
 	at org.apache.log4j.chainsaw.Generator.run(Generator.java:94)
]]></log4j:throwable>
  <log4j:locationInfo class="org.apache.log4j.chainsaw.Generator"
method="run" file="Generator.java" line="94"/>
  <log4j:properties>
    <log4j:data name="log4jmachinename" value="windows"/>
    <log4j:data name="log4japp" value="udp-generator"/>
  </log4j:properties>
</log4j:event>

		*/

		/// <summary>
		/// Actualy do the writing of the xml
		/// </summary>
		/// <param name="writer">the writer to use</param>
		/// <param name="loggingEvent">the event to write</param>
		override protected void FormatXml(XmlWriter writer, LoggingEvent loggingEvent)
		{
			// Translate logging events for log4j

			// Translate hostname property
			if (loggingEvent.Properties[LoggingEvent.HostNameProperty] != null && 
				loggingEvent.Properties["log4jmachinename"] == null)
			{
				loggingEvent.Properties["log4jmachinename"] = loggingEvent.Properties[LoggingEvent.HostNameProperty];
			}

			// translate appdomain name
			if (loggingEvent.Properties["log4japp"] == null && 
				loggingEvent.Domain != null && 
				loggingEvent.Domain.Length > 0)
			{
				loggingEvent.Properties["log4japp"] = loggingEvent.Domain;
			}

			// translate identity name
			if (loggingEvent.Identity != null && 
				loggingEvent.Identity.Length > 0 && 
				loggingEvent.Properties[LoggingEvent.IdentityProperty] == null)
			{
				loggingEvent.Properties[LoggingEvent.IdentityProperty] = loggingEvent.Identity;
			}

			// translate user name
			if (loggingEvent.UserName != null && 
				loggingEvent.UserName.Length > 0 && 
				loggingEvent.Properties[LoggingEvent.UserNameProperty] == null)
			{
				loggingEvent.Properties[LoggingEvent.UserNameProperty] = loggingEvent.UserName;
			}

			// Write the start element
			writer.WriteStartElement("log4j:event");
			writer.WriteAttributeString("logger", loggingEvent.LoggerName);

			// Calculate the timestamp as the number of milliseconds since january 1970
			TimeSpan timeSince1970 = loggingEvent.TimeStamp - s_date1970;
			writer.WriteAttributeString("timestamp", XmlConvert.ToString((long)timeSince1970.TotalMilliseconds));
			writer.WriteAttributeString("level", loggingEvent.Level.ToString());
			writer.WriteAttributeString("thread", loggingEvent.ThreadName);
    
			// Append the message text
			writer.WriteStartElement("log4j:message");
			Transform.WriteEscapedXmlString(writer, loggingEvent.RenderedMessage);
			writer.WriteEndElement();

			if (loggingEvent.NestedContext != null && loggingEvent.NestedContext.Length > 0)
			{
				// Append the NDC text
				writer.WriteStartElement("log4j:NDC");
				Transform.WriteEscapedXmlString(writer, loggingEvent.NestedContext);
				writer.WriteEndElement();
			}

			if (loggingEvent.MappedContext != null && loggingEvent.MappedContext.Count > 0)
			{
				// Append the MDC text
				writer.WriteStartElement("log4j:MDC");
				foreach(System.Collections.DictionaryEntry entry in loggingEvent.MappedContext)
				{
					writer.WriteStartElement("log4j:data");
					writer.WriteAttributeString("name", entry.Key.ToString());
					writer.WriteAttributeString("value", entry.Value.ToString());
					writer.WriteEndElement();
				}
				writer.WriteEndElement();
			}

			if (loggingEvent.Properties != null)
			{
				// Append the properties text
				string[] propKeys = loggingEvent.Properties.GetKeys();
				if (propKeys.Length > 0)
				{
					writer.WriteStartElement("log4j:properties");
					foreach(string key in propKeys)
					{
						writer.WriteStartElement("log4j:data");
						writer.WriteAttributeString("name", key);
						writer.WriteAttributeString("value", loggingEvent.Properties[key].ToString());
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}
			}

			string exceptionStr = loggingEvent.GetExceptionStrRep();
			if (exceptionStr != null && exceptionStr.Length > 0)
			{
				// Append the stack trace line
				writer.WriteStartElement("log4j:throwable");
				Transform.WriteEscapedXmlString(writer, exceptionStr);
				writer.WriteEndElement();
			}

			if (LocationInfo)
			{ 
				LocationInfo locationInfo = loggingEvent.LocationInformation;

				writer.WriteStartElement("log4j:locationInfo");
				writer.WriteAttributeString("class", locationInfo.ClassName);
				writer.WriteAttributeString("method", locationInfo.MethodName);
				writer.WriteAttributeString("file", locationInfo.FileName);
				writer.WriteAttributeString("line", locationInfo.LineNumber);
				writer.WriteEndElement();
			}

			writer.WriteEndElement();
		}
	}
}

