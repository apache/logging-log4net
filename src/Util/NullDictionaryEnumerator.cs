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
	/// An always empty <see cref="IDictionaryEnumerator"/>.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public sealed class NullDictionaryEnumerator : IDictionaryEnumerator
	{
		#region Private Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NullDictionaryEnumerator" /> class. 
		/// </summary>
		/// <remarks>
		/// Uses a private access modifier to enforce the singleton pattern.
		/// </remarks>
		private NullDictionaryEnumerator()
		{
		}

		#endregion Private Instance Constructors

		#region Public Static Properties
  
		/// <summary>
		/// Gets the singleton instance of the <see cref="NullDictionaryEnumerator" />.
		/// </summary>
		/// <returns>The singleton instance of the <see cref="NullDictionaryEnumerator" />.</returns>
		public static NullDictionaryEnumerator Instance
		{
			get { return s_instance; }
		}

		#endregion Public Static Properties

		#region Implementation of IEnumerator

		/// <summary>
		/// Gets the current object from the enumerator.
		/// </summary>
		/// <remarks>
		/// Throws an <see cref="InvalidOperationException" /> because the 
		/// <see cref="NullDictionaryEnumerator" /> never has a current value.
		/// </remarks>
		public object Current 
		{
			get	{ throw new InvalidOperationException(); }
		}
  
		/// <summary>
		/// Test if the enumerator can advance, if so advance.
		/// </summary>
		/// <returns><c>false</c> as the <see cref="NullDictionaryEnumerator" /> cannot advance.</returns>
		public bool MoveNext()
		{
			return false;
		}
  
		/// <summary>
		/// Resets the enumerator back to the start.
		/// </summary>
		public void Reset() 
		{
		}

		#endregion Implementation of IEnumerator

		#region Implementation of IDictionaryEnumerator

		/// <summary>
		/// Gets the current key from the enumerator.
		/// </summary>
		/// <remarks>
		/// Throws an exception because the <see cref="NullDictionaryEnumerator" />
		/// never has a current value.
		/// </remarks>
		public object Key 
		{
			get	{ throw new InvalidOperationException(); }
		}

		/// <summary>
		/// Gets the current value from the enumerator.
		/// </summary>
		/// <value>The current value from the enumerator.</value>
		/// <remarks>
		/// Throws an <see cref="InvalidOperationException" /> because the 
		/// <see cref="NullDictionaryEnumerator" /> never has a current value.
		/// </remarks>
		public object Value 
		{
			get	{ throw new InvalidOperationException(); }
		}

		/// <summary>
		/// Gets the current entry from the enumerator.
		/// </summary>
		/// <remarks>
		/// Throws an <see cref="InvalidOperationException" /> because the 
		/// <see cref="NullDictionaryEnumerator" /> never has a current entry.
		/// </remarks>
		public DictionaryEntry Entry 
		{
			get	{ throw new InvalidOperationException(); }
		}
  
		#endregion Implementation of IDictionaryEnumerator

		#region Private Static Fields

		/// <summary>
		/// The singleton instance of the <see cref="NullDictionaryEnumerator" />.
		/// </summary>
		private readonly static NullDictionaryEnumerator s_instance = new NullDictionaryEnumerator();
  
		#endregion Private Static Fields
	}
}
