#region Copyright & License
//
// Copyright 2001-2005 The Apache Software Foundation
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
