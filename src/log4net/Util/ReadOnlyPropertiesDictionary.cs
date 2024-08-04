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
using System.Xml;

namespace log4net.Util
{
  /// <summary>
  /// String keyed object map that is read only.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This collection is readonly and cannot be modified. It is not thread-safe.
  /// </para>
  /// <para>
  /// While this collection is serializable, only member
  /// objects that are serializable will
  /// be serialized along with this collection.
  /// </para>
  /// </remarks>
  /// <author>Nicko Cadell</author>
  /// <author>Gert Driesen</author>
  [Serializable]
  public class ReadOnlyPropertiesDictionary : ISerializable, IDictionary, IDictionary<string, object?>
  {
    private const string ReadOnlyMessage = "This is a read-only dictionary and cannot be modified";

    /// <summary>
    /// Constructor
    /// </summary>
    /// <remarks>
    /// <para>
    /// Initializes a new instance of the <see cref="ReadOnlyPropertiesDictionary" /> class.
    /// </para>
    /// </remarks>
    public ReadOnlyPropertiesDictionary()
    {
    }

    /// <summary>
    /// Copy Constructor
    /// </summary>
    /// <param name="propertiesDictionary">properties to copy</param>
    /// <remarks>
    /// <para>
    /// Initializes a new instance of the <see cref="ReadOnlyPropertiesDictionary" /> class.
    /// </para>
    /// </remarks>
    public ReadOnlyPropertiesDictionary(ReadOnlyPropertiesDictionary propertiesDictionary)
    {
      foreach (KeyValuePair<string, object?> entry in propertiesDictionary)
      {
        InnerHashtable[entry.Key] = entry.Value;
      }
    }

    /// <summary>
    /// Deserialization constructor
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data.</param>
    /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
    /// <remarks>
    /// <para>
    /// Initializes a new instance of the <see cref="ReadOnlyPropertiesDictionary" /> class 
    /// with serialized data.
    /// </para>
    /// </remarks>
    protected ReadOnlyPropertiesDictionary(SerializationInfo info, StreamingContext context)
    {
      foreach (var entry in info)
      {
        // The keys are stored as Xml encoded names
        InnerHashtable[XmlConvert.DecodeName(entry.Name) ?? string.Empty] = entry.Value;
      }
    }

    /// <summary>
    /// Gets the key names.
    /// </summary>
    /// <returns>An array of all the keys.</returns>
    /// <remarks>
    /// <para>
    /// Gets the key names.
    /// </para>
    /// </remarks>
    public string[] GetKeys()
    {
      var keys = new String[InnerHashtable.Count];
      InnerHashtable.Keys.CopyTo(keys, 0);
      return keys;
    }

    /// <summary>
    /// See <see cref="IDictionary{TKey,TValue}.ContainsKey(TKey)"/>.
    /// </summary>
    public bool ContainsKey(string key) => InnerHashtable.ContainsKey(key);

    /// <summary>
    /// See <see cref="IDictionary{TKey,TValue}.Add(TKey,TValue)"/>.
    /// </summary>
    public virtual void Add(string key, object? value)
    {
      throw new NotSupportedException(ReadOnlyMessage);
    }

    /// <summary>
    /// See <see cref="IDictionary{TKey,TValue}.Remove(TKey)"/>.
    /// </summary>
    public virtual bool Remove(string key)
    {
      throw new NotSupportedException(ReadOnlyMessage);
    }

    /// <summary>
    /// See <see cref="IDictionary{TKey,TValue}.TryGetValue(TKey,out TValue)"/>.
    /// </summary>
    public bool TryGetValue(string key, out object? value)
    {
      return InnerHashtable.TryGetValue(key, out value);
    }

    /// <summary>
    /// Gets or sets the value of the property with the specified key.
    /// </summary>
    /// <value>
    /// The value of the property with the specified key, or null if a property is not present in the dictionary.
    /// Note this is the <see cref="IDictionary"/> semantic, not that of <see cref="IDictionary{TKey,TValue}"/>.
    /// </value>
    /// <param name="key">The key of the property to get or set.</param>
    /// <remarks>
    /// <para>
    /// The property value will only be serialized if it is serializable.
    /// If it cannot be serialized it will be silently ignored if
    /// a serialization operation is performed.
    /// </para>
    /// </remarks>
    public virtual object? this[string key]
    {
      get
      {
        InnerHashtable.TryGetValue(key, out object? val);
        return val;
      }
      set => throw new NotSupportedException(ReadOnlyMessage);
    }

    /// <summary>
    /// Test if the dictionary contains a specified key
    /// </summary>
    /// <param name="key">the key to look for</param>
    /// <returns>true if the dictionary contains the specified key</returns>
    /// <remarks>
    /// <para>
    /// Test if the dictionary contains a specified key
    /// </para>
    /// </remarks>
    public bool Contains(string key)
    {
      return InnerHashtable.ContainsKey(key);
    }

