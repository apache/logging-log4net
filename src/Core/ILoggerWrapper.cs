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

using log4net;
using log4net.Core;
using log4net.Repository;

namespace log4net.Core
{
	/// <summary>
	/// Base interface for all wrappers
	/// </summary>
	/// <remarks>
	/// <para>Base interface for all wrappers</para>
	/// <para>All wrappers must extend this interface</para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public interface ILoggerWrapper
	{
		/// <summary>
		/// Get the implementation behind this wrapper object.
		/// </summary>
		/// <value>
		/// The <see cref="ILogger"/> object that in implementing this object.
		/// </value>
		/// <remarks>
		/// The <see cref="ILogger"/> object that in implementing this
		/// object. The <c>Logger</c> object may not 
		/// be the same object as this object because of logger decorators.
		/// This gets the actual underlying objects that is used to process
		/// the log events.
		/// </remarks>
		ILogger Logger { get; }
	}
}
