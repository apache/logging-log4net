#region Copyright & License
//
// Copyright 2001-2005 The Apache Software Foundation
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

namespace log4net.Util
{
	/// <summary>
	/// Implements log4net's default error handling policy which consists 
	/// of emitting a message for the first error in an appender and 
	/// ignoring all subsequent errors.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The error message is printed on the standard error output stream.
	/// </para>
	/// <para>
	/// This policy aims at protecting an otherwise working application
	/// from being flooded with error messages when logging fails.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class OnlyOnceErrorHandler : IErrorHandler
	{
		#region Public Instance Constructors

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
			m_prefix = "";
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

		#endregion Public Instance Constructors

		#region Implementation of IErrorHandler

		/// <summary>
		/// Log an Error
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="e">The exception.</param>
		/// <param name="errorCode">The internal error code.</param>
		/// <remarks>
		/// <para>
		/// Prints the message and the stack trace of the exception on the standard
		/// error output stream.
		/// </para>
		/// </remarks>
		public void Error(string message, Exception e, ErrorCode errorCode) 
		{ 
			if (IsEnabled) 
			{
				LogLog.Error("[" + m_prefix + "] " + message, e);
			}
		}

		/// <summary>
		/// Log an Error
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="e">The exception.</param>
		/// <remarks>
		/// <para>
		/// Prints the message and the stack trace of the exception on the standard
		/// error output stream.
		/// </para>
		/// </remarks>
		public void Error(string message, Exception e) 
		{ 
			if (IsEnabled) 
			{
				LogLog.Error("[" + m_prefix + "] " + message, e);
			}
		}

		/// <summary>
		/// Log an error
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <remarks>
		/// <para>
		/// Print a the error message passed as parameter on the standard
		/// error output stream.
		/// </para>
		/// </remarks>
		public void Error(string message) 
		{
			if (IsEnabled) 
			{
				LogLog.Error("[" + m_prefix + "] " + message);
			}
		}

		#endregion Implementation of IErrorHandler

		/// <summary>
		/// Is error logging enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Is error logging enabled. Logging is only enabled for the
		/// first error delivered to the <see cref="OnlyOnceErrorHandler"/>.
		/// </para>
		/// </remarks>
		private bool IsEnabled
		{
			get
			{
				// Allow first error message to be logged
				if (m_firstTime)
				{
					m_firstTime = false;
					return true;
				}

				// Check if InternalDebugging is enabled
				if (LogLog.InternalDebugging && !LogLog.QuietMode)
				{
					return true;
				}
				return false;
			}
		}

		#region Private Instance Fields

		/// <summary>
		/// Flag to indicate if it is the first error
		/// </summary>
		private bool m_firstTime = true;

		/// <summary>
		/// String to prefix each message with
		/// </summary>
		private readonly string m_prefix;

		#endregion Private Instance Fields
	}
}
