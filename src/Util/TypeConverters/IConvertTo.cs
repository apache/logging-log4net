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
	/// This interface supports conversion from a single type to arbitary types.
	/// See <see cref="TypeConverterAttribute"/>.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public interface IConvertTo
	{
		/// <summary>
		/// Returns whether this converter can convert the object to the specified type
		/// </summary>
		/// <param name="targetType">A Type that represents the type you want to convert to</param>
		/// <returns>true if the conversion is possible</returns>
		bool CanConvertTo(Type targetType);

		/// <summary>
		/// Converts the given value object to the specified type, using the arguments
		/// </summary>
		/// <param name="source">the object to convert</param>
		/// <param name="targetType">The Type to convert the value parameter to</param>
		/// <returns>the converted object</returns>
		object ConvertTo(object source, Type targetType);
	}
}
