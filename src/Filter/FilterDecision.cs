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

using log4net.Core;

namespace log4net.Filter
{
	/// <summary>
	/// The return result from <see cref="IFilter.Decide"/>
	/// </summary>
	/// <remarks>
	/// The return result from <see cref="IFilter.Decide"/>
	/// </remarks>
	public enum FilterDecision : int
	{
		/// <summary>
		/// The log event must be dropped immediately without 
		/// consulting with the remaining filters, if any, in the chain.
		/// </summary>
		Deny = -1,
  
		/// <summary>
		/// This filter is neutral with respect to the log event. 
		/// The remaining filters, if any, should be consulted for a final decision.
		/// </summary>
		Neutral = 0,

		/// <summary>
		/// The log event must be logged immediately without 
		/// consulting with the remaining filters, if any, in the chain.
		/// </summary>
		Accept = 1,
	}

}
