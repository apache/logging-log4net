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
using System.Globalization;
using System.Reflection;
using System.Collections;

namespace log4net.Util.TypeConverters
{
	/// <summary>
	/// Register of type converters for specific types.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public sealed class ConverterRegistry
	{
		#region Internal Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ConverterRegistry" /> class.
		/// </summary>
		private ConverterRegistry() 
		{
			// Initialize the type2converter hashtable
			m_type2converter = new Hashtable();
		}

		#endregion Internal Instance Constructors

		#region Static Constructor

		/// <summary>
		/// Static constructor.
		/// </summary>
		/// <remarks>
		/// This constructor defines the intrinsic type converters
		/// </remarks>
		static ConverterRegistry()
		{
			// Create the registry
			s_registry = new ConverterRegistry();

			// Add predefined converters here
			AddConverter(typeof(bool), typeof(BooleanConverter));
			AddConverter(typeof(System.Text.Encoding), typeof(EncodingConverter));
			AddConverter(typeof(log4net.Util.PatternString), typeof(PatternStringConverter));
		}

		#endregion Static Constructor

		#region Public Static Methods

		/// <summary>
		/// Adds a converter for a specific type.
		/// </summary>
		/// <param name="destinationType">The type being converted to.</param>
		/// <param name="converter">The type converter to use to convert to the destination type.</param>
		public static void AddConverter(Type destinationType, object converter)
		{
			if (destinationType != null && converter != null)
			{
				s_registry.m_type2converter[destinationType] = converter;
			}
		}

		/// <summary>
		/// Adds a converter for a specific type.
		/// </summary>
		/// <param name="destinationType">The type being converted to.</param>
		/// <param name="converterType">The type of the type converter to use to convert to the destination type.</param>
		public static void AddConverter(Type destinationType, Type converterType)
		{
			AddConverter(destinationType, CreateConverterInstance(converterType));
		}

		/// <summary>
		/// Gets the type converter to use to convert values to the destination type.
		/// </summary>
		/// <param name="sourceType">The type being converted from.</param>
		/// <param name="destinationType">The type being converted to.</param>
		/// <returns>
		/// The type converter instance to use for type conversions or <c>null</c> 
		/// if no type converter is found.
		/// </returns>
		public static IConvertTo GetConvertTo(Type sourceType, Type destinationType)
		{
			// TODO: Support inheriting type converters.

			// i.e. getting a type converter for a base of destinationType
			IConvertTo converter = null;

			// Lookup in the static registry
			object obj = s_registry.m_type2converter[sourceType];
			if ((obj != null) && (obj is IConvertTo))
			{
				converter = (IConvertTo)obj;
			}

			if (converter == null)
			{
				// Lookup using attributes
				converter = GetConverterFromAttribute(sourceType) as IConvertTo;

				if (converter != null)
				{
					// Store in registry
					s_registry.m_type2converter[sourceType] = converter;
				}
			}

			return converter;
		}

		/// <summary>
		/// Gets the type converter to use to convert values to the destination type.
		/// </summary>
		/// <param name="destinationType">The type being converted to.</param>
		/// <returns>
		/// The type converter instance to use for type conversions or <c>null</c> 
		/// if no type converter is found.
		/// </returns>
		public static IConvertFrom GetConvertFrom(Type destinationType)
		{
			// TODO: Support inheriting type converters.

			// i.e. getting a type converter for a base of destinationType
			IConvertFrom converter = null;

			// Lookup in the static registry
			object obj = s_registry.m_type2converter[destinationType];
			if ((obj != null) && (obj is IConvertFrom))
			{
				converter = (IConvertFrom)obj;
			}

			if (converter == null)
			{
				// Lookup using attributes
				converter = GetConverterFromAttribute(destinationType) as IConvertFrom;

				if (converter != null)
				{
					// Store in registry
					s_registry.m_type2converter[destinationType] = converter;
				}
			}

			return converter;
		}
		
		/// <summary>
		/// Lookups the type converter to use as specified by the attributes on the 
		/// destination type.
		/// </summary>
		/// <param name="destinationType">The type being converted to.</param>
		/// <returns>
		/// The type converter instance to use for type conversions or <c>null</c> 
		/// if no type converter is found.
		/// </returns>
		private static object GetConverterFromAttribute(Type destinationType)
		{
			// Look for an attribute on the destination type
			object[] attributes = destinationType.GetCustomAttributes(typeof(TypeConverterAttribute), true);
			if (attributes != null && attributes.Length > 0)
			{
				TypeConverterAttribute tcAttr = attributes[0] as TypeConverterAttribute;
				if (tcAttr != null)
				{
					Type converterType = SystemInfo.GetTypeFromString(destinationType, tcAttr.ConverterTypeName, false, true);
					return CreateConverterInstance(converterType);
				}
			}

			// Not found converter using attributes
			return null;
		}

		/// <summary>
		/// Creates the instance of the type converter.
		/// </summary>
		/// <param name="converterType">The type of the type converter.</param>
		/// <remarks>
		/// The type specified for the type converter must implement 
		/// the <see cref="IConvertFrom"/> or <see cref="IConvertTo"/> interfaces 
		/// and must have a public default (no argument) constructor.
		/// </remarks>
		/// <returns>
		/// The type converter instance to use for type conversions or <c>null</c> 
		/// if no type converter is found.</returns>
		private static object CreateConverterInstance(Type converterType)
		{
			if (converterType != null)
			{
				// Check type is a converter
				if (typeof(IConvertFrom).IsAssignableFrom(converterType) || typeof(IConvertTo).IsAssignableFrom(converterType))
				{
					// Create the type converter
					ConstructorInfo ci = converterType.GetConstructor(log4net.Util.SystemInfo.EmptyTypes);
					if (ci != null)
					{
						return ci.Invoke(BindingFlags.Public | BindingFlags.Instance, null, new object[0], CultureInfo.InvariantCulture);
					}
				}
			}
			return null;
		}

		#endregion Public Static Methods

		#region Private Static Fields

		/// <summary>
		/// The singleton registry.
		/// </summary>
		private static ConverterRegistry s_registry;

		#endregion

		#region Private Instance Fields

		/// <summary>
		/// Mapping from <see cref="Type" /> to type converter.
		/// </summary>
		private Hashtable m_type2converter;

		#endregion
	}
}
