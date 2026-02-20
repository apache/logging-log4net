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
using System.IO;

using log4net.Util.PatternStringConverters;
using log4net.Core;
using System.Collections.Generic;

namespace log4net.Util;

/// <summary>
/// This class implements a patterned string.
/// </summary>
/// <remarks>
/// <para>
/// This string has embedded patterns that are resolved and expanded
/// when the string is formatted.
/// </para>
/// <para>
/// This class functions similarly to the <see cref="log4net.Layout.PatternLayout"/>
/// in that it accepts a pattern and renders it to a string. Unlike the 
/// <see cref="log4net.Layout.PatternLayout"/> however the <see cref="PatternString"/>
/// does not render the properties of a specific <see cref="LoggingEvent"/> but
/// of the process in general.
/// </para>
/// <para>
/// The recognized conversion pattern names are:
/// </para>
/// <list type="table">
///     <listheader>
///         <term>Conversion Pattern Name</term>
///         <description>Effect</description>
///     </listheader>
///     <item>
///         <term>appdomain</term>
///         <description>
///             <para>
///             Used to output the friendly name of the current AppDomain.
///             </para>
///         </description>
///     </item>
///     <item>
///         <term>appsetting</term>
///         <description>
///             <para>
///             Used to output the value of a specific appSetting key in the application
///             configuration file.
///             </para>
///         </description>
///     </item>
///     <item>
///         <term>date</term>
///         <description>
///       <para>
///       Used to output the current date and time in the local time zone. 
///       To output the date in universal time use the <c>%utcdate</c> pattern.
///       The date conversion 
///       specifier may be followed by a <i>date format specifier</i> enclosed 
///       between braces. For example, <b>%date{HH:mm:ss,fff}</b> or
///       <b>%date{dd MMM yyyy HH:mm:ss,fff}</b>.  If no date format specifier is 
///       given then ISO8601 format is
///       assumed (<see cref="log4net.DateFormatter.Iso8601DateFormatter"/>).
///       </para>
///       <para>
///       The date format specifier admits the same syntax as the
///       time pattern string of the <see cref="DateTime.ToString(string)"/>.
///       </para>
///       <para>
///       For better results it is recommended to use the log4net date
///       formatters. These can be specified using one of the strings
///       "ABSOLUTE", "DATE" and "ISO8601" for specifying 
///       <see cref="log4net.DateFormatter.AbsoluteTimeDateFormatter"/>, 
///       <see cref="log4net.DateFormatter.DateTimeDateFormatter"/> and respectively 
///       <see cref="log4net.DateFormatter.Iso8601DateFormatter"/>. For example, 
///       <b>%date{ISO8601}</b> or <b>%date{ABSOLUTE}</b>.
///       </para>
///       <para>
///       These dedicated date formatters perform significantly
///       better than <see cref="DateTime.ToString(string)"/>.
///       </para>
///         </description>
///     </item>
///     <item>
///         <term>env</term>
///         <description>
///             <para>
///       Used to output the a specific environment variable. The key to 
///       lookup must be specified within braces and directly following the
///       pattern specifier, e.g. <b>%env{COMPUTERNAME}</b> would include the value
///       of the <c>COMPUTERNAME</c> environment variable.
///             </para>
///             <para>
///             The <c>env</c> pattern is not supported on the .NET Compact Framework.
///             </para>
///         </description>
///     </item>
///     <item>
///         <term>identity</term>
///         <description>
///        <para>
///        Used to output the user name for the currently active user
///        (Principal.Identity.Name).
///        </para>
///         </description>
///     </item>
///     <item>
///         <term>newline</term>
///         <description>
///       <para>
///       Outputs the platform dependent line separator character or
///       characters.
///       </para>
///       <para>
///       This conversion pattern name offers the same performance as using 
///       non-portable line separator strings such as  "\n", or "\r\n". 
///       Thus, it is the preferred way of specifying a line separator.
///       </para> 
///         </description>
///     </item>
///     <item>
///         <term>processid</term>
///         <description>
///             <para>
///        Used to output the system process ID for the current process.
///             </para>
///         </description>
///     </item>
///     <item>
///         <term>property</term>
///         <description>
///       <para>
///       Used to output a specific context property. The key to 
///       lookup must be specified within braces and directly following the
///       pattern specifier, e.g. <b>%property{user}</b> would include the value
///       from the property that is keyed by the string 'user'. Each property value
///       that is to be included in the log must be specified separately.
///       Properties are stored in logging contexts. By default 
///       the <c>log4net:HostName</c> property is set to the name of machine on 
///       which the event was originally logged.
///       </para>
///       <para>
///       If no key is specified, e.g. <b>%property</b> then all the keys and their
///       values are printed in a comma separated list.
///       </para>
///       <para>
///       The properties of an event are combined from a number of different
///       contexts. These are listed below in the order in which they are searched.
///       </para>
///       <list type="definition">
///         <item>
///           <term>the thread properties</term>
///           <description>
///           The <see cref="ThreadContext.Properties"/> that are set on the current
///           thread. These properties are shared by all events logged on this thread.
///           </description>
///         </item>
///         <item>
///           <term>the global properties</term>
///           <description>
///           The <see cref="GlobalContext.Properties"/> that are set globally. These 
///           properties are shared by all the threads in the AppDomain.
///           </description>
///         </item>
///       </list>
///         </description>
///     </item>
///     <item>
///         <term>random</term>
///         <description>
///             <para>
///             Used to output a random string of characters. The string is made up of
///             uppercase letters and numbers. By default the string is 4 characters long.
///             The length of the string can be specified within braces directly following the
///       pattern specifier, e.g. <b>%random{8}</b> would output an 8 character string.
///             </para>
///         </description>
///     </item>
///     <item>
///         <term>username</term>
///         <description>
///        <para>
///        Used to output the WindowsIdentity for the currently
///        active user.
///        </para>
///         </description>
///     </item>
///     <item>
///         <term>utcdate</term>
///         <description>
///       <para>
///       Used to output the date of the logging event in universal time. 
///       The date conversion 
///       specifier may be followed by a <i>date format specifier</i> enclosed 
///       between braces. For example, <b>%utcdate{HH:mm:ss,fff}</b> or
///       <b>%utcdate{dd MMM yyyy HH:mm:ss,fff}</b>.  If no date format specifier is 
///       given then ISO8601 format is
///       assumed (<see cref="log4net.DateFormatter.Iso8601DateFormatter"/>).
///       </para>
///       <para>
///       The date format specifier admits the same syntax as the
///       time pattern string of the <see cref="DateTime.ToString(string)"/>.
///       </para>
///       <para>
///       For better results it is recommended to use the log4net date
///       formatters. These can be specified using one of the strings
///       "ABSOLUTE", "DATE" and "ISO8601" for specifying 
///       <see cref="log4net.DateFormatter.AbsoluteTimeDateFormatter"/>, 
///       <see cref="log4net.DateFormatter.DateTimeDateFormatter"/> and respectively 
///       <see cref="log4net.DateFormatter.Iso8601DateFormatter"/>. For example, 
///       <b>%utcdate{ISO8601}</b> or <b>%utcdate{ABSOLUTE}</b>.
///       </para>
///       <para>
///       These dedicated date formatters perform significantly
///       better than <see cref="DateTime.ToString(string)"/>.
///       </para>
///         </description>
///     </item>
///    <item>
///      <term>%</term>
///      <description>
///       <para>
///       The sequence %% outputs a single percent sign.
///       </para>
///      </description>
///    </item>
/// </list>
/// <para>
/// Additional pattern converters may be registered with a specific <see cref="PatternString"/>
/// instance using <see cref="AddConverter(ConverterInfo)"/> or
/// <see cref="AddConverter(string, Type)" />.
/// </para>
/// <para>
/// See the <see cref="log4net.Layout.PatternLayout"/> for details on the 
/// <i>format modifiers</i> supported by the patterns.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
public class PatternString : IOptionHandler
{
  /// <summary>
  /// Internal map of converter identifiers to converter types.
  /// </summary>
  private static readonly Dictionary<string, Type> _sGlobalRulesRegistry = new(StringComparer.Ordinal)
  {
    // TODO - have added common variants of casing for utcdate and appsetting.
    // Wouldn't it be better to use a case-insensitive dictionary?

    ["appdomain"] = typeof(AppDomainPatternConverter),
    ["appsetting"] = typeof(AppSettingPatternConverter),
    ["appSetting"] = typeof(AppSettingPatternConverter),
    ["AppSetting"] = typeof(AppSettingPatternConverter),
    ["date"] = typeof(DatePatternConverter),
    ["env"] = typeof(EnvironmentPatternConverter),
    ["envFolderPath"] = typeof(EnvironmentFolderPathPatternConverter),
    ["identity"] = typeof(IdentityPatternConverter),
    ["literal"] = typeof(LiteralPatternConverter),
    ["newline"] = typeof(NewLinePatternConverter),
    ["processid"] = typeof(ProcessIdPatternConverter),
    ["property"] = typeof(PropertyPatternConverter),
    ["random"] = typeof(RandomStringPatternConverter),
    ["username"] = typeof(UserNamePatternConverter),
    ["utcdate"] = typeof(UtcDatePatternConverter),
    ["utcDate"] = typeof(UtcDatePatternConverter),
    ["UtcDate"] = typeof(UtcDatePatternConverter),
  };

