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

namespace log4net.Layout.Pattern
{
	/// <summary>
	/// Converter to include event location information
	/// </summary>
	/// <author>Nicko Cadell</author>
	internal sealed class MethodLocationPatternConverter : PatternLayoutConverter 
	{
		/// <summary>
		/// Convert the pattern to the rendered message
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="loggingEvent">the event being logged</param>
		/// <returns>the relevant location information</returns>
		override protected void Convert(TextWriter writer, LoggingEvent loggingEvent)
		{
			writer.Write( loggingEvent.LocationInformation.MethodName );
		}
	}
}
