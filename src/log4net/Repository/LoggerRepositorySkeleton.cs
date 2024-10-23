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
using System.Collections;
using log4net.ObjectRenderer;
using log4net.Core;
using log4net.Util;
using log4net.Plugin;
using System.Threading;
using log4net.Appender;

namespace log4net.Repository;

/// <summary>
/// Base implementation of <see cref="ILoggerRepository"/>
/// </summary>
/// <remarks>
/// <para>
/// Default abstract implementation of the <see cref="ILoggerRepository"/> interface.
/// </para>
/// <para>
/// Skeleton implementation of the <see cref="ILoggerRepository"/> interface.
/// All <see cref="ILoggerRepository"/> types can extend this type.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
public abstract class LoggerRepositorySkeleton : ILoggerRepository, IFlushable
{
  private readonly RendererMap _rendererMap = new();
  private readonly LevelMap _levelMap = new();
  private Level _threshold = Level.All;  // Don't disable any levels by default.
  private ICollection _configurationMessages = EmptyCollection.Instance;

  /// <summary>
  /// Default Constructor
  /// </summary>
  /// <remarks>
  /// <para>
  /// Initializes the repository with default (empty) properties.
  /// </para>
  /// </remarks>
  protected LoggerRepositorySkeleton()
    : this([])
  {
  }

  /// <summary>
  /// Construct the repository using specific properties
  /// </summary>
  /// <param name="properties">the properties to set for this repository</param>
  /// <remarks>
  /// <para>
  /// Initializes the repository with specified properties.
  /// </para>
  /// </remarks>
  protected LoggerRepositorySkeleton(PropertiesDictionary properties)
  {
    Properties = properties;
    PluginMap = new PluginMap(this);
    AddBuiltinLevels();
  }

  /// <summary>
  /// The name of the repository
  /// </summary>
  /// <value>
  /// The string name of the repository
  /// </value>
  /// <remarks>
  /// <para>
  /// The name of this repository. The name is
  /// used to store and lookup the repositories 
  /// stored by the <see cref="IRepositorySelector"/>.
  /// </para>
  /// </remarks>
  public virtual string Name { get; set; } = string.Empty;

  /// <summary>
  /// The threshold for all events in this repository
  /// </summary>
  /// <value>
  /// The threshold for all events in this repository
  /// </value>
  /// <remarks>
  /// <para>
  /// The threshold for all events in this repository
  /// </para>
  /// </remarks>
  public virtual Level Threshold
  {
    get => _threshold;
    set
    {
      if (value is not null)
      {
        _threshold = value;
      }
      else
      {
        // Must not set threshold to null
        LogLog.Warn(_declaringType, "LoggerRepositorySkeleton: Threshold cannot be set to null. Setting to ALL");
        _threshold = Level.All;
      }
    }
  }

  /// <summary>
  /// RendererMap accesses the object renderer map for this repository.
  /// </summary>
  /// <value>
  /// RendererMap accesses the object renderer map for this repository.
  /// </value>
  /// <remarks>
  /// <para>
  /// RendererMap accesses the object renderer map for this repository.
  /// </para>
  /// <para>
  /// The RendererMap holds a mapping between types and
  /// <see cref="IObjectRenderer"/> objects.
  /// </para>
  /// </remarks>
  public virtual RendererMap RendererMap => _rendererMap;

  /// <summary>
  /// The plugin map for this repository.
  /// </summary>
  /// <value>
  /// The plugin map for this repository.
  /// </value>
  /// <remarks>
  /// <para>
  /// The plugin map holds the <see cref="IPlugin"/> instances
  /// that have been attached to this repository.
  /// </para>
  /// </remarks>
  public virtual PluginMap PluginMap { get; }

  /// <summary>
  /// Get the level map for the Repository.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Get the level map for the Repository.
  /// </para>
  /// <para>
  /// The level map defines the mappings between
  /// level names and <see cref="Level"/> objects in
  /// this repository.
  /// </para>
  /// </remarks>
  public virtual LevelMap LevelMap => _levelMap;

  /// <summary>
  /// Test if logger exists
  /// </summary>
  /// <param name="name">The name of the logger to lookup</param>
  /// <returns>The Logger object with the name specified</returns>
  /// <remarks>
  /// <para>
  /// Check if the named logger exists in the repository. If so return
  /// its reference, otherwise returns <c>null</c>.
  /// </para>
  /// </remarks>
  public abstract ILogger? Exists(string name);

  /// <summary>
  /// Returns all the currently defined loggers in the repository
  /// </summary>
  /// <returns>All the defined loggers</returns>
  /// <remarks>
  /// <para>
  /// Returns all the currently defined loggers in the repository as an Array.
  /// </para>
  /// </remarks>
  public abstract ILogger[] GetCurrentLoggers();

  /// <summary>
  /// Return a new logger instance
  /// </summary>
  /// <param name="name">The name of the logger to retrieve</param>
  /// <returns>The logger object with the name specified</returns>
  /// <remarks>
  /// <para>
  /// Return a new logger instance.
  /// </para>
  /// <para>
  /// If a logger of that name already exists, then it will be
  /// returned. Otherwise, a new logger will be instantiated and
  /// then linked with its existing ancestors as well as children.
  /// </para>
  /// </remarks>
  public abstract ILogger GetLogger(string name);

