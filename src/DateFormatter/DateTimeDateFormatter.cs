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
