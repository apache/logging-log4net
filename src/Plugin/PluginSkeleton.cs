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
	/// Class from which logger repository plugins derive.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public abstract class PluginSkeleton : IPlugin
	{
		#region Protected Instance Constructors

		/// <summary>
		/// Initializes a new Plugin with the specified name.
		/// </summary>
		protected PluginSkeleton(string name)
		{
			m_name = name;
		}

		#endregion Protected Instance Constructors

		#region Implementation of IPlugin

		/// <summary>
		/// Gets or sets the name of this plugin.
		/// </summary>
		/// <value>
		/// The name of this plugin.
		/// </value>
		public virtual string Name 
		{ 
			get { return m_name; }
			set { m_name = value; }
		}

		/// <summary>
		/// Attaches this plugin to a <see cref="ILoggerRepository"/>.
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
		public virtual void Attach(ILoggerRepository repository)
		{
			m_repository = repository;
		}

		/// <summary>
		/// Is called when the plugin is to shutdown.
		/// </summary>
		public virtual void Shutdown()
		{
		}

		#endregion Implementation of IPlugin

		#region Protected Instance Properties

		/// <summary>
		/// Gets or sets the <see cref="ILoggerRepository" /> that this plugin is 
		/// attached to.
		/// </summary>
		/// <value>
		/// The <see cref="ILoggerRepository" /> that this plugin is attached to.
		/// </value>
		protected virtual ILoggerRepository LoggerRepository 
		{
			get { return this.m_repository;	}
			set { this.m_repository = value; }
		}

		#endregion Protected Instance Properties

		#region Private Instance Fields

		/// <summary>
		/// The name of this plugin.
		/// </summary>
		private string m_name;

		/// <summary>
		/// The repository this plugin is attached to.
		/// </summary>
		private ILoggerRepository m_repository;

		#endregion Private Instance Fields
	}
}
