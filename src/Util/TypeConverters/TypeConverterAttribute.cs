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
	/// Class and Interface level attribute that specifes a type converter
	/// to use with the associated type.
	/// </summary>
	/// <remarks>
	/// To associate a type converter with a target type apply a
	/// <c>TypeConverterAttribute</c> to the target type. Specify the
	/// type of the type converter on the attribute.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	[AttributeUsage(AttributeTargets.Class|AttributeTargets.Interface|AttributeTargets.Enum)]
	public sealed class TypeConverterAttribute : Attribute
	{
		#region Member Variables

		/// <summary>
		/// The string type name of the type converter
		/// </summary>
		private string m_typeName = null;

		#endregion

		#region Constructors

		/// <summary>
		/// Default constructor
		/// </summary>
		public TypeConverterAttribute()
		{
		}

		/// <summary>
		/// Create a new type converter attribute for the specified type name
		/// </summary>
		/// <param name="typeName">The string type name of the type converter</param>
		/// <remarks>
		/// The type specified must implement the <see cref="IConvertFrom"/> 
		/// or the <see cref="IConvertTo"/> interfaces.
		/// </remarks>
		public TypeConverterAttribute(string typeName)
		{
			m_typeName = typeName;
		}

		/// <summary>
		/// Create a new type converter attribute for the specified type
		/// </summary>
		/// <param name="converterType">The type of the type converter</param>
		/// <remarks>
		/// The type specified must implement the <see cref="IConvertFrom"/> 
		/// or the <see cref="IConvertTo"/> interfaces.
		/// </remarks>
		public TypeConverterAttribute(Type converterType)
		{
			m_typeName = log4net.Util.SystemInfo.AssemblyQualifiedName(converterType);
		}

		#endregion

		/// <summary>
		/// The string type name of the type converter 
		/// </summary>
		/// <value>
		/// The string type name of the type converter 
		/// </value>
		/// <remarks>
		/// The type specified must implement the <see cref="IConvertFrom"/> 
		/// or the <see cref="IConvertTo"/> interfaces.
		/// </remarks>
		public string ConverterTypeName
		{
			get { return m_typeName; }
			set { m_typeName = value ; }
		}
	}
}
