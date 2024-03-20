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
using System.Collections.Generic;
using log4net.Core;

namespace log4net.Util
{
  /// <summary>
  /// Manages an ordered mapping from <see cref="Level"/> instances 
  /// to <see cref="LevelMappingEntry"/> subclasses.
  /// </summary>
  /// <author>Nicko Cadell</author>
  public sealed class LevelMapping : IOptionHandler
  {
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
      m_entriesMap[entry.Level] = entry;
    }

    /// <summary>
    /// Looks up the value for the specified level. Finds the nearest
    /// mapping value for the level that is equal to or less than the
    /// <paramref name="level"/> specified.
    /// </summary>
    /// <param name="level">the level to look up.</param>
    /// <returns>The <see cref="LevelMappingEntry"/> for the level or <c>null</c> if no mapping found</returns>
    public LevelMappingEntry? Lookup(Level? level)
    {
      if (m_entries is not null)
      {
        foreach (LevelMappingEntry entry in m_entries)
        {
          if (level >= entry.Level)
          {
            return entry;
          }
        }
      }
      return null;
    }

    /// <summary>
    /// Initialize options
    /// </summary>
    /// <remarks>
    /// <para>
    /// Caches the sorted list of <see cref="LevelMappingEntry"/> in an array
    /// </para>
    /// </remarks>
    public void ActivateOptions()
    {
      Level[] sortKeys = new Level[m_entriesMap.Count];
      LevelMappingEntry[] sortValues = new LevelMappingEntry[m_entriesMap.Count];

      m_entriesMap.Keys.CopyTo(sortKeys, 0);
      m_entriesMap.Values.CopyTo(sortValues, 0);

      // Sort in level order
      Array.Sort(sortKeys, sortValues, 0, sortKeys.Length, null);

      // Reverse list so that highest level is first
      Array.Reverse(sortValues, 0, sortValues.Length);

      foreach (LevelMappingEntry entry in sortValues)
      {
        entry.ActivateOptions();
      }

      m_entries = sortValues;
    }

    private readonly Dictionary<Level, LevelMappingEntry> m_entriesMap = new();
    private LevelMappingEntry[]? m_entries;
  }
}
