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
using System.Diagnostics;

namespace log4net.Repository.Hierarchy;

/// <summary>
/// Used internally to accelerate hash table searches.
/// </summary>
/// <remarks>
/// <para>
/// Internal class used to improve performance of 
/// string keyed hashtables.
/// </para>
/// <para>
/// The hashcode of the string is cached for reuse.
/// The string is stored as an interned value.
/// When comparing two <see cref="LoggerKey"/> objects for equality 
/// the reference equality of the interned strings is compared.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
[DebuggerDisplay("{Name}")]
internal readonly struct LoggerKey
{
  /// <summary>
  /// Construct key with string name
  /// </summary>
  /// <remarks>
  /// <para>
  /// Initializes a new instance of the <see cref="LoggerKey" /> class 
  /// with the specified name.
  /// </para>
  /// <para>
  /// Stores the hashcode of the string and interns
  /// the string key to optimize comparisons.
  /// </para>
  /// <note>
  /// The Compact Framework 1.0 the <see cref="string.Intern"/>
  /// method does not work. On the Compact Framework
  /// the string keys are not interned nor are they
  /// compared by reference.
  /// </note>
  /// </remarks>
  /// <param name="name">The name of the logger.</param>
  internal LoggerKey(string name)
  {
    Name = string.Intern(name);
    hashCache = name.GetHashCode();
  }

  /// <summary>
  /// Returns a hash code for the current instance.
  /// </summary>
  /// <returns>A hash code for the current instance.</returns>
  /// <remarks>
  /// <para>
  /// Returns the cached hashcode.
  /// </para>
  /// </remarks>
  public override int GetHashCode() => hashCache;

  /// <summary>
  /// Name of the Logger
  /// </summary>
  internal string Name { get; }

  private readonly int hashCache;

  public static Comparer ComparerInstance { get; } = new();

  public sealed class Comparer : IEqualityComparer<LoggerKey>
  {
    public bool Equals(LoggerKey x, LoggerKey y) => x.hashCache == y.hashCache && x.Name == y.Name;

    public int GetHashCode(LoggerKey obj) => obj.hashCache;
  }
}
