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
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;

using log4net.Core;
using log4net.Layout.Pattern;
using log4net.Util;
using log4net.Util.PatternStringConverters;
using AppDomainPatternConverter = log4net.Layout.Pattern.AppDomainPatternConverter;
using DatePatternConverter = log4net.Layout.Pattern.DatePatternConverter;
using IdentityPatternConverter = log4net.Layout.Pattern.IdentityPatternConverter;
using PropertyPatternConverter = log4net.Layout.Pattern.PropertyPatternConverter;
using UserNamePatternConverter = log4net.Layout.Pattern.UserNamePatternConverter;
using UtcDatePatternConverter = log4net.Layout.Pattern.UtcDatePatternConverter;

namespace log4net.Layout;

/// <summary>
/// A flexible layout configurable with pattern string.
/// </summary>
/// <remarks>
/// See <see href="https://logging.apache.org/log4net/manual/configuration/layouts/patterns.html">Conversion Patterns</see>
/// in the manual.
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
/// <author>Douglas de la Torre</author>
/// <author>Daniel Cazzulino</author>
public class PatternLayout : LayoutSkeleton
{
  /// <summary>
  /// Default pattern string for log output. 
  /// </summary>
  /// <remarks>
  /// <para>
  /// Default pattern string for log output. 
  /// Currently set to the string <b>"%message%newline"</b> 
  /// which just prints the application supplied message. 
  /// </para>
  /// </remarks>
  public const string DefaultConversionPattern = "%message%newline";

  /// <summary>
  /// A detailed conversion pattern
  /// </summary>
  /// <remarks>
  /// <para>
  /// A conversion pattern which includes Time, Thread, Logger, and Nested Context.
  /// Current value is <b>%timestamp [%thread] %level %logger %ndc - %message%newline</b>.
  /// </para>
  /// </remarks>
  public const string DetailConversionPattern = "%timestamp [%thread] %level %logger %ndc - %message%newline";

  /// <summary>
  /// Internal map of converter identifiers to converter types.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This static map is overridden by the converterRegistry instance map
  /// </para>
  /// </remarks>
  private static readonly Dictionary<string, ConverterInfo> _sGlobalRulesRegistry = CreateGlobalRulesRegistry();

  /// <summary>
  /// Builds the registry of built-in pattern converters.
  /// </summary>
  /// <returns>the built-in rules, keyed by the name used in a conversion pattern</returns>
  /// <remarks>
  /// <para>
  /// The registry holds <see cref="ConverterInfo"/> rather than a bare <see cref="Type"/> because a
  /// <see cref="Type"/> put into a collection loses any
  /// <see cref="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute"/> annotation,
  /// which is what left these converters without a constructor once a Native AOT build had trimmed
  /// them. <see cref="ConverterInfo.Type"/> carries the annotation, so it survives the round trip.
  /// </para>
  /// <para>
  /// The <c>new()</c> constraint states the same requirement a second time, structurally: a
  /// converter that loses its public parameterless constructor becomes a compile error here rather
  /// than a run-time failure that only shows up in a trimmed build.
  /// </para>
  /// </remarks>
  private static Dictionary<string, ConverterInfo> CreateGlobalRulesRegistry()
  {
    Dictionary<string, ConverterInfo> rules = new(StringComparer.Ordinal);

    Add<LiteralPatternConverter>("literal");
    Add<NewLinePatternConverter>("newline", "n");

    // .NET Standard has no support for ASP.NET
#if NET462_OR_GREATER
    Add<AspNetCachePatternConverter>("aspnet-cache");
    Add<AspNetContextPatternConverter>("aspnet-context");
    Add<AspNetRequestPatternConverter>("aspnet-request");
    Add<AspNetSessionPatternConverter>("aspnet-session");
#endif

    Add<LoggerPatternConverter>("c", "logger");
    Add<TypeNamePatternConverter>("C", "class", "type");
    Add<DatePatternConverter>("d", "date");
    Add<ExceptionPatternConverter>("exception");
    Add<FileLocationPatternConverter>("F", "file");
    Add<FullLocationPatternConverter>("l", "location");
    Add<LineLocationPatternConverter>("L", "line");
    Add<MessagePatternConverter>("m", "message");
    Add<MethodLocationPatternConverter>("M", "method");
    Add<LevelPatternConverter>("p", "level");
    Add<RelativeTimePatternConverter>("r", "timestamp");
    Add<StackTracePatternConverter>("stacktrace");
    Add<StackTraceDetailPatternConverter>("stacktracedetail");
    Add<ThreadPatternConverter>("t", "thread");

    // For backwards compatibility the NDC patterns
    Add<NdcPatternConverter>("x", "ndc");

    // For backwards compatibility the MDC patterns just do a property lookup
    Add<PropertyPatternConverter>("P", "property", "properties", "X", "mdc");

    Add<AppDomainPatternConverter>("a", "appdomain");
    Add<IdentityPatternConverter>("u", "identity");
    Add<UtcDatePatternConverter>("utcdate", "utcDate", "UtcDate");
    Add<UserNamePatternConverter>("w", "username");

    return rules;

    void Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(
      params string[] names) where T : PatternConverter, new()
    {
      foreach (string name in names)
      {
        rules[name] = new() { Name = name, Type = typeof(T) };
      }
    }
  }

  /// <summary>
  /// the head of the pattern converter chain
  /// </summary>
  private PatternConverter? _head;

