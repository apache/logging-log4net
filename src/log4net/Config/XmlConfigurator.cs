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
using System.Xml;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Net;

using log4net.Util;
using log4net.Repository;
using System.Collections.Generic;

namespace log4net.Config
{
  /// <summary>
  /// Configures a <see cref="ILoggerRepository"/> using an XML tree.
  /// </summary>
  /// <author>Nicko Cadell</author>
  /// <author>Gert Driesen</author>
  public static class XmlConfigurator
  {
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
    public static ICollection Configure(ILoggerRepository repository)
    {
      var configurationMessages = new List<LogLog>();

      using (new LogLog.LogReceivedAdapter(configurationMessages))
      {
        InternalConfigure(repository);
      }

      repository.ConfigurationMessages = configurationMessages;

      return configurationMessages;
    }

    private static void InternalConfigure(ILoggerRepository repository)
    {
      LogLog.Debug(declaringType, $"configuring repository [{repository.Name}] using .config file section");

      try
      {
        LogLog.Debug(declaringType, $"Application config file is [{SystemInfo.ConfigurationFileLocation}]");
      }
      catch
      {
        // ignore error
        LogLog.Debug(declaringType, "Application config file location unknown");
      }

      try
      {
        if (System.Configuration.ConfigurationManager.GetSection("log4net") is not XmlElement configElement)
        {
          // Failed to load the xml config using configuration settings handler
          LogLog.Error(declaringType, "Failed to find configuration section 'log4net' in the application's .config file. Check your .config file for the <log4net> and <configSections> elements. The configuration section should look like: <section name=\"log4net\" type=\"log4net.Config.Log4NetConfigurationSectionHandler,log4net\" />");
        }
        else
        {
          // Configure using the xml loaded from the config file
          InternalConfigureFromXml(repository, configElement);
        }
      }
      catch (System.Configuration.ConfigurationException confEx)
      {
        if (confEx.BareMessage.IndexOf("Unrecognized element", StringComparison.Ordinal) >= 0)
        {
          // Looks like the XML file is not valid
          LogLog.Error(declaringType, "Failed to parse config file. Check your .config file is well formed XML.", confEx);
        }
        else
        {
          // This exception is typically due to the assembly name not being correctly specified in the section type.
          string configSectionStr = "<section name=\"log4net\" type=\"log4net.Config.Log4NetConfigurationSectionHandler," + Assembly.GetExecutingAssembly().FullName + "\" />";
          LogLog.Error(declaringType, "Failed to parse config file. Is the <configSections> specified as: " + configSectionStr, confEx);
        }
      }
    }

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
    public static ICollection Configure()
    {
      return Configure(LogManager.GetRepository(Assembly.GetCallingAssembly()));
    }

