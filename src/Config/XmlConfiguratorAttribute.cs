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
using System.IO;

using log4net.Util;
using log4net.Repository;
using log4net.Repository.Hierarchy;

namespace log4net.Config
{
	/// <summary>
	/// Assembly level attribute to configure the <see cref="XmlConfigurator"/>.
	/// </summary>
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
	/// <author>Gert Driesen</author>
	[AttributeUsage(AttributeTargets.Assembly)]
	[Serializable]
	public /*sealed*/ class XmlConfiguratorAttribute : ConfiguratorAttribute
	{
		//
		// Class is not sealed because DOMConfiguratorAttribute extends it while it is obseleted
		// 

		#region Public Instance Properties

		/// <summary>
		/// Gets or sets the filename of the configuration file.
		/// </summary>
		/// <value>
		/// The filename of the configuration file.
		/// </value>
		/// <remarks>
		/// <para>
		/// If specified, this is the name of the configuration file to use with
		/// the <see cref="XmlConfigurator"/>. This file path is relative to the
		/// <b>application base</b> directory (<see cref="AppDomain.BaseDirectory"/>).
		/// </para>
		/// </remarks>
		public string ConfigFile
		{
			get { return m_configFile; }
			set { m_configFile = value; }
		}

		/// <summary>
		/// Gets or sets the extension of the configuration file.
		/// </summary>
		/// <value>
		/// The extension of the configuration file.
		/// </value>
		/// <remarks>
		/// <para>
		/// If specified this is the extension for the configuration file.
		/// The path to the config file is built by using the <b>application 
		/// base</b> directory (<see cref="AppDomain.BaseDirectory"/>),
		/// the <b>assembly name</b> and the config file extension.
		/// </para>
		/// </remarks>
		public string ConfigFileExtension
		{
			get { return m_configFileExtension; }
			set { m_configFileExtension = value; }
		}

#if (!SSCLI)
		/// <summary>
		/// Gets or sets a value indicating whether to watch the configuration file.
		/// </summary>
		/// <value>
		/// <c>true</c> if the configuration should be watched, <c>false</c> otherwise.
		/// </value>
		/// <remarks>
		/// <para>
		/// If this flag is specified and set to <c>true</c> then the framework
		/// will watch the configuration file and will reload the config each time 
		/// the file is modified.
		/// </para>
		/// </remarks>
		public bool Watch
		{
			get { return m_configureAndWatch; }
			set { m_configureAndWatch = value; }
		}
#endif

		#endregion Public Instance Properties

		#region Override ConfiguratorAttribute

		/// <summary>
		/// Configures the <see cref="ILoggerRepository"/> for the specified assembly.
		/// </summary>
		/// <param name="assembly">The assembly that this attribute was defined on.</param>
		/// <param name="repository">The repository to configure.</param>
		/// <remarks>
		/// <para>
		/// Configure the repository using the <see cref="XmlConfigurator"/>.
		/// The <paramref name="repository"/> specified must extend the <see cref="Hierarchy"/>
		/// class otherwise the <see cref="XmlConfigurator"/> will not be able to
		/// configure it.
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentOutOfRangeException">The <paramref name="repository" /> does not extend <see cref="Hierarchy"/>.</exception>
		override public void Configure(Assembly assembly, ILoggerRepository repository)
		{
			// Work out the full path to the config file
			string fullPath2ConfigFile = null;
			
			// Select the config file
			if (m_configFile == null || m_configFile.Length == 0)
			{
				if (m_configFileExtension == null || m_configFileExtension.Length == 0)
				{
					// Use the default .config file for the AppDomain
					fullPath2ConfigFile = SystemInfo.ConfigurationFileLocation;
				}
				else
				{
					// Force the extension to start with a '.'
					if (m_configFileExtension[0] != '.')
					{
						m_configFileExtension = "." + m_configFileExtension;
					}

					fullPath2ConfigFile = Path.Combine(SystemInfo.ApplicationBaseDirectory, SystemInfo.AssemblyFileName(assembly) + m_configFileExtension);
				}
			}
			else
			{
				// Just the base dir + the config file
				fullPath2ConfigFile = Path.Combine(SystemInfo.ApplicationBaseDirectory, m_configFile);
			}

#if (SSCLI)
			XmlConfigurator.Configure(repository, new FileInfo(fullPath2ConfigFile));
#else
			// Do we configure just once or do we configure and then watch?
			if (m_configureAndWatch)
			{
				XmlConfigurator.ConfigureAndWatch(repository, new FileInfo(fullPath2ConfigFile));
			}
			else
			{
				XmlConfigurator.Configure(repository, new FileInfo(fullPath2ConfigFile));
			}
#endif
		}

		#endregion

		#region Private Instance Fields

		private string m_configFile = null;
		private string m_configFileExtension = null;

#if (!SSCLI)
		private bool m_configureAndWatch = false;
#endif

		#endregion Private Instance Fields
	}
}
