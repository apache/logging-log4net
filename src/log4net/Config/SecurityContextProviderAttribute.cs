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
using System.Reflection;

using log4net.Util;
using log4net.Repository;
using log4net.Core;

namespace log4net.Config;

/// <summary>
/// Assembly level attribute to configure the <see cref="SecurityContextProvider"/>.
/// </summary>
/// <param name="providerType">the type of the provider to use</param>
/// <remarks>
/// <para>
/// This attribute may only be used at the assembly scope and can only
/// be used once per assembly.
/// </para>
/// <para>
/// Use this attribute to configure the <see cref="XmlConfigurator"/>
/// without calling one of the <see cref="XmlConfigurator.Configure()"/>
/// methods.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
[AttributeUsage(AttributeTargets.Assembly)]
[Log4NetSerializable]
public sealed class SecurityContextProviderAttribute(Type providerType) 
  : ConfiguratorAttribute(100)
{
  /// <summary>
  /// Gets or sets the type of the provider to use.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The provider specified must subclass the <see cref="SecurityContextProvider"/>
  /// class.
  /// </para>
  /// </remarks>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
  public Type ProviderType { get; set; } = providerType;

  /// <summary>
  /// Configures the SecurityContextProvider
  /// </summary>
  /// <param name="sourceAssembly">The assembly that this attribute was defined on.</param>
  /// <param name="targetRepository">The repository to configure.</param>
  /// <remarks>
  /// <para>
  /// Creates a provider instance from the <see cref="ProviderType"/> specified.
  /// Sets this as the default security context provider <see cref="SecurityContextProvider.DefaultProvider"/>.
  /// </para>
  /// </remarks>
  public override void Configure(Assembly sourceAssembly, ILoggerRepository targetRepository)
  {
    if (ProviderType is null)
    {
      LogLog.Error(_declaringType, $"Attribute specified on assembly [{sourceAssembly.FullName}] with null ProviderType.");
    }
    else
    {
      LogLog.Debug(_declaringType, $"Creating provider of type [{ProviderType.FullName}]");

      if (Activator.CreateInstance(ProviderType) is not SecurityContextProvider provider)
      {
        LogLog.Error(_declaringType, $"Failed to create SecurityContextProvider instance of type [{ProviderType.Name}].");
      }
      else
      {
        SecurityContextProvider.DefaultProvider = provider;
      }
    }
  }

  /// <summary>
  /// The fully qualified type of the SecurityContextProviderAttribute class.
  /// </summary>
  /// <remarks>
  /// Used by the internal logger to record the Type of the log message.
  /// </remarks>
  private static readonly Type _declaringType = typeof(SecurityContextProviderAttribute);
}