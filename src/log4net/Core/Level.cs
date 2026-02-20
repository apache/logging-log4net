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
using System.Runtime.Serialization;
using System.Security;
using log4net.Util;

namespace log4net.Core;

/// <summary>
/// Defines the default set of levels recognized by the system.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="LoggingEvent"/> has an associated <see cref="Level"/>.
/// </para>
/// <para>
/// Levels have a numeric <see cref="Value"/> that defines the relative 
/// ordering between levels. Two Levels with the same <see cref="Value"/> 
/// are deemed to be equivalent.
/// </para>
/// <para>
/// The levels that are recognized by log4net are set for each <see cref="Repository.ILoggerRepository"/>
/// and each repository can have different levels defined. The levels are stored
/// in the <see cref="Repository.ILoggerRepository.LevelMap"/> on the repository. Levels are
/// looked up by name from the <see cref="Repository.ILoggerRepository.LevelMap"/>.
/// </para>
/// <para>
/// When logging at level INFO the actual level used is not <see cref="Info"/> but
/// the value of <c>LoggerRepository.LevelMap["INFO"]</c>. The default value for this is
/// <see cref="Info"/>, but this can be changed by reconfiguring the level map.
/// </para>
/// <para>
/// Each level has a <see cref="DisplayName"/> in addition to its <see cref="Name"/>. The 
/// <see cref="DisplayName"/> is the string that is written into the output log. By default
/// the display name is the same as the level name, but this can be used to alias levels
/// or to localize the log output.
/// </para>
/// <para>
/// Some of the predefined levels recognized by the system are:
/// </para>
/// <list type="bullet">
///    <item>
///      <description><see cref="Off"/>.</description>
///    </item>
///    <item>
///      <description><see cref="Fatal"/>.</description>
///    </item>
///    <item>
///      <description><see cref="Error"/>.</description>
///    </item>
///    <item>
///      <description><see cref="Warn"/>.</description>
///    </item>
///    <item>
///      <description><see cref="Info"/>.</description>
///    </item>
///    <item>
///      <description><see cref="Debug"/>.</description>
///    </item>
///    <item>
///      <description><see cref="All"/>.</description>
///    </item>
/// </list>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
[Log4NetSerializable]
public class Level : IComparable, ILog4NetSerializable, IEquatable<Level>, IComparable<Level>
{
  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="level">Integer value for this level, higher values represent more severe levels.</param>
  /// <param name="levelName">The string name of this level.</param>
  /// <param name="displayName">The display name for this level. This may be localized or otherwise different from the name</param>
  /// <remarks>
  /// <para>
  /// Initializes a new instance of the <see cref="Level" /> class with
  /// the specified level name and value.
  /// </para>
  /// </remarks>
  public Level(int level, string levelName, string displayName)
  {
    Value = level;
    Name = string.Intern(levelName.EnsureNotNull());
    DisplayName = displayName.EnsureNotNull();
  }

  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="level">Integer value for this level, higher values represent more severe levels.</param>
  /// <param name="levelName">The string name of this level.</param>
  /// <remarks>
  /// <para>
  /// Initializes a new instance of the <see cref="Level" /> class with
  /// the specified level name and value.
  /// </para>
  /// </remarks>
  public Level(int level, string levelName)
    : this(level, levelName, levelName)
  { }

  /// <summary>
  /// Serialization constructor
  /// </summary>
  /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data.</param>
  /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
  /// <remarks>
  /// <para>
  /// Initializes a new instance of the <see cref="Level" /> class 
  /// with serialized data.
  /// </para>
  /// </remarks>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter")]
  protected Level(SerializationInfo info, StreamingContext context)
  {
    // Use member names from log4net 2.x implicit serialzation for cross-version compat.
    Value = info.EnsureNotNull().GetInt32("m_levelValue");
    Name = info.GetString("m_levelName") ?? string.Empty;
    DisplayName = info.GetString("m_levelDisplayName") ?? string.Empty;
  }

  /// <summary>
  /// Gets the name of this level.
  /// </summary>
  /// <value>
  /// The name of this level.
  /// </value>
  /// <remarks>
  /// <para>
  /// Gets the name of this level.
  /// </para>
  /// </remarks>
  public string Name { get; }

