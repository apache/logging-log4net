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

using log4net.Repository;

namespace log4net.Plugin
{
	/// <summary>
	/// Interface implemented by logger repository plugins.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public interface IPlugin
	{
		/// <summary>
		/// Gets or sets the name of the plugin.
		/// </summary>
		/// <value>
		/// The name of the plugin.
		/// </value>
		string Name { get; set; }

		/// <summary>
		/// Attaches the plugin to the specified <see cref="ILoggerRepository"/>.
		/// </summary>
		/// <param name="repository">The <see cref="ILoggerRepository"/> that this plugin should be attached to.</param>
		/// <remarks>
		/// <para>
		/// A plugin may only be attached to a single repository.
		/// </para>
		/// <para>
		/// This method is called when the plugin is attached to the repository.
		/// </para>
		/// </remarks>
		void Attach(ILoggerRepository repository);

		/// <summary>
		/// Is called when the plugin is to shutdown.
		/// </summary>
		void Shutdown();
	}
}
