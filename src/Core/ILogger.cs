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

using log4net.Core;
using log4net.Repository;

namespace log4net.Core
{
	/// <summary>
	/// Interface that all loggers should implement.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public interface ILogger
	{
		/// <summary>
		/// Gets the name of the logger.
		/// </summary>
		/// <value>
		/// The name of the logger.
		/// </value>
		string Name { get; }

		/// <summary>
		/// This generic form is intended to be used by wrappers.
		/// </summary>
		/// <param name="callerStackBoundaryDeclaringType">The declaring type of the method that is
		/// the stack boundary into the logging system for this call.</param>
		/// <param name="level">The level of the message to be logged.</param>
		/// <param name="message">The message object to log.</param>
		/// <param name="t">the exception to log, including its stack trace. Pass <c>null</c> to not log an exception.</param>
		/// <remarks>
		/// Generates a logging event for the specified <paramref name="level"/> using
		/// the <paramref name="message"/> and <paramref name="t"/>.
		/// </remarks>
		void Log(Type callerStackBoundaryDeclaringType, Level level, object message, Exception t);
  
		/// <summary>
		/// This is the most generic printing method that is intended to be used 
		/// by wrappers.
		/// </summary>
		/// <param name="logEvent">The event being logged.</param>
		/// <remarks>
		/// Logs the specified logging event.
		/// </remarks>
		void Log(LoggingEvent logEvent);

		/// <summary>
		/// Checks if this logger is enabled for a given <see cref="Level"/> passed as parameter.
		/// </summary>
		/// <param name="level">The level to check.</param>
		/// <returns>
		/// <c>true</c> if this logger is enabled for <c>level</c>, otherwise <c>false</c>.
		/// </returns>
		bool IsEnabledFor(Level level);

		/// <summary>
		/// Gets the <see cref="ILoggerRepository"/> where this 
		/// <c>Logger</c> instance is attached to.
		/// </summary>
		/// <value>The <see cref="ILoggerRepository" /> that this logger belongs to.</value>
		ILoggerRepository Repository { get; }
	}
}
