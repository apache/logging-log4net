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

namespace log4net.Repository.Hierarchy
{
	/// <summary>
	/// Implement this interface to create new instances of <see cref="Logger" /> 
	/// or a sub-class of <see cref="Logger" />.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This interface is used by the <see cref="Hierarchy"/> to 
	/// create new <see cref="Logger"/> objects.
	/// </para>
	/// <para>
	/// The <see cref="MakeNewLoggerInstance"/> method is called
	/// to create a named <see cref="Logger" />.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public interface ILoggerFactory
	{
		/// <summary>
		/// Constructs a new <see cref="Logger" /> instance with the 
		/// specified name.
		/// </summary>
		/// <param name="name">The name of the <see cref="Logger" />.</param>
		/// <returns>The <see cref="Logger" /> instance for the specified name.</returns>
		/// <remarks>
		/// <para>
		/// Called by the <see cref="Hierarchy"/> to create
		/// new named <see cref="Logger"/> instances.
		/// </para>
		/// </remarks>
		Logger MakeNewLoggerInstance(string name);
	}
}