  /// <summary>
  /// patterns defined on this PatternLayout only
  /// </summary>
  private readonly Dictionary<string, ConverterInfo> _instanceRulesRegistry = new(StringComparer.Ordinal);

  /// <summary>
  /// Constructs a PatternLayout using the DefaultConversionPattern
  /// </summary>
  /// <remarks>
  /// <para>
  /// The default pattern just produces the application supplied message.
  /// </para>
  /// <para>
  /// Note to Inheritors: This constructor calls the virtual method
  /// <see cref="CreatePatternParser"/>. If you override this method be
  /// aware that it will be called before your is called constructor.
  /// </para>
  /// <para>
  /// As per the <see cref="IOptionHandler"/> contract the <see cref="ActivateOptions"/>
  /// method must be called after the properties on this object have been
  /// configured.
  /// </para>
  /// </remarks>
  public PatternLayout() : this(DefaultConversionPattern)
  { }

  /// <summary>
  /// Constructs a PatternLayout using the supplied conversion pattern
  /// </summary>
  /// <param name="pattern">the pattern to use</param>
  /// <remarks>
  /// <para>
  /// Note to Inheritors: This constructor calls the virtual method
  /// <see cref="CreatePatternParser"/>. If you override this method be
  /// aware that it will be called before your is called constructor.
  /// </para>
  /// <para>
  /// When using this constructor the <see cref="ActivateOptions"/> method 
  /// need not be called. This may not be the case when using a subclass.
  /// </para>
  /// </remarks>
  public PatternLayout(string? pattern)
  {
    // By default, we do not process the exception
    IgnoresException = true;

    ConversionPattern = pattern ?? DefaultConversionPattern;

    ActivateOptions();
  }

  /// <summary>
  /// Gets or sets the pattern formatting string.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The <b>ConversionPattern</b> option. This is the string which
  /// controls formatting and consists of a mix of literal content and
  /// conversion specifiers.
  /// </para>
  /// </remarks>
  public string ConversionPattern { get; set; }

  /// <summary>
  /// Create the pattern parser instance
  /// </summary>
  /// <param name="pattern">the pattern to parse</param>
  /// <returns>The <see cref="PatternParser"/> that will format the event</returns>
  /// <remarks>
  /// <para>
  /// Creates the <see cref="PatternParser"/> used to parse the conversion string. Sets the
  /// global and instance rules on the <see cref="PatternParser"/>.
  /// </para>
  /// </remarks>
  protected virtual PatternParser CreatePatternParser(string pattern)
  {
    PatternParser patternParser = new(pattern);

    // Add all the builtin patterns. The registry entries are copied rather than handed out:
    // PatternConverters is public, and PatternParser assigns ConverterInfo.Properties to every
    // converter it creates, so sharing one instance would let a single layout mutate state that
    // every other layout in the process can see.
    foreach (KeyValuePair<string, ConverterInfo> entry in _sGlobalRulesRegistry)
    {
      patternParser.PatternConverters[entry.Key] = new ConverterInfo
      {
        Name = entry.Value.Name,
        Type = entry.Value.Type
      };
    }
    // Add the instance patterns
    foreach (KeyValuePair<string, ConverterInfo> entry in _instanceRulesRegistry)
    {
      patternParser.PatternConverters[entry.Key] = entry.Value;
    }

    return patternParser;
  }

  /// <summary>
  /// Initializes layout options.
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
  public override void ActivateOptions()
  {
    _head = CreatePatternParser(ConversionPattern).Parse();

    PatternConverter? curConverter = _head;
    while (curConverter is not null)
    {
      if (curConverter is PatternLayoutConverter { IgnoresException: false })
      {
        // Found converter that handles the exception
        IgnoresException = false;
        break;
      }
      curConverter = curConverter.Next;
    }
  }

  /// <summary>
  /// Produces a formatted string as specified by the conversion pattern.
  /// </summary>
  /// <param name="loggingEvent">The event being logged.</param>
  /// <param name="writer">The TextWriter to write the formatted event to.</param>
  /// <remarks>
  /// <para>
  /// Parses the <see cref="LoggingEvent"/> using the patter format
  /// specified in the <see cref="ConversionPattern"/> property.
  /// </para>
  /// </remarks>
  public override void Format(TextWriter writer, LoggingEvent loggingEvent)
  {
    writer.EnsureNotNull();
    loggingEvent.EnsureNotNull();

    PatternConverter? c = _head;
    // loop through the chain of pattern converters
    while (c is not null)
    {
      c.Format(writer, loggingEvent);
      c = c.Next;
    }
  }

  /// <summary>
  /// Add a converter to this PatternLayout
  /// </summary>
  /// <param name="converterInfo">the converter info</param>
  /// <remarks>
  /// <para>
  /// This version of the method is used by the configurator.
  /// Programmatic users should use the alternative <see cref="AddConverter(string,Type)"/> method.
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
  /// Adds a named pattern converter to this PatternLayout.
  /// </summary>
  /// <param name="name">the name of the conversion pattern for this converter</param>
  /// <param name="type">the type of the converter</param>
  /// <remarks>
  /// <para>
  /// This converter will be used in the formatting of the event.
  /// This method must be called before <see cref="ActivateOptions"/>.
  /// </para>
  /// <para>
  /// The <paramref name="type"/> specified must extend the 
  /// <see cref="PatternConverter"/> type.
  /// </para>
  /// </remarks>
  public void AddConverter(string name,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type) => AddConverter(new()
  {
    Name = name.EnsureNotNull(),
    Type = type.EnsureNotNull()
  });
}