#region Copyright & License
//
// Copyright 2004 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Text;

namespace log4net.Util.TypeConverters
{
	/// <summary>
	/// Implementation of <see cref="IConvertFrom"/> that converts to a <see cref="Type"/> instance from a string.
	/// </summary>
	/// <author>Nicko Cadell</author>
	public class TypeConverter : IConvertFrom 
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
		/// <param name="source">the object to convert to a Type</param>
		/// <returns>the Type</returns>
		public object ConvertFrom(object source) 
		{
			if (source is string) 
			{
				return SystemInfo.GetTypeFromString((string)source, true, true);
			}
			throw ConversionNotSupportedException.Create(typeof(Type), source);
		}

		#endregion
	}
}
