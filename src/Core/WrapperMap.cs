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

using log4net.Repository;

namespace log4net.Core
{
	#region WrapperCreationHandler

	/// <summary>
	/// Delegate used to handle creation of new wrappers.
	/// </summary>
	/// <param name="logger">The logger to wrap in a wrapper.</param>
	public delegate ILoggerWrapper WrapperCreationHandler(ILogger logger);

	#endregion WrapperCreationHandler

	/// <summary>
	/// Maps between logger objects and wrapper objects.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class maintains a mapping between <see cref="ILogger"/> objects and
	/// <see cref="ILoggerWrapper"/> objects. Use the indexer accessor to lookup the 
	/// <see cref="ILoggerWrapper"/> for the specified <see cref="ILogger"/>.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class WrapperMap
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="WrapperMap" /> class with 
		/// the specified handler to create the wrapper objects.
		/// </summary>
		/// <param name="createWrapperHandler">The handler to use to create the wrapper objects.</param>
		public WrapperMap(WrapperCreationHandler createWrapperHandler) 
		{
			m_createWrapperHandler = createWrapperHandler;

			// Create the delegates for the event callbacks
			m_shutdownHandler = new LoggerRepositoryShutdownEventHandler(OnShutdown);
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets the wrapper object for the specified logger.
		/// </summary>
		/// <value>
		/// The wrapper object for the specified logger.
		/// </value>
		/// <remarks>
		/// If the logger is null then the corresponding wrapper is null
		/// </remarks>
		virtual public ILoggerWrapper this[ILogger logger]
		{
			get
			{
				// If the logger is null then the corresponding wrapper is null
				if (logger == null)
				{
					return null;
				}

				lock(this)
				{
					// Lookup hierarchy in map.
					Hashtable wrappersMap = (Hashtable)m_repositories[logger.Repository];

					if (wrappersMap == null)
					{
						// Hierarchy does not exist in map.
						// Must register with hierarchy

						wrappersMap = new Hashtable();
						m_repositories[logger.Repository] = wrappersMap;

						// Register for config reset & shutdown on repository
						logger.Repository.ShutdownEvent += m_shutdownHandler;
					}

					// Look for the wrapper object in the map
					ILoggerWrapper wrapperObject = wrappersMap[logger] as ILoggerWrapper;

					if (wrapperObject == null)
					{
						// No wrapper object exists for the specified logger

						// Create a new wrapper wrapping the logger
						wrapperObject = CreateNewWrapperObject(logger);
					
						// Store wrapper logger in map
						wrappersMap[logger] = wrapperObject;
					}

					return wrapperObject;
				}
			}
		}

		#endregion Public Instance Properties

		#region Protected Instance Properties

		/// <summary>
		/// Gets the map of logger repositories.
		/// </summary>
		/// <value>
		/// Map of logger repositories.
		/// </value>
		protected Hashtable Repositories 
		{
			get { return this.m_repositories; }
		}

		#endregion Protected Instance Properties

		#region Protected Instance Methods

		/// <summary>
		/// Creates the wrapper object for the specified logger.
		/// </summary>
		/// <param name="logger">The logger to wrap in a wrapper.</param>
		/// <remarks>
		/// This implementation uses the <see cref="WrapperCreationHandler"/>
		/// passed to the constructor to create the wrapper. This method
		/// can be overridden in a subclass.
		/// </remarks>
		/// <returns>The wrapper object for the logger.</returns>
		virtual protected ILoggerWrapper CreateNewWrapperObject(ILogger logger)
		{
			if (m_createWrapperHandler != null)
			{
				return m_createWrapperHandler(logger);
			}
			return null;
		}

		/// <summary>
		/// Event handler for repository shutdown event.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">The event args.</param>
		virtual protected void OnShutdown(object sender, EventArgs e)
		{
			lock(this)
			{
				if (sender is ILoggerRepository)
				{
					// Remove all repository from map
					m_repositories.Remove(sender);

					// Unhook all events from the repository
					((ILoggerRepository)sender).ShutdownEvent -= m_shutdownHandler;
				}
			}
		}

		#endregion Protected Instance Methods

		#region Private Instance Variables

		/// <summary>
		/// Map of logger repositories.
		/// </summary>
		private Hashtable m_repositories = new Hashtable();

		/// <summary>
		/// The handler to use to create the extension wrapper objects.
		/// </summary>
		private WrapperCreationHandler m_createWrapperHandler;

		/// <summary>
		/// Internal reference to the delegate used to register for repository shutdown events.
		/// </summary>
		private LoggerRepositoryShutdownEventHandler m_shutdownHandler;
 
		#endregion Private Instance Variables
	}
}
