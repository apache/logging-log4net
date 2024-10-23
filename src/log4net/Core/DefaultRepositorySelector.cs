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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using log4net.Config;
using log4net.Util;
using log4net.Repository;

namespace log4net.Core;

/// <summary>
/// The default implementation of the <see cref="IRepositorySelector"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// Uses attributes defined on the calling assembly to determine how to
/// configure the hierarchy for the repository.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
public class DefaultRepositorySelector : IRepositorySelector
{
  /// <summary>
  /// Event to notify that a logger repository has been created.
  /// </summary>
  /// <value>
  /// Event to notify that a logger repository has been created.
  /// </value>
  /// <remarks>
  /// <para>
  /// Event raised when a new repository is created.
  /// The event source will be this selector. The event args will
  /// be a <see cref="LoggerRepositoryCreationEventArgs"/> which
  /// holds the newly created <see cref="ILoggerRepository"/>.
  /// </para>
  /// </remarks>
  public event LoggerRepositoryCreationEventHandler? LoggerRepositoryCreatedEvent;

  /// <summary>
  /// Creates a new repository selector.
  /// </summary>
  /// <param name="defaultRepositoryType">The type of the repositories to create, must implement <see cref="ILoggerRepository"/></param>
  /// <remarks>
  /// <para>
  /// Create a new repository selector.
  /// The default type for repositories must be specified,
  /// an appropriate value would be <see cref="Repository.Hierarchy.Hierarchy"/>.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"><paramref name="defaultRepositoryType"/> is <see langword="null" />.</exception>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="defaultRepositoryType"/> does not implement <see cref="ILoggerRepository"/>.</exception>
  public DefaultRepositorySelector(Type defaultRepositoryType)
  {
    // Check that the type is a repository
    if (!typeof(ILoggerRepository).IsAssignableFrom(defaultRepositoryType.EnsureNotNull()))
    {
      throw SystemInfo.CreateArgumentOutOfRangeException("defaultRepositoryType", defaultRepositoryType, $"Parameter: defaultRepositoryType, Value: [{defaultRepositoryType}] out of range. Argument must implement the ILoggerRepository interface");
    }

    this._defaultRepositoryType = defaultRepositoryType;

    LogLog.Debug(_declaringType, $"defaultRepositoryType [{this._defaultRepositoryType}]");
  }

  /// <summary>
  /// Gets the <see cref="ILoggerRepository"/> for the specified assembly.
  /// </summary>
  /// <param name="assembly">The assembly use to look up the <see cref="ILoggerRepository"/>.</param>
  /// <remarks>
  /// <para>
  /// The type of the <see cref="ILoggerRepository"/> created and the repository 
  /// to create can be overridden by specifying the <see cref="RepositoryAttribute"/> 
  /// attribute on the <paramref name="assembly"/>.
  /// </para>
  /// <para>
  /// The default values are to use the <see cref="Repository.Hierarchy.Hierarchy"/> 
  /// implementation of the <see cref="ILoggerRepository"/> interface and to use the
  /// <see cref="AssemblyName.Name"/> as the name of the repository.
  /// </para>
  /// <para>
  /// The <see cref="ILoggerRepository"/> created will be automatically configured using 
  /// any <see cref="ConfiguratorAttribute"/> attributes defined on
  /// the <paramref name="assembly"/>.
  /// </para>
  /// </remarks>
  /// <returns>The <see cref="ILoggerRepository"/> for the assembly</returns>
  /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is <see langword="null" />.</exception>
  public ILoggerRepository GetRepository(Assembly assembly)
    => CreateRepository(assembly.EnsureNotNull(), _defaultRepositoryType);