  /// <summary>
  /// the head of the pattern converter chain
  /// </summary>
  private PatternConverter? _head;

  /// <summary>
  /// patterns defined on this PatternString only
  /// </summary>
  private readonly Dictionary<string, ConverterInfo> _instanceRulesRegistry = new(StringComparer.Ordinal);

  /// <summary>
  /// Default constructor
  /// </summary>
  /// <remarks>
  /// <para>
  /// Initialize a new instance of <see cref="PatternString"/>
  /// </para>
  /// </remarks>
  public PatternString()
  { }

  /// <summary>
  /// Constructs a PatternString
  /// </summary>
  /// <param name="pattern">The pattern to use with this PatternString</param>
  /// <remarks>
  /// <para>
  /// Initialize a new instance of <see cref="PatternString"/> with the pattern specified.
  /// </para>
  /// </remarks>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2214:Do not call overridable methods in constructors")]
  public PatternString(string? pattern)
  {
    ConversionPattern = pattern;
    ActivateOptions();
  }

  /// <summary>
  /// Gets or sets the pattern formatting string
  /// </summary>
  /// <value>
  /// The pattern formatting string
  /// </value>
  /// <remarks>
  /// <para>
  /// The <b>ConversionPattern</b> option. This is the string which
  /// controls formatting and consists of a mix of literal content and
  /// conversion specifiers.
  /// </para>
  /// </remarks>
  public string? ConversionPattern { get; set; }

