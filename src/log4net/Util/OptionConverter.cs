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
using System.Globalization;
using System.Reflection;
using System.Text;

using log4net.Core;
using log4net.Util.TypeConverters;

namespace log4net.Util;

/// <summary>
/// A convenience class to convert property values to specific types.
/// </summary>
/// <remarks>
/// <para>
/// Utility functions for converting types and parsing values.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
public static class OptionConverter
{
  /// <summary>
  /// Converts a string to a <see cref="bool" /> value.
  /// </summary>
  /// <param name="argValue">String to convert.</param>
  /// <param name="defaultValue">The default value.</param>
  /// <returns>The <see cref="bool" /> value of <paramref name="argValue" />.</returns>
  /// <remarks>
  /// <para>
  /// If <paramref name="argValue"/> is "true", then <c>true</c> is returned. 
  /// If <paramref name="argValue"/> is "false", then <c>false</c> is returned. 
  /// Otherwise, <paramref name="defaultValue"/> is returned.
  /// </para>
  /// </remarks>
  public static bool ToBoolean(string? argValue, bool defaultValue)
  {
    if (!string.IsNullOrEmpty(argValue))
    {
      try
      {
        return bool.Parse(argValue);
      }
      catch (Exception e)
      {
        LogLog.Error(_declaringType, $"[{argValue}] is not in proper bool form.", e);
      }
    }
    return defaultValue;
  }

  /// <summary>
  /// Parses a file size into a number.
  /// </summary>
  /// <param name="argValue">String to parse.</param>
  /// <param name="defaultValue">The default value.</param>
  /// <returns>The <see cref="long" /> value of <paramref name="argValue" />.</returns>
  /// <remarks>
  /// <para>
  /// Parses a file size of the form: number[KB|MB|GB] into a
  /// long value. It is scaled with the appropriate multiplier.
  /// </para>
  /// <para>
  /// <paramref name="defaultValue"/> is returned when <paramref name="argValue"/>
  /// cannot be converted to a <see cref="long" /> value.
  /// </para>
  /// </remarks>
  public static long ToFileSize(string? argValue, long defaultValue)
  {
    if (argValue is null)
    {
      return defaultValue;
    }

    string s = argValue.Trim().ToUpperInvariant();
    long multiplier = 1;
    int index;

    if ((index = s.IndexOf("KB", StringComparison.Ordinal)) != -1)
    {
      multiplier = 1024;
      s = s.Substring(0, index);
    }
    else if ((index = s.IndexOf("MB", StringComparison.Ordinal)) != -1)
    {
      multiplier = 1024 * 1024;
      s = s.Substring(0, index);
    }
    else if ((index = s.IndexOf("GB", StringComparison.Ordinal)) != -1)
    {
      multiplier = 1024 * 1024 * 1024;
      s = s.Substring(0, index);
    }

    // Try again to remove whitespace between the number and the size specifier
    s = s.Trim();

    if (SystemInfo.TryParse(s, out long longVal))
    {
      return longVal * multiplier;
    }
    else
    {
      LogLog.Error(_declaringType, $"OptionConverter: [{s}] is not in the correct file size syntax.");
    }
    return defaultValue;
  }

  /// <summary>
  /// Converts a string to an object.
  /// </summary>
  /// <param name="target">The target type to convert to.</param>
  /// <param name="txt">The string to convert to an object.</param>
  /// <returns>
  /// The object converted from a string or <c>null</c> when the 
  /// conversion failed.
  /// </returns>
  /// <remarks>
  /// <para>
  /// Converts a string to an object. Uses the converter registry to try
  /// to convert the string value into the specified target type.
  /// </para>
  /// </remarks>
  public static object? ConvertStringTo(Type target, string txt)
  {
    if (target is null)
    {
      throw new ArgumentNullException(nameof(target));
    }

    // If we want a string we already have the correct type
    if (typeof(string) == target || typeof(object) == target)
    {
      return txt;
    }

