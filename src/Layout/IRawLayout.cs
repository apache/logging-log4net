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

using log4net;
using log4net.Core;
using log4net.Util.TypeConverters;

namespace log4net.Layout
{
	/// <summary>
	/// Interface for raw layout objects
	/// </summary>
	/// <remarks>
	/// <para>Interface used to format a <see cref="LoggingEvent"/>
	/// to an object.</para>
	/// 
	/// <para>This interface should not be confused with the
	/// <see cref="ILayout"/> interface. This interface is used in
	/// only certain specialised situations where a raw object is
	/// required rather than a formatted string. The <see cref="ILayout"/>
	/// is not generally usefull than this interface.</para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	[TypeConverter(typeof(RawLayoutConverter))]
	public interface IRawLayout
	{
		/// <summary>
		/// Implement this method to create your own layout format.
		/// </summary>
		/// <param name="loggingEvent">The event to format</param>
		/// <returns>returns the formatted event</returns>
		/// <remarks>
		/// <para>Implement this method to create your own layout format.</para>
		/// </remarks>
		object Format(LoggingEvent loggingEvent);
	}
}
