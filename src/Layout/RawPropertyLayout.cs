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
	/// Extract the value of a property from the <see cref="LoggingEvent"/>
	/// </summary>
	/// <remarks>
	/// Extract the value of a property from the <see cref="LoggingEvent"/>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public class RawPropertyLayout : IRawLayout
	{
		#region Constructors

		/// <summary>
		/// Constructs a RawPropertyLayout
		/// </summary>
		/// <remarks>
		/// </remarks>
		public RawPropertyLayout()
		{
		}

		#endregion

		private string m_key;

		/// <summary>
		/// The name of the value to lookup in the <see cref="LoggingEvent.Properties"/> collection.
		/// </summary>
		public string Key
		{
			get { return m_key; }
			set { m_key = value; }
		}
  
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
			return loggingEvent.Properties[m_key];
		}

		#endregion
	}
}
