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

using log4net;
using log4net.Core;
using log4net.Util;

namespace log4net.Filter
{
	/// <summary>
	/// This is a simple filter based on <see cref="Level"/> matching.
	/// </summary>
	/// <remarks>
	/// <para>The filter admits three options <see cref="LevelMin"/> and <see cref="LevelMax"/>
	/// that determine the range of priorities that are matched, and
	/// <see cref="AcceptOnMatch"/>. If there is a match between the range
	/// of priorities and the <see cref="Level"/> of the <see cref="LoggingEvent"/>, then the 
	/// <see cref="Decide"/> method returns <see cref="FilterDecision.Accept"/> in case the <see cref="AcceptOnMatch"/> 
	/// option value is set to <c>true</c>, if it is <c>false</c>
	/// then <see cref="FilterDecision.Deny"/> is returned. If there is no match, <see cref="FilterDecision.Deny"/> is returned.</para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class LevelRangeFilter : FilterSkeleton
	{
		#region Member Variables

		/// <summary>
		/// Flag to indicate the behaviour when matching a <see cref="Level"/>
		/// </summary>
		private bool m_acceptOnMatch = true;

		/// <summary>
		/// the minimum <see cref="Level"/> value to match
		/// </summary>
		private Level m_levelMin;

		/// <summary>
		/// the maximum <see cref="Level"/> value to match
		/// </summary>
		private Level m_levelMax;

		#endregion

		#region Constructors

		/// <summary>
		/// Default constructor
		/// </summary>
		public LevelRangeFilter()
		{
		}

		#endregion

		/// <summary>
		/// The <see cref="AcceptOnMatch"/> property is a flag that determines
		/// the behaviour when a matching <see cref="Level"/> is found. If the
		/// flag is set to true then the filter will <see cref="FilterDecision.Accept"/> the 
		/// logging event, otherwise it will <see cref="FilterDecision.Neutral"/> the event.
		/// </summary>
		public bool AcceptOnMatch
		{
			get { return m_acceptOnMatch; }
			set { m_acceptOnMatch = value; }
		}

		/// <summary>
		/// Set the minimum matched <see cref="Level"/>
		/// </summary>
		public Level LevelMin
		{
			get { return m_levelMin; }
			set { m_levelMin = value; }
		}

		/// <summary>
		/// Sets the maximum matched <see cref="Level"/>
		/// </summary>
		public Level LevelMax
		{
			get { return m_levelMax; }
			set { m_levelMax = value; }
		}

		#region Override implementation of FilterSkeleton

		/// <summary>
		/// Check if the event should be logged.
		/// </summary>
		/// <remarks>
		/// If the <see cref="Level"/> of the logging event is outside the range
		/// matched by this filter then <see cref="FilterDecision.Deny"/>
		/// is returned. If the <see cref="Level"/> is matched then the value of
		/// <see cref="AcceptOnMatch"/> is checked. If it is true then
		/// <see cref="FilterDecision.Accept"/> is returned, otherwise
		/// <see cref="FilterDecision.Neutral"/> is returned.
		/// </remarks>
		/// <param name="loggingEvent">the logging event to check</param>
		/// <returns>see remarks</returns>
		override public FilterDecision Decide(LoggingEvent loggingEvent) 
		{
			if (loggingEvent == null)
			{
				throw new ArgumentNullException("loggingEvent");
			}

			if (m_levelMin != null) 
			{
				if (loggingEvent.Level < m_levelMin) 
				{
					// level of event is less than minimum
					return FilterDecision.Deny;
				}
			}

			if (m_levelMax != null) 
			{
				if (loggingEvent.Level > m_levelMax) 
				{
					// level of event is greater than maximum
					return FilterDecision.Deny;
				}
			}

			if (m_acceptOnMatch) 
			{
				// this filter set up to bypass later filters and always return
				// accept if level in range
				return FilterDecision.Accept;
			}
			else 
			{
				// event is ok for this filter; allow later filters to have a look..
				return FilterDecision.Neutral;
			}
		}

		#endregion
	}
}