    // First lets try to find a type converter
    IConvertFrom? typeConverter = ConverterRegistry.GetConvertFrom(target);
    if (typeConverter is not null && typeConverter.CanConvertFrom(typeof(string)))
    {
      // Found appropriate converter
      return typeConverter.ConvertFrom(txt);
    }
    else
    {
      if (target.IsEnum)
      {
        // Target type is an enum.

        // Use the Enum.Parse(EnumType, string) method to get the enum value
        return ParseEnum(target, txt, true);
      }
      else
      {
        // We essentially make a guess that to convert from a string
        // to an arbitrary type T there will be a static method defined on type T called Parse
        // that will take an argument of type string. i.e. T.Parse(string)->T we call this
        // method to convert the string to the type required by the property.
        if (target.GetMethod("Parse", [typeof(string)]) is MethodInfo meth)
        {
          // Call the Parse method
          return meth.Invoke(null, BindingFlags.InvokeMethod, null, [txt], CultureInfo.InvariantCulture);
        }
        else
        {
          // No Parse() method found.
        }
      }
    }
    return null;
  }

  /// <summary>
  /// Checks if there is an appropriate type conversion from the source type to the target type.
  /// </summary>
  /// <param name="sourceType">The type to convert from.</param>
  /// <param name="targetType">The type to convert to.</param>
  /// <returns><c>true</c> if there is a conversion from the source type to the target type.</returns>
  /// <remarks>
  /// Checks if there is an appropriate type conversion from the source type to the target type.
  /// <para>
  /// </para>
  /// </remarks>
  public static bool CanConvertTypeTo(Type? sourceType, Type? targetType)
  {
    if (sourceType is null || targetType is null)
    {
      return false;
    }

    // Check if we can assign directly from the source type to the target type
    if (targetType.IsAssignableFrom(sourceType))
    {
      return true;
    }

    // Look for a To converter
    if (ConverterRegistry.GetConvertTo(sourceType, targetType) is IConvertTo tcSource)
    {
      if (tcSource.CanConvertTo(targetType))
      {
        return true;
      }
    }

    // Look for a From converter
    if (ConverterRegistry.GetConvertFrom(targetType) is IConvertFrom tcTarget)
    {
      if (tcTarget.CanConvertFrom(sourceType))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Converts an object to the target type.
  /// </summary>
  /// <param name="sourceInstance">The object to convert to the target type.</param>
  /// <param name="targetType">The type to convert to.</param>
  /// <returns>The converted object.</returns>
  /// <remarks>
  /// <para>
  /// Converts an object to the target type.
  /// </para>
  /// </remarks>
  public static object ConvertTypeTo(object sourceInstance, Type targetType)
  {
    Type sourceType = sourceInstance.GetType();

    // Check if we can assign directly from the source type to the target type
    if (targetType.IsAssignableFrom(sourceType))
    {
      return sourceInstance;
    }

    // Look for a TO converter
    if (ConverterRegistry.GetConvertTo(sourceType, targetType) is IConvertTo tcSource)
    {
      if (tcSource.CanConvertTo(targetType))
      {
        return tcSource.ConvertTo(sourceInstance, targetType);
      }
    }

    // Look for a FROM converter
    if (ConverterRegistry.GetConvertFrom(targetType) is IConvertFrom tcTarget)
    {
      if (tcTarget.CanConvertFrom(sourceType))
      {
        return tcTarget.ConvertFrom(sourceInstance);
      }
    }

    throw new ArgumentException($"Cannot convert source object [{sourceInstance}] to target type [{targetType.Name}]", nameof(sourceInstance));
  }

  /// <summary>
  /// Instantiates an object given a class name.
  /// </summary>
  /// <param name="className">The fully qualified class name of the object to instantiate.</param>
  /// <param name="superClass">The class to which the new object should belong.</param>
  /// <param name="defaultValue">The object to return in case of non-fulfillment.</param>
  /// <returns>
  /// An instance of the <paramref name="className"/> or <paramref name="defaultValue"/>
  /// if the object could not be instantiated.
  /// </returns>
  /// <remarks>
  /// <para>
  /// Checks that the <paramref name="className"/> is a subclass of
  /// <paramref name="superClass"/>. If that test fails or the object could
  /// not be instantiated, then <paramref name="defaultValue"/> is returned.
  /// </para>
  /// </remarks>
  public static object? InstantiateByClassName(string? className, Type superClass, object? defaultValue)
  {
    if (className is not null)
    {
      try
      {
        Type? classObj = SystemInfo.GetTypeFromString(className, true, true);
        if (classObj is not null)
        {
          if (!superClass.IsAssignableFrom(classObj))
          {
            LogLog.Error(_declaringType, $"OptionConverter: A [{className}] object is not assignable to a [{superClass.FullName}] variable.");
            return defaultValue;
          }
          return Activator.CreateInstance(classObj);
        }

        LogLog.Error(_declaringType, $"Could not find class [{className}].");
      }
      catch (Exception e)
      {
        LogLog.Error(_declaringType, $"Could not instantiate class [{className}].", e);
      }
    }
    return defaultValue;
  }

  /// <summary>
  /// Performs variable substitution in string <paramref name="value"/> from the 
  /// values of keys found in <paramref name="props"/>.
  /// </summary>
  /// <param name="value">The string on which variable substitution is performed.</param>
  /// <param name="props">The dictionary to use to lookup variables.</param>
  /// <returns>The result of the substitutions.</returns>
  /// <remarks>
  /// <para>
  /// The variable substitution delimiters are <b>${</b> and <b>}</b>.
  /// </para>
  /// <para>
  /// For example, if props contains <c>key=value</c>, then the call
  /// </para>
  /// <para>
  /// <code lang="C#">
  /// string s = OptionConverter.SubstituteVariables("Value of key is ${key}.");
  /// </code>
  /// </para>
  /// <para>
  /// will set the variable <c>s</c> to "Value of key is value.".
  /// </para>
  /// <para>
  /// If no value could be found for the specified key, then substitution 
  /// defaults to an empty string.
  /// </para>
  /// <para>
  /// For example, if system properties contains no value for the key
  /// "nonExistentKey", then the call
  /// </para>
  /// <para>
  /// <code lang="C#">
  /// string s = OptionConverter.SubstituteVariables("Value of nonExistentKey is [${nonExistentKey}]");
  /// </code>
  /// </para>
  /// <para>
  /// will set <s>s</s> to "Value of nonExistentKey is []".   
  /// </para>
  /// <para>
  /// An Exception is thrown if <paramref name="value"/> contains a start 
  /// delimiter "${" which is not balanced by a stop delimiter "}". 
  /// </para>
  /// </remarks>
  public static string SubstituteVariables(string value, System.Collections.IDictionary props)
  {
    StringBuilder buf = new StringBuilder();

    int i = 0;
    int j, k;

    while (true)
    {
      j = value.IndexOf(DelimStart, i, StringComparison.Ordinal);
      if (j == -1)
      {
        if (i == 0)
        {
          return value;
        }
        else
        {
          buf.Append(value.Substring(i, value.Length - i));
          return buf.ToString();
        }
      }
      else
      {
        buf.Append(value.Substring(i, j - i));
        k = value.IndexOf(DelimStop, j);
        if (k == -1)
        {
          throw new LogException("[" + value + "] has no closing brace. Opening brace at position [" + j + "]");
        }
        else
        {
          j += DelimStartLen;
          string key = value.Substring(j, k - j);

          if (props[key] is string replacement)
          {
            buf.Append(replacement);
          }
          i = k + DelimStopLen;
        }
      }
    }
  }

  /// <summary>
  /// Converts the string representation of the name or numeric value of one or 
  /// more enumerated constants to an equivalent enumerated object.
  /// </summary>
  /// <param name="enumType">The type to convert to.</param>
  /// <param name="value">The enum string value.</param>
  /// <param name="ignoreCase">If <c>true</c>, ignore case; otherwise, regard case.</param>
  /// <returns>An object of type <paramref name="enumType" /> whose value is represented by <paramref name="value" />.</returns>
  private static object ParseEnum(Type enumType, string value, bool ignoreCase)
  {
    return Enum.Parse(enumType, value, ignoreCase);
  }

  /// <summary>
  /// The fully qualified type of the OptionConverter class.
  /// </summary>
  /// <remarks>
  /// Used by the internal logger to record the Type of the
  /// log message.
  /// </remarks>
  private static readonly Type _declaringType = typeof(OptionConverter);

  private const string DelimStart = "${";
  private const char DelimStop = '}';
  private const int DelimStartLen = 2;
  private const int DelimStopLen = 1;
}