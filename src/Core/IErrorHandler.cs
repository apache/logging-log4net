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
	/// Appenders may delegate their error handling to an <see cref="IErrorHandler" />.
	/// </summary>
	/// <remarks>
	/// Error handling is a particularly tedious to get right because by
	/// definition errors are hard to predict and to reproduce. 
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public interface IErrorHandler
	{
		/// <summary>
		/// Handles the error and information about the error condition is passed as 
		/// a parameter.
		/// </summary>
		/// <param name="message">The message associated with the error.</param>
		/// <param name="e">The <see cref="Exception" /> that was thrown when the error occurred.</param>
		/// <param name="errorCode">The error code associated with the error.</param>
		void Error(string message, Exception e, ErrorCode errorCode);

		/// <summary>
		/// Prints the error message passed as a parameter.
		/// </summary>
		/// <param name="message">The message associated with the error.</param>
		/// <param name="e">The <see cref="Exception" /> that was thrown when the error occurred.</param>
		void Error(string message, Exception e);

		/// <summary>
		/// Prints the error message passed as a parameter.
		/// </summary>
		/// <param name="message">The message associated with the error.</param>
		void Error(string message);
	}
}