  /// <summary>
  /// Gets the <see cref="ILoggerRepository"/> for the specified repository.
  /// </summary>
  /// <param name="repositoryName">The repository to use to look up the <see cref="ILoggerRepository"/>.</param>
  /// <returns>The <see cref="ILoggerRepository"/> for the specified repository.</returns>
  /// <remarks>
  /// <para>
  /// Returns the named repository. If <paramref name="repositoryName"/> is <c>null</c>
  /// a <see cref="ArgumentNullException"/> is thrown. If the repository 
  /// does not exist a <see cref="LogException"/> is thrown.
  /// </para>
  /// <para>
  /// Use <see cref="CreateRepository(string, Type)"/> to create a repository.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"><paramref name="repositoryName"/> is <see langword="null" />.</exception>
  /// <exception cref="LogException"><paramref name="repositoryName"/> does not exist.</exception>
  public ILoggerRepository GetRepository(string repositoryName)
  {
    lock (_syncRoot)
    {
      if (_name2Repository.TryGetValue(repositoryName.EnsureNotNull(), out ILoggerRepository? rep))
      {
        return rep;
      }
      throw new LogException($"Repository [{repositoryName}] is NOT defined.");
    }
  }

  /// <summary>
  /// Creates a new repository for the assembly specified 
  /// </summary>
  /// <param name="assembly">the assembly to use to create the repository to associate with the <see cref="ILoggerRepository"/>.</param>
  /// <param name="repositoryType">The type of repository to create, must implement <see cref="ILoggerRepository"/>.</param>
  /// <returns>The repository created.</returns>
  /// <remarks>
  /// <para>
  /// The <see cref="ILoggerRepository"/> created will be associated with the repository
  /// specified such that a call to <see cref="GetRepository(Assembly)"/> with the
  /// same assembly specified will return the same repository instance.
  /// </para>
  /// <para>
  /// The type of the <see cref="ILoggerRepository"/> created and
  /// the repository to create can be overridden by specifying the
  /// <see cref="RepositoryAttribute"/> attribute on the 
  /// <paramref name="assembly"/>.  The default values are to use the 
  /// <paramref name="repositoryType"/> implementation of the 
  /// <see cref="ILoggerRepository"/> interface and to use the
  /// <see cref="AssemblyName.Name"/> as the name of the repository.
  /// </para>
  /// <para>
  /// The <see cref="ILoggerRepository"/> created will be automatically
  /// configured using any <see cref="ConfiguratorAttribute"/> 
  /// attributes defined on the <paramref name="assembly"/>.
  /// </para>
  /// <para>
  /// If a repository for the <paramref name="assembly"/> already exists
  /// that repository will be returned. An error will not be raised and that 
  /// repository may be of a different type to that specified in <paramref name="repositoryType"/>.
  /// Also the <see cref="RepositoryAttribute"/> attribute on the
  /// assembly may be used to override the repository type specified in 
  /// <paramref name="repositoryType"/>.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is <see langword="null" />.</exception>
  public ILoggerRepository CreateRepository(Assembly assembly, Type repositoryType) 
    => CreateRepository(assembly, repositoryType, DefaultRepositoryName, true);

