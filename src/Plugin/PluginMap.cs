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
using System.Collections;

using log4net.Util;
using log4net.Repository;

namespace log4net.Plugin
{
	/// <summary>
	/// Map of repository plugins.
	/// </summary>
	/// <remarks>
	/// This class is a name keyed map of the plugins that are
	/// attached to a repository.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class PluginMap
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initiazlies a new instance of the <see cref="PluginMap" /> class with a 
		/// repository that the plugins should be attached to.
		/// </summary>
		/// <param name="reporitory">The repository that the plugins should be attached to.</param>
		public PluginMap(ILoggerRepository reporitory)
		{
			m_reporitory = reporitory;
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets a <see cref="IPlugin" /> by name.
		/// </summary>
		/// <param name="name">The name of the <see cref="IPlugin" /> to lookup.</param>
		/// <returns>
		/// The <see cref="IPlugin" /> from the map with the name specified, or 
		/// <c>null</c> if no plugin is found.
		/// </returns>
		virtual public IPlugin this[string name]
		{
			get
			{
				if (name == null)
				{
					throw new ArgumentNullException("name");
				}

				lock(this)
				{
					return (IPlugin)m_mapName2Plugin[name];
				}
			}
		}

		/// <summary>
		/// Gets all possible plugins as a list of <see cref="IPlugin" /> objects.
		/// </summary>
		/// <value>All possible plugins as a list of <see cref="IPlugin" /> objects.</value>
		virtual public PluginCollection AllPlugins
		{
			get
			{
				lock(this)
				{
					return new PluginCollection(m_mapName2Plugin.Values);
				}
			}
		}
		
		#endregion Public Instance Properties

		#region Public Instance Methods

		/// <summary>
		/// Adds a <see cref="IPlugin" /> to the map.
		/// </summary>
		/// <param name="plugin">The <see cref="IPlugin" /> to add to the map.</param>
		/// <remarks>
		/// The <see cref="IPlugin" /> will be attached to the repository when added.
		/// </remarks>
		virtual public void Add(IPlugin plugin)
		{
			if (plugin == null)
			{
				throw new ArgumentNullException("plugin");
			}

			IPlugin curPlugin = null;

			lock(this)
			{
				// Get the current plugin if it exists
				curPlugin = m_mapName2Plugin[plugin.Name] as IPlugin;

				// Store new plugin
				m_mapName2Plugin[plugin.Name] = plugin;
			}

			// Shutdown existing plugin with same name
			if (curPlugin != null)
			{
				curPlugin.Shutdown();
			}

			// Attach new plugin to repository
			plugin.Attach(m_reporitory);
		}

		/// <summary>
		/// Removes a <see cref="IPlugin" /> from the map.
		/// </summary>
		/// <param name="plugin">The <see cref="IPlugin" /> to remove from the map.</param>
		virtual public void Remove(IPlugin plugin)
		{
			if (plugin == null)
			{
				throw new ArgumentNullException("plugin");
			}
			lock(this)
			{
				m_mapName2Plugin.Remove(plugin.Name);
			}
		}

		#endregion Public Instance Methods

		#region Private Instance Fields

		private Hashtable m_mapName2Plugin = new Hashtable();
		private ILoggerRepository m_reporitory;

		#endregion Private Instance Fields
	}
}
