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
using System.IO;

using log4net.Core;
using log4net.Util;
using log4net.DateFormatter;

namespace log4net.Layout.Pattern
{
	/// <summary>
	/// Date pattern converter, uses a <see cref="IDateFormatter"/> to format the date
	/// </summary>
	/// <author>Nicko Cadell</author>
	internal sealed class DatePatternConverter : PatternLayoutConverter, IOptionHandler
	{
		private IDateFormatter m_df;
	
		#region Implementation of IOptionHandler

		public void ActivateOptions()
		{
			string dateFormatStr = Option;
			if (dateFormatStr == null)
			{
				dateFormatStr = AbsoluteTimeDateFormatter.Isi8601TimeDateFormat;
			}
			
			if (string.Compare(dateFormatStr, AbsoluteTimeDateFormatter.Isi8601TimeDateFormat, true, System.Globalization.CultureInfo.InvariantCulture) == 0) 
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
		/// <returns></returns>
		override protected void Convert(TextWriter writer, LoggingEvent loggingEvent)
		{
			try 
			{
				m_df.FormatDate(loggingEvent.TimeStamp, writer);
			}
			catch (Exception ex) 
			{
				LogLog.Error("PatternParser: Error occurred while converting date.", ex);
			}
		}
	}
}
