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
using System.Collections;
using System.Collections.Specialized;

using log4net.Util;

namespace log4net.Core
{
	/// <summary>
	/// Mapping between string name and Level object
	/// </summary>
	/// <remarks>
	/// Mapping between string name and Level object.
	/// This mapping is held separately for each ILoggerRepository.
	/// The level name is case insensitive.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public sealed class LevelMap
	{
		#region Member Variables

		/// <summary>
		/// Mapping from level name to Level object. The
		/// level name is case insensitive
		/// </summary>
		private Hashtable m_mapName2Level = new Hashtable(CaseInsensitiveHashCodeProvider.Default, CaseInsensitiveComparer.Default);

		/// <summary>
		/// Mapping from level value to Level object
		/// </summary>
		private Hashtable m_mapValue2Level = new Hashtable();

		#endregion

		/// <summary>
		/// Construct the level map
		/// </summary>
		/// <remarks>
		/// Construct the level map.
		/// </remarks>
		public LevelMap()
		{
		}

		/// <summary>
		/// Clear the internal maps of all levels
		/// </summary>
		/// <remarks>
		/// Clear the internal maps of all levels
		/// </remarks>
		public void Clear()
		{
			// Clear all current levels
			m_mapName2Level.Clear();
			m_mapValue2Level.Clear();
		}

		/// <summary>
		/// Lookup a <see cref="Level"/> by name
		/// </summary>
		/// <param name="name">The name of the Level to lookup</param>
		/// <returns>a Level from the map with the name specified</returns>
		/// <remarks>
		/// Returns the <see cref="Level"/> from the
		/// map with the name specified. If the no level is
		/// found then <c>null</c> is returned.
		/// </remarks>
		public Level this[string name]
		{
			get
			{
				if (name == null)
				{
					throw new ArgumentNullException("name");
				}

				lock(this)
				{
					return (Level)m_mapName2Level[name];
				}
			}
		}

		/// <summary>
		/// Lookup a <see cref="Level"/> by value
		/// </summary>
		/// <param name="value">The value of the Level to lookup</param>
		/// <returns>a Level from the map with the value specified</returns>
		/// <remarks>
		/// Returns the <see cref="Level"/> from the
		/// map with the value specified. If the no level is
		/// found then <c>null</c> is returned.
		/// </remarks>
		public Level this[int value]
		{
			get
			{
				lock(this)
				{
					return (Level)m_mapValue2Level[value];
				}
			}
		}

		/// <summary>
		/// Create a new Level and add it to the map
		/// </summary>
		/// <param name="name">the string to display for the Level</param>
		/// <param name="value">the level value to give to the Level</param>
		public void Add(string name, int value)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (name.Length == 0)
			{
				throw log4net.Util.SystemInfo.CreateArgumentOutOfRangeException("name", name, "Parameter: name, Value: ["+name+"] out of range. Level name must not be empty");
			}

			Add(new Level(value, name));
		}

		/// <summary>
		/// Add a Level it to the map
		/// </summary>
		/// <param name="level">the Level to add</param>
		public void Add(Level level)
		{
			if (level == null)
			{
				throw new ArgumentNullException("level");
			}
			lock(this)
			{
				m_mapName2Level[level.Name] = level;
				m_mapValue2Level[level.Value] = level;
			}
		}

		/// <summary>
		/// Return all possible levels as a list of Level objects.
		/// </summary>
		/// <returns>all possible levels as a list of Level objects</returns>
		public LevelCollection AllLevels
		{
			get
			{
				lock(this)
				{
					return new LevelCollection(m_mapName2Level.Values);
				}
			}
		}

	}
}
