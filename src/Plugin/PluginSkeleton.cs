#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
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
