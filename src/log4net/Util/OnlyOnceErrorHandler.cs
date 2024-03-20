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

using log4net.Core;

namespace log4net.Util
{
  /// <summary>
  /// Implements log4net's default error handling policy which consists 
  /// of emitting a message for the first error in an appender and 
  /// ignoring all subsequent errors.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The error message is processed using the LogLog sub-system by default.
  /// </para>
  /// <para>
  /// This policy aims at protecting an otherwise working application
  /// from being flooded with error messages when logging fails.
  /// </para>
  /// </remarks>
  /// <author>Nicko Cadell</author>
  /// <author>Gert Driesen</author>
  /// <author>Ron Grabowski</author>
  public class OnlyOnceErrorHandler : IErrorHandler
  {
    /// <summary>
    /// Default Constructor
    /// </summary>
    /// <remarks>
    /// <para>
    /// Initializes a new instance of the <see cref="OnlyOnceErrorHandler" /> class.
    /// </para>
    /// </remarks>
    public OnlyOnceErrorHandler()
    {
      m_prefix = string.Empty;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="prefix">The prefix to use for each message.</param>
    /// <remarks>
    /// <para>
    /// Initializes a new instance of the <see cref="OnlyOnceErrorHandler" /> class
    /// with the specified prefix.
    /// </para>
    /// </remarks>
    public OnlyOnceErrorHandler(string prefix)
    {
      m_prefix = prefix;
    }

    /// <summary>
    /// Reset the error handler back to its initial disabled state.
    /// </summary>
    public void Reset()
    {
      EnabledDateUtc = DateTime.MinValue;
      ErrorCode = ErrorCode.GenericFailure;
      Exception = null;
      ErrorMessage = null;
      IsEnabled = true;
    }

    /// <summary>
    /// Log an Error
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="e">The exception.</param>
    /// <param name="errorCode">The internal error code.</param>
    /// <remarks>
    /// <para>
    /// Invokes <see cref="FirstError"/> if and only if this is the first error or the first error after <see cref="Reset"/> has been called.
    /// </para>
    /// </remarks>
    public void Error(string message, Exception? e, ErrorCode errorCode)
    {
      if (IsEnabled)
      {
        FirstError(message, e, errorCode);
      }
    }

    /// <summary>
    /// Log the very first error
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="e">The exception.</param>
    /// <param name="errorCode">The internal error code.</param>
    /// <remarks>
    /// <para>
    /// Sends the error information to <see cref="LogLog"/>'s Error method.
    /// </para>
    /// </remarks>
    public virtual void FirstError(string message, Exception? e, ErrorCode errorCode)
    {
      EnabledDateUtc = DateTime.UtcNow;
      ErrorCode = errorCode;
      Exception = e;
      ErrorMessage = message;
      IsEnabled = false;

      if (LogLog.InternalDebugging && !LogLog.QuietMode)
      {
        LogLog.Error(declaringType, "[" + m_prefix + "] ErrorCode: " + errorCode.ToString() + ". " + message, e);
      }
    }

    /// <summary>
    /// Log an Error
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="e">The exception.</param>
    /// <remarks>
    /// <para>
    /// Invokes <see cref="FirstError"/> if and only if this is the first error or the first error after <see cref="Reset"/> has been called.
    /// </para>
    /// </remarks>
    public void Error(string message, Exception e)
    {
      Error(message, e, ErrorCode.GenericFailure);
    }

    /// <summary>
    /// Log an error
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <remarks>
    /// <para>
    /// Invokes <see cref="FirstError"/> if and only if this is the first error or the first error after <see cref="Reset"/> has been called.
    /// </para>
    /// </remarks>
    public void Error(string message)
    {
      Error(message, null, ErrorCode.GenericFailure);
    }

    /// <summary>
    /// Is error logging enabled
    /// </summary>
    /// <remarks>
    /// <para>
    /// Logging is only enabled for the first error delivered to the <see cref="OnlyOnceErrorHandler"/>.
    /// </para>
    /// </remarks>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// The date the first error that triggered this error handler occurred, or <see cref="DateTime.MinValue"/> if it has not been triggered.
    /// </summary>
    public DateTime EnabledDate
    {
      get
      {
        if (EnabledDateUtc == DateTime.MinValue)
        {
          return DateTime.MinValue;
        }

        return EnabledDateUtc.ToLocalTime();
      }
    }

    /// <summary>
    /// The UTC date the first error that triggered this error handler occured, or <see cref="DateTime.MinValue"/> if it has not been triggered.
    /// </summary>
    public DateTime EnabledDateUtc { get; private set; }

    /// <summary>
    /// The message from the first error that triggered this error handler.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// The exception from the first error that triggered this error handler.
    /// </summary>
    /// <remarks>
    /// May be <see langword="null" />.
    /// </remarks>
    public Exception? Exception { get; private set; }

    /// <summary>
    /// The error code from the first error that triggered this error handler.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="log4net.Core.ErrorCode.GenericFailure"/>
    /// </remarks>
    public ErrorCode ErrorCode { get; private set; } = ErrorCode.GenericFailure;

    /// <summary>
    /// String to prefix each message with
    /// </summary>
    private readonly string m_prefix;

    /// <summary>
    /// The fully qualified type of the OnlyOnceErrorHandler class.
    /// </summary>
    /// <remarks>
    /// Used by the internal logger to record the Type of the
    /// log message.
    /// </remarks>
    private static readonly Type declaringType = typeof(OnlyOnceErrorHandler);
  }
}
