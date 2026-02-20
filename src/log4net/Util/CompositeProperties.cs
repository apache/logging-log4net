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

namespace log4net.Util;

/// <summary>
/// This class aggregates several PropertiesDictionary collections together.
/// </summary>
/// <remarks>
/// <para>
/// Provides a dictionary style lookup over an ordered list of
/// <see cref="PropertiesDictionary"/> collections.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
public sealed class CompositeProperties
{
  private PropertiesDictionary? _flattened;
  private readonly List<ReadOnlyPropertiesDictionary> _nestedProperties = [];

  /// <summary>
  /// Constructor
  /// </summary>
  /// <remarks>
  /// <para>
  /// Initializes a new instance of the <see cref="CompositeProperties" /> class.
  /// </para>
  /// </remarks>
  internal CompositeProperties()
  { }

  /// <summary>
  /// Gets the value of a property
  /// </summary>
  /// <value>
  /// The value for the property with the specified key
  /// </value>
  /// <remarks>
  /// <para>
  /// Looks up the value for the <paramref name="key" /> specified.
  /// The <see cref="PropertiesDictionary"/> collections are searched
  /// in the order in which they were added to this collection. The value
  /// returned is the value held by the first collection that contains
  /// the specified key.
  /// </para>
  /// <para>
  /// If none of the collections contain the specified key then
  /// <see langword="null"/> is returned.
  /// </para>
  /// </remarks>
  public object? this[string key]
  {
    get
    {
      // Look in the flattened properties first
      if (_flattened is not null)
      {
        return _flattened[key];
      }

      // Look for the key in all the nested properties
      foreach (ReadOnlyPropertiesDictionary cur in _nestedProperties)
      {
        if (cur.TryGetValue(key, out object? val))
        {
          return val;
        }
      }
      return null;
    }
  }

  /// <summary>
  /// Add a Properties Dictionary to this composite collection
  /// </summary>
  /// <param name="properties">the properties to add</param>
  /// <remarks>
  /// <para>
  /// Properties dictionaries added first take precedence over dictionaries added
  /// later.
  /// </para>
  /// </remarks>
  public void Add(ReadOnlyPropertiesDictionary properties)
  {
    _flattened = null;
    _nestedProperties.Add(properties);
  }

  /// <summary>
  /// Flatten this composite collection into a single properties dictionary
  /// </summary>
  /// <returns>the flattened dictionary</returns>
  /// <remarks>
  /// <para>
  /// Reduces the collection of ordered dictionaries to a single dictionary
  /// containing the resultant values for the keys.
  /// </para>
  /// </remarks>
  public PropertiesDictionary Flatten()
  {
    if (_flattened is null)
    {
      _flattened = [];

      for (int i = _nestedProperties.Count; --i >= 0;)
      {
        ReadOnlyPropertiesDictionary cur = _nestedProperties[i];

        foreach (KeyValuePair<string, object?> entry in cur)
        {
          _flattened[entry.Key] = entry.Value;
        }
      }
    }
    return _flattened;
  }
}