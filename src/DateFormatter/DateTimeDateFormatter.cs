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
using System.Globalization;

namespace log4net.DateFormatter
{
	/// <summary>
	/// Formats a <see cref="DateTime"/> in the format "dd MMM YYYY HH:mm:ss,SSS" for example, "06 Nov 1994 15:49:37,459".
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	/// <author>Angelika Schnagl</author>
	public class DateTimeDateFormatter : AbsoluteTimeDateFormatter
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DateTimeDateFormatter" /> class.
		/// </summary>
		public DateTimeDateFormatter()
		{
			m_dateTimeFormatInfo = DateTimeFormatInfo.InvariantInfo;
		}

		#endregion Public Instance Constructors

		#region Override implementation of AbsoluteTimeDateFormatter

		/// <summary>
		/// Formats the date as: "dd MMM YYYY HH:mm:ss"
		/// the base class will append the ',SSS' milliseconds section.
		/// We will only be called at most once per second.
		/// </summary>
		/// <remarks>
		/// Formats a DateTime in the format "dd MMM YYYY HH:mm:ss" for example, "06 Nov 1994 15:49:37".
		/// </remarks>
		/// <param name="dateToFormat">The date to format.</param>
		/// <param name="buffer">The string builder to write to.</param>
		override protected void FormatDateWithoutMillis(DateTime dateToFormat, StringBuilder buffer)
		{
			int day = dateToFormat.Day;
			if (day < 10) 
			{
				buffer.Append('0');
			}
			buffer.Append(day);
			buffer.Append(' ');		

			buffer.Append(m_dateTimeFormatInfo.GetAbbreviatedMonthName(dateToFormat.Month));
			buffer.Append(' ');	

			buffer.Append(dateToFormat.Year);
			buffer.Append(' ');

			// Append the 'HH:mm:ss'
			base.FormatDateWithoutMillis(dateToFormat, buffer);
		}

		#endregion Override implementation of AbsoluteTimeDateFormatter

		#region Private Instance Fields

		/// <summary>
		/// The format info for the invariant culture.
		/// </summary>
		private readonly DateTimeFormatInfo  m_dateTimeFormatInfo;

		#endregion Private Instance Fields
	}
}
