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