  /// <summary>
  /// Creates a new repository for the assembly specified.
  /// </summary>
  /// <param name="repositoryAssembly">the assembly to use to create the repository to associate with the <see cref="ILoggerRepository"/>.</param>
  /// <param name="repositoryType">The type of repository to create, must implement <see cref="ILoggerRepository"/>.</param>
  /// <param name="repositoryName">The name to assign to the created repository</param>
  /// <param name="readAssemblyAttributes">Set to <c>true</c> to read and apply the assembly attributes</param>
  /// <returns>The repository created.</returns>
  /// <remarks>
  /// <para>
  /// The <see cref="ILoggerRepository"/> created will be associated with the repository
  /// specified such that a call to <see cref="GetRepository(Assembly)"/> with the
  /// same assembly specified will return the same repository instance.
  /// </para>
  /// <para>
  /// The type of the <see cref="ILoggerRepository"/> created and
  /// the repository to create can be overridden by specifying the
  /// <see cref="RepositoryAttribute"/> attribute on the 
  /// <paramref name="repositoryAssembly"/>.  The default values are to use the 
  /// <paramref name="repositoryType"/> implementation of the 
  /// <see cref="ILoggerRepository"/> interface and to use the
  /// <see cref="AssemblyName.Name"/> as the name of the repository.
  /// </para>
  /// <para>
  /// The <see cref="ILoggerRepository"/> created will be automatically
  /// configured using any <see cref="ConfiguratorAttribute"/> 
  /// attributes defined on the <paramref name="repositoryAssembly"/>.
  /// </para>
  /// <para>
  /// If a repository for the <paramref name="repositoryAssembly"/> already exists
  /// that repository will be returned. An error will not be raised and that 
  /// repository may be of a different type to that specified in <paramref name="repositoryType"/>.
  /// Also the <see cref="RepositoryAttribute"/> attribute on the
  /// assembly may be used to override the repository type specified in 
  /// <paramref name="repositoryType"/>.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"><paramref name="repositoryAssembly"/> is <see langword="null" />.</exception>
  public ILoggerRepository CreateRepository(Assembly repositoryAssembly, Type? repositoryType, string repositoryName, bool readAssemblyAttributes)
  {
    repositoryAssembly.EnsureNotNull();

    repositoryType ??= _defaultRepositoryType;

    lock (_syncRoot)
    {
      if (!_assembly2Repository.TryGetValue(repositoryAssembly, out ILoggerRepository? rep))
      {
        // Not found, therefore create
        LogLog.Debug(_declaringType, $"Creating repository for assembly [{repositoryAssembly}]");

        // Must specify defaults
        string actualRepositoryName = repositoryName;
        Type actualRepositoryType = repositoryType;

        if (readAssemblyAttributes)
        {
          // Get the repository and type from the assembly attributes
          GetInfoForAssembly(repositoryAssembly, ref actualRepositoryName, ref actualRepositoryType);
        }

        LogLog.Debug(_declaringType, $"Assembly [{repositoryAssembly}] using repository [{actualRepositoryName}] and repository type [{actualRepositoryType}]");

        // Lookup the repository in the map (as this may already be defined)
        if (!_name2Repository.TryGetValue(actualRepositoryName, out rep))
        {
          // Create the repository
          rep = CreateRepository(actualRepositoryName, actualRepositoryType);

          if (readAssemblyAttributes)
          {
            try
            {
              // Look for aliasing attributes
              LoadAliases(repositoryAssembly, rep);

              // Look for plugins defined on the assembly
              LoadPlugins(repositoryAssembly, rep);

              // Configure the repository using the assembly attributes
              ConfigureRepository(repositoryAssembly, rep);
            }
            catch (Exception e) when (!e.IsFatal())
            {
              LogLog.Error(_declaringType, $"Failed to configure repository [{actualRepositoryName}] from assembly attributes.", e);
            }
          }
        }
        else
        {
          LogLog.Debug(_declaringType, $"repository [{actualRepositoryName}] already exists, using repository type [{rep.GetType().FullName}]");

          if (readAssemblyAttributes)
          {
            try
            {
              // Look for plugins defined on the assembly
              LoadPlugins(repositoryAssembly, rep);
            }
            catch (Exception e) when (!e.IsFatal())
            {
              LogLog.Error(_declaringType, $"Failed to configure repository [{actualRepositoryName}] from assembly attributes.", e);
            }
          }
        }
        _assembly2Repository[repositoryAssembly] = rep;
      }
      return rep;
    }
  }

