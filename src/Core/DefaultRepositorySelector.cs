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

// .NET Compact Framework 1.0 has no support for reading assembly attributes
// and uses the CompactRepositorySelector instead
#if !NETCF

using System;
using System.Collections;
using System.Globalization;
using System.Reflection;

using log4net.Appender;
using log4net.Util;
using log4net.Repository;

namespace log4net.Core
{
	/// <summary>
	/// The default implementation of the <see cref="IRepositorySelector"/> interface.
	/// </summary>
	/// <remarks>
	/// Uses attributes defined on the calling assembly to determine how to
	/// configure the hierarchy for the repository.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class DefaultRepositorySelector : IRepositorySelector
	{
		#region Public Events

		/// <summary>
		/// Event to notify that a logger repository has been created.
		/// </summary>
		/// <value>
		/// Event to notify that a logger repository has been created.
		/// </value>
		public event LoggerRepositoryCreationEventHandler LoggerRepositoryCreatedEvent 
		{
			add { m_loggerRepositoryCreatedEvent += value; }
			remove { m_loggerRepositoryCreatedEvent -= value; }
		}

		#endregion Public Events

		#region Public Instance Constructors

		/// <summary>
		/// Creates a new repository selector.
		/// </summary>
		/// <param name="defaultRepositoryType">The type of the repositories to create, must implement <see cref="ILoggerRepository"/></param>
		/// <exception cref="ArgumentNullException"><paramref name="defaultRepositoryType"/> is <see langword="null" />.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="defaultRepositoryType"/> does not implement <see cref="ILoggerRepository"/>.</exception>
		public DefaultRepositorySelector(Type defaultRepositoryType)
		{
			if (defaultRepositoryType == null)
			{
				throw new ArgumentNullException("defaultRepositoryType");
			}

			// Check that the type is a repository
			if (! (typeof(ILoggerRepository).IsAssignableFrom(defaultRepositoryType)) )
			{
				throw new ArgumentOutOfRangeException("defaultRepositoryType", defaultRepositoryType, "Parameter: defaultRepositoryType, Value: [" + defaultRepositoryType + "] out of range. Argument must implement the ILoggerRepository interface");
			}

			m_defaultRepositoryType = defaultRepositoryType;

			LogLog.Debug("DefaultRepositorySelector: defaultRepositoryType [" + m_defaultRepositoryType + "]");
		}

		#endregion Public Instance Constructors

		#region Implementation of IRepositorySelector

		/// <summary>
		/// Gets the <see cref="ILoggerRepository"/> for the specified assembly.
		/// </summary>
		/// <param name="repositoryAssembly">The assembly use to lookup the <see cref="ILoggerRepository"/>.</param>
		/// <remarks>
		/// <para>
		/// The type of the <see cref="ILoggerRepository"/> created and the repository 
		/// to create can be overridden by specifying the <see cref="log4net.Config.RepositoryAttribute"/> 
		/// attribute on the <paramref name="assembly"/>.
		/// </para>
		/// <para>
		/// The default values are to use the <see cref="log4net.Repository.Hierarchy.Hierarchy"/> 
		/// implementation of the <see cref="ILoggerRepository"/> interface and to use the
		/// <see cref="AssemblyName.Name"/> as the name of the repository.
		/// </para>
		/// <para>
		/// The <see cref="ILoggerRepository"/> created will be automatically configured using 
		/// any <see cref="log4net.Config.ConfiguratorAttribute"/> attributes defined on
		/// the <paramref name="assembly"/>.
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentNullException"><paramref name="assembly"/> is <see langword="null" />.</exception>
		/// <returns>The <see cref="ILoggerRepository"/> for the assembly</returns>
		public ILoggerRepository GetRepository(Assembly repositoryAssembly)
		{
			if (repositoryAssembly == null)
			{
				throw new ArgumentNullException("repositoryAssembly");
			}
			return CreateRepository(repositoryAssembly, m_defaultRepositoryType);
		}

		/// <summary>
		/// Gets the <see cref="ILoggerRepository"/> for the specified repository.
		/// </summary>
		/// <param name="repository">The repository to use to lookup the <see cref="ILoggerRepository"/>.</param>
		/// <returns>The <see cref="ILoggerRepository"/> for the specified repository.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null" />.</exception>
		/// <exception cref="LogException"><paramref name="repository"/> does not exist.</exception>
		public ILoggerRepository GetRepository(string repository)
		{
			if (repository == null)
			{
				throw new ArgumentNullException("repository");
			}

			lock(this)
			{
				// Lookup in map
				ILoggerRepository rep = m_name2repositoryMap[repository] as ILoggerRepository;
				if (rep == null)
				{
					throw new LogException("Repository [" + repository + "] is NOT defined.");
				}
				return rep;
			}
		}

		/// <summary>
		/// Create a new repository for the assembly specified 
		/// </summary>
		/// <param name="repositoryAssembly">the assembly to use to create the repository to associate with the <see cref="ILoggerRepository"/>.</param>
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
		/// <see cref="log4net.Config.RepositoryAttribute"/> attribute on the 
		/// <paramref name="assembly"/>.  The default values are to use the 
		/// <paramref name="repositoryType"/> implementation of the 
		/// <see cref="ILoggerRepository"/> interface and to use the
		/// <see cref="AssemblyName.Name"/> as the name of the repository.
		/// </para>
		/// <para>
		/// The <see cref="ILoggerRepository"/> created will be automatically
		/// configured using any <see cref="log4net.Config.ConfiguratorAttribute"/> 
		/// attributes defined on the <paramref name="repositoryAssembly"/>.
		/// </para>
		/// <para>
		/// If a repository for the <paramref name="repositoryAssembly"/> already exists
		/// that repository will be returned. An error will not be raised and that 
		/// repository may be of a different type to that specified in <paramref name="repositoryType"/>.
		/// Also the <see cref="log4net.Config.RepositoryAttribute"/> attribute on the
		/// assembly may be used to override the repository type specified in 
		/// <paramref name="repositoryType"/>.
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentNullException"><paramref name="repositoryAssembly"/> is <see langword="null" />.</exception>
		public ILoggerRepository CreateRepository(Assembly repositoryAssembly, Type repositoryType)
		{
			return CreateRepository(repositoryAssembly, repositoryType, DefaultRepositoryName, true);
		}

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
		/// <see cref="log4net.Config.RepositoryAttribute"/> attribute on the 
		/// <paramref name="assembly"/>.  The default values are to use the 
		/// <paramref name="repositoryType"/> implementation of the 
		/// <see cref="ILoggerRepository"/> interface and to use the
		/// <see cref="AssemblyName.Name"/> as the name of the repository.
		/// </para>
		/// <para>
		/// The <see cref="ILoggerRepository"/> created will be automatically
		/// configured using any <see cref="log4net.Config.ConfiguratorAttribute"/> 
		/// attributes defined on the <paramref name="repositoryAssembly"/>.
		/// </para>
		/// <para>
		/// If a repository for the <paramref name="repositoryAssembly"/> already exists
		/// that repository will be returned. An error will not be raised and that 
		/// repository may be of a different type to that specified in <paramref name="repositoryType"/>.
		/// Also the <see cref="log4net.Config.RepositoryAttribute"/> attribute on the
		/// assembly may be used to override the repository type specified in 
		/// <paramref name="repositoryType"/>.
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentNullException"><paramref name="repositoryAssembly"/> is <see langword="null" />.</exception>
		public ILoggerRepository CreateRepository(Assembly repositoryAssembly, Type repositoryType, string repositoryName, bool readAssemblyAttributes)
		{
			if (repositoryAssembly == null)
			{
				throw new ArgumentNullException("repositoryAssembly");
			}

			// If the type is not set then use the default type
			if (repositoryType == null)
			{
				repositoryType = m_defaultRepositoryType;
			}

			lock(this)
			{
				// Lookup in map
				ILoggerRepository rep = m_assembly2repositoryMap[repositoryAssembly] as ILoggerRepository;
				if (rep == null)
				{
					// Not found, therefore create
					LogLog.Debug("DefaultRepositorySelector: Creating repository for assembly [" + repositoryAssembly + "]");

					// Must specify defaults
					string actualRepositoryName = repositoryName;
					Type actualRepositoryType = repositoryType;

					if (readAssemblyAttributes)
					{
						// Get the repository and type from the assembly attributes
						GetInfoForAssembly(repositoryAssembly, ref actualRepositoryName, ref actualRepositoryType);
					}

					LogLog.Debug("DefaultRepositorySelector: Assembly [" + repositoryAssembly + "] using repository [" + actualRepositoryName + "] and repository type [" + actualRepositoryType + "]");

					// Lookup the repository in the map (as this may already be defined)
					rep = m_name2repositoryMap[actualRepositoryName] as ILoggerRepository;
					if (rep == null)
					{

						// Create the repository
						rep = CreateRepository(actualRepositoryName, actualRepositoryType);

						if (readAssemblyAttributes)
						{
							// Look for aliasing attributes
							LoadAliases(repositoryAssembly, rep);

							// Look for plugins defined on the assembly
							LoadPlugins(repositoryAssembly, rep);

							// Configure the repository using the assembly attributes
							ConfigureRepository(repositoryAssembly, rep);
						}
					}
					else
					{
						LogLog.Debug("DefaultRepositorySelector: repository [" + actualRepositoryName + "] already exists, using repository type [" + rep.GetType().FullName + "]");

						if (readAssemblyAttributes)
						{
							// Look for plugins defined on the assembly
							LoadPlugins(repositoryAssembly, rep);
						}
					}
					m_assembly2repositoryMap[repositoryAssembly] = rep;
				}
				return rep;
			}
		}

		/// <summary>
		/// Creates a new repository for the specified repository.
		/// </summary>
		/// <param name="repository">The repository to associate with the <see cref="ILoggerRepository"/>.</param>
		/// <param name="repositoryType">The type of repository to create, must implement <see cref="ILoggerRepository"/>.
		/// If this param is <see langword="null" /> then the default repository type is used.</param>
		/// <returns>The new repository.</returns>
		/// <remarks>
		/// The <see cref="ILoggerRepository"/> created will be associated with the repository
		/// specified such that a call to <see cref="GetRepository(string)"/> with the
		/// same repository specified will return the same repository instance.
		/// </remarks>
		/// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null" />.</exception>
		/// <exception cref="LogException"><paramref name="repository"/> already exists.</exception>
		public ILoggerRepository CreateRepository(string repository, Type repositoryType)
		{
			if (repository == null)
			{
				throw new ArgumentNullException("repository");
			}

			// If the type is not set then use the default type
			if (repositoryType == null)
			{
				repositoryType = m_defaultRepositoryType;
			}

			lock(this)
			{
				ILoggerRepository rep = null;

				// First check that the repository does not exist
				rep = m_name2repositoryMap[repository] as ILoggerRepository;
				if (rep != null)
				{
					throw new LogException("Repository [" + repository + "] is already defined. Repositories cannot be redefined.");
				}
				else
				{
					// Lookup an alias before trying to create the new repository
					ILoggerRepository aliasedRepository = m_alias2repositoryMap[repository] as ILoggerRepository;
					if (aliasedRepository != null)
					{
						// Found an alias

						// Check repository type
						if (aliasedRepository.GetType() == repositoryType)
						{
							// Repository type is compatible
							LogLog.Debug("DefaultRepositorySelector: Aliasing repository [" + repository + "] to existing repository [" + aliasedRepository.Name + "]");
							rep = aliasedRepository;

							// Store in map
							m_name2repositoryMap[repository] = rep;
						}
						else
						{
							// Invalid repository type for alias
							LogLog.Error("DefaultRepositorySelector: Failed to alias repository [" + repository + "] to existing repository ["+aliasedRepository.Name+"]. Requested repository type ["+repositoryType.FullName+"] is not compatible with existing type [" + aliasedRepository.GetType().FullName + "]");

							// We now drop through to create the repository without aliasing
						}
					}

					// If we could not find an alias
					if (rep == null)
					{
						LogLog.Debug("DefaultRepositorySelector: Creating repository [" + repository + "] using type [" + repositoryType + "]");

						// Call the no arg constructor for the repositoryType
						rep = (ILoggerRepository) repositoryType.GetConstructor(SystemInfo.EmptyTypes).Invoke(BindingFlags.Public | BindingFlags.Instance, null, new object[0], CultureInfo.InvariantCulture);

						// Set the name of the repository
						rep.Name = repository;

						// Store in map
						m_name2repositoryMap[repository] = rep;

						// Notify listeners that the repository has been created
						FireLoggerRepositoryCreatedEvent(rep);
					}
				}

				return rep;
			}
		}

		/// <summary>
		/// Copies the list of <see cref="ILoggerRepository"/> objects.
		/// </summary>
		/// <returns>An array of all known <see cref="ILoggerRepository"/> objects.</returns>
		public ILoggerRepository[] GetAllRepositories()
		{
			lock(this)
			{
				ICollection reps = m_name2repositoryMap.Values;
				ILoggerRepository[] all = new ILoggerRepository[reps.Count];
				reps.CopyTo(all, 0);
				return all;
			}
		}

		#endregion Implementation of IRepositorySelector

		#region Public Instance Methods

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
		///	<para><paramref name="repositoryAlias" /> is <see langword="null" />.</para>
		///	<para>-or-</para>
		///	<para><paramref name="repositoryTarget" /> is <see langword="null" />.</para>
		/// </exception>
		public void AliasRepository(string repositoryAlias, ILoggerRepository repositoryTarget) 
		{
			if (repositoryAlias == null) 
			{
				throw new ArgumentNullException("repositoryAlias");
			}
			if (repositoryTarget == null) 
			{
				throw new ArgumentNullException("repositoryTarget");
			}

			lock(this) 
			{
				// Check if the alias is already set
				if (m_alias2repositoryMap.Contains(repositoryAlias)) 
				{
					// Check if this is a duplicate of the current alias
					if (repositoryTarget != ((ILoggerRepository)m_alias2repositoryMap[repositoryAlias])) 
					{
						// Cannot redefine existing alias
						throw new InvalidOperationException("Repository [" + repositoryAlias + "] is already aliased to repository [" + ((ILoggerRepository)m_alias2repositoryMap[repositoryAlias]).Name + "]. Aliases cannot be redefined.");
					}
				}
					// Check if the alias is already mapped to a repository
				else if (m_name2repositoryMap.Contains(repositoryAlias)) 
				{
					// Check if this is a duplicate of the current mapping
					if ( repositoryTarget != ((ILoggerRepository)m_name2repositoryMap[repositoryAlias]) ) 
					{
						// Cannot define alias for already mapped repository
						throw new InvalidOperationException("Repository [" + repositoryAlias + "] already exists and cannot be aliased to repository [" + repositoryTarget.Name + "].");
					}
				}
				else 
				{
					// Set the alias
					m_alias2repositoryMap[repositoryAlias] = repositoryTarget;
				}
			}
		}

		#endregion Public Instance Methods

		#region Protected Instance Methods

		/// <summary>
		/// Notifies the registered listeners that the repository has been created.
		/// </summary>
		/// <param name="repository">The repository that has been created.</param>
		protected void FireLoggerRepositoryCreatedEvent(ILoggerRepository repository) 
		{
			if (m_loggerRepositoryCreatedEvent != null) 
			{
				m_loggerRepositoryCreatedEvent(this, new LoggerRepositoryCreationEventArgs(repository));
			}
		}

		#endregion Protected Instance Methods

		#region Private Instance Methods

		/// <summary>
		/// Gets the repository name and repository type for the specified assembly.
		/// </summary>
		/// <param name="assembly">The assembly that has a <see cref="log4net.Config.RepositoryAttribute"/>.</param>
		/// <param name="repositoryName">in/out param to hold the repository name to use for the assembly, caller should set this to the default value before calling.</param>
		/// <param name="repositoryType">in/out param to hold the type of the repository to create for the assembly, caller should set this to the default value before calling.</param>
		/// <exception cref="ArgumentNullException"><paramref name="assembly" /> is <see langword="null" />.</exception>
		private void GetInfoForAssembly(Assembly assembly, ref string repositoryName, ref Type repositoryType)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException("assembly");
			}

