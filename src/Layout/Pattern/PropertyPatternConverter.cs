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
using System.Collections;

using log4net.Core;
using log4net.Repository;

namespace log4net.Layout.Pattern
{
	/// <summary>
	/// Property pattern converter
	/// </summary>
	/// <author>Nicko Cadell</author>
	internal sealed class PropertyPatternConverter : PatternLayoutConverter 
	{
		/// <summary>
		/// To the conversion
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="loggingEvent">the event being logged</param>
		/// <returns>the result of converting the pattern</returns>
		override protected void Convert(TextWriter writer, LoggingEvent loggingEvent)
		{
			if (Option != null)
			{
				// Write the value for the specified key
				WriteObject(writer, loggingEvent.Repository, loggingEvent.Properties[Option]);
			}
			else
			{
				// Write all the key value pairs
				WriteDictionary(writer, loggingEvent.Repository, loggingEvent.Properties);
			}
		}
	}
}
