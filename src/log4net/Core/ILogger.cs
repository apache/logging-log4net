#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
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

using log4net.Repository;

namespace log4net.Core
{
  /// <summary>
  /// Interface that all loggers implement to support logging events and testing if a level
  /// is enabled for logging.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These methods will not throw exceptions. Note to implementers, ensure
  /// that the implementation of these methods cannot allow an exception
  /// to be thrown to the caller.
  /// </para>
  /// </remarks>
  /// <author>Nicko Cadell</author>
  /// <author>Gert Driesen</author>
  public interface ILogger
  {
    /// <summary>
    /// Gets the name of the logger.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Generates a logging event for the specified <paramref name="level"/> using
    /// the <paramref name="message"/> and <paramref name="exception"/>.
    /// </summary>
    /// <param name="callerStackBoundaryDeclaringType">The declaring type of the method that is
    /// the stack boundary into the logging system for this call.</param>
    /// <param name="level">The level of the message to be logged.</param>
    /// <param name="message">The message object to log.</param>
    /// <param name="exception">the exception to log, including its stack trace. Pass <c>null</c> to not log an exception.</param>
    /// <remarks>
    /// <para>
    /// This generic form is intended to be used by wrappers.
    /// </para>
    /// </remarks>
    void Log(Type callerStackBoundaryDeclaringType, Level level, object? message, Exception? exception);

    /// <summary>
    /// Logs the specified logging event through this logger.
    /// </summary>
    /// <param name="logEvent">The event being logged.</param>
    /// <remarks>
    /// <para>
    /// This is the most generic printing method that is intended to be used 
    /// by wrappers.
    /// </para>
    /// </remarks>
    void Log(LoggingEvent logEvent);

    /// <summary>
    /// Checks if this logger is enabled for a given <see cref="Level"/> passed as parameter.
    /// </summary>
    /// <param name="level">The level to check.</param>
    /// <returns>
    /// <c>true</c> if this logger is enabled for <c>level</c>, otherwise <c>false</c>.
    /// </returns>
    bool IsEnabledFor(Level? level);

    /// <summary>
    /// Gets the <see cref="ILoggerRepository"/> where this 
    /// <c>Logger</c> instance is attached to.
    /// </summary>
    ILoggerRepository? Repository { get; }
  }
}
