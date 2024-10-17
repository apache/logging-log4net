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
using log4net.Appender;
using log4net.ObjectRenderer;
using log4net.Core;
using log4net.Plugin;
using log4net.Util;

namespace log4net.Repository;

/// <summary>
/// Delegate used to handle logger repository shutdown event notifications.
/// </summary>
/// <param name="sender">The <see cref="ILoggerRepository"/> that is shutting down.</param>
/// <param name="e">Empty event args</param>
public delegate void LoggerRepositoryShutdownEventHandler(object sender, EventArgs e);

/// <summary>
/// Delegate used to handle logger repository configuration reset event notifications.
/// </summary>
/// <param name="sender">The <see cref="ILoggerRepository"/> that has had its configuration reset.</param>
/// <param name="e">Empty event args</param>
public delegate void LoggerRepositoryConfigurationResetEventHandler(object sender, EventArgs e);

/// <summary>
/// Delegate used to handle event notifications for logger repository configuration changes.
/// </summary>
/// <param name="sender">The <see cref="ILoggerRepository"/> that has had its configuration changed.</param>
/// <param name="e">Empty event arguments.</param>
public delegate void LoggerRepositoryConfigurationChangedEventHandler(object sender, EventArgs e);

/// <summary>
/// Interface implemented by logger repositories, e.g. <see cref="Hierarchy"/>, and used by the
/// <see cref="LogManager"/> to obtain <see cref="ILog"/> instances.
/// </summary>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
public interface ILoggerRepository
{
  /// <summary>
  /// Gets or sets the name of the repository.
  /// </summary>
  string Name { get; set; }

  /// <summary>
  /// Gets the map from types to <see cref="IObjectRenderer"/> instances for custom rendering.
  /// </summary>
  RendererMap RendererMap { get; }

  /// <summary>
  /// Gets the map from plugin name to plugin value for plugins attacked to this repository.
  /// </summary>
  PluginMap PluginMap { get; }

  /// <summary>
  /// Gets the map from level names and <see cref="Level"/> values for this repository.
  /// </summary>
  LevelMap LevelMap { get; }

  /// <summary>
  /// Gets or sets the threshold for all events in this repository.
  /// </summary>
  Level Threshold { get; set; }

  /// <summary>
  /// Gets the named logger, or <c>null</c>.
  /// </summary>
  /// <param name="name">The name of the logger to look up.</param>
  /// <returns>The logger if found, or <c>null</c>.</returns>
  ILogger? Exists(string name);

  /// <summary>
  /// Gets all the currently defined loggers.
  /// </summary>
  ILogger[] GetCurrentLoggers();

  /// <summary>
  /// Returns a named logger instance
  /// </summary>
  /// <param name="name">The name of the logger to retrieve</param>
  /// <returns>The logger object with the name specified</returns>
  /// <remarks>
  /// <para>
  /// Returns a named logger instance.
  /// </para>
  /// <para>
  /// If a logger of that name already exists, then it will be
  /// returned.  Otherwise, a new logger will be instantiated and
  /// then linked with its existing ancestors as well as children.
  /// </para>
  /// </remarks>
  ILogger GetLogger(string name);

  /// <summary>
  /// Shuts down the repository, <i>safely</i> closing and removing
  /// all appenders in all loggers including the root logger.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Some appenders need to be closed before the
  /// application exists. Otherwise, pending logging events might be
  /// lost.
  /// </para>
  /// <para>
  /// The <see cref="M:Shutdown()"/> method is careful to close nested
  /// appenders before closing regular appenders. This allows
  /// configurations where a regular appender is attached to a logger
  /// and again to a nested appender.
  /// </para>
  /// </remarks>
  void Shutdown();

  /// <summary>
  /// Resets the repository configuration to a default state. Loggers are reset but not removed.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This method should be used sparingly and with care as it will
  /// block all logging until it is completed.
  /// </para>
  /// </remarks>
  void ResetConfiguration();

  /// <summary>
  /// Logs a <see cref="LoggingEvent"/> through this repository.
  /// </summary>
  /// <param name="logEvent">The event to log.</param>
  /// <remarks>
  /// <para>
  /// This method should not normally be used to log.
  /// The <see cref="ILog"/> interface should be used 
  /// for routine logging. This interface can be obtained
  /// using the <see cref="M:log4net.LogManager.GetLogger(string)"/> method.
  /// </para>
  /// <para>
  /// The <c>logEvent</c> is delivered to the appropriate logger and
  /// that logger is then responsible for logging the event.
  /// </para>
  /// </remarks>
  void Log(LoggingEvent logEvent);

  /// <summary>
  /// Gets or sets a value that indicates whether this repository has been configured.
  /// </summary>
  bool Configured { get; set; }

  /// <summary>
  /// Collection of internal messages captured during the most 
  /// recent configuration process.
  /// </summary>
  ICollection ConfigurationMessages { get; set; }

  /// <summary>
  /// Event to notify that the repository has been shut down.
  /// </summary>
  event LoggerRepositoryShutdownEventHandler? ShutdownEvent;

  /// <summary>
  /// Event to notify that the repository has had its configuration reset to default.
  /// </summary>
  event LoggerRepositoryConfigurationResetEventHandler? ConfigurationReset;

  /// <summary>
  /// Event to notify that the repository's configuration has changed.
  /// </summary>
  event LoggerRepositoryConfigurationChangedEventHandler? ConfigurationChanged;

  /// <summary>
  /// Repository specific properties.
  /// </summary>
  PropertiesDictionary Properties { get; }

  /// <summary>
  /// Gets all Appenders that are configured for this repository.
  /// </summary>
  IAppender[] GetAppenders();
}