    /// <summary>
    /// Configures log4net using a <c>log4net</c> element
    /// </summary>
    /// <remarks>
    /// <para>
    /// Loads the log4net configuration from the XML element
    /// supplied as <paramref name="element"/>.
    /// </para>
    /// </remarks>
    /// <param name="element">The element to parse.</param>
    public static ICollection Configure(XmlElement element)
    {
      var configurationMessages = new List<LogLog>();

      ILoggerRepository repository = LogManager.GetRepository(Assembly.GetCallingAssembly());

      using (new LogLog.LogReceivedAdapter(configurationMessages))
      {
        InternalConfigureFromXml(repository, element);
      }

      repository.ConfigurationMessages = configurationMessages;

      return configurationMessages;
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
    /// The log4net configuration file can possibly be specified in the application's
    /// configuration file (either <c>MyAppName.exe.config</c> for a
    /// normal application on <c>Web.config</c> for an ASP.NET application).
    /// </para>
    /// <para>
    /// The first element matching <c>&lt;configuration&gt;</c> will be read as the 
    /// configuration. If this file is also a .NET .config file then you must specify 
    /// a configuration section for the <c>log4net</c> element otherwise .NET will 
    /// complain. Set the type for the section handler to <see cref="System.Configuration.IgnoreSectionHandler"/>, for example:
    /// <code lang="XML" escaped="true">
    /// <configSections>
    ///    <section name="log4net" type="System.Configuration.IgnoreSectionHandler" />
    ///  </configSections>
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
    /// <code lang="XML" escaped="true">
    /// <configuration>
    ///    <appSettings>
    ///      <add key="log4net-config-file" value="log.config"/>
    ///    </appSettings>
    ///  </configuration>
    /// </code>
    /// </remarks>
    public static ICollection Configure(FileInfo configFile)
    {
      var configurationMessages = new List<LogLog>();

      using (new LogLog.LogReceivedAdapter(configurationMessages))
      {
        InternalConfigure(LogManager.GetRepository(Assembly.GetCallingAssembly()), configFile);
      }

      return configurationMessages;
    }

    /// <summary>
    /// Configures log4net using the specified configuration URI.
    /// </summary>
    /// <param name="configUri">A URI to load the XML configuration from.</param>
    /// <remarks>
    /// <para>
    /// The configuration data must be valid XML. It must contain
    /// at least one element called <c>log4net</c> that holds
    /// the log4net configuration data.
    /// </para>
    /// <para>
    /// The <see cref="System.Net.WebRequest"/> must support the URI scheme specified.
    /// </para>
    /// </remarks>
    public static ICollection Configure(Uri configUri)
    {
      var configurationMessages = new List<LogLog>();

      ILoggerRepository repository = LogManager.GetRepository(Assembly.GetCallingAssembly());
      using (new LogLog.LogReceivedAdapter(configurationMessages))
      {
        InternalConfigure(repository, configUri);
      }

      repository.ConfigurationMessages = configurationMessages;

      return configurationMessages;
    }

    /// <summary>
    /// Configures log4net using the specified configuration data stream.
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
    public static ICollection Configure(Stream configStream)
    {
      var configurationMessages = new List<LogLog>();

      ILoggerRepository repository = LogManager.GetRepository(Assembly.GetCallingAssembly());
      using (new LogLog.LogReceivedAdapter(configurationMessages))
      {
        InternalConfigure(repository, configStream);
      }

      repository.ConfigurationMessages = configurationMessages;

      return configurationMessages;
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
    public static ICollection Configure(ILoggerRepository repository, XmlElement element)
    {
      var configurationMessages = new List<LogLog>();

      using (new LogLog.LogReceivedAdapter(configurationMessages))
      {
        LogLog.Debug(declaringType, "configuring repository [" + repository.Name + "] using XML element");

        InternalConfigureFromXml(repository, element);
      }

      repository.ConfigurationMessages = configurationMessages;

      return configurationMessages;
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
    /// The log4net configuration file can possibly be specified in the application's
    /// configuration file (either <c>MyAppName.exe.config</c> for a
    /// normal application on <c>Web.config</c> for an ASP.NET application).
    /// </para>
    /// <para>
    /// The first element matching <c>&lt;configuration&gt;</c> will be read as the 
    /// configuration. If this file is also a .NET .config file then you must specify 
    /// a configuration section for the <c>log4net</c> element otherwise .NET will 
    /// complain. Set the type for the section handler to <see cref="System.Configuration.IgnoreSectionHandler"/>, for example:
    /// <code lang="XML" escaped="true">
    /// <configSections>
    ///    <section name="log4net" type="System.Configuration.IgnoreSectionHandler" />
    ///  </configSections>
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
    /// <code lang="XML" escaped="true">
    /// <configuration>
    ///    <appSettings>
    ///      <add key="log4net-config-file" value="log.config"/>
    ///    </appSettings>
    ///  </configuration>
    /// </code>
    /// </remarks>
    public static ICollection Configure(ILoggerRepository repository, FileInfo configFile)
    {
      var configurationMessages = new List<LogLog>();

      using (new LogLog.LogReceivedAdapter(configurationMessages))
      {
        InternalConfigure(repository, configFile);
      }

      repository.ConfigurationMessages = configurationMessages;

      return configurationMessages;
    }

    private static void InternalConfigure(ILoggerRepository repository, FileInfo? configFile)
    {
      LogLog.Debug(declaringType, $"configuring repository [{repository.Name}] using file [{configFile}]");

      if (configFile is null)
      {
        LogLog.Error(declaringType, "Configure called with null 'configFile' parameter");
      }
      else
      {
        // Have to use File.Exists() rather than configFile.Exists()
        // because configFile.Exists() caches the value, not what we want.
        if (File.Exists(configFile.FullName))
        {
          // Open the file for reading
          FileStream? fs = null;

          // Try hard to open the file
          for (int retry = 5; --retry >= 0;)
          {
            try
            {
              fs = configFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
              break;
            }
            catch (IOException ex)
            {
              if (retry == 0)
              {
                LogLog.Error(declaringType, $"Failed to open XML config file [{configFile.Name}]", ex);

                // The stream cannot be valid
                fs = null;
              }
              Thread.Sleep(250);
            }
          }

          if (fs is not null)
          {
            try
            {
              // Load the configuration from the stream
              InternalConfigure(repository, fs);
            }
            finally
            {
              // Force the file closed whatever happens
              fs.Dispose();
            }
          }
        }
        else
        {
          LogLog.Debug(declaringType, "config file [" + configFile.FullName + "] not found. Configuration unchanged.");
        }
      }
    }

    /// <summary>
    /// Configures the <see cref="ILoggerRepository"/> using the specified configuration 
    /// URI.
    /// </summary>
    /// <param name="repository">The repository to configure.</param>
    /// <param name="configUri">A URI to load the XML configuration from.</param>
    /// <remarks>
    /// <para>
    /// The configuration data must be valid XML. It must contain
    /// at least one element called <c>log4net</c> that holds
    /// the configuration data.
    /// </para>
    /// <para>
    /// The <see cref="System.Net.WebRequest"/> must support the URI scheme specified.
    /// </para>
    /// </remarks>
    public static ICollection Configure(ILoggerRepository repository, Uri configUri)
    {
      var configurationMessages = new List<LogLog>();

      using (new LogLog.LogReceivedAdapter(configurationMessages))
      {
        InternalConfigure(repository, configUri);
      }

      repository.ConfigurationMessages = configurationMessages;

      return configurationMessages;
    }

    private static void InternalConfigure(ILoggerRepository repository, Uri? configUri)
    {
      LogLog.Debug(declaringType, $"configuring repository [{repository.Name}] using URI [{configUri}]");

      if (configUri is null)
      {
        LogLog.Error(declaringType, "Configure called with null 'configUri' parameter");
      }
      else
      {
        if (configUri.IsFile)
        {
          // If URI is local file then call Configure with FileInfo
          InternalConfigure(repository, new FileInfo(configUri.LocalPath));
        }
        else
        {
          // NETCF dose not support WebClient
          WebRequest? configRequest = null;

          try
          {
            configRequest = WebRequest.Create(configUri);
          }
          catch (Exception ex)
          {
            LogLog.Error(declaringType, $"Failed to create WebRequest for URI [{configUri}]", ex);
          }

          if (configRequest is not null)
          {
            // authentication may be required, set client to use default credentials
            try
            {
              configRequest.Credentials = CredentialCache.DefaultCredentials;
            }
            catch
            {
              // ignore security exception
            }
            try
            {
              using WebResponse? response = configRequest.GetResponse();
              if (response is not null)
              {
                using var configStream = response.GetResponseStream();
                InternalConfigure(repository, configStream);
              }
            }
            catch (Exception ex)
            {
              LogLog.Error(declaringType, $"Failed to request config from URI [{configUri}]", ex);
            }
          }
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
    public static ICollection Configure(ILoggerRepository repository, Stream configStream)
    {
      var configurationMessages = new List<LogLog>();

      using (new LogLog.LogReceivedAdapter(configurationMessages))
      {
        InternalConfigure(repository, configStream);
      }

      repository.ConfigurationMessages = configurationMessages;

      return configurationMessages;
    }

    private static void InternalConfigure(ILoggerRepository repository, Stream? configStream)
    {
      LogLog.Debug(declaringType, $"configuring repository [{repository.Name}] using stream");

      if (configStream is null)
      {
        LogLog.Error(declaringType, "Configure called with null 'configStream' parameter");
      }
      else
      {
        // Load the config file into a document
        XmlDocument? doc = new XmlDocument { XmlResolver = null };
        try
        {
          // Allow the DTD to specify entity includes
          var settings = new XmlReaderSettings();
          // .NET 4.0 warning CS0618: 'System.Xml.XmlReaderSettings.ProhibitDtd'
          // is obsolete: 'Use XmlReaderSettings.DtdProcessing property instead.'
          settings.DtdProcessing = DtdProcessing.Ignore;

          // Create a reader over the input stream
          using XmlReader xmlReader = XmlReader.Create(configStream, settings);

          // load the data into the document
          doc.Load(xmlReader);
        }
        catch (Exception ex)
        {
          LogLog.Error(declaringType, "Error while loading XML configuration", ex);

          // The document is invalid
          doc = null;
        }

        if (doc is not null)
        {
          LogLog.Debug(declaringType, "loading XML configuration");

          // Configure using the 'log4net' element
          XmlNodeList configNodeList = doc.GetElementsByTagName("log4net");
          if (configNodeList.Count == 0)
          {
            LogLog.Debug(declaringType, "XML configuration does not contain a <log4net> element. Configuration Aborted.");
          }
          else if (configNodeList.Count > 1)
          {
            LogLog.Error(declaringType, $"XML configuration contains [{configNodeList.Count}] <log4net> elements. Only one is allowed. Configuration Aborted.");
          }
          else
          {
            InternalConfigureFromXml(repository, configNodeList[0] as XmlElement);
          }
        }
      }
    }

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
    /// a separate configuration file, see <see cref="M:Configure(FileInfo)"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="M:Configure(FileInfo)"/>
    public static ICollection ConfigureAndWatch(FileInfo configFile)
    {
      var configurationMessages = new List<LogLog>();

      ILoggerRepository repository = LogManager.GetRepository(Assembly.GetCallingAssembly());

      using (new LogLog.LogReceivedAdapter(configurationMessages))
      {
        InternalConfigureAndWatch(repository, configFile);
      }

      repository.ConfigurationMessages = configurationMessages;

      return configurationMessages;
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
    /// a separate configuration file, see <see cref="M:Configure(FileInfo)"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="M:Configure(FileInfo)"/>
    public static ICollection ConfigureAndWatch(ILoggerRepository repository, FileInfo configFile)
    {
      var configurationMessages = new List<LogLog>();

      using (new LogLog.LogReceivedAdapter(configurationMessages))
      {
        InternalConfigureAndWatch(repository, configFile);
      }

      repository.ConfigurationMessages = configurationMessages;

      return configurationMessages;
    }

    private static void InternalConfigureAndWatch(ILoggerRepository repository, FileInfo? configFile)
    {
      LogLog.Debug(declaringType, $"configuring repository [{repository.Name}] using file [{configFile}] watching for file updates");

      if (configFile is null)
      {
        LogLog.Error(declaringType, "ConfigureAndWatch called with null 'configFile' parameter");
      }
      else
      {
        // Configure log4net now
        InternalConfigure(repository, configFile);

        try
        {
          // Support multiple repositories each having their own watcher.
          // Create and start a watch handler that will reload the
          // configuration whenever the config file is modified.
          m_repositoryName2ConfigAndWatchHandler.AddOrUpdate(
            configFile.FullName,
            _ => new ConfigureAndWatchHandler(repository, configFile),
            (_, handler) =>
            {
              // Replace the old handler.
              handler.Dispose();
              return new ConfigureAndWatchHandler(repository, configFile);
            });
        }
        catch (Exception ex)
        {
          LogLog.Error(declaringType, $"Failed to initialize configuration file watcher for file [{configFile.FullName}]", ex);
        }
      }
    }

    /// <summary>
    /// Class used to watch config files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses the <see cref="FileSystemWatcher"/> to monitor
    /// changes to a specified file. Because multiple change notifications
    /// may be raised when the file is modified, a timer is used to
    /// compress the notifications into a single event. The timer
    /// waits for <see cref="TimeoutMillis"/> time before delivering
    /// the event notification. If any further <see cref="FileSystemWatcher"/>
    /// change notifications arrive while the timer is waiting it
    /// is reset and waits again for <see cref="TimeoutMillis"/> to
    /// elapse.
    /// </para>
    /// </remarks>
    private sealed class ConfigureAndWatchHandler : IDisposable
    {
      /// <summary>
      /// Holds the FileInfo used to configure the XmlConfigurator
      /// </summary>
      private readonly FileInfo m_configFile;

      /// <summary>
      /// Holds the repository being configured.
      /// </summary>
      private readonly ILoggerRepository m_repository;

      /// <summary>
      /// The timer used to compress the notification events.
      /// </summary>
      private readonly Timer m_timer;

      /// <summary>
      /// The default amount of time to wait after receiving notification
      /// before reloading the config file.
      /// </summary>
      private const int TimeoutMillis = 500;

      /// <summary>
      /// Watches file for changes. This object should be disposed when no longer
      /// needed to free system handles on the watched resources.
      /// </summary>
      private readonly FileSystemWatcher m_watcher;

      /// <summary>
      /// Initializes a new instance of the <see cref="ConfigureAndWatchHandler" /> class to
      /// watch a specified config file used to configure a repository.
      /// </summary>
      /// <param name="repository">The repository to configure.</param>
      /// <param name="configFile">The configuration file to watch.</param>
      /// <remarks>
      /// <para>
      /// Initializes a new instance of the <see cref="ConfigureAndWatchHandler" /> class.
      /// </para>
      /// </remarks>
      [System.Security.SecuritySafeCritical]
      public ConfigureAndWatchHandler(ILoggerRepository repository, FileInfo configFile)
      {
        m_repository = repository;
        m_configFile = configFile;

        // Create a new FileSystemWatcher and set its properties.
        m_watcher = new FileSystemWatcher()
        {
          Path = m_configFile.DirectoryName,
          Filter = m_configFile.Name,
          NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName,
        };

        // Add event handlers. OnChanged will do for all event handlers that fire a FileSystemEventArgs
        m_watcher.Changed += ConfigureAndWatchHandler_OnChanged;
        m_watcher.Created += ConfigureAndWatchHandler_OnChanged;
        m_watcher.Deleted += ConfigureAndWatchHandler_OnChanged;
        m_watcher.Renamed += ConfigureAndWatchHandler_OnRenamed;

        // Begin watching.
        m_watcher.EnableRaisingEvents = true;

        // Create the timer that will be used to deliver events. Set as disabled
        m_timer = new Timer(OnWatchedFileChange, state: null, Timeout.Infinite, Timeout.Infinite);
      }

      /// <summary>
      /// Event handler used by <see cref="ConfigureAndWatchHandler"/>.
      /// </summary>
      /// <param name="source">The <see cref="FileSystemWatcher"/> firing the event.</param>
      /// <param name="e">The argument indicates the file that caused the event to be fired.</param>
      /// <remarks>
      /// <para>
      /// This handler reloads the configuration from the file when the event is fired.
      /// </para>
      /// </remarks>
      private void ConfigureAndWatchHandler_OnChanged(object source, FileSystemEventArgs e)
      {
        LogLog.Debug(declaringType, $"ConfigureAndWatchHandler: {e.ChangeType} [{m_configFile.FullName}]");

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
      /// <para>
      /// This handler reloads the configuration from the file when the event is fired.
      /// </para>
      /// </remarks>
      private void ConfigureAndWatchHandler_OnRenamed(object source, RenamedEventArgs e)
      {
        LogLog.Debug(declaringType, $"ConfigureAndWatchHandler: {e.ChangeType} [{m_configFile.FullName}]");

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
        XmlConfigurator.InternalConfigure(m_repository, m_configFile);
      }

      /// <summary>
      /// Release the handles held by the watcher and timer.
      /// </summary>
      [System.Security.SecuritySafeCritical]
      public void Dispose()
      {
        m_watcher.EnableRaisingEvents = false;
        m_watcher.Dispose();
        m_timer.Dispose();
      }
    }

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
    private static void InternalConfigureFromXml(ILoggerRepository? repository, XmlElement? element)
    {
      if (element is null)
      {
        LogLog.Error(declaringType, "ConfigureFromXml called with null 'element' parameter");
      }
      else if (repository is null)
      {
        LogLog.Error(declaringType, "ConfigureFromXml called with null 'repository' parameter");
      }
      else
      {
        LogLog.Debug(declaringType, "Configuring Repository [" + repository.Name + "]");

        if (repository is not IXmlRepositoryConfigurator configurableRepository)
        {
          LogLog.Warn(declaringType, "Repository [" + repository + "] does not support the XmlConfigurator");
        }
        else
        {
          // Copy the xml data into the root of a new document
          // this isolates the xml config data from the rest of
          // the document
          XmlDocument newDoc = new XmlDocument { XmlResolver = null };
          XmlElement newElement = (XmlElement)newDoc.AppendChild(newDoc.ImportNode(element, true));

          // Pass the configurator the config element
          configurableRepository.Configure(newElement);
        }
      }
    }

    /// <summary>
    /// Maps repository names to ConfigAndWatchHandler instances to allow a particular
    /// ConfigAndWatchHandler to dispose of its FileSystemWatcher when a repository is 
    /// reconfigured.
    /// </summary>
    private static readonly ConcurrentDictionary<string, ConfigureAndWatchHandler> m_repositoryName2ConfigAndWatchHandler = new(StringComparer.Ordinal);

    /// <summary>
    /// The fully qualified type of the XmlConfigurator class.
    /// </summary>
    /// <remarks>
    /// Used by the internal logger to record the Type of the
    /// log message.
    /// </remarks>
    private static readonly Type declaringType = typeof(XmlConfigurator);
  }
}