  /// <summary>
  /// Gets the value of this level.
  /// </summary>
  public int Value { get; }

  /// <summary>
  /// Gets the display name of this level.
  /// </summary>
  public string DisplayName { get; }

  /// <summary>
  /// Returns the <see cref="string" /> representation of the current 
  /// <see cref="Level" />.
  /// </summary>
  /// <returns>
  /// A <see cref="string" /> representation of the current <see cref="Level" />.
  /// </returns>
  /// <remarks>
  /// <para>
  /// Returns the level <see cref="Name"/>.
  /// </para>
  /// </remarks>
  public override string ToString() => Name;

  /// <inheritdoc cref="Equals(Level)"/>
  public override bool Equals(object? obj) => obj is Level level && Equals(level);

  /// <summary>
  /// Compares levels.
  /// </summary>
  /// <param name="other">The object to compare against.</param>
  /// <returns><see langword="true"/> if the objects are equal.</returns>
  public bool Equals(Level? other) => other?.Value == Value;

  /// <summary>
  /// Returns a hash code
  /// </summary>
  /// <returns>A hash code for the current <see cref="Level" />.</returns>
  /// <remarks>
  /// <para>
  /// Returns a hash code suitable for use in hashing algorithms and data 
  /// structures like a hash table.
  /// </para>
  /// <para>
  /// Returns the hash code of the level <see cref="Value"/>.
  /// </para>
  /// </remarks>
  public override int GetHashCode() => Value;

  /// <inheritdoc cref="CompareTo(Level)"/>
  public int CompareTo(object? obj) => Compare(this, obj.EnsureIs<Level>());

  /// <summary>
  /// Compares this instance to a specified object and returns an 
  /// indication of their relative values.
  /// </summary>
  /// <param name="other">A <see cref="Level"/> instance or <see langword="null" /> to compare with this instance.</param>
  /// <returns>
  /// A 32-bit signed integer that indicates the relative order of the 
  /// values compared. The return value has these meanings:
  /// <list type="table">
  ///    <listheader>
  ///      <term>Value</term>
  ///      <description>Meaning</description>
  ///    </listheader>
  ///    <item>
  ///      <term>Less than zero</term>
  ///      <description>This instance is less than <paramref name="other" />.</description>
  ///    </item>
  ///    <item>
  ///      <term>Zero</term>
  ///      <description>This instance is equal to <paramref name="other" />.</description>
  ///    </item>
  ///    <item>
  ///      <term>Greater than zero</term>
  ///      <description>
  ///        <para>This instance is greater than <paramref name="other" />.</para>
  ///        <para>-or-</para>
  ///        <para><paramref name="other" /> is <see langword="null" />.</para>
  ///        </description>
  ///    </item>
  /// </list>
  /// </returns>
  /// <remarks>
  /// <para>
  /// <paramref name="other" /> must be an instance of <see cref="Level" /> 
  /// or <see langword="null" />; otherwise, an exception is thrown.
  /// </para>
  /// </remarks>
  public int CompareTo(Level other) => Compare(this, other.EnsureNotNull());

