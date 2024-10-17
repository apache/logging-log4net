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

using System.Collections.Generic;
using System.Linq;
using log4net.Core;

namespace log4net.Util;

/// <summary>
/// Manages an ordered mapping from <see cref="Level"/> instances 
/// to <see cref="LevelMappingEntry"/> subclasses.
/// </summary>
/// <author>Nicko Cadell</author>
public sealed class LevelMapping : IOptionHandler
{
  private readonly Dictionary<Level, LevelMappingEntry> _entries = [];
  private List<LevelMappingEntry>? _sortedEntries;

  /// <summary>
  /// Add a <see cref="LevelMappingEntry"/> to this mapping
  /// </summary>
  /// <param name="entry">the entry to add</param>
  /// <remarks>
  /// <para>
  /// If a <see cref="LevelMappingEntry"/> has previously been added
  /// for the same <see cref="Level"/> then that entry will be 
  /// overwritten.
  /// </para>
  /// </remarks>
  public void Add(LevelMappingEntry entry)
  {
    if (entry.Level is not null)
    {
      _entries[entry.Level] = entry;
    }
  }

  /// <summary>
  /// Looks up the value for the specified level. Finds the nearest
  /// mapping value for the level that is equal to or less than the
  /// <paramref name="level"/> specified.
  /// </summary>
  /// <param name="level">the level to look up.</param>
  /// <returns>The <see cref="LevelMappingEntry"/> for the level or <see langword="null"/> if no mapping found</returns>
  public LevelMappingEntry? Lookup(Level? level)
  {
    if (level is null || _sortedEntries is null)
    {
      return null;
    }

    foreach (LevelMappingEntry entry in _sortedEntries)
    {
      if (level >= entry.Level)
      {
        return entry;
      }
    }
    return null;
  }

  /// <summary>
  /// Initialize options
  /// </summary>
  /// <remarks>
  /// Caches the sorted list of <see cref="LevelMappingEntry"/>
  /// </remarks>
  public void ActivateOptions()
  {
    _sortedEntries = SortEntries();
    _sortedEntries.ForEach(entry => entry.ActivateOptions());
  }

  private List<LevelMappingEntry> SortEntries()
    => _entries.OrderByDescending(entry => entry.Key).Select(entry => entry.Value).ToList();
}