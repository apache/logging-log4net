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
	/// Converter for logger name
	/// </summary>
	/// <author>Nicko Cadell</author>
	internal sealed class LoggerPatternConverter : NamedPatternConverter 
	{
		/// <summary>
		/// Gets the fully qualified name of the logger
		/// </summary>
		/// <param name="loggingEvent">the event being logged</param>
		/// <returns>the logger name</returns>
		override protected string GetFullyQualifiedName(LoggingEvent loggingEvent) 
		{
			return loggingEvent.LoggerName;
		}
	}
}
