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

namespace log4net.Core
{
	/// <summary>
	/// Defined error codes that can be passed to the <see cref="IErrorHandler.Error"/> method.
	/// </summary>
	/// <author>Nicko Cadell</author>
	public enum ErrorCode : int
	{
		/// <summary>
		/// A general error
		/// </summary>
		GenericFailure = 0,

		/// <summary>
		/// Error while writing output
		/// </summary>
		WriteFailure,

		/// <summary>
		/// Failed to flush file
		/// </summary>
		FlushFailure,

		/// <summary>
		/// Failed to close file
		/// </summary>
		CloseFailure,

		/// <summary>
		/// Unable to open output file
		/// </summary>
		FileOpenFailure,

		/// <summary>
		/// No layout specified
		/// </summary>
		MissingLayout,

		/// <summary>
		/// Failed to parse address
		/// </summary>
		AddressParseFailure
	}
}
