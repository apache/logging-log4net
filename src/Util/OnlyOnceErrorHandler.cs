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

namespace log4net.Util
{
	/// <summary>
	/// Implements log4net's default error handling policy which consists 
	/// of emitting a message for the first error in an appender and 
	/// ignoring all suqsequent errors.
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
		/// Initializes a new instance of the <see cref="OnlyOnceErrorHandler" /> class.
		/// </summary>
		public OnlyOnceErrorHandler()
		{
			m_prefix = "";
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OnlyOnceErrorHandler" /> class
		/// with the specified prefix.
		/// </summary>
		/// <param name="prefix">The prefix to use for each message.</param>
		public OnlyOnceErrorHandler(string prefix)
		{
			m_prefix = prefix;
		}

		#endregion Public Instance Constructors

		#region Implementation of IErrorHandler

		/// <summary>
		/// Prints the message and the stack trace of the exception on the standard
		/// error output stream.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="e">The exception.</param>
		/// <param name="errorCode">The internal error code.</param>
		public void Error(string message, Exception e, ErrorCode errorCode) 
		{ 
			if (m_firstTime) 
			{
				m_firstTime = false;
				LogLog.Error("[" + m_prefix + "] " + message, e);
			}
		}

		/// <summary>
		/// Prints the message and the stack trace of the exception on the standard
		/// error output stream.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="e">The exception.</param>
		public void Error(string message, Exception e) 
		{ 
			if (m_firstTime) 
			{
				m_firstTime = false;
				LogLog.Error("[" + m_prefix + "] " + message, e);
			}
		}

		/// <summary>
		/// Print a the error message passed as parameter on the standard
		/// error output stream.
		/// </summary>
		/// <param name="message">The error message.</param>
		public void Error(string message) 
		{
			if (m_firstTime) 
			{
				m_firstTime = false;
				LogLog.Error("[" + m_prefix + "] " + message);
			}
		}

		#endregion Implementation of IErrorHandler

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
