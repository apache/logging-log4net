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
	/// An always empty <see cref="ICollection"/>.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
#if !NETCF
	[Serializable]
#endif
	public sealed class EmptyCollection : ICollection
	{
		#region Private Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="EmptyCollection" /> class. 
		/// </summary>
		/// <remarks>
		/// Uses a private access modifier to enforce the singleton pattern.
		/// </remarks>
		private EmptyCollection()
		{
		}

		#endregion Private Instance Constructors
  
		#region Public Static Properties

		/// <summary>
		/// Gets the singleton instance of the empty collection.
		/// </summary>
		/// <returns>The singletion instance of the empty collection.</returns>
		public static EmptyCollection Instance
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
		/// For the <see cref="EmptyCollection"/> this property is always <c>true</c>.
		/// </remarks>
		public bool IsSynchronized
		{
			get	{ return true; }
		}

		/// <summary>
		/// Gets the number of elements contained in the <see cref="ICollection"/>.
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
		public IEnumerator GetEnumerator()
		{
			return NullEnumerator.Instance;
		}

		#endregion Implementation of IEnumerable

		#region Private Static Fields

		/// <summary>
		/// The singleton instance of the empty collection.
		/// </summary>
		private readonly static EmptyCollection s_instance = new EmptyCollection();
  
		#endregion Private Static Fields
	}
}
