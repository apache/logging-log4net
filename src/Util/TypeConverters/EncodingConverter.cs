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

namespace log4net.Util.TypeConverters
{
	/// <summary>
	/// Implementation of <see cref="IConvertFrom"/> that converts an <see cref="Encoding"/>
	/// instance from a string.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class EncodingConverter : IConvertFrom 
	{
		#region Implementation of IConvertFrom

		/// <summary>
		/// Overrides the CanConvertFrom method of IConvertFrom.
		/// The ITypeDescriptorContext interface provides the context for the
		/// conversion. Typically this interface is used at design time to 
		/// provide information about the design-time container.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <returns>true if the source is a string</returns>
		public bool CanConvertFrom(Type sourceType) 
		{
			return (sourceType == typeof(string));
		}

		/// <summary>
		/// Overrides the ConvertFrom method of IConvertFrom.
		/// </summary>
		/// <param name="source">the object to convert to an encoding</param>
		/// <returns>the encoding</returns>
		public object ConvertFrom(object source) 
		{
			if (source is string) 
			{
				return Encoding.GetEncoding((string)source);
			}
			throw ConversionNotSupportedException.Create(typeof(Encoding), source);
		}

		#endregion
	}
}
