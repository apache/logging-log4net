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

using log4net.Core;
using log4net.Util;

namespace log4net.Layout
{
	/// <summary>
	/// Extract the value of a property from the <see cref="LoggingEvent"/>
	/// </summary>
	/// <remarks>
	/// Extract the value of a property from the <see cref="LoggingEvent"/>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public class RawPropertyLayout : IRawLayout
	{
		#region Constructors

		/// <summary>
		/// Constructs a RawPropertyLayout
		/// </summary>
		/// <remarks>
		/// </remarks>
		public RawPropertyLayout()
		{
		}

		#endregion

		private string m_key;

		/// <summary>
		/// The name of the value to lookup in the <see cref="LoggingEvent.Properties"/> collection.
		/// </summary>
		public string Key
		{
			get { return m_key; }
			set { m_key = value; }
		}
  
		#region Implementation of IRawLayout

		/// <summary>
		/// Implement this method to create your own layout format.
		/// </summary>
		/// <param name="loggingEvent">The event to format</param>
		/// <returns>returns the formatted event</returns>
		/// <remarks>
		/// <para>Implement this method to create your own layout format.</para>
		/// </remarks>
		public virtual object Format(LoggingEvent loggingEvent)
		{
			return loggingEvent.Properties[m_key];
		}

		#endregion
	}
}
