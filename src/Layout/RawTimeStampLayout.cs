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

using log4net.Core;
using log4net.Util;

namespace log4net.Layout
{
	/// <summary>
	/// Extract the date from the <see cref="LoggingEvent"/>
	/// </summary>
	/// <remarks>
	/// Extract the date from the <see cref="LoggingEvent"/>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class RawTimeStampLayout : IRawLayout
	{
		#region Constructors

		/// <summary>
		/// Constructs a RawDateLayout
		/// </summary>
		/// <remarks>
		/// </remarks>
		public RawTimeStampLayout()
		{
		}

		#endregion
  
		#region Implementation of IRawLayout

		/// <summary>
		/// Implement this method to create your own layout format.
		/// </summary>
		/// <param name="loggingEvent">The event to format</param>
		/// <returns>returns the formatted event</returns>
		/// <remarks>
		/// <para>Implement this method to create your own layout format.</para>
		/// </remarks>
		public virtual object Format(LoggingEvent loggingEvent)
		{
			return loggingEvent.TimeStamp;
		}

		#endregion
	}
}
