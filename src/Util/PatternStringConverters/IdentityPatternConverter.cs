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

namespace log4net.Util.PatternStringConverters
{
	/// <summary>
	/// Converter to include event identity
	/// </summary>
	/// <author>Nicko Cadell</author>
	internal sealed class IdentityPatternConverter : PatternConverter 
	{
		/// <summary>
		/// Convert the pattern to the rendered message
		/// </summary>
		/// <param name="writer">the writer to write to</param>
		/// <param name="state">null, state is not set</param>
		override protected void Convert(TextWriter writer, object state) 
		{
#if (NETCF || SSCLI)
			// On compact framework there's no notion of current thread principals
			writer.Write( "NOT AVAILABLE" );
#else
			try
			{
				if (System.Threading.Thread.CurrentPrincipal != null && 
					System.Threading.Thread.CurrentPrincipal.Identity != null &&
					System.Threading.Thread.CurrentPrincipal.Identity.Name != null)
				{
					writer.Write( System.Threading.Thread.CurrentPrincipal.Identity.Name );
				}
			}
			catch(System.Security.SecurityException)
			{
				// This security exception will occur if the caller does not have 
				// some undefined set of SecurityPermission flags.
				LogLog.Debug("LoggingEvent: Security exception while trying to get current thread principal. Error Ignored.");

				writer.Write( "NOT AVAILABLE" );
			}
#endif
		}
	}
}