  /// <summary>
  /// Initialize object options
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is part of the <see cref="IOptionHandler"/> delayed object
  /// activation scheme. The <see cref="ActivateOptions"/> method must 
  /// be called on this object after the configuration properties have
  /// been set. Until <see cref="ActivateOptions"/> is called this
  /// object is in an undefined state and must not be used. 
  /// </para>
  /// <para>
  /// If any of the configuration properties are modified then 
  /// <see cref="ActivateOptions"/> must be called again.
  /// </para>
  /// </remarks>
  public virtual void ActivateOptions()
  {
    if (ConversionPattern is null)
    {
      _head = null;
    }
    else
    {
      _head = CreatePatternParser(ConversionPattern).Parse();
    }
  }

  /// <summary>
  /// Create the <see cref="PatternParser"/> used to parse the pattern
  /// </summary>
  /// <param name="pattern">the pattern to parse</param>
  /// <returns>The <see cref="PatternParser"/></returns>
  /// <remarks>
  /// <para>
  /// Returns PatternParser used to parse the conversion string. Subclasses
  /// may override this to return a subclass of PatternParser which recognize
  /// custom conversion pattern name.
  /// </para>
  /// </remarks>
  private PatternParser CreatePatternParser(string pattern)
  {
    PatternParser patternParser = new(pattern);

    // Add all the builtin patterns
    foreach (KeyValuePair<string, Type> entry in _sGlobalRulesRegistry)
    {
      ConverterInfo converterInfo = new()
      {
        Name = entry.Key,
        Type = entry.Value
      };
      patternParser.PatternConverters.Add(entry.Key, converterInfo);
    }
    // Add the instance patterns
    foreach (KeyValuePair<string, ConverterInfo> entry in _instanceRulesRegistry)
    {
      patternParser.PatternConverters[entry.Key] = entry.Value;
    }

    return patternParser;
  }

  /// <summary>
  /// Produces a formatted string as specified by the conversion pattern.
  /// </summary>
  /// <param name="writer">The TextWriter to write the formatted event to</param>
  /// <remarks>
  /// <para>
  /// Format the pattern to the <paramref name="writer"/>.
  /// </para>
  /// </remarks>
  public void Format(TextWriter writer)
  {
    writer.EnsureNotNull();

    PatternConverter? c = _head;

    // loop through the chain of pattern converters
    while (c is not null)
    {
      c.Format(writer, null);
      c = c.Next;
    }
  }

  /// <summary>
  /// Format the pattern as a string
  /// </summary>
  /// <returns>the pattern formatted as a string</returns>
  /// <remarks>
  /// <para>
  /// Format the pattern to a string.
  /// </para>
  /// </remarks>
  public string Format()
  {
    using StringWriter writer = new(System.Globalization.CultureInfo.InvariantCulture);
    Format(writer);
    return writer.ToString();
  }

  /// <summary>
  /// Adds a converter to this PatternString.
  /// </summary>
  /// <param name="converterInfo">the converter info</param>
  /// <remarks>
  /// <para>
  /// This version of the method is used by the configurator.
  /// Programmatic users should use the alternative <see cref="AddConverter(string,Type)"/> method.
  /// The converter name is case-insensitive.
  /// </para>
  /// </remarks>
  public void AddConverter(ConverterInfo converterInfo)
  {
    if (!typeof(PatternConverter).IsAssignableFrom(converterInfo.EnsureNotNull().Type))
    {
      throw new ArgumentException($"The converter type specified [{converterInfo.Type}] must be a subclass of log4net.Util.PatternConverter", nameof(converterInfo));
    }
    _instanceRulesRegistry[converterInfo.Name] = converterInfo;
  }

  /// <summary>
  /// Add a converter to this PatternString
  /// </summary>
  /// <param name="name">the name of the conversion pattern for this converter</param>
  /// <param name="type">the type of the converter</param>
  public void AddConverter(string name, Type type)
  {
    AddConverter(new()
    {
      Name = name.EnsureNotNull(),
      Type = type.EnsureNotNull()
    });
  }
}