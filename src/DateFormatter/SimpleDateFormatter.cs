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
using System.IO;

namespace log4net.DateFormatter
{
	/// <summary>
	/// Formats the <see cref="DateTime"/> using the <see cref="DateTime.ToString"/> method.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class SimpleDateFormatter : IDateFormatter
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleDateFormatter" /> class 
		/// with the specified format string.
		/// </summary>
		/// <remarks>
		/// The format string must be compatible with the options
		/// that can be supplied to <see cref="DateTime.ToString"/>.
		/// </remarks>
		/// <param name="format">The format string.</param>
		public SimpleDateFormatter(string format)
		{
			m_formatString = format;
		}

		#endregion Public Instance Constructors

		#region Implementation of IDateFormatter

		/// <summary>
		/// Formats the date using <see cref="DateTime.ToString"/>.
		/// </summary>
		/// <param name="dateToFormat">The date to convert to a string.</param>
		/// <param name="writer">The writer to write to.</param>
		virtual public void FormatDate(DateTime dateToFormat, TextWriter writer)
		{
			writer.Write(dateToFormat.ToString(m_formatString, System.Globalization.DateTimeFormatInfo.InvariantInfo));
		}

		#endregion

		#region Private Instance Fields

		/// <summary>
		/// The format string used to format the <see cref="DateTime" />.
		/// </summary>
		/// <remarks>
		/// The format string must be compatible with the options
		/// that can be supplied to <see cref="DateTime.ToString"/>.
		/// </remarks>
		private readonly string m_formatString;

		#endregion Private Instance Fields
	}
}
