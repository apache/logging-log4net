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
using System.Reflection;

using log4net.Repository;

namespace log4net.Core
{
	#region LoggerRepositoryCreationEvent

	/// <summary>
	/// Delegate used to handle logger repository creation event notifications
	/// </summary>
	/// <param name="sender">The <see cref="IRepositorySelector"/> which created the repository.</param>
	/// <param name="e">The <see cref="LoggerRepositoryCreationEventArgs"/> event args
	/// that holds the <see cref="ILoggerRepository"/> instance that has been created.</param>
	/// <remarks>
	/// Delegate used to handle logger repository creation event notifications
	/// </remarks>
	public delegate void LoggerRepositoryCreationEventHandler(object sender, LoggerRepositoryCreationEventArgs e);

	/// <summary>
	/// Provides data for the <see cref="IRepositorySelector.LoggerRepositoryCreatedEvent"/> event.
	/// </summary>
	/// <remarks>
	/// A <see cref="IRepositorySelector.LoggerRepositoryCreatedEvent"/> event is raised every time a
	/// <see cref="ILoggerRepository"/> is created.
	/// </remarks>
	public class LoggerRepositoryCreationEventArgs : EventArgs
	{
		/// <summary>
		/// The <see cref="ILoggerRepository"/> created
		/// </summary>
		private ILoggerRepository m_repository;

		/// <summary>
		/// Construct instance using <see cref="ILoggerRepository"/> specified
		/// </summary>
		/// <param name="repository">the <see cref="ILoggerRepository"/> that has been created</param>
		public LoggerRepositoryCreationEventArgs(ILoggerRepository repository)
		{
			m_repository = repository;
		}

		/// <summary>
		/// The <see cref="ILoggerRepository"/> that has been created
		/// </summary>
		/// <value>
		/// The <see cref="ILoggerRepository"/> that has been created
		/// </value>
		public ILoggerRepository LoggerRepository
		{
			get { return m_repository; }
		}
	}

	#endregion

	/// <summary>
	/// Interface used my the <see cref="LogManager"/> to select the <see cref="ILoggerRepository"/>.
	/// </summary>
	/// <remarks>
	/// The <see cref="LogManager"/> uses a <see cref="IRepositorySelector"/> to specify the policy for
	/// selecting the correct <see cref="ILoggerRepository"/> to return to the caller.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public interface IRepositorySelector
	{
		/// <summary>
		/// Gets the <see cref="ILoggerRepository"/> for the specified assembly.
		/// </summary>
		/// <param name="domainAssembly">The assembly to use to lookup to the <see cref="ILoggerRepository"/></param>
		/// <returns>The <see cref="ILoggerRepository"/> for the assembly.</returns>
		ILoggerRepository GetRepository(Assembly domainAssembly);

		/// <summary>
		/// Gets the <see cref="ILoggerRepository"/> for the specified domain
		/// </summary>
		/// <param name="domain">The domain to use to lookup to the <see cref="ILoggerRepository"/>.</param>
		/// <returns>The <see cref="ILoggerRepository"/> for the domain.</returns>
		ILoggerRepository GetRepository(string domain);

		/// <summary>
		/// Creates a new repository for the assembly specified.
		/// </summary>
		/// <param name="domainAssembly">The assembly to use to create the domain to associate with the <see cref="ILoggerRepository"/>.</param>
		/// <param name="repositoryType">The type of repository to create, must implement <see cref="ILoggerRepository"/>.</param>
		/// <returns>The repository created.</returns>
		/// <remarks>
		/// <para>The <see cref="ILoggerRepository"/> created will be associated with the domain
		/// specified such that a call to <see cref="GetRepository(Assembly)"/> with the
		/// same assembly specified will return the same repository instance.</para>
		/// </remarks>
		ILoggerRepository CreateRepository(Assembly domainAssembly, Type repositoryType);

		/// <summary>
		/// Creates a new repository for the domain specified.
		/// </summary>
		/// <param name="domain">The domain to associate with the <see cref="ILoggerRepository"/>.</param>
		/// <param name="repositoryType">The type of repository to create, must implement <see cref="ILoggerRepository"/>.</param>
		/// <returns>The repository created.</returns>
		/// <remarks>
		/// <para>The <see cref="ILoggerRepository"/> created will be associated with the domain
		/// specified such that a call to <see cref="GetRepository(string)"/> with the
		/// same domain specified will return the same repository instance.</para>
		/// </remarks>
		ILoggerRepository CreateRepository(string domain, Type repositoryType);

		/// <summary>
		/// Gets the list of currently defined repositories.
		/// </summary>
		/// <returns>
		/// An array of the <see cref="ILoggerRepository"/> instances created by 
		/// this <see cref="IRepositorySelector"/>.</returns>
		ILoggerRepository[] GetAllRepositories();

		/// <summary>
		/// Event to notify that a logger repository has been created.
		/// </summary>
		/// <value>
		/// Event to notify that a logger repository has been created.
		/// </value>
		event LoggerRepositoryCreationEventHandler LoggerRepositoryCreatedEvent;
	}
}
