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
using System.IO;

using log4net;
using log4net.Core;

namespace log4net.Layout
{
	/// <summary>
	/// Interface for raw layout objects
	/// </summary>
	/// <remarks>
	/// <para>Interface used to format a <see cref="LoggingEvent"/>
	/// to an object.</para>
	/// 
	/// <para>This interface should not be confused with the
	/// <see cref="ILayout"/> interface. This interface is used in
	/// only certain specialized situations where a raw object is
	/// required rather than a formatted string. The <see cref="ILayout"/>
	/// is not generally useful than this interface.</para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class Layout2RawLayoutAdapter : IRawLayout
	{
		#region Member Variables

		/// <summary>
		/// The layout to adapt
		/// </summary>
		private ILayout m_layout;

		#endregion

		#region Constructors

		/// <summary>
		/// Construct a new adapter
		/// </summary>
		/// <param name="layout">the layout to adapt</param>
		public Layout2RawLayoutAdapter(ILayout layout)
		{
			m_layout = layout;
		}

		#endregion

		#region Implementation of IRawLayout

		/// <summary>
		/// Format the logging event as an object.
		/// </summary>
		/// <param name="loggingEvent">The event to format</param>
		/// <returns>returns the formatted event</returns>
		/// <remarks>
		/// <para>Format the logging event as an object.</para>
		/// <para>Uses the <see cref="ILayout"/> object supplied to 
		/// the constructor to perform the formatting.</para>
		/// </remarks>
		virtual public object Format(LoggingEvent loggingEvent)
		{
			StringWriter writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
			m_layout.Format(writer, loggingEvent);
			return writer.ToString();
		}

		#endregion
	}
}
