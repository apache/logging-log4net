#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
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

using System;
using System.Collections.Concurrent;
using log4net.Repository;

namespace log4net.Plugin;

/// <summary>
/// Map of repository plugins.
/// </summary>
/// <remarks>
/// <para>
/// This class is a name keyed map of the plugins that are
/// attached to a repository.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
public sealed class PluginMap
{
  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="repository">The repository that the plugins should be attached to.</param>
  /// <remarks>
  /// <para>
  /// Initialize a new instance of the <see cref="PluginMap" /> class with a 
  /// repository that the plugins should be attached to.
  /// </para>
  /// </remarks>
  public PluginMap(ILoggerRepository repository)
  {
    this._repository = repository;
  }

  /// <summary>
  /// Gets a <see cref="IPlugin" /> by name.
  /// </summary>
  /// <param name="name">The name of the <see cref="IPlugin" /> to lookup.</param>
  /// <returns>
  /// The <see cref="IPlugin" /> from the map with the name specified, or 
  /// <c>null</c> if no plugin is found.
  /// </returns>
  public IPlugin? this[string name]
  {
    get
    {
      if (name is null)
      {
        throw new ArgumentNullException(nameof(name));
      }

      _mapName2Plugin.TryGetValue(name, out IPlugin? plugin);
      return plugin;
    }
  }

  /// <summary>
  /// Gets all possible plugins as a list of <see cref="IPlugin" /> objects.
  /// </summary>
  /// <value>All possible plugins as a list of <see cref="IPlugin" /> objects.</value>
  public PluginCollection AllPlugins => new PluginCollection(_mapName2Plugin.Values);

  /// <summary>
  /// Adds a <see cref="IPlugin" /> to the map.
  /// </summary>
  /// <param name="plugin">The <see cref="IPlugin" /> to add to the map.</param>
  /// <remarks>
  /// <para>
  /// The <see cref="IPlugin" /> will be attached to the repository when added.
  /// </para>
  /// <para>
  /// If there already exists a plugin with the same name 
  /// attached to the repository then the old plugin will
  /// be <see cref="IPlugin.Shutdown"/> and replaced with
  /// the new plugin.
  /// </para>
  /// </remarks>
  public void Add(IPlugin plugin)
  {
    if (plugin is null)
    {
      throw new ArgumentNullException(nameof(plugin));
    }

    IPlugin? curPlugin = null;
    _mapName2Plugin.AddOrUpdate(plugin.Name, plugin, (_, existingPlugin) =>
    {
      curPlugin = existingPlugin;
      return plugin;
    });

    // Shutdown existing plugin with same name
    curPlugin?.Shutdown();

    // Attach new plugin to repository
    plugin.Attach(_repository);
  }

  /// <summary>
  /// Removes an <see cref="IPlugin" /> from the map.
  /// </summary>
  /// <param name="plugin">The <see cref="IPlugin" /> to remove from the map.</param>
  public void Remove(IPlugin plugin)
  {
    if (plugin is null)
    {
      throw new ArgumentNullException(nameof(plugin));
    }
    _mapName2Plugin.TryRemove(plugin.Name, out _);
  }

  private readonly ConcurrentDictionary<string, IPlugin> _mapName2Plugin = new(StringComparer.Ordinal);
  private readonly ILoggerRepository _repository;
}
