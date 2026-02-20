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
using log4net.Util;

namespace log4net.Core;

/// <summary>
/// Maps between string name and Level object.
/// </summary>
/// <remarks>
/// <para>
/// This mapping is held separately for each <see cref="Repository.ILoggerRepository"/>.
/// The level name is case-insensitive.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
public sealed class LevelMap
{
  /// <summary>
  /// Mapping from level name to Level object. The
  /// level name is case-insensitive
  /// </summary>
  private readonly ConcurrentDictionary<string, Level> _mapName2Level = new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Clear the internal maps of all levels
  /// </summary>
  /// <remarks>
  /// <para>
  /// Clear the internal maps of all levels
  /// </para>
  /// </remarks>
  public void Clear() =>
    // Clear all current levels
    _mapName2Level.Clear();

  /// <summary>
  /// Looks up a <see cref="Level"/> by name
  /// </summary>
  /// <param name="name">The name of the Level to look up.</param>
  /// <returns>A Level from the map with the name specified, or <see langword="null"/> if none is found.</returns>
  public Level? this[string name]
  {
    get
    {
      _mapName2Level.TryGetValue(name.EnsureNotNull(), out Level? level);
      return level;
    }
  }

  /// <summary>
  /// Creates a new Level and adds it to the map.
  /// </summary>
  /// <param name="name">the string to display for the Level</param>
  /// <param name="value">the level value to give to the Level</param>
  /// <seealso cref="Add(string,int,string)"/>
  public void Add(string name, int value) => Add(name, value, null);

  /// <summary>
  /// Creates a new Level and adds it to the map.
  /// </summary>
  /// <param name="name">the string to display for the Level</param>
  /// <param name="value">the level value to give to the Level</param>
  /// <param name="displayName">the display name to give to the Level</param>
  public void Add(string name, int value, string? displayName)
  {
    if (name.EnsureNotNull().Length == 0)
    {
      throw SystemInfo.CreateArgumentOutOfRangeException(nameof(name), name, 
        $"Parameter: name, Value: [{name}] out of range. Level name must not be empty");
    }

    if (string.IsNullOrEmpty(displayName))
    {
      displayName = name;
    }

    Add(new Level(value, name, displayName!));
  }

  /// <summary>
  /// Adds a Level to the map.
  /// </summary>
  /// <param name="level">the Level to add</param>
  public void Add(Level level) => _mapName2Level[level.EnsureNotNull().Name] = level;

  /// <summary>
  /// Gets all possible levels as a collection of Level objects.
  /// </summary>
  public LevelCollection AllLevels => new(_mapName2Level.Values);

  /// <summary>
  /// Looks up a named level from the map.
  /// </summary>
  /// <param name="defaultLevel">
  /// The name of the level to look up is taken from this level. 
  /// If the level is not set in the map then this level is added.
  /// If no level with the specified name is found then the 
  /// <paramref name="defaultLevel"/> argument is added to the level map
  /// and returned.
  /// </param>
  /// <returns>the level in the map with the name specified</returns>
  public Level LookupWithDefault(Level defaultLevel) 
    => _mapName2Level.GetOrAdd(defaultLevel.EnsureNotNull().Name, defaultLevel);
}
