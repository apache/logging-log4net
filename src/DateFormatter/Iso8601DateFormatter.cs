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

namespace log4net.DateFormatter
{
	/// <summary>
	/// Formats the <see cref="DateTime"/> specified as a string: 'YYYY-MM-dd HH:mm:ss,SSS'.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class Iso8601DateFormatter : AbsoluteTimeDateFormatter
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Iso8601DateFormatter" /> class.
		/// </summary>
		public Iso8601DateFormatter()
		{
		}

		#endregion Public Instance Constructors

		#region Override implementation of AbsoluteTimeDateFormatter

		/// <summary>
		/// Formats the date specified as a string: 'YYYY-MM-dd HH:mm:ss'
		/// the base class will append the ',SSS' milliseconds section.
		/// We will only be called at most once per second.
		/// </summary>
		/// <param name="dateToFormat">The date to format.</param>
		/// <param name="buffer">The string builder to write to.</param>
		override protected void FormatDateWithoutMillis(DateTime dateToFormat, StringBuilder buffer)
		{
			buffer.Append(dateToFormat.Year);

			buffer.Append('-');
			int month = dateToFormat.Month;
			if (month < 10)
			{
				buffer.Append('0');
			}
			buffer.Append(month);
			buffer.Append('-');

			int day = dateToFormat.Day;
			if (day < 10) 
			{
				buffer.Append('0');
			}
			buffer.Append(day);
			buffer.Append(' ');

			// Append the 'HH:mm:ss'
			base.FormatDateWithoutMillis(dateToFormat, buffer);
		}

		#endregion Override implementation of AbsoluteTimeDateFormatter
	}
}
