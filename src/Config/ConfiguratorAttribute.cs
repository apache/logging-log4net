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
using System.Reflection;

using log4net.Repository;

namespace log4net.Config
{
	/// <summary>
	/// Base class for all log4net configuration attributes.
	/// </summary>
	/// <remarks>
	/// This is an abstract class that must be extended by 
	/// specific configurators. This attribute allows the
	/// configurator to be parameterised by an assembly level
	/// attribute.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	[AttributeUsage(AttributeTargets.Assembly)]
	public abstract class ConfiguratorAttribute : Attribute
	{
		/// <summary>
		/// Configures the <see cref="ILoggerRepository"/> for the specified assembly.
		/// </summary>
		/// <param name="assembly">The assembly that this attribute was defined on.</param>
		/// <param name="repository">The repository to configure.</param>
		public abstract void Configure(Assembly assembly, ILoggerRepository repository);
	}
}
