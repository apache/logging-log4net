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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace log4net.Util;

/// <summary>
/// String keyed object map.
/// </summary>
/// <remarks>
/// <para>
/// While this collection is serializable, only member objects that are serializable
/// will be serialized along with this collection.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
[Log4NetSerializable]
public sealed class PropertiesDictionary : ReadOnlyPropertiesDictionary, ILog4NetSerializable, IDictionary
{
  /// <summary>
  /// Constructor
  /// </summary>
  /// <remarks>
  /// <para>
  /// Initializes a new instance of the <see cref="PropertiesDictionary" /> class.
  /// </para>
  /// </remarks>
  public PropertiesDictionary()
  { }

  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="propertiesDictionary">properties to copy</param>
  /// <remarks>
  /// <para>
  /// Initializes a new instance of the <see cref="PropertiesDictionary" /> class.
  /// </para>
  /// </remarks>
  public PropertiesDictionary(ReadOnlyPropertiesDictionary propertiesDictionary)
    : base(propertiesDictionary)
  { }

  /// <summary>
  /// Initializes a new instance of the <see cref="PropertiesDictionary" /> class 
  /// with serialized data.
  /// </summary>
  /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data.</param>
  /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
  /// <remarks>
  /// <para>
  /// Because this class is sealed the serialization constructor is private.
  /// </para>
  /// </remarks>
  private PropertiesDictionary(SerializationInfo info, StreamingContext context)
    : base(info, context)
  { }

  /// <summary>
  /// Gets or sets the value of the  property with the specified key.
  /// </summary>
  /// <value>
  /// The value of the property with the specified key.
  /// </value>
  /// <param name="key">The key of the property to get or set.</param>
  /// <remarks>
  /// <para>
  /// The property value will only be serialized if it is serializable.
  /// If it cannot be serialized it will be silently ignored if
  /// a serialization operation is performed.
  /// </para>
  /// </remarks>
  public override object? this[string key]
  {
    get => base[key];
    set => InnerHashtable[key] = value;
  }

  /// <summary>
  /// See <see cref="IDictionary{TKey,TValue}.Add(TKey,TValue)"/>.
  /// </summary>
  public override void Add(string key, object? value) => InnerHashtable.Add(key, value);

  /// <summary>
  /// Remove the entry with the specified key from this dictionary
  /// </summary>
  /// <param name="key">the key for the entry to remove</param>
  /// <remarks>
  /// <para>
  /// Remove the entry with the specified key from this dictionary
  /// </para>
  /// </remarks>
  public override bool Remove(string key) => InnerHashtable.Remove(key);

  /// <summary>
  /// See <see cref="IDictionary.GetEnumerator"/>
  /// </summary>
  /// <returns>an enumerator</returns>
  /// <remarks>
  /// <para>
  /// Returns a <see cref="IDictionaryEnumerator"/> over the contest of this collection.
  /// </para>
  /// </remarks>
  IDictionaryEnumerator IDictionary.GetEnumerator() => InnerHashtable.GetEnumerator();

  /// <summary>
  /// See <see cref="IDictionary.Remove"/>
  /// </summary>
  /// <param name="key">the key to remove</param>
  /// <remarks>
  /// <para>
  /// Remove the entry with the specified key from this dictionary
  /// </para>
  /// </remarks>
  void IDictionary.Remove(object key)
  {
    if (key is not string k)
    {
      throw new ArgumentException("key must be a string");
    }

    InnerHashtable.Remove(k);
  }

  /// <summary>
  /// Remove all properties from the properties collection
  /// </summary>
  /// <remarks>
  /// <para>
  /// Remove all properties from the properties collection
  /// </para>
  /// </remarks>
  public override void Clear() => InnerHashtable.Clear();

  /// <summary>
  /// See <see cref="IDictionary.Add"/>
  /// </summary>
  /// <param name="key">the key</param>
  /// <param name="value">the value to store for the key</param>
  /// <remarks>
  /// <para>
  /// Store a value for the specified <see cref="string"/> <paramref name="key"/>.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentException">Thrown if the <paramref name="key"/> is not a string</exception>
  void IDictionary.Add(object key, object? value) => InnerHashtable.Add(key.EnsureIs<string>(), value);

  /// <summary>
  /// See <see cref="IDictionary.IsReadOnly"/>
  /// </summary>
  /// <value>
  /// <see langword="false"/>
  /// </value>
  /// <remarks>
  /// <para>
  /// This collection is modifiable. This property always
  /// returns <see langword="false"/>.
  /// </para>
  /// </remarks>
  bool IDictionary.IsReadOnly => false;

  /// <summary>
  /// See <see cref="IDictionary.this"/>
  /// </summary>
  /// <value>
  /// The value for the key specified.
  /// </value>
  /// <remarks>
  /// <para>
  /// Get or set a value for the specified <see cref="string"/> <paramref name="key"/>.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentException">Thrown if the <paramref name="key"/> is not a string</exception>
  object? IDictionary.this[object key]
  {
    get
    {
      InnerHashtable.TryGetValue(key.EnsureIs<string>(), out object? val);
      return val;
    }
    set => InnerHashtable[key.EnsureIs<string>()] = value;
  }
}