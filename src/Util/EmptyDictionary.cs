#region Copyright
//
// This framework is based on log4j see http://jakarta.apache.org/log4j
// Copyright (C) The Apache Software Foundation. All rights reserved.
//
// This software is published under the terms of the Apache Software
// License version 1.1, a copy of which has been included with this
// distribution in the LICENSE.txt file.
// 
#endregion

using System;
using System.Collections;

namespace log4net.Util
{
	/// <summary>
	/// An always empty <see cref="IDictionary"/>.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
#if !NETCF
	[Serializable]
#endif
	public sealed class EmptyDictionary : IDictionary
	{
		#region Private Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="EmptyDictionary" /> class. 
		/// </summary>
		/// <remarks>
		/// Uses a private access modifier to enforce the singleton pattern.
		/// </remarks>
		private EmptyDictionary()
		{
		}

		#endregion Private Instance Constructors
  
		#region Public Static Properties

		/// <summary>
		/// Gets the singleton instance of the <see cref="EmptyDictionary" />.
		/// </summary>
		/// <returns>The singleton instance of the <see cref="EmptyDictionary" />.</returns>
		public static EmptyDictionary Instance
		{
			get { return s_instance; }
		}

		#endregion Public Static Properties

		#region Implementation of ICollection

		/// <summary>
		/// Copies the elements of the <see cref="ICollection"/> to an 
		/// <see cref="Array"/>, starting at a particular Array index.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="Array"/> 
		/// that is the destination of the elements copied from 
		/// <see cref="ICollection"/>. The Array must have zero-based 
		/// indexing.</param>
		/// <param name="index">The zero-based index in array at which 
		/// copying begins.</param>
		public void CopyTo(System.Array array, int index)
		{
			// copy nothing
		}

		/// <summary>
		/// Gets a value indicating if access to the <see cref="ICollection"/> is synchronized (thread-safe).
		/// </summary>
		/// <value>
		/// <b>true</b> if access to the <see cref="ICollection"/> is synchronized (thread-safe); otherwise, <b>false</b>.
		/// </value>
		/// <remarks>
		/// For the <see cref="EmptyCollection"/> this property is always <b>true</b>.
		/// </remarks>
		public bool IsSynchronized
		{
			get	{ return true; }
		}

		/// <summary>
		/// Gets the number of elements contained in the <see cref="ICollection"/>
		/// </summary>
		/// <value>
		/// The number of elements contained in the <see cref="ICollection"/>.
		/// </value>
		public int Count
		{
			get { return 0; }
		}

		/// <summary>
		/// Gets an object that can be used to synchronize access to the <see cref="ICollection"/>.
		/// </summary>
		/// <value>
		/// An object that can be used to synchronize access to the <see cref="ICollection"/>.
		/// </value>
		public object SyncRoot
		{
			get { return this; }
		}

		#endregion Implementation of ICollection

		#region Implementation of IEnumerable

		/// <summary>
		/// Returns an enumerator that can iterate through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="IEnumerator"/> that can be used to 
		/// iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return NullEnumerator.Instance;
		}

		#endregion Implementation of IEnumerable

		#region Implementation of IDictionary

		/// <summary>
		/// Adds an element with the provided key and value to the 
		/// <see cref="EmptyDictionary" />.
		/// </summary>
		/// <param name="key">The <see cref="object" /> to use as the key of the element to add.</param>
		/// <param name="value">The <see cref="object" /> to use as the value of the element to add.</param>
		public void Add(object key, object value)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Removes all elements from the <see cref="EmptyDictionary" />.
		/// </summary>
		public void Clear()
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Determines whether the <see cref="EmptyDictionary" /> contains an element 
		/// with the specified key.
		/// </summary>
		/// <param name="key">The key to locate in the <see cref="EmptyDictionary" />.</param>
		/// <returns><c>false</c></returns>
		public bool Contains(object key)
		{
			return false;
		}

		/// <summary>
		/// Returns an enumerator that can iterate through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="IEnumerator"/> that can be used to 
		/// iterate through the collection.
		/// </returns>
		public IDictionaryEnumerator GetEnumerator()
		{
			return NullDictionaryEnumerator.Instance;
		}

		/// <summary>
		/// Removes the element with the specified key from the <see cref="EmptyDictionary" />.
		/// </summary>
		/// <param name="key">The key of the element to remove.</param>
		public void Remove(object key)
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Gets a value indicating whether the <see cref="EmptyDictionary" /> has a fixed size.
		/// </summary>
		/// <value><c>true</c></value>
		public bool IsFixedSize
		{
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating whether the <see cref="EmptyDictionary" /> is read-only.
		/// </summary>
		/// <value><c>true</c></value>
		public bool IsReadOnly
		{
			get	{ return true; }
		}

		/// <summary>
		/// Gets an <see cref="ICollection" /> containing the keys of the <see cref="EmptyDictionary" />.
		/// </summary>
		/// <value>An <see cref="ICollection" /> containing the keys of the <see cref="EmptyDictionary" />.</value>
		public System.Collections.ICollection Keys
		{
			get { return EmptyCollection.Instance; }
		}

		/// <summary>
		/// Gets an <see cref="ICollection" /> containing the values of the <see cref="EmptyDictionary" />.
		/// </summary>
		/// <value>An <see cref="ICollection" /> containing the values of the <see cref="EmptyDictionary" />.</value>
		public System.Collections.ICollection Values
		{
			get { return EmptyCollection.Instance; }
		}

		/// <summary>
		/// Gets or sets the element with the specified key.
		/// </summary>
		/// <param name="key">The key of the element to get or set.</param>
		/// <value><c>null</c></value>
		public object this[object key]
		{
			get { return null; }
			set { }
		}

		#endregion Implementation of IDictionary

		#region Private Static Fields

		/// <summary>
		/// The singleton instance of the empty dictionary.
		/// </summary>
		private readonly static EmptyDictionary s_instance = new EmptyDictionary();
  
		#endregion Private Static Fields
	}
}
