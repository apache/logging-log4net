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
using System.IO;
using System.Text;
using System.Xml;

using log4net.Util;
using log4net.Core;

namespace log4net.Layout
{
	/// <summary>
	/// Layout that formats the log events as XML elements.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is an abstract class that must be subclassed by an implementation 
	/// to conform to a specific schema.
	/// </para>
	/// <para>
	/// Deriving classes must implement the <see cref="FormatXml"/> method.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	abstract public class XmlLayoutBase : LayoutSkeleton
	{
		#region Protected Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlLayoutBase" /> class
		/// with no location info.
		/// </summary>
		protected XmlLayoutBase() : this(false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlLayoutBase" /> class.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The <paramref name="locationInfo" /> parameter determines whether 
		/// location information will be output by the layout. If 
		/// <paramref name="locationInfo" /> is set to <c>true</c>, then the 
		/// file name and line number of the statement at the origin of the log 
		/// statement will be output. 
		/// </para>
		/// <para>
		/// If you are embedding this layout within an SMTPAppender
		/// then make sure to set the <b>LocationInfo</b> option of that 
		/// appender as well.
		/// </para>
		/// </remarks>
		protected XmlLayoutBase(bool locationInfo)
		{
			m_locationInfo = locationInfo;
		}

		#endregion Protected Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets a value indicating whether to include location information in 
		/// the XML events.
		/// </summary>
		/// <value>
		/// <c>true</c> if location information should be included in the XML 
		/// events; otherwise, <c>false</c>.
		/// </value>
		/// <remarks>
		/// <para>
		/// If <see cref="LocationInfo" /> is set to <c>true</c>, then the file 
		/// name and line number of the statement at the origin of the log 
		/// statement will be output. 
		/// </para>
		/// <para>
		/// If you are embedding this layout within an SMTPAppender
		/// then make sure to set the <b>LocationInfo</b> option of that 
		/// appender as well.
		/// </para>
		/// </remarks>
		public bool LocationInfo
		{
			get { return m_locationInfo; }
			set { m_locationInfo = value; }
		}

		#endregion

		#region Implementation of IOptionHandler

		/// <summary>
		/// Initialize layout options
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is part of the <see cref="IOptionHandler"/> delayed object
		/// activation scheme. The <see cref="ActivateOptions"/> method must 
		/// be called on this object after the configuration properties have
		/// been set. Until <see cref="ActivateOptions"/> is called this
		/// object is in an undefined state and must not be used. 
		/// </para>
		/// <para>
		/// If any of the configuration properties are modified then 
		/// <see cref="ActivateOptions"/> must be called again.
		/// </para>
		/// </remarks>
		override public void ActivateOptions() 
		{
			// nothing to do
		}

		#endregion Implementation of IOptionHandler

		#region Override implementation of LayoutSkeleton

		/// <summary>
		/// Gets the content type output by this layout. 
		/// </summary>
		/// <value>As this is the XML layout, the value is always "text/xml".</value>
		override public string ContentType
		{
			get { return "text/xml"; }
		}

		/// <summary>
		/// The XMLLayout does handle the exception contained within
		/// LoggingEvents. Thus, it returns <c>false</c>.
		/// </summary>
		override public bool IgnoresException
		{
			get { return false; }
		}

		/// <summary>
		/// Produces a formatted string.
		/// </summary>
		/// <param name="loggingEvent">The event being logged.</param>
		/// <param name="writer">The TextWriter to write the formatted event to</param>
		override public void Format(TextWriter writer, LoggingEvent loggingEvent) 
		{
			if (loggingEvent == null)
			{
				throw new ArgumentNullException("loggingEvent");
			}

			// Attach the protected writer to the TextWriter passed in
			m_protectCloseTextWriter.Attach(writer);

			XmlTextWriter xmlWriter = new XmlTextWriter(m_protectCloseTextWriter);
			xmlWriter.Formatting = Formatting.None;
			xmlWriter.Namespaces = false;

			// Write the event to the writer
			FormatXml(xmlWriter, loggingEvent);

			xmlWriter.WriteWhitespace(SystemInfo.NewLine);

			// Close on xmlWriter will ensure xml is flushed
			// the protected writer will ignore the actual close
			xmlWriter.Close();

			// detach from the writer
			m_protectCloseTextWriter.Attach(null);
		}

		#endregion Override implementation of LayoutSkeleton

		#region Protected Instance Methods

		/// <summary>
		/// Does the actual writing of the XML.
		/// </summary>
		/// <param name="writer">The writer to use to output the event to.</param>
		/// <param name="loggingEvent">The event to write.</param>
		abstract protected void FormatXml(XmlWriter writer, LoggingEvent loggingEvent);

		#endregion Protected Instance Methods

		#region Private Instance Fields
  
		/// <summary>
		/// Flag to indicate if location information should be included in
		/// the XML events.
		/// </summary>
		private bool m_locationInfo = false;

		/// <summary>
		/// Writer adapter that ignores Close
		/// </summary>
		private readonly ProtectCloseTextWriter m_protectCloseTextWriter = new ProtectCloseTextWriter(null);

		#endregion Private Instance Fields
	}
}
