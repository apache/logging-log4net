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
using System.Text;
using System.Collections;

using log4net.Core;

namespace log4net.Util
{
	/// <summary>
	/// Implementation of List for the <see cref="log4net.ThreadContext"/>
	/// </summary>
	/// <remarks>
	/// <para>
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public sealed class ThreadContextList : IFixingRequired
	{
		#region Private Static Fields

		/// <summary>
		/// The thread local data slot used to store the stack.
		/// </summary>
		private readonly ArrayList m_list = new ArrayList();

		#endregion Private Static Fields

		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ThreadContextStack" /> class. 
		/// </summary>
		internal ThreadContextList()
		{
		}

		#endregion Public Instance Constructors

		#region Public Methods

		/// <summary>
		/// Clears all the contextual information held in this list.
		/// </summary>
		public void Clear() 
		{
			m_list.Clear();
		}

		/// <summary>
		/// Append a message to this list
		/// </summary>
		/// <param name="message">the message to append to this list</param>
		public void Append(string message) 
		{
			m_list.Add(message);
		}

		#endregion Public Methods

		#region Internal Methods

		/// <summary>
		/// Gets the current context information for this list.
		/// </summary>
		/// <returns>The current context information.</returns>
		internal string GetFullMessage() 
		{
			if (m_list.Count > 1)
			{
				// Build message using a string builder

				StringBuilder buf = new StringBuilder();

				buf.Append((string)m_list[0]);

				for(int i=1; i<m_list.Count; i++)
				{
					buf.Append(' ');
					buf.Append((string)m_list[i]);
				}

				return buf.ToString();
			}
			else if (m_list.Count == 1)
			{
				// Only one element, just return it
				return (string)m_list[0];
			}
			return null;
		}
  
		#endregion Internal Methods

		/// <summary>
		/// Gets the current context information for this list.
		/// </summary>
		/// <returns>Gets the current context information</returns>
		public override string ToString()
		{
			return GetFullMessage();
		}

		/// <summary>
		/// Get a portable version of this object
		/// </summary>
		/// <returns>the portable instance of this object</returns>
		object IFixingRequired.GetFixedObject()
		{
			return GetFullMessage();
		}
	}
}