			LogLog.Debug("DefaultRepositorySelector: Assembly [" + assembly.FullName + "] Loaded From [" + SystemInfo.AssemblyLocationInfo(assembly) + "]");

			// Look for the RepositoryAttribute on the assembly 
			object[] repositoryAttributes = Attribute.GetCustomAttributes(assembly, typeof(log4net.Config.RepositoryAttribute), false);
			if (repositoryAttributes == null || repositoryAttributes.Length == 0)
			{
				// This is not a problem, but its nice to know what is going on.
				LogLog.Debug("DefaultRepositorySelector: Assembly [" + assembly + "] does not have a RepositoryAttribute specified.");
			}
			else
			{
				if (repositoryAttributes.Length > 1)
				{
					LogLog.Error("DefaultRepositorySelector: Assembly [" + assembly + "] has multiple log4net.Config.RepositoryAttribute assembly attributes. Only using first occurrence.");
				}

				log4net.Config.RepositoryAttribute domAttr = repositoryAttributes[0] as log4net.Config.RepositoryAttribute;

				if (domAttr == null)
				{
					LogLog.Error("DefaultRepositorySelector: Assembly [" + assembly + "] has a RepositoryAttribute but it does not!.");
				}
				else
				{
					// If the Name property is set then override the default
					if (domAttr.Name != null)
					{
						repositoryName = domAttr.Name;
					}

					// If the RepositoryType property is set then override the default
					if (domAttr.RepositoryType != null)
					{
						// Check that the type is a repository
						if (typeof(ILoggerRepository).IsAssignableFrom(domAttr.RepositoryType))
						{
							repositoryType = domAttr.RepositoryType;
						}
						else
						{
							LogLog.Error("DefaultRepositorySelector: Repository Type [" + domAttr.RepositoryType + "] must implement the ILoggerRepository interface.");
						}
					}
				}
			}
		}

		/// <summary>
		/// Configures the repository using information from the assembly.
		/// </summary>
		/// <param name="assembly">The assembly containing <see cref="log4net.Config.ConfiguratorAttribute"/>
		/// attributes which define the configuration for the repository.</param>
		/// <param name="repository">The repository to configure.</param>
		/// <exception cref="ArgumentNullException">
		///	<para><paramref name="assembly" /> is <see langword="null" />.</para>
		///	<para>-or-</para>
		///	<para><paramref name="repository" /> is <see langword="null" />.</para>
		/// </exception>
		private void ConfigureRepository(Assembly assembly, ILoggerRepository repository)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException("assembly");
			}
			if (repository == null)
			{
				throw new ArgumentNullException("repository");
			}

			// Look for the Configurator attributes (e.g. DOMConfiguratorAttribute) on the assembly
			object[] configAttributes = Attribute.GetCustomAttributes(assembly, typeof(log4net.Config.ConfiguratorAttribute), false);
			if (configAttributes != null && configAttributes.Length > 0)
			{
				// Delegate to the attribute the job of configuring the repository
				foreach(log4net.Config.ConfiguratorAttribute configAttr in configAttributes)
				{
					configAttr.Configure(assembly, repository);
				}
			}
		}

		/// <summary>
		/// Loads the attribute defined plugins on the assembly.
		/// </summary>
		/// <param name="assembly">The assembly that contains the attributes.</param>
		/// <param name="repository">The repository to add the plugins to.</param>
		/// <exception cref="ArgumentNullException">
		///	<para><paramref name="assembly" /> is <see langword="null" />.</para>
		///	<para>-or-</para>
		///	<para><paramref name="repository" /> is <see langword="null" />.</para>
		/// </exception>
		private void LoadPlugins(Assembly assembly, ILoggerRepository repository)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException("assembly");
			}
			if (repository == null)
			{
				throw new ArgumentNullException("repository");
			}

			// Look for the PluginAttribute on the assembly
			object[] configAttributes = Attribute.GetCustomAttributes(assembly, typeof(log4net.Config.PluginAttribute), false);
			if (configAttributes != null && configAttributes.Length > 0)
			{
				foreach(log4net.Plugin.IPluginFactory configAttr in configAttributes)
				{
					try
					{
						// Create the plugin and add it to the repository
						repository.PluginMap.Add(configAttr.CreatePlugin());
					}
					catch(Exception ex)
					{
						LogLog.Error("DefaultRepositorySelector: Failed to create plugin. Attribute [" + configAttr.ToString() + "]", ex);
					}
				}
			}
		}

		/// <summary>
		/// Loads the attribute defined aliases on the assembly.
		/// </summary>
		/// <param name="assembly">The assembly that contains the attributes.</param>
		/// <param name="repository">The repository to alias to.</param>
		/// <exception cref="ArgumentNullException">
		///	<para><paramref name="assembly" /> is <see langword="null" />.</para>
		///	<para>-or-</para>
		///	<para><paramref name="repository" /> is <see langword="null" />.</para>
		/// </exception>
		private void LoadAliases(Assembly assembly, ILoggerRepository repository)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException("assembly");
			}
			if (repository == null)
			{
				throw new ArgumentNullException("repository");
			}

			// Look for the AliasRepositoryAttribute on the assembly
			object[] configAttributes = Attribute.GetCustomAttributes(assembly, typeof(log4net.Config.AliasRepositoryAttribute), false);
			if (configAttributes != null && configAttributes.Length > 0)
			{
				foreach(log4net.Config.AliasRepositoryAttribute configAttr in configAttributes)
				{
					try
					{
						AliasRepository(configAttr.Name, repository);
					}
					catch(Exception ex)
					{
						LogLog.Error("DefaultRepositorySelector: Failed to alias repository [" + configAttr.Name + "]", ex);
					}
				}
			}
		}

		#endregion Private Instance Methods

		#region Private Static Fields

		private const string DefaultRepositoryName = "log4net-default-repository";

		#endregion Private Static Fields

		#region Private Instance Fields

		private IDictionary m_name2repositoryMap = new Hashtable();
		private IDictionary m_assembly2repositoryMap = new Hashtable();
		private IDictionary m_alias2repositoryMap = new Hashtable();
		private Type m_defaultRepositoryType;

		private event LoggerRepositoryCreationEventHandler m_loggerRepositoryCreatedEvent;

		#endregion Private Instance Fields
	}
}

#endif // !NETCF