  /// <summary>
  /// Creates a new repository for the specified repository.
  /// </summary>
  /// <param name="repositoryName">The repository to associate with the <see cref="ILoggerRepository"/>.</param>
  /// <param name="repositoryType">The type of repository to create, must implement <see cref="ILoggerRepository"/>.
  /// If this param is <see langword="null" /> then the default repository type is used.</param>
  /// <returns>The new repository.</returns>
  /// <remarks>
  /// <para>
  /// The <see cref="ILoggerRepository"/> created will be associated with the repository
  /// specified such that a call to <see cref="GetRepository(string)"/> with the
  /// same repository specified will return the same repository instance.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException"><paramref name="repositoryName"/> is <see langword="null" />.</exception>
  /// <exception cref="LogException"><paramref name="repositoryName"/> already exists.</exception>
  public ILoggerRepository CreateRepository(string repositoryName, Type? repositoryType)
  {
    repositoryName.EnsureNotNull();

    // If the type is not set then use the default type
    repositoryType ??= _defaultRepositoryType;

    lock (_syncRoot)
    {
      ILoggerRepository? rep = null;

      // First check that the repository does not exist
      if (_name2Repository.ContainsKey(repositoryName))
      {
        throw new LogException($"Repository [{repositoryName}] is already defined. Repositories cannot be redefined.");
      }

      // Look up an alias before trying to create the new repository
      if (_alias2Repository.TryGetValue(repositoryName, out ILoggerRepository? aliasedRepository))
      {
        // Found an alias

        // Check repository type
        if (aliasedRepository.GetType() == repositoryType)
        {
          // Repository type is compatible
          LogLog.Debug(_declaringType, $"Aliasing repository [{repositoryName}] to existing repository [{aliasedRepository.Name}]");
          rep = aliasedRepository;

          // Store in map
          _name2Repository[repositoryName] = rep;
        }
        else
        {
          // Invalid repository type for alias
          LogLog.Error(_declaringType, $"Failed to alias repository [{repositoryName}] to existing repository [{aliasedRepository.Name}]. Requested repository type [{repositoryType.FullName}] is not compatible with existing type [{aliasedRepository.GetType().FullName}]");

          // We now drop through to create the repository without aliasing
        }
      }

      // If we could not find an alias
      if (rep is null)
      {
        LogLog.Debug(_declaringType, $"Creating repository [{repositoryName}] using type [{repositoryType}]");

        // Call the no arg constructor for the repositoryType
        rep = Activator.CreateInstance(repositoryType).EnsureIs<ILoggerRepository>();

        // Set the name of the repository
        rep.Name = repositoryName;

        // Store in map
        _name2Repository[repositoryName] = rep;

        // Notify listeners that the repository has been created
        OnLoggerRepositoryCreatedEvent(rep);
      }

      return rep;
    }
  }

  /// <summary>
  /// Test if a named repository exists
  /// </summary>
  /// <param name="repositoryName">the named repository to check</param>
  /// <returns><c>true</c> if the repository exists</returns>
  /// <remarks>
  /// <para>
  /// Test if a named repository exists. Use <see cref="CreateRepository(string, Type)"/>
  /// to create a new repository and <see cref="GetRepository(string)"/> to retrieve 
  /// a repository.
  /// </para>
  /// </remarks>
  public bool ExistsRepository(string repositoryName)
  {
    lock (_syncRoot)
    {
      return _name2Repository.ContainsKey(repositoryName);
    }
  }

  /// <summary>
  /// Gets a list of <see cref="ILoggerRepository"/> objects
  /// </summary>
  /// <returns>an array of all known <see cref="ILoggerRepository"/> objects</returns>
  /// <remarks>
  /// <para>
  /// Gets an array of all repositories created by this selector.
  /// </para>
  /// </remarks>
  public ILoggerRepository[] GetAllRepositories()
  {
    lock (_syncRoot)
    {
      return _name2Repository.Values.ToArray();
    }
  }

