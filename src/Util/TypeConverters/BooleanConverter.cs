#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
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

namespace log4net.Util.TypeConverters
{
	/// <summary>
	/// Type converter for Boolean.
	/// </summary>
	/// <remarks>
	/// Supports conversion from string to boolean type.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class BooleanConverter : IConvertFrom
	{
		#region Implementation of IConvertFrom

		/// <summary>
		/// Can the source type be converted to the type supported by this object
		/// </summary>
		/// <param name="sourceType">the type to convert</param>
		/// <returns>true if the conversion is possible</returns>
		public bool CanConvertFrom(Type sourceType)
		{
			return sourceType == typeof(string);
		}

		/// <summary>
		/// Convert the source object to the type supported by this object
		/// </summary>
		/// <param name="source">the object to convert</param>
		/// <returns>the converted object</returns>
		public object ConvertFrom(object source)
		{
			string str = source as string;
			if (str != null)
			{
				return bool.Parse(str);
			}
			throw ConversionNotSupportedException.Create(typeof(bool), source);
		}

		#endregion
	}
}