    /// <summary>
    /// The hashtable used to store the properties
    /// </summary>
    /// <value>
    /// The internal collection used to store the properties
    /// </value>
    /// <remarks>
    /// <para>
    /// The hashtable used to store the properties
    /// </para>
    /// </remarks>
    protected Dictionary<string, object?> InnerHashtable { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Serializes this object into the <see cref="SerializationInfo" /> provided.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo" /> to populate with data.</param>
    /// <param name="context">The destination for this serialization.</param>
    /// <remarks>
    /// <para>
    /// Serializes this object into the <see cref="SerializationInfo" /> provided.
    /// </para>
    /// </remarks>
    [System.Security.SecurityCritical]
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter = true)]
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      foreach (KeyValuePair<string, object?> entry in InnerHashtable)
      {
        // If value is serializable then we add it to the list
        var isSerializable = entry.Value?.GetType().IsSerializable ?? false;
        if (!isSerializable)
        {
          continue;
        }

        // Store the keys as an XML encoded local name as it may contain colons (':')
        // which are NOT escaped by the Xml Serialization framework.
        // This must be a bug in the serialization framework as we cannot be expected
        // to know the implementation details of all the possible transport layers.
        var localKeyName = XmlConvert.EncodeLocalName(entry.Key);
        if (localKeyName is not null)
        {
          info.AddValue(localKeyName, entry.Value);
        }
      }
    }

    /// <summary>
    /// See <see cref="IDictionary.GetEnumerator"/>
    /// </summary>
    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
      return InnerHashtable.GetEnumerator();
    }

    /// <summary>
    /// See <see cref="IEnumerable{T}.GetEnumerator"/>
    /// </summary>
    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
      return InnerHashtable.GetEnumerator();
    }

    /// <summary>
    /// See <see cref="IDictionary.Remove"/>
    /// </summary>
    /// <param name="key"></param>
    void IDictionary.Remove(object key)
    {
      throw new NotSupportedException(ReadOnlyMessage);
    }

    /// <summary>
    /// See <see cref="IDictionary.Contains"/>
    /// </summary>
    bool IDictionary.Contains(object key)
    {
      if (key is not string k)
      {
        throw new ArgumentException("key must be a string");
      }
      return InnerHashtable.ContainsKey(k);
    }

    /// <summary>
    /// See <see cref="ICollection{T}.Add(T)"/>.
    /// </summary>
    public void Add(KeyValuePair<string, object?> item)
    {
      InnerHashtable.Add(item.Key, item.Value);
    }

    /// <summary>
    /// Removes all properties from the properties collection
    /// </summary>
    public virtual void Clear()
    {
      throw new NotSupportedException(ReadOnlyMessage);
    }

    /// <summary>
    /// See <see cref="ICollection{T}.Contains(T)"/>.
    /// </summary>
    public bool Contains(KeyValuePair<string, object?> item)
    {
      return InnerHashtable.TryGetValue(item.Key, out object? v) && item.Value == v;
    }

    /// <summary>
    /// See <see cref="ICollection{T}.CopyTo(T[],int)"/>.
    /// </summary>
    public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
    {
      int i = arrayIndex;
      foreach (var kvp in InnerHashtable)
      {
        array[i] = kvp;
        i++;
      }
    }

    /// <summary>
    /// See <see cref="ICollection{T}.Remove(T)"/>.
    /// </summary>
    public bool Remove(KeyValuePair<string, object?> item)
    {
      return InnerHashtable.Remove(item.Key);
    }

    /// <summary>
    /// See <see cref="IDictionary.Add"/>.
    /// </summary>
    void IDictionary.Add(object key, object? value)
    {
      throw new NotSupportedException(ReadOnlyMessage);
    }

    /// <summary>
    /// See <see cref="IDictionary.IsReadOnly"/>.
    /// </summary>
    bool IDictionary.IsReadOnly => true;

    /// <summary>
    /// See <see cref="IDictionary.this[object]"/>
    /// </summary>
    object? IDictionary.this[object key]
    {
      get
      {
        if (key is not string k)
        {
          throw new ArgumentException("key must be a string", nameof(key));
        }
        InnerHashtable.TryGetValue(k, out object? val);
        return val;
      }
      set => throw new NotSupportedException(ReadOnlyMessage);
    }

    /// <summary>
    /// See <see cref="IDictionary{TKey,TValue}.Keys"/>.
    /// </summary>
    public ICollection<string> Keys => InnerHashtable.Keys;

    /// <summary>
    /// See <see cref="IDictionary{TKey,TValue}.Values"/>.
    /// </summary>
    public ICollection<object?> Values => InnerHashtable.Values;

    /// <summary>
    /// See <see cref="IDictionary.Values"/>
    /// </summary>
    ICollection IDictionary.Values => InnerHashtable.Values;

    /// <summary>
    /// See <see cref="IDictionary.Keys"/>
    /// </summary>
    ICollection IDictionary.Keys => InnerHashtable.Keys;

    /// <summary>
    /// See <see cref="IDictionary.IsFixedSize"/>
    /// </summary>
    bool IDictionary.IsFixedSize => false;

    /// <summary>
    /// See <see cref="ICollection.CopyTo"/>
    /// </summary>
    void ICollection.CopyTo(Array array, int index)
    {
      ((ICollection)InnerHashtable).CopyTo(array, index);
    }

    /// <summary>
    /// See <see cref="ICollection.IsSynchronized"/>
    /// </summary>
    bool ICollection.IsSynchronized => false;

    /// <summary>
    /// The number of properties in this collection
    /// </summary>
    public int Count => InnerHashtable.Count;

    /// <summary>
    /// See <see cref="IDictionary.IsReadOnly"/>.
    /// </summary>
    public bool IsReadOnly => true;

    /// <summary>
    /// See <see cref="ICollection.SyncRoot"/>
    /// </summary>
    object ICollection.SyncRoot => InnerHashtable;

    /// <summary>
    /// See <see cref="IEnumerable.GetEnumerator"/>
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return ((IEnumerable)InnerHashtable).GetEnumerator();
    }
  }
}
