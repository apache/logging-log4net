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

namespace log4net.Repository.Hierarchy
{
	/// <summary>
	/// Used internally to accelerate hash table searches.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	internal class LoggerKey
	{
		#region Internal Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="LoggerKey" /> class 
		/// with the specified name.
		/// </summary>
		/// <param name="name">The name of the logger.</param>
		internal LoggerKey(string name) 
		{
#if NETCF
			// NETCF: String.Intern causes Native Exception
			m_name = name;
#else
			m_name = string.Intern(name);
#endif
			m_hashCache = name.GetHashCode();
		}

		#endregion Internal Instance Constructors

		#region Internal Instance Properties

		/// <summary>
		/// Gets the name of the logger.
		/// </summary>
		/// <value>
		/// The name of the logger.
		/// </value>
		internal string Value
		{
			get { return m_name; }
		}

		#endregion Internal Instance Properties

		#region Override implementation of Object

		/// <summary>
		/// Returns a hash code for the current instance.
		/// </summary>
		/// <returns>A hash code for the current instance.</returns>
		override public int GetHashCode() 
		{
			return m_hashCache;
		}

		/// <summary>
		/// Determines whether two <see cref="LoggerKey" /> instances 
		/// are equal.
		/// </summary>
		/// <param name="obj">The <see cref="object" /> to compare with the current <see cref="LoggerKey" />.</param>
		/// <returns>
		/// <c>true</c> if the specified <see cref="object" /> is equal to the current <see cref="LoggerKey" />; otherwise, <c>false</c>.</returns>
		override public bool Equals(object obj) 
		{
			if (this == obj)
			{
				return true;
			}
			if ((obj != null) && (obj is LoggerKey)) 
			{
#if NETCF
				return m_name == ((LoggerKey)obj).m_name;
#else
				// Compare reference types rather than string's overloaded ==
				return ((object)m_name) == ((object)((LoggerKey)obj).m_name);
#endif
			}
			return false;
		}

		#endregion

		#region Private Instance Fields

		private string m_name;  
		private int m_hashCache;

		#endregion Private Instance Fields
	}	
}

