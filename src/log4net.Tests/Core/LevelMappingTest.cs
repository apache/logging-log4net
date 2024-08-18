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
using System.Linq;
using System.Reflection;
using log4net.Core;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Core;

/// <summary>
/// Used for internal unit testing the <see cref="LevelMapping"/> class.
/// </summary>
[TestFixture]
public sealed class LevelMappingTest
{
  /// <inheritdoc/>
  private sealed class MappingEntry : LevelMappingEntry
  {
    /// <inheritdoc/>
    internal MappingEntry(Level level) => Level = level;

    /// <inheritdoc/>
    public override string ToString() => $"{Level?.Value} - {Level?.Name}";
  }

  /// <summary>
  /// Tests the sorting of the entries
  /// </summary>
  [Test]
  public void SortEntriesTest()
  {
    MappingEntry[] unsorted = [
      new(Level.Info),
      new(Level.Off),
      new(Level.Emergency),
      new(Level.Error),
      new(Level.Alert),
      new(Level.All),
      new(Level.Critical),
      new(Level.Debug),
      new(Level.Fatal),
      new(Level.Fine),
      new(Level.Finer),
      new(Level.Finest),
      new(Level.Log4Net_Debug),
      new(Level.Notice),
      new(Level.Severe),
      new(Level.Trace),
      new(Level.Verbose),
      new(Level.Warn)
    ];
    LevelMapping mapping = new();
    foreach (MappingEntry entry in unsorted)
    {
      mapping.Add(entry);
    }

    List<MappingEntry> withoutDuplicates = unsorted.GroupBy(entry => entry.Level!.Value)
      .Select(group => group.Last()).ToList();

    List<LevelMappingEntry> sorted = (List<LevelMappingEntry>)typeof(LevelMapping)
      .GetMethod("SortEntries", BindingFlags.NonPublic | BindingFlags.Instance)!
      .Invoke(mapping, Array.Empty<object>())!;

    CollectionAssert.AreEquivalent(withoutDuplicates, sorted);
    CollectionAssert.AreNotEqual(withoutDuplicates, sorted);

    int lowestLevelSeen = int.MaxValue;
    foreach (LevelMappingEntry entry in sorted)
    {
      Assert.IsTrue(lowestLevelSeen >= entry.Level!.Value, entry.Level.Name);
      lowestLevelSeen = entry.Level!.Value;
    }
    Assert.AreEqual(Level.All.Value, lowestLevelSeen);
  }

  /// <summary>
  /// Tests the <see cref="LevelMapping.Lookup(Level?)"/> method
  /// </summary>
  [Test]
  public void LookupTest()
  {
    LevelMapping mapping = new();
    mapping.Add(new MappingEntry(Level.Info));
    mapping.Add(new MappingEntry(Level.Off));
    mapping.Add(new MappingEntry(Level.Emergency));
    mapping.Add(new MappingEntry(Level.Warn));

    Assert.IsNull(mapping.Lookup(Level.Info)?.Level);

    mapping.ActivateOptions();

    Assert.AreEqual(Level.Info, mapping.Lookup(Level.Info)?.Level);
    Assert.AreEqual(Level.Off, mapping.Lookup(Level.Off)?.Level);
    Assert.AreEqual(Level.Emergency, mapping.Lookup(Level.Emergency)?.Level);
    Assert.AreEqual(Level.Warn, mapping.Lookup(Level.Warn)?.Level);
    Assert.AreEqual(Level.Warn, mapping.Lookup(Level.Error)?.Level);
    Assert.IsNull(mapping.Lookup(Level.Fine)?.Level);
    Assert.AreEqual(Level.Emergency, mapping.Lookup(Level.Log4Net_Debug)?.Level);
    Assert.IsNull(mapping.Lookup(Level.Trace)?.Level);
    Assert.AreEqual(Level.Warn, mapping.Lookup(Level.Alert)?.Level);
    Assert.IsNull(mapping.Lookup(Level.All)?.Level);
  }
}