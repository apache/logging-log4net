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
using System.Collections;

namespace log4net.Core
{
	/// <summary>
	/// Defines the set of levels recognised by the system.
	/// </summary>
	/// <remarks>
	/// Some of the predefined levels recognised by the system are
	/// <list type="bullet">
	///		<item>
	///			<description><see cref="Off"/>.</description>
	///		</item>
	///		<item>
	///			<description><see cref="Fatal"/>.</description>
	///		</item>
	///		<item>
	///			<description><see cref="Error"/>.</description>
	///		</item>
	///		<item>
	///			<description><see cref="Warn"/>.</description>
	///		</item>
	///		<item>
	///			<description><see cref="Info"/>.</description>
	///		</item>
	///		<item>
	///			<description><see cref="Debug"/>.</description>
	///		</item>
	///		<item>
	///			<description><see cref="All"/>.</description>
	///		</item>
	/// </list>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
#if !NETCF
	[Serializable]
#endif
	sealed public class Level : IComparable
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Level" /> class with
		/// the specicied level name and value.
		/// </summary>
		/// <param name="level">Integer value for this level, higher values represent more severe levels.</param>
		/// <param name="levelName">The string name of this level.</param>
		public Level(int level, string levelName) 
		{
			m_level = level;
			m_levelStr = string.Intern(levelName);
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets the name of the level.
		/// </summary>
		/// <value>
		/// The name of the level.
		/// </value>
		public string Name
		{
			get { return m_levelStr; }
		}

		/// <summary>
		/// Gets the value of the level.
		/// </summary>
		/// <value>
		/// The value of the level.
		/// </value>
		public int Value
		{
			get { return m_level; }
		}

		#endregion Public Instance Properties

		#region Override implementation of Object

		/// <summary>
		/// Returns the <see cref="string" /> representation of the current 
		/// <see cref="Level" />.
		/// </summary>
		/// <returns>
		/// A <see cref="string" /> representation of the current <see cref="Level" />.
		/// </returns>
		override public string ToString() 
		{
			return m_levelStr;
		}

		/// <summary>
		/// Compares the levels of <see cref="Level" /> instances, and 
		/// defers to base class if the target object is not a <see cref="Level" />
		/// instance.
		/// </summary>
		/// <param name="o">The object to compare against.</param>
		/// <returns><c>true</c> if the objects are equal.</returns>
		override public bool Equals(object o)
		{
			if (o != null && o is Level)
			{
				return m_level == ((Level)o).m_level;
			}
			else
			{
				return base.Equals(o);
			}
		}

		/// <summary>
		/// Returns a hash code suitable for use in hashing algorithms and data 
		/// structures like a hash table.
		/// </summary>
		/// <returns>A hash code for the current <see cref="Level" />.</returns>
		override public int GetHashCode()
		{
			return m_level;
		}

		#endregion Override implementation of Object

		#region Implementation of IComparable

		/// <summary>
		/// Compares this instance to a specified object and returns an 
		/// indication of their relative values.
		/// </summary>
		/// <param name="r">A <see cref="Level"/> instance or <see langword="null" /> to compare with this instance.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the 
		/// comparands. The return value has these meanings:
		/// <list type="table">
		///		<listheader>
		///			<term>Value</term>
		///			<description>Meaning</description>
		///		</listheader>
		///		<item>
		///			<term>Less than zero</term>
		///			<description>This instance is less than <paramref name="r" />.</description>
		///		</item>
		///		<item>
		///			<term>Zero</term>
		///			<description>This instance is equal to <paramref name="r" />.</description>
		///		</item>
		///		<item>
		///			<term>Greater than zero</term>
		///			<description>
		///				<para>This instance is greater than <paramref name="r" />.</para>
		///				<para>-or-</para>
		///				<para><paramref name="r" /> is <see langword="null" />.</para>
		///				</description>
		///		</item>
		/// </list>
		/// </returns>
		/// <remarks>
		/// <para>
		/// <paramref name="r" /> must be an instance of <see cref="Level" /> 
		/// or <see langword="null" />; otherwise, an exception is thrown.
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentException"><paramref name="r" /> is not a <see cref="Level" />.</exception>
		public int CompareTo(object r)
		{
			if (r is Level)
			{
				return Compare(this, (Level) r);
			}
			throw new ArgumentException("Parameter: r, Value: [" + r + "] is not an instance of Level");
		}

		#endregion Implementation of IComparable

		#region Operators

		/// <summary>
		/// Returns a value indicating whether a specified <see cref="Level" /> 
		/// is greater than another specified <see cref="Level" />.
		/// </summary>
		/// <param name="l">A <see cref="Level" /></param>
		/// <param name="r">A <see cref="Level" /></param>
		/// <returns>
		/// <c>true</c> if <paramref name="l" /> is greater than 
		/// <paramref name="r" />; otherwise, <c>false</c>.
		/// </returns>
		public static bool operator > (Level l, Level r)
		{
			return l.m_level > r.m_level;
		}

		/// <summary>
		/// Returns a value indicating whether a specified <see cref="Level" /> 
		/// is less than another specified <see cref="Level" />.
		/// </summary>
		/// <param name="l">A <see cref="Level" /></param>
		/// <param name="r">A <see cref="Level" /></param>
		/// <returns>
		/// <c>true</c> if <paramref name="l" /> is less than 
		/// <paramref name="r" />; otherwise, <c>false</c>.
		/// </returns>
		public static bool operator < (Level l, Level r)
		{
			return l.m_level < r.m_level;
		}

		/// <summary>
		/// Returns a value indicating whether a specified <see cref="Level" /> 
		/// is greater than or equal to another specified <see cref="Level" />.
		/// </summary>
		/// <param name="l">A <see cref="Level" /></param>
		/// <param name="r">A <see cref="Level" /></param>
		/// <returns>
		/// <c>true</c> if <paramref name="l" /> is greater than or equal to 
		/// <paramref name="r" />; otherwise, <c>false</c>.
		/// </returns>
		public static bool operator >= (Level l, Level r)
		{
			return l.m_level >= r.m_level;
		}

		/// <summary>
		/// Returns a value indicating whether a specified <see cref="Level" /> 
		/// is less than or equal to another specified <see cref="Level" />.
		/// </summary>
		/// <param name="l">A <see cref="Level" /></param>
		/// <param name="r">A <see cref="Level" /></param>
		/// <returns>
		/// <c>true</c> if <paramref name="l" /> is less than or equal to 
		/// <paramref name="r" />; otherwise, <c>false</c>.
		/// </returns>
		public static bool operator <= (Level l, Level r)
		{
			return l.m_level <= r.m_level;
		}

		/// <summary>
		/// Returns a value indicating whether two specified <see cref="Level" /> 
		/// objects have the same value.
		/// </summary>
		/// <param name="l">A <see cref="Level" /> or <see langword="null" />.</param>
		/// <param name="r">A <see cref="Level" /> or <see langword="null" />.</param>
		/// <returns>
		/// <c>true</c> if the value of <paramref name="l" /> is the same as the 
		/// value of <paramref name="r" />; otherwise, <c>false</c>.
		/// </returns>
		public static bool operator == (Level l, Level r)
		{
			if (((object)l) != null && ((object)r) != null)
			{
				return l.m_level == r.m_level;
			}
			else
			{
				return ((object) l) == ((object) r);
			}
		}

		/// <summary>
		/// Returns a value indicating whether two specified <see cref="Level" /> 
		/// objects have different values.
		/// </summary>
		/// <param name="l">A <see cref="Level" /> or <see langword="null" />.</param>
		/// <param name="r">A <see cref="Level" /> or <see langword="null" />.</param>
		/// <returns>
		/// <c>true</c> if the value of <paramref name="l" /> is different from
		/// the value of <paramref name="r" />; otherwise, <c>false</c>.
		/// </returns>
		public static bool operator != (Level l, Level r)
		{
			return !(l==r);
		}

		#endregion Operators

		#region Public Static Methods

		/// <summary>
		/// Compares two specified <see cref="Level"/> instances.
		/// </summary>
		/// <param name="l">The first <see cref="Level"/> to compare.</param>
		/// <param name="r">The second <see cref="Level"/> to compare.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the 
		/// two comparands. The return value has these meanings:
		/// <list type="table">
		///		<listheader>
		///			<term>Value</term>
		///			<description>Meaning</description>
		///		</listheader>
		///		<item>
		///			<term>Less than zero</term>
		///			<description><paramref name="l" /> is less than <paramref name="r" />.</description>
		///		</item>
		///		<item>
		///			<term>Zero</term>
		///			<description><paramref name="l" /> is equal to <paramref name="r" />.</description>
		///		</item>
		///		<item>
		///			<term>Greater than zero</term>
		///			<description><paramref name="l" /> is greater than <paramref name="r" />.</description>
		///		</item>
		/// </list>
		/// </returns>
		public static int Compare(Level l, Level r)
		{
			if (l == null && r == null)
			{
				return 0;
			}
			if (l == null)
			{
				return -1;
			}
			if (r == null)
			{
				return 1;
			}

			return l.m_level - r.m_level;
		}

		#endregion Public Static Methods

		#region Public Static Fields

		/// <summary>
		/// The <see cref="Off" /> level designates a higher level than all the 
		/// rest.
		/// </summary>
		public readonly static Level Off = new Level(int.MaxValue, "OFF");

		/// <summary>
		/// The <see cref="Emergency" /> level designates very severe error events. 
		/// System unusable, emergencies.
		/// </summary>
		public readonly static Level Emergency = new Level(120000, "EMERGENCY");

		/// <summary>
		/// The <see cref="Fatal" /> level designates very severe error events 
		/// that will presumably lead the application to abort.
		/// </summary>
		public readonly static Level Fatal = new Level(110000, "FATAL");

		/// <summary>
		/// The <see cref="Alert" /> level designates very severe error events. 
		/// Take immediate action, alerts.
		/// </summary>
		public readonly static Level Alert = new Level(100000, "ALERT");

		/// <summary>
		/// The <see cref="Critical" /> level designates very severe error events. 
		/// Critical condition, critical.
		/// </summary>
		public readonly static Level Critical = new Level(90000, "CRITICAL");

		/// <summary>
		/// The <see cref="Severe" /> level designates very severe error events.
		/// </summary>
		public readonly static Level Severe = new Level(80000, "SEVERE");

		/// <summary>
		/// The <see cref="Error" /> level designates error events that might 
		/// still allow the application to continue running.
		/// </summary>
		public readonly static Level Error = new Level(70000, "ERROR");

		/// <summary>
		/// The <see cref="Warn" /> level designates potentially harmful 
		/// situations.
		/// </summary>
		public readonly static Level Warn  = new Level(60000, "WARN");

		/// <summary>
		/// The <see cref="Notice" /> level designates informational messages 
		/// that highlight the progress of the application at the highest level.
		/// </summary>
		public readonly static Level Notice  = new Level(50000, "NOTICE");

		/// <summary>
		/// The <see cref="Info" /> level designates informational messages that 
		/// highlight the progress of the application at coarse-grained level.
		/// </summary>
		public readonly static Level Info  = new Level(40000, "INFO");

		/// <summary>
		/// The <see cref="Debug" /> level designates fine-grained informational 
		/// events that are most useful to debug an application.
		/// </summary>
		public readonly static Level Debug = new Level(30000, "DEBUG");

		/// <summary>
		/// The <see cref="Fine" /> level designates fine-grained informational 
		/// events that are most useful to debug an application.
		/// </summary>
		public readonly static Level Fine = new Level(30000, "FINE");

		/// <summary>
		/// The <see cref="Trace" /> level designates fine-grained informational 
		/// events that are most useful to debug an application.
		/// </summary>
		public readonly static Level Trace = new Level(20000, "TRACE");

		/// <summary>
		/// The <see cref="Finer" /> level designates fine-grained informational 
		/// events that are most useful to debug an application.
		/// </summary>
		public readonly static Level Finer = new Level(20000, "FINER");

		/// <summary>
		/// The <see cref="Verbose" /> level designates fine-grained informational 
		/// events that are most useful to debug an application.
		/// </summary>
		public readonly static Level Verbose = new Level(10000, "VERBOSE");

		/// <summary>
		/// The <see cref="Finest" /> level designates fine-grained informational 
		/// events that are most useful to debug an application.
		/// </summary>
		public readonly static Level Finest = new Level(10000, "FINEST");

		/// <summary>
		/// The <see cref="All" /> level designates the lowest level possible.
		/// </summary>
		public readonly static Level All = new Level(int.MinValue, "ALL");

		#endregion Public Static Fields

		#region Private Instance Fields

		private int m_level;
		private string m_levelStr;

		#endregion Private Instance Fields

	}
}
