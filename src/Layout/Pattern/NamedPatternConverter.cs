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