  /// <summary>
  /// Shutdown the repository
  /// </summary>
  /// <remarks>
  /// <para>
  /// Shutdown the repository. Can be overridden in a subclass.
  /// This base class implementation notifies the <see cref="ShutdownEvent"/>
  /// listeners and all attached plugins of the shutdown event.
  /// </para>
  /// </remarks>
  public virtual void Shutdown()
  {
    // Shutdown attached plugins
    foreach (IPlugin plugin in PluginMap.AllPlugins)
    {
      plugin.Shutdown();
    }

    // Notify listeners
    OnShutdown(null);
  }

  /// <summary>
  /// Reset the repositories configuration to a default state
  /// </summary>
  /// <remarks>
  /// <para>
  /// Reset all values contained in this instance to their
  /// default state.
  /// </para>
  /// <para>
  /// Existing loggers are not removed. They are just reset.
  /// </para>
  /// <para>
  /// This method should be used sparingly and with care as it will
  /// block all logging until it is completed.
  /// </para>
  /// </remarks>
  public virtual void ResetConfiguration()
  {
    // Clear internal data structures
    _rendererMap.Clear();
    _levelMap.Clear();
    _configurationMessages = EmptyCollection.Instance;

    // Add the predefined levels to the map
    AddBuiltinLevels();

    Configured = false;

    // Notify listeners
    OnConfigurationReset(null);
  }

  /// <summary>
  /// Log the logEvent through this repository.
  /// </summary>
  /// <param name="logEvent">the event to log</param>
  /// <remarks>
  /// <para>
  /// This method should not normally be used to log.
  /// The <see cref="ILog"/> interface should be used 
  /// for routine logging. This interface can be obtained
  /// using the <see cref="log4net.LogManager.GetLogger(string)"/> method.
  /// </para>
  /// <para>
  /// The <c>logEvent</c> is delivered to the appropriate logger and
  /// that logger is then responsible for logging the event.
  /// </para>
  /// </remarks>
  public abstract void Log(LoggingEvent logEvent);

  /// <summary>
  /// Flag indicates if this repository has been configured.
  /// </summary>
  public virtual bool Configured { get; set; }

  /// <summary>
  /// Contains a list of internal messages captured during the 
  /// last configuration.
  /// </summary>
  public virtual ICollection ConfigurationMessages
  {
    get => _configurationMessages;
    set => _configurationMessages = value;
  }

  /// <summary>
  /// Event to notify that the repository has been shutdown.
  /// </summary>
  /// <value>
  /// Event to notify that the repository has been shutdown.
  /// </value>
  /// <remarks>
  /// <para>
  /// Event raised when the repository has been shutdown.
  /// </para>
  /// </remarks>
  public event LoggerRepositoryShutdownEventHandler? ShutdownEvent;

  /// <summary>
  /// Event to notify that the repository has had its configuration reset.
  /// </summary>
  /// <value>
  /// Event to notify that the repository has had its configuration reset.
  /// </value>
  /// <remarks>
  /// <para>
  /// Event raised when the repository's configuration has been
  /// reset to default.
  /// </para>
  /// </remarks>
  public event LoggerRepositoryConfigurationResetEventHandler? ConfigurationReset;

  /// <summary>
  /// Event to notify that the repository has had its configuration changed.
  /// </summary>
  /// <value>
  /// Event to notify that the repository has had its configuration changed.
  /// </value>
  /// <remarks>
  /// <para>
  /// Event raised when the repository's configuration has been changed.
  /// </para>
  /// </remarks>
  public event LoggerRepositoryConfigurationChangedEventHandler? ConfigurationChanged;

  /// <summary>
  /// Repository specific properties
  /// </summary>
  /// <value>
  /// Repository specific properties
  /// </value>
  /// <remarks>
  /// These properties can be specified on a repository specific basis
  /// </remarks>
  public PropertiesDictionary Properties { get; }

  /// <summary>
  /// Returns all the Appenders that are configured as an Array.
  /// </summary>
  /// <returns>All the Appenders</returns>
  /// <remarks>
  /// <para>
  /// Returns all the Appenders that are configured as an Array.
  /// </para>
  /// </remarks>
  public abstract IAppender[] GetAppenders();

  /// <summary>
  /// The fully qualified type of the LoggerRepositorySkeleton class.
  /// </summary>
  /// <remarks>
  /// Used by the internal logger to record the Type of the
  /// log message.
  /// </remarks>
  private static readonly Type _declaringType = typeof(LoggerRepositorySkeleton);

