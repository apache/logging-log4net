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

namespace log4net.Util.TypeConverters
{
	/// <summary>
	/// Interface supported by type converters
	/// </summary>
	/// <remarks>
	/// This interface supports conversion from arbitary types
	/// to a single target type. See <see cref="TypeConverterAttribute"/>.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public interface IConvertFrom
	{
		/// <summary>
		/// Can the source type be converted to the type supported by this object
		/// </summary>
		/// <param name="sourceType">the type to convert</param>
		/// <returns>true if the conversion is possible</returns>
		bool CanConvertFrom(Type sourceType);

		/// <summary>
		/// Convert the source object to the type supported by this object
		/// </summary>
		/// <param name="source">the object to convert</param>
		/// <returns>the converted object</returns>
		object ConvertFrom(object source);
	}
}
