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

using System;
using System.Collections;

using System.Reflection;

using log4net.Appender;
using log4net.Util;
using log4net.Repository;


namespace log4net.Core
{
	/// <summary>
	/// The implementation of the <see cref="IRepositorySelector"/> interface suitable
	/// for use with the compact framework
	/// </summary>
	/// <remarks>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public class CompactRepositorySelector : IRepositorySelector
	{
		#region Member Variables

		private const string DefaultRepositoryName = "log4net-default-repository";

		private IDictionary m_name2repositoryMap = new Hashtable();
		private Type m_defaultRepositoryType;

		private event LoggerRepositoryCreationEventHandler m_loggerRepositoryCreatedEvent;

		#endregion

		#region Constructors

		/// <summary>
		/// Create a new repository selector
		/// </summary>
		/// <param name="defaultRepositoryType">the type of the repositories to create, must implement <see cref="ILoggerRepository"/></param>
		/// <exception cref="ArgumentNullException">throw if <paramref name="defaultRepositoryType"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">throw if <paramref name="defaultRepositoryType"/> does not implement <see cref="ILoggerRepository"/></exception>
		public CompactRepositorySelector(Type defaultRepositoryType)
		{
			if (defaultRepositoryType == null)
			{
				throw new ArgumentNullException("defaultRepositoryType");
			}

			// Check that the type is a repository
			if (! (typeof(ILoggerRepository).IsAssignableFrom(defaultRepositoryType)) )
			{
				throw new ArgumentOutOfRangeException("Parameter: defaultRepositoryType, Value: ["+defaultRepositoryType+"] out of range. Argument must implement the ILoggerRepository interface");
			}

			m_defaultRepositoryType = defaultRepositoryType;

			LogLog.Debug("CompactRepositorySelector: defaultRepositoryType ["+m_defaultRepositoryType+"]");
		}

		#endregion

		#region Implementation of IRepositorySelector

		/// <summary>
		/// Get the <see cref="ILoggerRepository"/> for the specified assembly
		/// </summary>
		/// <param name="assembly">the assembly to use to lookup to the <see cref="ILoggerRepository"/></param>
		/// <returns>The <see cref="ILoggerRepository"/> for the assembly</returns>
		/// <remarks>
		/// <para>Assemblies use the default repository.</para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">throw if <paramref name="assembly"/> is null</exception>
		public ILoggerRepository GetRepository(Assembly assembly)
		{
			return CreateRepository(assembly, m_defaultRepositoryType);
		}

		/// <summary>
		/// Get the <see cref="ILoggerRepository"/> for the specified repository
		/// </summary>
		/// <param name="repository">the repository to use to lookup to the <see cref="ILoggerRepository"/></param>
		/// <returns>The <see cref="ILoggerRepository"/> for the repository</returns>
		/// <exception cref="ArgumentNullException">throw if <paramref name="repository"/> is null</exception>
		/// <exception cref="LogException">throw if the <paramref name="repository"/> does not exist</exception>
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
					throw new LogException("Repository ["+repository+"] is NOT defined.");
				}
				return rep;
			}
		}

		/// <summary>
		/// Create a new repository for the assembly specified 
		/// </summary>
		/// <param name="assembly">the assembly to use to create the repository to associate with the <see cref="ILoggerRepository"/></param>
		/// <param name="repositoryType">the type of repository to create, must implement <see cref="ILoggerRepository"/></param>
		/// <returns>the repository created</returns>
		/// <remarks>
		/// <para>The <see cref="ILoggerRepository"/> created will be associated with the repository
		/// specified such that a call to <see cref="GetRepository(Assembly)"/> with the
		/// same assembly specified will return the same repository instance.</para>
		/// 
		/// <para>Assemblies use the default repository.</para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">throw if <paramref name="assembly"/> is null</exception>
		public ILoggerRepository CreateRepository(Assembly assembly, Type repositoryType)
		{
			// If the type is not set then use the default type
			if (repositoryType == null)
			{
				repositoryType = m_defaultRepositoryType;
			}

			lock(this)
			{
				// First check that the repository does not exist
				ILoggerRepository rep = m_name2repositoryMap[DefaultRepositoryName] as ILoggerRepository;
				if (rep == null)
				{
					// Must create the repository
					rep = CreateRepository(DefaultRepositoryName, repositoryType);
				}

				return rep;
			}		
		}

		/// <summary>
		/// Create a new repository for the repository specified
		/// </summary>
		/// <param name="repository">the repository to associate with the <see cref="ILoggerRepository"/></param>
		/// <param name="repositoryType">the type of repository to create, must implement <see cref="ILoggerRepository"/>.
		/// If this param is null then the default repository type is used.</param>
		/// <returns>the repository created</returns>
		/// <remarks>
		/// The <see cref="ILoggerRepository"/> created will be associated with the repository
		/// specified such that a call to <see cref="GetRepository(string)"/> with the
		/// same repository specified will return the same repository instance.
		/// </remarks>
		/// <exception cref="ArgumentNullException">throw if <paramref name="repository"/> is null</exception>
		/// <exception cref="LogException">throw if the <paramref name="repository"/> already exists</exception>
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
					throw new LogException("Repository ["+repository+"] is already defined. Repositories cannot be redefined.");
				}
				else
				{
					LogLog.Debug("DefaultRepositorySelector: Creating repository ["+repository+"] using type ["+repositoryType+"]");

					// Call the no arg constructor for the repositoryType
					rep = (ILoggerRepository)repositoryType.GetConstructor(SystemInfo.EmptyTypes).Invoke(null);

					// Set the name of the repository
					rep.Name = repository;

					// Store in map
					m_name2repositoryMap[repository] = rep;

					// Notify listeners that the repository has been created
					FireLoggerRepositoryCreatedEvent(rep);
				}

				return rep;
			}
		}

		/// <summary>
		/// Copy the list of <see cref="ILoggerRepository"/> objects
		/// </summary>
		/// <returns>an array of all known <see cref="ILoggerRepository"/> objects</returns>
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

		#endregion

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

		/// <summary>
		/// Notify the registered listeners that the repository has been created
		/// </summary>
		/// <param name="repository">The repository that has been created</param>
		protected void FireLoggerRepositoryCreatedEvent(ILoggerRepository repository)
		{
			if (m_loggerRepositoryCreatedEvent != null)
			{
				m_loggerRepositoryCreatedEvent(this, new LoggerRepositoryCreationEventArgs(repository));
			}
		}
	}
}