  private void AddBuiltinLevels()
  {
    // Add the predefined levels to the map
    _levelMap.Add(Level.Off);

    // Unrecoverable errors
    _levelMap.Add(Level.Emergency);
    _levelMap.Add(Level.Fatal);
    _levelMap.Add(Level.Alert);

    // Recoverable errors
    _levelMap.Add(Level.Critical);
    _levelMap.Add(Level.Severe);
    _levelMap.Add(Level.Error);
    _levelMap.Add(Level.Warn);

    // Information
    _levelMap.Add(Level.Notice);
    _levelMap.Add(Level.Info);

    // Debug
    _levelMap.Add(Level.Debug);
    _levelMap.Add(Level.Fine);
    _levelMap.Add(Level.Trace);
    _levelMap.Add(Level.Finer);
    _levelMap.Add(Level.Verbose);
    _levelMap.Add(Level.Finest);

    _levelMap.Add(Level.All);
  }

  /// <summary>
  /// Adds an object renderer for a specific class. 
  /// </summary>
  /// <param name="typeToRender">The type that will be rendered by the renderer supplied.</param>
  /// <param name="rendererInstance">The object renderer used to render the object.</param>
  /// <remarks>
  /// <para>
  /// Adds an object renderer for a specific class. 
  /// </para>
  /// </remarks>
  public virtual void AddRenderer(Type typeToRender, IObjectRenderer rendererInstance) 
    => _rendererMap.Put(typeToRender.EnsureNotNull(), rendererInstance.EnsureNotNull());

  /// <summary>
  /// Notify the registered listeners that the repository is shutting down
  /// </summary>
  /// <param name="e">Empty EventArgs</param>
  /// <remarks>
  /// <para>
  /// Notify any listeners that this repository is shutting down.
  /// </para>
  /// </remarks>
  protected virtual void OnShutdown(EventArgs? e)
  {
    e ??= EventArgs.Empty;
    ShutdownEvent?.Invoke(this, e);
  }

  /// <summary>
  /// Notify the registered listeners that the repository has had its configuration reset
  /// </summary>
  /// <param name="e">Empty EventArgs</param>
  /// <remarks>
  /// <para>
  /// Notify any listeners that this repository's configuration has been reset.
  /// </para>
  /// </remarks>
  protected virtual void OnConfigurationReset(EventArgs? e)
    => ConfigurationReset?.Invoke(this, e ?? EventArgs.Empty);

  /// <summary>
  /// Notify the registered listeners that the repository has had its configuration changed
  /// </summary>
  /// <param name="e">Empty EventArgs</param>
  protected virtual void OnConfigurationChanged(EventArgs? e)
    => ConfigurationChanged?.Invoke(this, e ?? EventArgs.Empty);

  /// <summary>
  /// Raise a configuration changed event on this repository
  /// </summary>
  /// <param name="e">EventArgs.Empty</param>
  /// <remarks>
  /// <para>
  /// Applications that programmatically change the configuration of the repository should
  /// raise this event notification to notify listeners.
  /// </para>
  /// </remarks>
  public void RaiseConfigurationChanged(EventArgs e) => OnConfigurationChanged(e);

  private static int GetWaitTime(DateTime startTimeUtc, int millisecondsTimeout)
  {
    if (millisecondsTimeout is 0 or Timeout.Infinite)
    {
      return millisecondsTimeout;
    }

    int elapsedMilliseconds = (int)(DateTime.UtcNow - startTimeUtc).TotalMilliseconds;
    int timeout = millisecondsTimeout - elapsedMilliseconds;
    return Math.Max(0, timeout);
  }

  /// <summary>
  /// Flushes all configured Appenders that implement <see cref="log4net.Appender.IFlushable"/>.
  /// </summary>
  /// <param name="millisecondsTimeout">The maximum time in milliseconds to wait for logging events from asynchronous appenders to be flushed,
  /// or <see cref="Timeout.Infinite"/> to wait indefinitely.</param>
  /// <returns><c>True</c> if all logging events were flushed successfully, else <c>false</c>.</returns>
  public bool Flush(int millisecondsTimeout)
  {
    if (millisecondsTimeout < -1)
    {
      throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout),
        "Timeout must be -1 (Timeout.Infinite) or non-negative");
    }

    // Assume success until one of the appenders fails
    bool result = true;

    // Use DateTime.UtcNow rather than a System.Diagnostics.Stopwatch for compatibility with .NET 1.x
    DateTime startTimeUtc = DateTime.UtcNow;

    // Do buffering appenders first.  These may be forwarding to other appenders
    foreach (IAppender appender in GetAppenders())
    {
      if (appender is not IFlushable flushable)
      {
        continue;
      }

      if (appender is BufferingAppenderSkeleton)
      {
        int timeout = GetWaitTime(startTimeUtc, millisecondsTimeout);
        if (!flushable.Flush(timeout))
        {
          result = false;
        }
      }
    }

    // Do non-buffering appenders.
    foreach (IAppender appender in GetAppenders())
    {
      if (appender is not IFlushable flushable)
      {
        continue;
      }

      if (appender is not BufferingAppenderSkeleton)
      {
        int timeout = GetWaitTime(startTimeUtc, millisecondsTimeout);
        if (!flushable.Flush(timeout))
        {
          result = false;
        }
      }
    }

    return result;
  }
}