  /// <summary>
  /// Serializes this object into the <see cref="SerializationInfo" /> provided.
  /// </summary>
  /// <param name="info">The <see cref="SerializationInfo" /> to populate with data.</param>
  /// <param name="context">The destination for this serialization.</param>
  [SecurityCritical]
  [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand,
    SerializationFormatter = true)]
  public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
  {
    // Use member names from log4net 2.x implicit serialzation for cross-version compat.
    info.EnsureNotNull().AddValue("m_levelValue", Value);
    info.AddValue("m_levelName", Name);
    info.AddValue("m_levelDisplayName", DisplayName);
  }

  /// <summary>
  /// Returns a value indicating whether a specified <see cref="Level" /> 
  /// is greater than another specified <see cref="Level" />.
  /// </summary>
  /// <param name="l">A <see cref="Level" /></param>
  /// <param name="r">A <see cref="Level" /></param>
  /// <returns>
  /// <see langword="true"/> if <paramref name="l" /> is greater than 
  /// <paramref name="r" />; otherwise, <see langword="false"/>.
  /// </returns>
  public static bool operator >(Level? l, Level? r) => l?.Value > r?.Value;

  /// <summary>
  /// Returns a value indicating whether a specified <see cref="Level" /> 
  /// is less than another specified <see cref="Level" />.
  /// </summary>
  /// <param name="l">A <see cref="Level" /></param>
  /// <param name="r">A <see cref="Level" /></param>
  /// <returns>
  /// <see langword="true"/> if <paramref name="l" /> is less than 
  /// <paramref name="r" />; otherwise, <see langword="false"/>.
  /// </returns>
  public static bool operator <(Level? l, Level? r) => l?.Value < r?.Value;

  /// <summary>
  /// Returns a value indicating whether a specified <see cref="Level" /> 
  /// is greater than or equal to another specified <see cref="Level" />.
  /// </summary>
  /// <param name="l">A <see cref="Level" /></param>
  /// <param name="r">A <see cref="Level" /></param>
  /// <returns>
  /// <see langword="true"/> if <paramref name="l" /> is greater than or equal to 
  /// <paramref name="r" />; otherwise, <see langword="false"/>.
  /// </returns>
  public static bool operator >=(Level? l, Level? r) => l?.Value >= r?.Value;

  /// <summary>
  /// Returns a value indicating whether a specified <see cref="Level" /> 
  /// is less than or equal to another specified <see cref="Level" />.
  /// </summary>
  /// <param name="l">A <see cref="Level" /></param>
  /// <param name="r">A <see cref="Level" /></param>
  /// <returns>
  /// <see langword="true"/> if <paramref name="l" /> is less than or equal to 
  /// <paramref name="r" />; otherwise, <see langword="false"/>.
  /// </returns>
  public static bool operator <=(Level? l, Level? r) => l?.Value <= r?.Value;

  /// <summary>
  /// Returns a value indicating whether two specified <see cref="Level" /> 
  /// objects have the same value.
  /// </summary>
  /// <param name="l">A <see cref="Level" /> or <see langword="null" />.</param>
  /// <param name="r">A <see cref="Level" /> or <see langword="null" />.</param>
  /// <returns>
  /// <see langword="true"/> if the value of <paramref name="l" /> is the same as the 
  /// value of <paramref name="r" />; otherwise, <see langword="false"/>.
  /// </returns>
  public static bool operator ==(Level? l, Level? r) => l?.Value == r?.Value;

  /// <summary>
  /// Returns a value indicating whether two specified <see cref="Level" /> 
  /// objects have different values.
  /// </summary>
  /// <param name="l">A <see cref="Level" /> or <see langword="null" />.</param>
  /// <param name="r">A <see cref="Level" /> or <see langword="null" />.</param>
  /// <returns>
  /// <see langword="true"/> if the value of <paramref name="l" /> is different from
  /// the value of <paramref name="r" />; otherwise, <see langword="false"/>.
  /// </returns>
  public static bool operator !=(Level? l, Level? r) => !(l == r);

  /// <summary>
  /// Compares two specified <see cref="Level"/> instances.
  /// </summary>
  /// <param name="l">The first <see cref="Level"/> to compare.</param>
  /// <param name="r">The second <see cref="Level"/> to compare.</param>
  /// <returns>
  /// A 32-bit signed integer that indicates the relative order of the 
  /// two values compared. The return value has these meanings:
  /// <list type="table">
  ///    <listheader>
  ///      <term>Value</term>
  ///      <description>Meaning</description>
  ///    </listheader>
  ///    <item>
  ///      <term>Less than zero</term>
  ///      <description><paramref name="l" /> is less than <paramref name="r" />.</description>
  ///    </item>
  ///    <item>
  ///      <term>Zero</term>
  ///      <description><paramref name="l" /> is equal to <paramref name="r" />.</description>
  ///    </item>
  ///    <item>
  ///      <term>Greater than zero</term>
  ///      <description><paramref name="l" /> is greater than <paramref name="r" />.</description>
  ///    </item>
  /// </list>
  /// </returns>
  public static int Compare(Level? l, Level? r)
  {
    if (ReferenceEquals(l, r))
    {
      return 0;
    }
    if (l is null)
    {
      return -1;
    }
    if (r is null)
    {
      return 1;
    }
    return l.Value.CompareTo(r.Value);
  }

  /// <summary>
  /// The <see cref="Off" /> level designates a higher level than all the rest.
  /// </summary>
  public static readonly Level Off = new(int.MaxValue, "OFF");

  /// <summary>
  /// The <see cref="Emergency" /> level designates very severe error events. 
  /// System unusable, emergencies.
  /// </summary>
  // ReSharper disable once InconsistentNaming
  public static readonly Level Log4Net_Debug = new(120_000, "log4net:DEBUG");

  /// <summary>
  /// The <see cref="Emergency" /> level designates very severe error events. 
  /// System unusable, emergencies.
  /// </summary>
  public static readonly Level Emergency = new(120_000, "EMERGENCY");

  /// <summary>
  /// The <see cref="Fatal" /> level designates very severe error events 
  /// that will presumably lead the application to abort.
  /// </summary>
  public static readonly Level Fatal = new(110_000, "FATAL");

  /// <summary>
  /// The <see cref="Alert" /> level designates very severe error events. 
  /// Take immediate action, alerts.
  /// </summary>
  public static readonly Level Alert = new(100_000, "ALERT");

  /// <summary>
  /// The <see cref="Critical" /> level designates very severe error events. 
  /// Critical condition, critical.
  /// </summary>
  public static readonly Level Critical = new(90_000, "CRITICAL");

  /// <summary>
  /// The <see cref="Severe" /> level designates very severe error events.
  /// </summary>
  public static readonly Level Severe = new(80_000, "SEVERE");

  /// <summary>
  /// The <see cref="Error" /> level designates error events that might 
  /// still allow the application to continue running.
  /// </summary>
  public static readonly Level Error = new(70_000, "ERROR");

  /// <summary>
  /// The <see cref="Warn" /> level designates potentially harmful 
  /// situations.
  /// </summary>
  public static readonly Level Warn = new(60_000, "WARN");

  /// <summary>
  /// The <see cref="Notice" /> level designates informational messages 
  /// that highlight the progress of the application at the highest level.
  /// </summary>
  public static readonly Level Notice = new(50_000, "NOTICE");

  /// <summary>
  /// The <see cref="Info" /> level designates informational messages that 
  /// highlight the progress of the application at coarse-grained level.
  /// </summary>
  public static readonly Level Info = new(40_000, "INFO");

  /// <summary>
  /// The <see cref="Debug" /> level designates fine-grained informational 
  /// events that are most useful to debug an application.
  /// </summary>
  public static readonly Level Debug = new(30_000, "DEBUG");

  /// <summary>
  /// The <see cref="Fine" /> level designates fine-grained informational 
  /// events that are most useful to debug an application.
  /// </summary>
  public static readonly Level Fine = new(30_000, "FINE");

  /// <summary>
  /// The <see cref="Trace" /> level designates fine-grained informational 
  /// events that are most useful to debug an application.
  /// </summary>
  public static readonly Level Trace = new(20_000, "TRACE");

  /// <summary>
  /// The <see cref="Finer" /> level designates fine-grained informational 
  /// events that are most useful to debug an application.
  /// </summary>
  public static readonly Level Finer = new(20_000, "FINER");

  /// <summary>
  /// The <see cref="Verbose" /> level designates fine-grained informational 
  /// events that are most useful to debug an application.
  /// </summary>
  public static readonly Level Verbose = new(10_000, "VERBOSE");

  /// <summary>
  /// The <see cref="Finest" /> level designates fine-grained informational 
  /// events that are most useful to debug an application.
  /// </summary>
  public static readonly Level Finest = new(10_000, "FINEST");

  /// <summary>
  /// The <see cref="All" /> level designates the lowest level possible.
  /// </summary>
  public static readonly Level All = new(int.MinValue, "ALL");
}
