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
using System.Globalization;
using System.Text;
using System.IO;

using log4net.Core;
using log4net.Util;

namespace log4net.Layout.Pattern
{
	/// <summary>
	/// Converter to deal with '.' separated strings
	/// </summary>
	/// <author>Nicko Cadell</author>
	internal abstract class NamedPatternConverter : PatternLayoutConverter, IOptionHandler
	{
		protected int m_precision = 0;

		#region Implementation of IOptionHandler

		public void ActivateOptions()
		{
			if (Option == null) 
			{
				m_precision = 0;
			}
			else
			{
				string optStr = Option.Trim();
				if (optStr.Length == 0)
				{
					m_precision = 0;
				}
				else
				{
					try 
					{
						m_precision = int.Parse(optStr, NumberFormatInfo.InvariantInfo);
						if (m_precision <= 0) 
						{
							LogLog.Error("Precision option (" + optStr + ") isn't a positive integer.");
							m_precision = 0;
						}
					} 
					catch (Exception e) 
					{
						LogLog.Error("Precision option \"" + optStr + "\" not a decimal integer.", e);
						m_precision = 0;
					}
				}
			}
		}

		#endregion

		/// <summary>
		/// Overridden by subclasses to get the fully qualified name before the
		/// precision is applied to it.
		/// </summary>
		/// <param name="loggingEvent">the event being logged</param>
		/// <returns>the fully qualified name</returns>
		abstract protected string GetFullyQualifiedName(LoggingEvent loggingEvent);
	
		/// <summary>
		/// Convert the pattern to the rendered message
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="loggingEvent">the event being logged</param>
		/// <returns>the precision of the fully qualified name specified</returns>
		override protected void Convert(TextWriter writer, LoggingEvent loggingEvent)
		{
			string n = GetFullyQualifiedName(loggingEvent);
			if (m_precision <= 0)
			{
				writer.Write( n );
			}
			else 
			{
				int len = n.Length;

				// We subtract 1 from 'len' when assigning to 'end' to avoid out of
				// bounds exception in return r.substring(end+1, len). This can happen if
				// precision is 1 and the logger name ends with a dot. 
				int end = len -1 ;
				for(int i = m_precision; i > 0; i--) 
				{	  
					end = n.LastIndexOf('.', end-1);
					if (end == -1)
					{
						writer.Write( n );
						return;
					}
				}
				writer.Write( n.Substring(end+1, len-end-1) );
			}	  
		}
	}
}
