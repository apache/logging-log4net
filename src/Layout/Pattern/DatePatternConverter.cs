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
using System.Text;
using System.IO;

using log4net.Core;
using log4net.Util;
using log4net.DateFormatter;

namespace log4net.Layout.Pattern
{
	/// <summary>
	/// Date pattern converter, uses a <see cref="IDateFormatter"/> to format 
	/// the date of a <see cref="LoggingEvent"/>.
	/// </summary>
	/// <author>Nicko Cadell</author>
	internal sealed class DatePatternConverter : PatternLayoutConverter, IOptionHandler
	{
		private IDateFormatter m_df;
	
		#region Implementation of IOptionHandler

		/// <summary>
		/// Initialize the converter pattern based on the <see cref="PatternConverter.Option"/> property.
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
		public void ActivateOptions()
		{
			string dateFormatStr = Option;
			if (dateFormatStr == null)
			{
				dateFormatStr = AbsoluteTimeDateFormatter.Iso8601TimeDateFormat;
			}
			
			if (string.Compare(dateFormatStr, AbsoluteTimeDateFormatter.Iso8601TimeDateFormat, true, System.Globalization.CultureInfo.InvariantCulture) == 0) 
			{
				m_df = new Iso8601DateFormatter();
			}
			else if (string.Compare(dateFormatStr, AbsoluteTimeDateFormatter.AbsoluteTimeDateFormat, true, System.Globalization.CultureInfo.InvariantCulture) == 0)
			{
				m_df = new AbsoluteTimeDateFormatter();
			}
			else if (string.Compare(dateFormatStr, AbsoluteTimeDateFormatter.DateAndTimeDateFormat, true, System.Globalization.CultureInfo.InvariantCulture) == 0)
			{
				m_df = new DateTimeDateFormatter();
			}
			else 
			{
				try 
				{
					m_df = new SimpleDateFormatter(dateFormatStr);
				}
				catch (Exception e) 
				{
					LogLog.Error("DatePatternConverter: Could not instantiate SimpleDateFormatter with ["+dateFormatStr+"]", e);
					m_df = new Iso8601DateFormatter();
				}	
			}
		}

		#endregion

		/// <summary>
		/// Convert the pattern into the rendered message
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="loggingEvent">the event being logged</param>
		override protected void Convert(TextWriter writer, LoggingEvent loggingEvent)
		{
			try 
			{
				m_df.FormatDate(loggingEvent.TimeStamp, writer);
			}
			catch (Exception ex) 
			{
				LogLog.Error("DatePatternConverter: Error occurred while converting date.", ex);
			}
		}
	}
}
