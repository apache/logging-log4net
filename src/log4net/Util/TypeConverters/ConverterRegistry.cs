#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
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
using System.Collections.Concurrent;

namespace log4net.Util.TypeConverters;

/// <summary>
/// Register of type converters for specific types.
/// </summary>
/// <remarks>
/// <para>
/// Maintains a registry of type converters used to convert between types.
/// </para>
/// <para>
/// Use the <see cref="AddConverter(Type, object)"/> and 
/// <see cref="AddConverter(Type, Type)"/> methods to register new converters.
/// The <see cref="GetConvertTo"/> and <see cref="GetConvertFrom"/> methods
/// lookup appropriate converters to use.
/// </para>
/// </remarks>
/// <seealso cref="IConvertFrom"/>
/// <seealso cref="IConvertTo"/>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
public static class ConverterRegistry
{
  /// <summary>
  /// This class constructor adds the intrinsic type converters
  /// </summary>
  static ConverterRegistry()
  {
    // Add predefined converters here
    AddConverter(typeof(bool), typeof(BooleanConverter));
    AddConverter(typeof(System.Text.Encoding), typeof(EncodingConverter));
    AddConverter(typeof(Type), typeof(TypeConverter));
    AddConverter(typeof(Layout.PatternLayout), typeof(PatternLayoutConverter));
    AddConverter(typeof(PatternString), typeof(PatternStringConverter));
    AddConverter(typeof(System.Net.IPAddress), typeof(IpAddressConverter));
  }

  /// <summary>
  /// Adds a converter for a specific type.
  /// </summary>
  /// <param name="destinationType">The type being converted to.</param>
  /// <param name="converter">The type converter to use to convert to the destination type.</param>
  public static void AddConverter(Type? destinationType, object? converter)
  {
    if (destinationType is null || converter is null)
    {
      return;
    }
    if (converter is IConvertTo convertTo)
    {
      _sType2ConvertTo[destinationType] = convertTo;
    }
    if (converter is IConvertFrom convertFrom)
    {
      _sType2ConvertFrom[destinationType] = convertFrom;
    }
  }

  /// <summary>
  /// Adds a converter for a specific type.
  /// </summary>
  /// <param name="destinationType">The type being converted to.</param>
  /// <param name="converterType">The type of the type converter to use to convert to the destination type.</param>
  public static void AddConverter(Type destinationType, Type converterType)
    => AddConverter(destinationType, CreateConverterInstance(converterType.EnsureNotNull()));

  /// <summary>
  /// Gets the type converter to use to convert values to the destination type.
  /// </summary>
  /// <param name="sourceType">The type being converted from.</param>
  /// <param name="destinationType">The type being converted to.</param>
  /// <returns>
  /// The type converter instance to use for type conversions or <c>null</c> 
  /// if no type converter is found.
  /// </returns>
  public static IConvertTo? GetConvertTo(Type sourceType, Type destinationType)
  {
    // TODO: Support inheriting type converters.
    // i.e. getting a type converter for a base of sourceType

    // TODO: Is destinationType required? We don't use it for anything.

    // Look up in the static registry
    if (!_sType2ConvertTo.TryGetValue(sourceType, out IConvertTo? converter))
    {
      // Look up using attributes
      converter = GetConverterFromAttribute(sourceType.EnsureNotNull()) as IConvertTo;
      if (converter is not null)
      {
        // Store in registry
        _sType2ConvertTo[sourceType] = converter;
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
  public static IConvertFrom? GetConvertFrom(Type destinationType)
  {
    // TODO: Support inheriting type converters.
    // i.e. getting a type converter for a base of destinationType

    // Lookup in the static registry
    if (!_sType2ConvertFrom.TryGetValue(destinationType, out IConvertFrom? converter))
    {
      // Look up using attributes
      converter = GetConverterFromAttribute(destinationType.EnsureNotNull()) as IConvertFrom;
      if (converter is not null)
      {
        // Store in registry
        _sType2ConvertFrom[destinationType] = converter;
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
  private static object? GetConverterFromAttribute(Type destinationType)
  {
    // Look for an attribute on the destination type
    object[] attributes = destinationType.GetCustomAttributes(typeof(TypeConverterAttribute), true);
    foreach (var attribute in attributes)
    {
      if (attribute is TypeConverterAttribute tcAttr)
      {
        if (SystemInfo.GetTypeFromString(destinationType, tcAttr.ConverterTypeName, false, true) is Type converterType)
        {
          return CreateConverterInstance(converterType);
        }
      }
    }

    // Not found converter using attributes
    return null;
  }

  /// <summary>
  /// Creates the instance of the type converter.
  /// </summary>
  /// <param name="converterType">The type of the type converter.</param>
  /// <returns>
  /// The type converter instance to use for type conversions or <c>null</c> 
  /// if no type converter is found.
  /// </returns>
  /// <remarks>
  /// <para>
  /// The type specified for the type converter must implement 
  /// the <see cref="IConvertFrom"/> or <see cref="IConvertTo"/> interfaces 
  /// and must have a public default (no argument) constructor.
  /// </para>
  /// </remarks>
  private static object? CreateConverterInstance(Type converterType)
  {
    // Check type is a converter
    if (typeof(IConvertFrom).IsAssignableFrom(converterType) || typeof(IConvertTo).IsAssignableFrom(converterType))
    {
      try
      {
        // Create the type converter
        return Activator.CreateInstance(converterType);
      }
      catch (Exception e) when (!e.IsFatal())
      {
        LogLog.Error(_declaringType, $"Cannot CreateConverterInstance of type [{converterType.FullName}], exception in call to Activator.CreateInstance", e);
      }
    }
    else
    {
      LogLog.Error(_declaringType, $"Cannot CreateConverterInstance of type [{converterType.FullName}], type does not implement IConvertFrom or IConvertTo");
    }
    return null;
  }

  /// <summary>
  /// The fully qualified type of the ConverterRegistry class.
  /// </summary>
  /// <remarks>
  /// Used by the internal logger to record the Type of the
  /// log message.
  /// </remarks>
  private static readonly Type _declaringType = typeof(ConverterRegistry);

  private static readonly ConcurrentDictionary<Type, IConvertTo> _sType2ConvertTo = new();
  private static readonly ConcurrentDictionary<Type, IConvertFrom> _sType2ConvertFrom = new();
}