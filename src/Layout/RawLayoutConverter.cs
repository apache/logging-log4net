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
	/// Type converter for the <see cref="IRawLayout"/> interface
	/// </summary>
	/// <remarks>
	/// <para>Used to convert objects to the <see cref="IRawLayout"/> interface</para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class RawLayoutConverter : IConvertFrom
	{
		#region Override Implementation of IRawLayout

		/// <summary>
		/// Can the sourceType be converted to an <see cref="IRawLayout"/>
		/// </summary>
		/// <param name="sourceType">the source tybe to be converted</param>
		/// <returns><c>true</c> if the source type can be converted to <see cref="IRawLayout"/></returns>
		public bool CanConvertFrom(Type sourceType) 
		{
			// Accept an ILayout object
			return (typeof(ILayout).IsAssignableFrom(sourceType));
		}

		/// <summary>
		/// Convert the value to a <see cref="IRawLayout"/> object
		/// </summary>
		/// <param name="source">the value to convert</param>
		/// <returns>the <see cref="IRawLayout"/> object</returns>
		public object ConvertFrom(object source) 
		{
			if (source is ILayout) 
			{
				return new Layout2RawLayoutAdapter((ILayout)source);
			}
			throw ConversionNotSupportedException.Create(typeof(IRawLayout), source);
		}

		#endregion
	}
}
