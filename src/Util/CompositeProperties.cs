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
	/// This class aggregates several PropertiesDictionary collections together.
	/// </summary>
	/// <author>Nicko Cadell</author>
	public sealed class CompositeProperties
	{
		#region Private Instance Fields

		private PropertiesDictionary m_flattened = null;
		private ArrayList m_nestedProperties = new ArrayList();

		#endregion Private Instance Fields

		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="CompositeProperties" /> class.
		/// </summary>
		internal CompositeProperties()
		{
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets the value of a property
		/// </summary>
		/// <value>
		/// The value for the property with the specified key
		/// </value>
		public object this[string key]
		{
			get 
			{
				// Look in the flattened properties first
				if (m_flattened != null)
				{
					return m_flattened[key];
				}

				// Look for the key in all the nested properties
				foreach(ReadOnlyPropertiesDictionary cur in m_nestedProperties)
				{
					if (cur.Contains(key))
					{
						return cur[key];
					}
				}
				return null;
			}
		}

		#endregion Public Instance Properties

		#region Public Instance Methods

		/// <summary>
		/// Add a Properties Dictionary to this composite collection
		/// </summary>
		/// <param name="properties">the properties to add</param>
		/// <remarks>
		/// <para>
		/// Properties dictionaries added first take precedence over dictionaries added
		/// later.
		/// </para>
		/// </remarks>
		public void Add(ReadOnlyPropertiesDictionary properties)
		{
			m_flattened = null;
			m_nestedProperties.Add(properties);
		}

		/// <summary>
		/// Flatten this composite collection into a single properties dictionary
		/// </summary>
		/// <returns>the flattened dictionary</returns>
		public PropertiesDictionary Flatten()
		{
			if (m_flattened == null)
			{
				m_flattened = new PropertiesDictionary();

				for(int i=m_nestedProperties.Count; --i>=0; )
				{
					ReadOnlyPropertiesDictionary cur = (ReadOnlyPropertiesDictionary)m_nestedProperties[i];

					foreach(DictionaryEntry entry in cur)
					{
						m_flattened[(string)entry.Key] = entry.Value;
					}
				}
			}
			return m_flattened;
		}

		#endregion Public Instance Methods
	}
}

