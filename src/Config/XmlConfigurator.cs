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
using System.Xml;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;

using log4net.Appender;
using log4net.Util;
using log4net.Repository;

namespace log4net.Config
{
	/// <summary>
	/// Use this class to initialize the log4net environment using an Xml tree.
	/// </summary>
	/// <remarks>
	/// Configures a <see cref="ILoggerRepository"/> using an Xml tree.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public sealed class XmlConfigurator
	{
		#region Private Instance Constructors

		/// <summary>
		/// Private constructor
		/// </summary>
		private XmlConfigurator() 
		{ 
		}

		#endregion Protected Instance Constructors

		#region Configure static methods

		/// <summary>
		/// Automatically configures the log4net system based on the 
		/// application's configuration settings.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Each application has a configuration file. This has the
		/// same name as the application with '.config' appended.
		/// This file is XML and calling this function prompts the
		/// configurator to look in that file for a section called
		/// <c>log4net</c> that contains the configuration data.
		/// </para>
		/// <para>
		/// To use this method to configure log4net you must specify 
		/// the <see cref="Log4NetConfigurationSectionHandler"/> section
		/// handler for the <c>log4net</c> configuration section. See the
		/// <see cref="Log4NetConfigurationSectionHandler"/> for an example.
		/// </para>
		/// </remarks>
		/// <seealso cref="Log4NetConfigurationSectionHandler"/>
		static public void Configure() 
		{
			Configure(LogManager.GetRepository(Assembly.GetCallingAssembly()));
		}

		/// <summary>
		/// Automatically configures the <see cref="ILoggerRepository"/> using settings
		/// stored in the application's configuration file.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Each application has a configuration file. This has the
		/// same name as the application with '.config' appended.
		/// This file is XML and calling this function prompts the
		/// configurator to look in that file for a section called
		/// <c>log4net</c> that contains the configuration data.
		/// </para>
		/// <para>
		/// To use this method to configure log4net you must specify 
		/// the <see cref="Log4NetConfigurationSectionHandler"/> section
		/// handler for the <c>log4net</c> configuration section. See the
		/// <see cref="Log4NetConfigurationSectionHandler"/> for an example.
		/// </para>
		/// </remarks>
		/// <param name="repository">The repository to configure.</param>
		static public void Configure(ILoggerRepository repository) 
		{
			LogLog.Debug("XmlConfigurator: configuring repository [" + repository.Name + "] using .config file section");

			try
			{
				LogLog.Debug("XmlConfigurator: Application config file is [" + SystemInfo.ConfigurationFileLocation + "]");
			}
			catch
			{
				// ignore error
				LogLog.Debug("XmlConfigurator: Application config file location unknown");
			}

#if NETCF
			// No config file reading stuff. Just go straight for the file
			Configure(repository, new FileInfo(SystemInfo.ConfigurationFileLocation));
#else
			try
			{
				XmlElement configElement = System.Configuration.ConfigurationSettings.GetConfig("log4net") as XmlElement;
				if (configElement == null)
				{
					// Failed to load the xml config using configuration settings handler
					LogLog.Error("XmlConfigurator: Failed to find configuration section 'log4net' in the application's .config file. Check your .config file for the <log4net> and <configSections> elements. The configuration section should look like: <section name=\"log4net\" type=\"log4net.Config.Log4NetConfigurationSectionHandler,log4net\" />");
				}
				else
				{
					// Configure using the xml loaded from the config file
					ConfigureFromXml(repository, configElement);
				}
			}
			catch(System.Configuration.ConfigurationException confEx)
			{
				if (confEx.BareMessage.IndexOf("Unrecognized element") >= 0)
				{
					// Looks like the XML file is not valid
					LogLog.Error("XmlConfigurator: Failed to parse config file. Check your .config file is well formed XML.", confEx);
				}
				else
				{
					// This exception is typically due to the assembly name not being correctly specified in the section type.
					string configSectionStr = "<section name=\"log4net\" type=\"log4net.Config.Log4NetConfigurationSectionHandler," + Assembly.GetExecutingAssembly().FullName + "\" />";
					LogLog.Error("XmlConfigurator: Failed to parse config file. Is the <configSections> specified as: " + configSectionStr, confEx);
				}
			}
#endif
		}

		/// <summary>
		/// Configures log4net using a <c>log4net</c> element
		/// </summary>
		/// <remarks>
		/// Loads the log4net configuration from the XML element
		/// supplied as <paramref name="element"/>.
		/// </remarks>
		/// <param name="element">The element to parse.</param>
		static public void Configure(XmlElement element) 
		{
			ConfigureFromXml(LogManager.GetRepository(Assembly.GetCallingAssembly()), element);
		}

		/// <summary>
		/// Configures the <see cref="ILoggerRepository"/> using the specified XML 
		/// element.
		/// </summary>
		/// <remarks>
		/// Loads the log4net configuration from the XML element
		/// supplied as <paramref name="element"/>.
		/// </remarks>
		/// <param name="repository">The repository to configure.</param>
		/// <param name="element">The element to parse.</param>
		static public void Configure(ILoggerRepository repository, XmlElement element) 
		{
			LogLog.Debug("XmlConfigurator: configuring repository [" + repository.Name + "] using XML element");

			ConfigureFromXml(repository, element);
		}

		/// <summary>
		/// Configures log4net using the specified configuration file.
		/// </summary>
		/// <param name="configFile">The XML file to load the configuration from.</param>
		/// <remarks>
		/// <para>
		/// The configuration file must be valid XML. It must contain
		/// at least one element called <c>log4net</c> that holds
		/// the log4net configuration data.
		/// </para>
		/// <para>
		/// The log4net configuration file can possible be specified in the application's
		/// configuration file (either <c>MyAppName.exe.config</c> for a
		/// normal application on <c>Web.config</c> for an ASP.NET application).
		/// </para>
		/// <para>
		/// The first element matching <c>&lt;configuration&gt;</c> will be read as the 
		/// configuration. If this file is also a .NET .config file then you must specify 
		/// a configuration section for the <c>log4net</c> element otherwise .NET will 
		/// complain. Set the type for the section handler to <see cref="System.Configuration.IgnoreSectionHandler"/>, for example:
		/// <code>
		/// &lt;configSections&gt;
		///		&lt;section name="log4net" type="System.Configuration.IgnoreSectionHandler" /&gt;
		///	&lt;/configSections&gt;
		/// </code>
		/// </para>
		/// <example>
		/// The following example configures log4net using a configuration file, of which the 
		/// location is stored in the application's configuration file :
		/// </example>
		/// <code lang="C#">
		/// using log4net.Config;
		/// using System.IO;
		/// using System.Configuration;
		/// 
		/// ...
		/// 
		/// XmlConfigurator.Configure(new FileInfo(ConfigurationSettings.AppSettings["log4net-config-file"]));
		/// </code>
		/// <para>
		/// In the <c>.config</c> file, the path to the log4net can be specified like this :
		/// </para>
		/// <code>
		/// &lt;configuration&gt;
		///		&lt;appSettings&gt;
		///			&lt;add key="log4net-config-file" value="log.config"/&gt;
		///		&lt;/appSettings&gt;
		///	&lt;/configuration&gt;
		/// </code>
		/// </remarks>
		static public void Configure(FileInfo configFile)
		{
			Configure(LogManager.GetRepository(Assembly.GetCallingAssembly()), configFile);
		}

		/// <summary>
		/// Configures log4net using the specified configuration file.
		/// </summary>
		/// <param name="configStream">A stream to load the XML configuration from.</param>
		/// <remarks>
		/// <para>
		/// The configuration data must be valid XML. It must contain
		/// at least one element called <c>log4net</c> that holds
		/// the log4net configuration data.
		/// </para>
		/// <para>
		/// Note that this method will NOT close the stream parameter.
		/// </para>
		/// </remarks>
		static public void Configure(Stream configStream)
		{
			Configure(LogManager.GetRepository(Assembly.GetCallingAssembly()), configStream);
		}

		/// <summary>
		/// Configures the <see cref="ILoggerRepository"/> using the specified configuration 
		/// file.
		/// </summary>
		/// <param name="repository">The repository to configure.</param>
		/// <param name="configFile">The XML file to load the configuration from.</param>
		/// <remarks>
		/// <para>
		/// The configuration file must be valid XML. It must contain
		/// at least one element called <c>log4net</c> that holds
		/// the configuration data.
		/// </para>
		/// <para>
		/// The log4net configuration file can possible be specified in the application's
		/// configuration file (either <c>MyAppName.exe.config</c> for a
		/// normal application on <c>Web.config</c> for an ASP.NET application).
		/// </para>
		/// <para>
		/// The first element matching <c>&lt;configuration&gt;</c> will be read as the 
		/// configuration. If this file is also a .NET .config file then you must specify 
		/// a configuration section for the <c>log4net</c> element otherwise .NET will 
		/// complain. Set the type for the section handler to <see cref="System.Configuration.IgnoreSectionHandler"/>, for example:
		/// <code>
		/// &lt;configSections&gt;
		///		&lt;section name="log4net" type="System.Configuration.IgnoreSectionHandler" /&gt;
		///	&lt;/configSections&gt;
		/// </code>
		/// </para>
		/// <example>
		/// The following example configures log4net using a configuration file, of which the 
		/// location is stored in the application's configuration file :
		/// </example>
		/// <code lang="C#">
		/// using log4net.Config;
		/// using System.IO;
		/// using System.Configuration;
		/// 
		/// ...
		/// 
		/// XmlConfigurator.Configure(new FileInfo(ConfigurationSettings.AppSettings["log4net-config-file"]));
		/// </code>
		/// <para>
		/// In the <c>.config</c> file, the path to the log4net can be specified like this :
		/// </para>
		/// <code>
		/// &lt;configuration&gt;
		///		&lt;appSettings&gt;
		///			&lt;add key="log4net-config-file" value="log.config"/&gt;
		///		&lt;/appSettings&gt;
		///	&lt;/configuration&gt;
		/// </code>
		/// </remarks>
		static public void Configure(ILoggerRepository repository, FileInfo configFile)
		{
			LogLog.Debug("XmlConfigurator: configuring repository [" + repository.Name + "] using file [" + configFile + "]");

			if (configFile == null)
			{
				LogLog.Error("XmlConfigurator: Configure called with null 'configFile' parameter");
			}
			else
			{
				// Have to use File.Exists() rather than configFile.Exists()
				// because configFile.Exists() caches the value, not what we want.
				if (File.Exists(configFile.FullName))
				{
					// Open the file for reading
					FileStream fs = null;
					
					// Try hard to open the file
					for(int retry = 5; --retry >= 0; )
					{
						try
						{
							fs = configFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
							break;
						}
						catch(IOException ex)
						{
							if (retry == 0)
							{
								LogLog.Error("XmlConfigurator: Failed to open XML config file [" + configFile.Name + "]", ex);

								// The stream cannot be valid
								fs = null;
							}
							System.Threading.Thread.Sleep(250);
						}
					}

					if (fs != null)
					{
						try
						{
							// Load the configuration from the stream
							Configure(repository, fs);
						}
						finally
						{
							// Force the file closed whatever happens
							fs.Close();
						}
					}
				}
				else
				{
					LogLog.Debug("XmlConfigurator: config file [" + configFile.FullName + "] not found. Configuration unchanged.");
				}
			}
		}


		/// <summary>
		/// Configures the <see cref="ILoggerRepository"/> using the specified configuration 
		/// file.
		/// </summary>
		/// <param name="repository">The repository to configure.</param>
		/// <param name="configStream">The stream to load the XML configuration from.</param>
		/// <remarks>
		/// <para>
		/// The configuration data must be valid XML. It must contain
		/// at least one element called <c>log4net</c> that holds
		/// the configuration data.
		/// </para>
		/// <para>
		/// Note that this method will NOT close the stream parameter.
		/// </para>
		/// </remarks>
		static public void Configure(ILoggerRepository repository, Stream configStream)
		{
			LogLog.Debug("XmlConfigurator: configuring repository [" + repository.Name + "] using stream");

			if (configStream == null)
			{
				LogLog.Error("XmlConfigurator: Configure called with null 'configStream' parameter");
			}
			else
			{
				// Load the config file into a document
				XmlDocument doc = new XmlDocument();
				try
				{
#if (NETCF)
					// Create a text reader for the file stream
					XmlTextReader xmlReader = new XmlTextReader(configStream);
#else
					// Create a validating reader around a text reader for the file stream
					XmlValidatingReader xmlReader = new XmlValidatingReader(new XmlTextReader(configStream));

					// Specify that the reader should not perform validation, but that it should
					// expand entity refs.
					xmlReader.ValidationType = ValidationType.None;
					xmlReader.EntityHandling = EntityHandling.ExpandEntities;
#endif
					
					// load the data into the document
					doc.Load(xmlReader);
				}
				catch(Exception ex)
				{
					LogLog.Error("XmlConfigurator: Error while loading XML configuration", ex);

					// The document is invalid
					doc = null;
				}

				if (doc != null)
				{
					LogLog.Debug("XmlConfigurator: loading XML configuration");

					// Configure using the 'log4net' element
					XmlNodeList configNodeList = doc.GetElementsByTagName("log4net");
					if (configNodeList.Count == 0)
					{
						LogLog.Debug("XmlConfigurator: XML configuration does not contain a <log4net> element. Configuration Aborted.");
					}
					else if (configNodeList.Count > 1)
					{
						LogLog.Error("XmlConfigurator: XML configuration contains [" + configNodeList.Count + "] <log4net> elements. Only one is allowed. Configuration Aborted.");
					}
					else
					{
						ConfigureFromXml(repository, configNodeList[0] as XmlElement);
					}
				}
			}
		}

		#endregion Configure static methods

		#region ConfigureAndWatch static methods

#if (!NETCF && !SSCLI)

		/// <summary>
		/// Configures log4net using the file specified, monitors the file for changes 
		/// and reloads the configuration if a change is detected.
		/// </summary>
		/// <param name="configFile">The XML file to load the configuration from.</param>
		/// <remarks>
		/// <para>
		/// The configuration file must be valid XML. It must contain
		/// at least one element called <c>log4net</c> that holds
		/// the configuration data.
		/// </para>
		/// <para>
		/// The configuration file will be monitored using a <see cref="FileSystemWatcher"/>
		/// and depends on the behavior of that class.
		/// </para>
		/// <para>
		/// For more information on how to configure log4net using
		/// a separate configuration file, see <see cref="Configure(FileInfo)"/>.
		/// </para>
		/// </remarks>
		/// <seealso cref="Configure(FileInfo)"/>
		static public void ConfigureAndWatch(FileInfo configFile)
		{
			ConfigureAndWatch(LogManager.GetRepository(Assembly.GetCallingAssembly()), configFile);
		}

		/// <summary>
		/// Configures the <see cref="ILoggerRepository"/> using the file specified, 
		/// monitors the file for changes and reloads the configuration if a change 
		/// is detected.
		/// </summary>
		/// <param name="repository">The repository to configure.</param>
		/// <param name="configFile">The XML file to load the configuration from.</param>
		/// <remarks>
		/// <para>
		/// The configuration file must be valid XML. It must contain
		/// at least one element called <c>log4net</c> that holds
		/// the configuration data.
		/// </para>
		/// <para>
		/// The configuration file will be monitored using a <see cref="FileSystemWatcher"/>
		/// and depends on the behavior of that class.
		/// </para>
		/// <para>
		/// For more information on how to configure log4net using
		/// a separate configuration file, see <see cref="Configure(FileInfo)"/>.
		/// </para>
		/// </remarks>
		/// <seealso cref="Configure(FileInfo)"/>
		static public void ConfigureAndWatch(ILoggerRepository repository, FileInfo configFile)
		{
			LogLog.Debug("XmlConfigurator: configuring repository [" + repository.Name + "] using file [" + configFile + "] watching for file updates");

			if (configFile == null)
			{
				LogLog.Error("XmlConfigurator: ConfigureAndWatch called with null 'configFile' parameter");
			}
			else
			{
				// Configure log4net now
				Configure(repository, configFile);

				try
				{
					// Create a watch handler that will reload the
					// configuration whenever the config file is modified.
					new ConfigureAndWatchHandler(repository, configFile);
				}
				catch(Exception ex)
				{
					LogLog.Error("Failed to initialize configuration file watcher for file ["+configFile.FullName+"]", ex);
				}
			}
		}
#endif

		#endregion ConfigureAndWatch static methods

		#region ConfigureAndWatchHandler

#if (!NETCF && !SSCLI)
		/// <summary>
		/// Class used to watch config files.
		/// </summary>
		/// <remarks>
		/// Uses the <see cref="FileSystemWatcher"/> to monitor
		/// changes to a specified file. Because multiple change notifications
		/// may be raised when the file is modified, a timer is used to
		/// compress the notifications into a single event. The timer
		/// waits for <see cref="TimeoutMillis"/> time before delivering
		/// the event notification. If any further <see cref="FileSystemWatcher"/>
		/// change notifications arrive while the timer is waiting it
		/// is reset and waits again for <see cref="TimeoutMillis"/> to
		/// elapse.
		/// </remarks>
		private sealed class ConfigureAndWatchHandler
		{
			/// <summary>
			/// Holds the FileInfo used to configure the XmlConfigurator
			/// </summary>
			private FileInfo m_configFile;

			/// <summary>
			/// Holds the repository being configured.
			/// </summary>
			private ILoggerRepository m_repository;

			/// <summary>
			/// The timer used to compress the notification events.
			/// </summary>
			private Timer m_timer;

			/// <summary>
			/// The default amount of time to wait after receiving notification
			/// before reloading the config file.
			/// </summary>
			private const int TimeoutMillis = 500;

			/// <summary>
			/// Initializes a new instance of the <see cref="ConfigureAndWatchHandler" /> class.
			/// </summary>
			/// <param name="repository">The repository to configure.</param>
			/// <param name="configFile">The configuration file to watch.</param>
			internal ConfigureAndWatchHandler(ILoggerRepository repository, FileInfo configFile)
			{
				m_repository = repository;
				m_configFile = configFile;

				// Create a new FileSystemWatcher and set its properties.
				FileSystemWatcher watcher = new FileSystemWatcher();

				watcher.Path = m_configFile.DirectoryName;
				watcher.Filter = m_configFile.Name;

				// Set the notification filters
				watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName;

				// Add event handlers. OnChanged will do for all event handlers that fire a FileSystemEventArgs
				watcher.Changed += new FileSystemEventHandler(ConfigureAndWatchHandler_OnChanged);
				watcher.Created += new FileSystemEventHandler(ConfigureAndWatchHandler_OnChanged);
				watcher.Deleted += new FileSystemEventHandler(ConfigureAndWatchHandler_OnChanged);
				watcher.Renamed += new RenamedEventHandler(ConfigureAndWatchHandler_OnRenamed);

				// Begin watching.
				watcher.EnableRaisingEvents = true;

				// Create the timer that will be used to deliver events. Set as disabled
				m_timer = new Timer(new TimerCallback(OnWatchedFileChange), null, Timeout.Infinite, Timeout.Infinite);
			}

			/// <summary>
			/// Event handler used by <see cref="ConfigureAndWatchHandler"/>.
			/// </summary>
			/// <param name="source">The <see cref="FileSystemWatcher"/> firing the event.</param>
			/// <param name="e">The argument indicates the file that caused the event to be fired.</param>
			/// <remarks>
			/// This handler reloads the configuration from the file when the event is fired.
			/// </remarks>
			private void ConfigureAndWatchHandler_OnChanged(object source, FileSystemEventArgs e)
			{
				LogLog.Debug("ConfigureAndWatchHandler: "+e.ChangeType+" [" + m_configFile.FullName + "]");

				// Deliver the event in TimeoutMillis time
				// timer will fire only once
				m_timer.Change(TimeoutMillis, Timeout.Infinite);
			}

			/// <summary>
			/// Event handler used by <see cref="ConfigureAndWatchHandler"/>.
			/// </summary>
			/// <param name="source">The <see cref="FileSystemWatcher"/> firing the event.</param>
			/// <param name="e">The argument indicates the file that caused the event to be fired.</param>
			/// <remarks>
			/// This handler reloads the configuration from the file when the event is fired.
			/// </remarks>
			private void ConfigureAndWatchHandler_OnRenamed(object source, RenamedEventArgs e)
			{
				LogLog.Debug("ConfigureAndWatchHandler: " + e.ChangeType + " [" + m_configFile.FullName + "]");

				// Deliver the event in TimeoutMillis time
				// timer will fire only once
				m_timer.Change(TimeoutMillis, Timeout.Infinite);
			}

			/// <summary>
			/// Called by the timer when the configuration has been updated.
			/// </summary>
			/// <param name="state">null</param>
			private void OnWatchedFileChange(object state)
			{
				XmlConfigurator.Configure(m_repository, m_configFile);
			}
		}
#endif

		#endregion ConfigureAndWatchHandler

		#region Private Static Methods

		/// <summary>
		/// Configures the specified repository using a <c>log4net</c> element.
		/// </summary>
		/// <param name="repository">The hierarchy to configure.</param>
		/// <param name="element">The element to parse.</param>
		/// <remarks>
		/// <para>
		/// Loads the log4net configuration from the XML element
		/// supplied as <paramref name="element"/>.
		/// </para>
		/// <para>
		/// This method is ultimately called by one of the Configure methods 
		/// to load the configuration from an <see cref="XmlElement"/>.
		/// </para>
		/// </remarks>
		static private void ConfigureFromXml(ILoggerRepository repository, XmlElement element) 
		{
			if (element == null)
			{
				LogLog.Error("XmlConfigurator: ConfigureFromXml called with null 'element' parameter");
			}
			else if (repository == null)
			{
				LogLog.Error("XmlConfigurator: ConfigureFromXml called with null 'repository' parameter");
			}
			else
			{
				LogLog.Debug("XmlConfigurator: Configuring Repository [" + repository.Name + "]");

				// Copy the xml data into the root of a new document
				// this isolates the xml config data from the rest of
				// the document
				XmlDocument newDoc = new XmlDocument();
				XmlElement newElement = (XmlElement) newDoc.AppendChild(newDoc.ImportNode(element, true));

				// Check the type of the repository
				if (repository is IXmlRepositoryConfigurator)
				{
					// Pass the configurator the config element
					((IXmlRepositoryConfigurator) repository).Configure(newElement);
				}
				else
				{
					LogLog.Warn("Repository [" + repository + "] does not support the XmlConfigurator");
				}
			}
		}

		#endregion Private Static Methods
	}
}

