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

namespace log4net.Core
{
	/// <summary>
	/// Implementations of this interface allow certain appenders to decide
	/// when to perform an appender specific action.
	/// </summary>
	/// <remarks>
	/// Implementations of this interface allow certain appenders to decide
	/// when to perform an appender specific action.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public interface ITriggeringEventEvaluator
	{
		/// <summary>
		/// Is this the triggering event?
		/// </summary>
		/// <param name="loggingEvent">The event to check</param>
		/// <returns><c>true</c> if this event triggers the action, otherwise <c>false</c></returns>
		/// <remarks>
		/// Return <c>true</c> if this event triggers the action
		/// </remarks>
		bool IsTriggeringEvent(LoggingEvent loggingEvent);
	}
}
