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
		/// <param name="callerFullName">The wrapper class' fully qualified class name.</param>
		/// <param name="level">The level of the message to be logged.</param>
		/// <param name="message">The message object to log.</param>
		/// <param name="t">the exception to log, including its stack trace. Pass <c>null</c> to not log an exception.</param>
		/// <remarks>
		/// Generates a logging event for the specified <paramref name="level"/> using
		/// the <paramref name="message"/> and <paramref name="t"/>.
		/// </remarks>
		void Log(string callerFullName, Level level, object message, Exception t);
  
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
