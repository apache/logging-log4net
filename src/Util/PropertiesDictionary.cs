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
#if !NETCF
using System.Runtime.Serialization;
using System.Xml;
#endif

namespace log4net.Util
{
	/// <summary>
	/// String keyed object map.
	/// </summary>
	/// <remarks>
	/// Only member objects that are serializable will
	/// be serialized along with this collection.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
#if NETCF
	public class PropertiesDictionary : IDictionary
#else
	[Serializable] public sealed class PropertiesDictionary : ISerializable, IDictionary
#endif
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertiesDictionary" /> class.
		/// </summary>
		public PropertiesDictionary()
		{
		}

		#endregion Public Instance Constructors

		#region Private Instance Constructors

#if !NETCF
		/// <summary>
		/// Initializes a new instance of the <see cref="PropertiesDictionary" /> class 
		/// with serialized data.
		/// </summary>
		/// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data.</param>
		/// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
		/// <remarks>
		/// Because this class is sealed the serialization constructor is private.
		/// </remarks>
		private PropertiesDictionary(SerializationInfo info, StreamingContext context)
		{
			foreach(SerializationEntry entry in info)
			{
				m_ht[XmlConvert.EncodeLocalName(entry.Name)] = entry.Value;
			}
		}
#endif

		#endregion Protected Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets the key names.
		/// </summary>
		/// <value>An array of key names.</value>
		/// <returns>An array of all the keys.</returns>
		public string[] GetKeys()
		{
			string[] keys = new String[m_ht.Count];
			m_ht.Keys.CopyTo(keys, 0);
			return keys;
		}

		/// <summary>
		/// Gets or sets the value of the  property with the specified key.
		/// </summary>
		/// <value>
		/// The value of the property with the specified key.
		/// </value>
		/// <param name="key">The key of the property to get or set.</param>
		/// <remarks>
		/// The property value will only be serialized if it is serializable.
		/// If it cannot be serialized it will be silently ignored if
		/// a serialization operation is performed.
		/// </remarks>
		public object this[string key]
		{
			get { return m_ht[key]; }
			set { m_ht[key] = value; }
		}

		#endregion Public Instance Properties

		#region Implementation of ISerializable

#if !NETCF
		/// <summary>
		/// Serializes this object into the <see cref="SerializationInfo" /> provided.
		/// </summary>
		/// <param name="info">The <see cref="SerializationInfo" /> to populate with data.</param>
		/// <param name="context">The destination for this serialization.</param>
		[System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter=true)]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			foreach(DictionaryEntry entry in m_ht)
			{
				// If value is serializable then we add it to the list
				if (entry.Value.GetType().IsSerializable)
				{
					info.AddValue(XmlConvert.EncodeLocalName(entry.Key as string), entry.Value);
				}
			}
		}
#endif

		#endregion Implementation of ISerializable

		#region Private Instance Fields

		private Hashtable m_ht = new Hashtable();

		#endregion Private Instance Fields

		#region Implementation of IDictionary

		/// <summary>
		/// See <see cref="IDictionary.GetEnumerator"/>
		/// </summary>
		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return m_ht.GetEnumerator();
		}

		/// <summary>
		/// See <see cref="IDictionary.Remove"/>
		/// </summary>
		/// <param name="key"></param>
		void IDictionary.Remove(object key)
		{
			m_ht.Remove(key);
		}

		/// <summary>
		/// See <see cref="IDictionary.Contains"/>
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		bool IDictionary.Contains(object key)
		{
			return m_ht.Contains(key);
		}

		/// <summary>
		/// Remove all properties from the properties collection
		/// </summary>
		public void Clear()
		{
			m_ht.Clear();
		}

		/// <summary>
		/// See <see cref="IDictionary.Add"/>
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		void IDictionary.Add(object key, object value)
		{
			if (!(key is string)) throw new ArgumentException("key must be a string");
			m_ht.Add(key, value);
		}

		/// <summary>
		/// See <see cref="IDictionary.IsReadOnly"/>
		/// </summary>
		bool IDictionary.IsReadOnly
		{
			get
			{
				return m_ht.IsReadOnly;
			}
		}

		/// <summary>
		/// See <see cref="IDictionary.this"/>
		/// </summary>
		object IDictionary.this[object key]
		{
			get
			{
				if (!(key is string)) throw new ArgumentException("key must be a string");
				return m_ht[key];
			}
			set
			{
				if (!(key is string)) throw new ArgumentException("key must be a string");
				m_ht[key] = value;
			}
		}

		/// <summary>
		/// See <see cref="IDictionary.Values"/>
		/// </summary>
		ICollection IDictionary.Values
		{
			get
			{
				return m_ht.Values;
			}
		}

		/// <summary>
		/// See <see cref="IDictionary.Keys"/>
		/// </summary>
		ICollection IDictionary.Keys
		{
			get
			{
				return m_ht.Keys;
			}
		}

		/// <summary>
		/// See <see cref="IDictionary.IsFixedSize"/>
		/// </summary>
		bool IDictionary.IsFixedSize
		{
			get
			{
				return m_ht.IsFixedSize;
			}
		}

		#endregion

		#region Implementation of ICollection

		/// <summary>
		/// See <see cref="ICollection.CopyTo"/>
		/// </summary>
		/// <param name="array"></param>
		/// <param name="index"></param>
		void ICollection.CopyTo(Array array, int index)
		{
			m_ht.CopyTo(array, index);
		}

		/// <summary>
		/// See <see cref="ICollection.IsSynchronized"/>
		/// </summary>
		bool ICollection.IsSynchronized
		{
			get
			{
				return m_ht.IsSynchronized;
			}
		}

		/// <summary>
		/// The number of properties in this collection
		/// </summary>
		public int Count
		{
			get
			{
				return m_ht.Count;
			}
		}

		/// <summary>
		/// See <see cref="ICollection.SyncRoot"/>
		/// </summary>
		object ICollection.SyncRoot
		{
			get
			{
				return m_ht.SyncRoot;
			}
		}

		#endregion

		#region Implementation of IEnumerable

		/// <summary>
		/// See <see cref="IEnumerable.GetEnumerator"/>
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)m_ht).GetEnumerator();
		}

		#endregion
	}
}

