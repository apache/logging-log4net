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