  /// <summary>
  /// Aliases a repository to an existing repository.
  /// </summary>
  /// <param name="repositoryAlias">The repository to alias.</param>
  /// <param name="repositoryTarget">The repository that the repository is aliased to.</param>
  /// <remarks>
  /// <para>
  /// The repository specified will be aliased to the repository when created. 
  /// The repository must not already exist.
  /// </para>
  /// <para>
  /// When the repository is created it must utilize the same repository type as 
  /// the repository it is aliased to, otherwise the aliasing will fail.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException">
  ///  <para><paramref name="repositoryAlias" /> is <see langword="null" />.</para>
  ///  <para>-or-</para>
  ///  <para><paramref name="repositoryTarget" /> is <see langword="null" />.</para>
  /// </exception>
  public void AliasRepository(string repositoryAlias, ILoggerRepository repositoryTarget)
  {
    repositoryAlias.EnsureNotNull();
    repositoryTarget.EnsureNotNull();

    lock (_syncRoot)
    {
      // Check if the alias is already set
      if (_alias2Repository.TryGetValue(repositoryAlias, out ILoggerRepository? existingTarget))
      {
        // Check if this is a duplicate of the current alias
        if (repositoryTarget != existingTarget)
        {
          // Cannot redefine existing alias
          throw new InvalidOperationException($"Repository [{repositoryAlias}] is already aliased to repository [{existingTarget.Name}]. Aliases cannot be redefined.");
        }
      }
      // Check if the alias is already mapped to a repository
      else if (_name2Repository.TryGetValue(repositoryAlias, out existingTarget))
      {
        // Check if this is a duplicate of the current mapping
        if (repositoryTarget != existingTarget)
        {
          // Cannot define alias for already mapped repository
          throw new InvalidOperationException($"Repository [{repositoryAlias}] already exists and cannot be aliased to repository [{repositoryTarget.Name}].");
        }
      }
      else
      {
        // Set the alias
        _alias2Repository[repositoryAlias] = repositoryTarget;
      }
    }
  }

  /// <summary>
  /// Notifies the registered listeners that the repository has been created.
  /// </summary>
  /// <param name="repository">The repository that has been created.</param>
  /// <remarks>
  /// <para>
  /// Raises the <see cref="LoggerRepositoryCreatedEvent"/> event.
  /// </para>
  /// </remarks>
  protected virtual void OnLoggerRepositoryCreatedEvent(ILoggerRepository repository)
  {
    LoggerRepositoryCreatedEvent?.Invoke(this, new LoggerRepositoryCreationEventArgs(repository));
  }

