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

using log4net.Util;
using log4net.DateFormatter;
using log4net.Core;

namespace log4net.Util.PatternStringConverters
{
	/// <summary>
	/// Environment pattern converter expamds environment variables
	/// </summary>
	/// <author>Nicko Cadell</author>
	internal sealed class EnvironmentPatternConverter : PatternConverter
	{
		/// <summary>
		/// Convert the pattern into the rendered message
		/// </summary>
		/// <param name="writer">the writer to write to</param>
		/// <param name="state">null, state is not set</param>
		override protected void Convert(TextWriter writer, object state) 
		{
			try 
			{
				if (this.Option != null && this.Option.Length > 0)
				{
					// Lookup the environement vairable
					string envValue = Environment.GetEnvironmentVariable(this.Option);
					if (envValue != null && envValue.Length > 0)
					{
						writer.Write(envValue);
					}
				}
			}
			catch(System.Security.SecurityException secEx)
			{
				// This security exception will occur if the caller does not have 
				// unrestricted environment permission. If this occurs the expansion 
				// will be skipped with the following warning message.
				LogLog.Debug("DOMConfigurator: Security exception while trying to expand environment variables. Error Ignored. No Expansion.", secEx);
			}
			catch (Exception ex) 
			{
				LogLog.Error("EnvironmentPatternConverter: Error occurred while converting environment variable.", ex);
			}
		}
	}
}
