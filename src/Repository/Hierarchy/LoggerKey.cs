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

