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
