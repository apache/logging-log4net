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

using log4net.Util;

namespace log4net.Util.TypeConverters
{
	/// <summary>
	/// Implementation of <see cref="IConvertFrom"/> that converts 
	/// a <see cref="PatternString"/> to a string ans a string into
	/// a <see cref="PatternString"/>.
	/// </summary>
	/// <author>Nicko Cadell</author>
	public class PatternStringConverter : IConvertTo, IConvertFrom
	{
		#region Implementation of IConvertTo

		/// <summary>
		/// Returns whether this converter can convert the object to the specified type
		/// </summary>
		/// <param name="targetType">A Type that represents the type you want to convert to</param>
		/// <returns>true if the conversion is possible</returns>
		public bool CanConvertTo(Type targetType)
		{
			return (typeof(string).IsAssignableFrom(targetType));
		}

		/// <summary>
		/// Converts the given value object to the specified type, using the arguments
		/// </summary>
		/// <param name="source">the object to convert</param>
		/// <param name="targetType">The Type to convert the value parameter to</param>
		/// <returns>the converted object</returns>
		public object ConvertTo(object source, Type targetType)
		{
			if (typeof(string).IsAssignableFrom(targetType) && source is PatternString)
			{
				return ((PatternString)source).Format();
			}
			throw ConversionNotSupportedException.Create(targetType, source);
		}

		#endregion

		#region Implementation of IConvertFrom

		/// <summary>
		/// Overrides the CanConvertFrom method of IConvertFrom.
		/// The ITypeDescriptorContext interface provides the context for the
		/// conversion. Typically this interface is used at design time to 
		/// provide information about the design-time container.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <returns>true if the source is a string</returns>
		public bool CanConvertFrom(System.Type sourceType)
		{
			return (sourceType == typeof(string));
		}

		/// <summary>
		/// Overrides the ConvertFrom method of IConvertFrom.
		/// </summary>
		/// <param name="source">the object to convert to a PatternString</param>
		/// <returns>the PatternString</returns>
		public object ConvertFrom(object source) 
		{
			if (source is string) 
			{
				return new PatternString((string)source);
			}
			throw ConversionNotSupportedException.Create(typeof(PatternString), source);
		}

		#endregion
	}
}