  /// <summary>
  /// Gets the repository name and repository type for the specified assembly.
  /// </summary>
  /// <param name="assembly">The assembly that has a <see cref="RepositoryAttribute"/>.</param>
  /// <param name="repositoryName">in/out param to hold the repository name to use for the assembly, caller should set this to the default value before calling.</param>
  /// <param name="repositoryType">in/out param to hold the type of the repository to create for the assembly, caller should set this to the default value before calling.</param>
  /// <exception cref="ArgumentNullException"><paramref name="assembly" /> is <see langword="null" />.</exception>
  private void GetInfoForAssembly(Assembly assembly, ref string repositoryName, ref Type repositoryType)
  {
    assembly.EnsureNotNull();

    try
    {
      LogLog.Debug(_declaringType, $"Assembly [{assembly.FullName}] Loaded From [{SystemInfo.AssemblyLocationInfo(assembly)}]");
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // Ignore exception from debug call
    }

    try
    {
      // Look for the RepositoryAttribute on the assembly 
      object[] repositoryAttributes = Attribute.GetCustomAttributes(assembly, typeof(RepositoryAttribute), false);
      if (repositoryAttributes.Length == 0)
      {
        // This is not a problem, but it's nice to know what is going on.
        LogLog.Debug(_declaringType, $"Assembly [{assembly}] does not have a {nameof(RepositoryAttribute)} specified.");
      }
      else
      {
        if (repositoryAttributes.Length > 1)
        {
          LogLog.Error(_declaringType, $"Assembly [{assembly}] has multiple log4net.Config.RepositoryAttribute assembly attributes. Only using first occurrence.");
        }

        RepositoryAttribute domAttr = (RepositoryAttribute)repositoryAttributes[0];
        // If the Name property is set then override the default
        if (domAttr.Name is not null)
        {
          repositoryName = domAttr.Name;
        }

        // If the RepositoryType property is set then override the default
        if (domAttr.RepositoryType is not null)
        {
          // Check that the type is a repository
          if (typeof(ILoggerRepository).IsAssignableFrom(domAttr.RepositoryType))
          {
            repositoryType = domAttr.RepositoryType;
          }
          else
          {
            LogLog.Error(_declaringType, $"DefaultRepositorySelector: Repository Type [{domAttr.RepositoryType}] must implement the ILoggerRepository interface.");
          }
        }
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      LogLog.Error(_declaringType, "Unhandled exception in GetInfoForAssembly", e);
    }
  }

  /// <summary>
  /// Configures the repository using information from the assembly.
  /// </summary>
  /// <param name="assembly">The assembly containing <see cref="ConfiguratorAttribute"/>
  /// attributes which define the configuration for the repository.</param>
  /// <param name="repository">The repository to configure.</param>
  /// <exception cref="ArgumentNullException">
  ///  <para><paramref name="assembly" /> is <see langword="null" />.</para>
  ///  <para>-or-</para>
  ///  <para><paramref name="repository" /> is <see langword="null" />.</para>
  /// </exception>
  private static void ConfigureRepository(Assembly assembly, ILoggerRepository repository)
  {
    repository.EnsureNotNull();

    // Look for the Configurator attributes (e.g. XmlConfiguratorAttribute) on the assembly
    ConfiguratorAttribute[] configAttributes = Attribute.GetCustomAttributes(assembly.EnsureNotNull(), typeof(ConfiguratorAttribute), false)
      .EnsureIs<ConfiguratorAttribute[]>();
    if (configAttributes.Length > 0)
    {
      // Sort the ConfiguratorAttributes in priority order
      Array.Sort(configAttributes);

      // Delegate to the attribute the job of configuring the repository
      foreach (ConfiguratorAttribute configAttr in configAttributes)
      {
        try
        {
          configAttr.Configure(assembly, repository);
        }
        catch (Exception e) when (!e.IsFatal())
        {
          LogLog.Error(_declaringType, $"Exception calling [{configAttr.GetType().FullName}] .Configure method.", e);
        }
      }
    }

    if (repository.Name == DefaultRepositoryName)
    {
      // Try to configure the default repository using an AppSettings specified config file
      // Do this even if the repository has been configured (or claims to be), this allows overriding
      // of the default config files etc., if that is required.

      string? repositoryConfigFile = SystemInfo.GetAppSetting("log4net.Config");
      if (repositoryConfigFile is not null && repositoryConfigFile.Length > 0)
      {
        string? applicationBaseDirectory = null;
        try
        {
          applicationBaseDirectory = SystemInfo.ApplicationBaseDirectory;
        }
        catch (Exception e) when (!e.IsFatal())
        {
          LogLog.Warn(_declaringType, $"Exception getting ApplicationBaseDirectory. appSettings log4net.Config path [{repositoryConfigFile}] will be treated as an absolute URI", e);
        }

        string repositoryConfigFilePath = repositoryConfigFile;
        if (applicationBaseDirectory is not null)
        {
          repositoryConfigFilePath = Path.Combine(applicationBaseDirectory, repositoryConfigFile);
        }

        // Determine whether to watch the file or not based on an app setting value:
        bool.TryParse(SystemInfo.GetAppSetting("log4net.Config.Watch"), out bool watchRepositoryConfigFile);

        if (watchRepositoryConfigFile)
        {
          // As we are going to watch the config file it is required to resolve it as a 
          // physical file system path pass that in a FileInfo object to the Configurator
          FileInfo? repositoryConfigFileInfo = null;
          try
          {
            repositoryConfigFileInfo = new FileInfo(repositoryConfigFilePath);
          }
          catch (Exception e) when (!e.IsFatal())
          {
            LogLog.Error(_declaringType, $"DefaultRepositorySelector: Exception while parsing log4net.Config file physical path [{repositoryConfigFilePath}]", e);
          }

          if (repositoryConfigFileInfo is not null)
          {
            try
            {
              LogLog.Debug(_declaringType, $"Loading and watching configuration for default repository from AppSettings specified Config path [{repositoryConfigFilePath}]");

              XmlConfigurator.ConfigureAndWatch(repository, repositoryConfigFileInfo);
            }
            catch (Exception e) when (!e.IsFatal())
            {
              LogLog.Error(_declaringType, $"DefaultRepositorySelector: Exception calling XmlConfigurator.ConfigureAndWatch method with ConfigFilePath [{repositoryConfigFilePath}]", e);
            }
          }
        }
        else
        {
          // As we are not going to watch the config file it is easiest to just resolve it as a 
          // URI and pass that to the Configurator
          Uri? repositoryConfigUri = null;
          try
          {
            repositoryConfigUri = new Uri(repositoryConfigFilePath);
          }
          catch (Exception e) when (!e.IsFatal())
          {
            LogLog.Error(_declaringType, $"Exception while parsing log4net.Config file path [{repositoryConfigFile}]", e);
          }

          if (repositoryConfigUri is not null)
          {
            LogLog.Debug(_declaringType, $"Loading configuration for default repository from AppSettings specified Config URI [{repositoryConfigUri}]");

            try
            {
              // TODO: Support other types of configurator
              XmlConfigurator.Configure(repository, repositoryConfigUri);
            }
            catch (Exception e) when (!e.IsFatal())
            {
              LogLog.Error(_declaringType, $"Exception calling XmlConfigurator.Configure method with ConfigUri [{repositoryConfigUri}]", e);
            }
          }
        }
      }
    }
  }

  /// <summary>
  /// Loads the attribute defined plugins on the assembly.
  /// </summary>
  /// <param name="assembly">The assembly that contains the attributes.</param>
  /// <param name="repository">The repository to add the plugins to.</param>
  /// <exception cref="ArgumentNullException">
  ///  <para><paramref name="assembly" /> is <see langword="null" />.</para>
  ///  <para>-or-</para>
  ///  <para><paramref name="repository" /> is <see langword="null" />.</para>
  /// </exception>
  private static void LoadPlugins(Assembly assembly, ILoggerRepository repository)
  {
    // Look for the PluginAttribute on the assembly
    PluginAttribute[] configAttributes = Attribute.GetCustomAttributes(assembly, typeof(PluginAttribute), false)
      .EnsureIs<PluginAttribute[]>();
    foreach (PluginAttribute configAttr in configAttributes)
    {
      try
      {
        // Create the plugin and add it to the repository
        repository.PluginMap.Add(configAttr.CreatePlugin());
      }
      catch (Exception e) when (!e.IsFatal())
      {
        LogLog.Error(_declaringType, $"Failed to create plugin. Attribute [{configAttr}]", e);
      }
    }
  }

  /// <summary>
  /// Loads the attribute defined aliases on the assembly.
  /// </summary>
  /// <param name="assembly">The assembly that contains the attributes.</param>
  /// <param name="repository">The repository to alias to.</param>
  /// <exception cref="ArgumentNullException">
  ///  <para><paramref name="assembly" /> is <see langword="null" />.</para>
  ///  <para>-or-</para>
  ///  <para><paramref name="repository" /> is <see langword="null" />.</para>
  /// </exception>
  private void LoadAliases(Assembly assembly, ILoggerRepository repository)
  {
    // Look for the AliasRepositoryAttribute on the assembly
    AliasRepositoryAttribute[] configAttributes = Attribute
      .GetCustomAttributes(assembly, typeof(AliasRepositoryAttribute), false)
      .EnsureIs<AliasRepositoryAttribute[]>();
    foreach (AliasRepositoryAttribute configAttr in configAttributes)
    {
      try
      {
        AliasRepository(configAttr.Name, repository);
      }
      catch (Exception e) when (!e.IsFatal())
      {
        LogLog.Error(_declaringType, $"Failed to alias repository [{configAttr.Name}]", e);
      }
    }
  }

  /// <summary>
  /// The fully qualified type of the DefaultRepositorySelector class.
  /// </summary>
  /// <remarks>
  /// Used by the internal logger to record the Type of the
  /// log message.
  /// </remarks>
  private static readonly Type _declaringType = typeof(DefaultRepositorySelector);

  private const string DefaultRepositoryName = "log4net-default-repository";

  private readonly object _syncRoot = new();
  private readonly Dictionary<string, ILoggerRepository> _name2Repository = new(StringComparer.Ordinal);
  private readonly Dictionary<Assembly, ILoggerRepository> _assembly2Repository = [];
  private readonly Dictionary<string, ILoggerRepository> _alias2Repository = new(StringComparer.Ordinal);
  private readonly Type _defaultRepositoryType;
}