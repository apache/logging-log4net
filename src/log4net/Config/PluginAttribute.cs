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

using log4net.Core;
using log4net.Util;
using log4net.Plugin;

namespace log4net.Config;

/// <summary>
/// Assembly level attribute that specifies a plugin to attach to 
/// the repository.
/// </summary>
/// <remarks>
/// <para>
/// Specifies the type of a plugin to create and attach to the
/// assembly's repository. The plugin type must implement the
/// <see cref="IPlugin"/> interface.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[Log4NetSerializable]
public sealed class PluginAttribute : Attribute, IPluginFactory
{
  /// <summary>
  /// Initializes a new instance of the <see cref="PluginAttribute" /> class
  /// with the specified type.
  /// </summary>
  /// <param name="typeName">The type name of plugin to create.</param>
  /// <remarks>
  /// <para>
  /// Create the attribute with the plugin type specified.
  /// </para>
  /// <para>
  /// Where possible use the constructor that takes a <see cref="System.Type"/>.
  /// </para>
  /// </remarks>
  public PluginAttribute(string typeName) => TypeName = typeName;

  /// <summary>
  /// Initializes a new instance of the <see cref="PluginAttribute" /> class
  /// with the specified type.
  /// </summary>
  /// <param name="type">The type of plugin to create.</param>
  /// <remarks>
  /// <para>
  /// Create the attribute with the plugin type specified.
  /// </para>
  /// </remarks>
  public PluginAttribute(Type type) => Type = type;

  /// <summary>
  /// Gets or sets the type for the plugin.
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
  public Type? Type { get; set; }

  /// <summary>
  /// Gets or sets the type name for the plugin.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Where possible use the <see cref="Type"/> property instead.
  /// </para>
  /// </remarks>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
  public string? TypeName { get; set; }

  /// <summary>
  /// Creates the plugin object defined by this attribute.
  /// </summary>
  /// <returns>The plugin object.</returns>
  public IPlugin CreatePlugin()
  {
    // Get the plugin object type from the type first before trying the string type name.
    Type? pluginType = Type ?? SystemInfo.GetTypeFromString(TypeName!, true, true);

    // Check that the type is a plugin
    if (!typeof(IPlugin).IsAssignableFrom(pluginType))
    {
      throw new LogException($"Plugin type [{pluginType?.FullName}] does not implement the log4net.IPlugin interface");
    }

    // Create an instance of the plugin using the default constructor
    return Activator.CreateInstance(pluginType).EnsureIs<IPlugin>();
  }

  /// <inheritdoc/>
  public override string ToString() => $"PluginAttribute[Type={Type?.FullName ?? TypeName}]";
}