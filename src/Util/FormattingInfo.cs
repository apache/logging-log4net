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
using log4net.Util;

namespace log4net.Util
{
	/// <summary>
	/// Contain the information obtained when parsing formatting modifiers 
	/// in conversion modifiers.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class FormattingInfo
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="FormattingInfo" /> class.
		/// </summary>
		public FormattingInfo() 
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FormattingInfo" /> class.
		/// </summary>
		public FormattingInfo(int min, int max, bool leftAlign) 
		{
			m_min = min;
			m_max = max;
			m_leftAlign = leftAlign;
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets or sets the minimum value.
		/// </summary>
		/// <value>The minimum value.</value>
		public int Min
		{
			get { return m_min; }
			set { m_min = value; }
		}

		/// <summary>
		/// Gets or sets the maximum value.
		/// </summary>
		/// <value>The maximum value.</value>
		public int Max
		{
			get { return m_max; }
			set { m_max = value; }
		}

		/// <summary>
		/// Gets or sets a flag indicating whether left align is enabled
		/// or not.
		/// </summary>
		/// <value>A flag indicating whether left align is enabled or not.</value>
		public bool LeftAlign
		{
			get { return m_leftAlign; }
			set { m_leftAlign = value; }
		}

		#endregion Public Instance Properties

		#region Public Instance Methods

		/// <summary>
		/// Resets all properties to their default values.
		/// </summary>
		public void Reset() 
		{
			m_min = -1;
			m_max = int.MaxValue;
			m_leftAlign = false;	  
		}

		/// <summary>
		/// Dump debug info
		/// </summary>
		public void Dump() 
		{
			LogLog.Debug("FormattingInfo: min [" + m_min + "], max [" + m_max + "], leftAlign [" + m_leftAlign + "]");
		}

		#endregion Public Instance Methods

		#region Private Instance Fields

		private int m_min = -1;
		private int m_max = int.MaxValue;
		private bool m_leftAlign = false;

		#endregion Private Instance Fields
	}
}
