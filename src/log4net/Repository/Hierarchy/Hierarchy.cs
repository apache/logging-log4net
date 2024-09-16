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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using log4net.Appender;
using log4net.Core;
using log4net.Util;

namespace log4net.Repository.Hierarchy
{
  /// <summary>
  /// Delegate used to handle logger creation event notifications.
  /// </summary>
  /// <param name="sender">The <see cref="Hierarchy"/> in which the <see cref="Logger"/> has been created.</param>
  /// <param name="e">The <see cref="LoggerCreationEventArgs"/> event args that hold the <see cref="Logger"/> instance that has been created.</param>
  public delegate void LoggerCreationEventHandler(object sender, LoggerCreationEventArgs e);

  /// <summary>
  /// Provides data for the <see cref="Hierarchy.LoggerCreatedEvent"/> event.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A <see cref="Hierarchy.LoggerCreatedEvent"/> event is raised every time a
  /// <see cref="Logger"/> is created.
  /// </para>
  /// </remarks>
  public class LoggerCreationEventArgs : EventArgs
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="log">The <see cref="Logger"/> that has been created.</param>
    public LoggerCreationEventArgs(Logger log) => Logger = log;

    /// <summary>
    /// Gets the <see cref="Logger"/> that has been created.
    /// </summary>
    public Logger Logger { get; }
  }

  /// <summary>
  /// Hierarchical organization of loggers
  /// </summary>
  /// <remarks>
  /// <para>
  /// <i>The casual user should not have to deal with this class
  /// directly.</i>
  /// </para>
  /// <para>
  /// This class is specialized in retrieving loggers by name and also maintaining the logger
  /// hierarchy. Implements the <see cref="ILoggerRepository"/> interface.
  /// </para>
  /// <para>
  /// The structure of the logger hierarchy is maintained by the
  /// <see cref="GetLogger(string)"/> method. The hierarchy is such that children
  /// link to their parent but parents do not have any references to their
  /// children. Moreover, loggers can be instantiated in any order, in
  /// particular descendant before ancestor.
  /// </para>
  /// <para>
  /// In case a descendant is created before a particular ancestor, then it creates a provision node
  /// for the ancestor and adds itself to the provision node. Other descendants of the same ancestor 
  /// add themselves to the previously created provision node.
  /// </para>
  /// </remarks>
  /// <author>Nicko Cadell</author>
  /// <author>Gert Driesen</author>
  public class Hierarchy : LoggerRepositorySkeleton, IBasicRepositoryConfigurator, IXmlRepositoryConfigurator
  {
    private readonly ConcurrentDictionary<LoggerKey, object> loggers = new(LoggerKey.ComparerInstance);
    private ILoggerFactory defaultFactory;
    private Logger? rootLogger;

    /// <summary>
    /// The fully qualified type of the Hierarchy class.
    /// </summary>
    /// <remarks>
    /// Used by the internal logger to record the type of the log message.
    /// </remarks>
    private static readonly Type declaringType = typeof(Hierarchy);

    /// <summary>
    /// Event used to notify that a logger has been created.
    /// </summary>
    public event LoggerCreationEventHandler? LoggerCreatedEvent;

    /// <summary>
    /// Default constructor
    /// </summary>
    public Hierarchy() : this(new DefaultLoggerFactory())
    { }

    /// <summary>
    /// Construct with properties
    /// </summary>
    /// <param name="properties">The properties to pass to this repository.</param>
    public Hierarchy(PropertiesDictionary properties) : this(properties, new DefaultLoggerFactory())
    { }

    /// <summary>
    /// Construct with a logger factory
    /// </summary>
    /// <param name="loggerFactory">The factory to use to create new logger instances.</param>
    public Hierarchy(ILoggerFactory loggerFactory) : this(new PropertiesDictionary(), loggerFactory)
    { }

    /// <summary>
    /// Construct with properties and a logger factory
    /// </summary>
    /// <param name="properties">The properties to pass to this repository.</param>
    /// <param name="loggerFactory">The factory to use to create new logger instances.</param>
    public Hierarchy(PropertiesDictionary properties, ILoggerFactory loggerFactory)
      : base(properties) => defaultFactory = loggerFactory.EnsureNotNull();

    /// <summary>
    /// Has no appender warning been emitted
    /// </summary>
    /// <remarks>
    /// Flag to indicate if we have already issued a warning about not having an appender warning.
    /// </remarks>
    internal bool EmittedNoAppenderWarning { get; set; }

    /// <summary>
    /// Get the root of this hierarchy
    /// </summary>
    public Logger Root
    {
      get
      {
        if (rootLogger is null)
        {
          Logger root = defaultFactory.CreateLogger(this, null);
          root.Hierarchy = this;
          Interlocked.CompareExchange(ref rootLogger, root, null);
        }
        return rootLogger;
      }
    }

    /// <summary>
    /// Gets or sets the default <see cref="ILoggerFactory" /> instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The logger factory is used to create logger instances.
    /// </para>
    /// </remarks>
    public ILoggerFactory LoggerFactory
    {
      get => defaultFactory;
      set => defaultFactory = value.EnsureNotNull();
    }

    /// <summary>
    /// Test if a logger exists
    /// </summary>
    /// <param name="name">The name of the logger to lookup</param>
    /// <returns>The Logger object with the name specified</returns>
    /// <remarks>
    /// <para>
    /// Check if the named logger exists in the hierarchy. If so return
    /// its reference, otherwise returns <see langword="null"/>.
    /// </para>
    /// </remarks>
    public override ILogger? Exists(string name)
    {
      loggers.TryGetValue(new(name.EnsureNotNull()), out object? o);
      return o as Logger;
    }

    /// <summary>
    /// Returns all the currently defined loggers in the hierarchy as an Array
    /// </summary>
    /// <returns>All the defined loggers</returns>
    /// <remarks>
    /// <para>
    /// Returns all the currently defined loggers in the hierarchy as an Array.
    /// The root logger is <b>not</b> included in the returned
    /// enumeration.
    /// </para>
    /// </remarks>
    public override ILogger[] GetCurrentLoggers()
    {
      // The accumulation in loggers is necessary because not all elements in
      // loggers are Logger objects as there might be some ProvisionNodes as well.
      return loggers.Select(logger => logger.Value).OfType<ILogger>().ToArray();
    }

    /// <summary>
    /// Return a new logger instance named as the first parameter using
    /// the default factory.
    /// </summary>
    /// <remarks>
    /// If a logger of that name already exists, then it will be
    /// returned.  Otherwise, a new logger will be instantiated and
    /// then linked with its existing ancestors as well as children.
    /// </remarks>
    /// <param name="name">The name of the logger to retrieve</param>
    /// <returns>The logger object with the name specified</returns>
    public override ILogger GetLogger(string name)
      => GetLogger(name.EnsureNotNull(), defaultFactory);

    /// <summary>
    /// Shutting down a hierarchy will <i>safely</i> close and remove
    /// all appenders in all loggers including the root logger.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Shutting down a hierarchy will <i>safely</i> close and remove
    /// all appenders in all loggers including the root logger.
    /// </para>
    /// <para>
    /// Some appenders need to be closed before the
    /// application exists. Otherwise, pending logging events might be
    /// lost.
    /// </para>
    /// <para>
    /// The <see cref="Shutdown"/> method is careful to close nested
    /// appenders before closing regular appenders. This allows
    /// configurations where a regular appender is attached to a logger
    /// and again to a nested appender.
    /// </para>
    /// </remarks>
    public override void Shutdown()
    {
      LogLog.Debug(declaringType, $"Shutdown called on Hierarchy [{Name}]");

      // begin by closing nested appenders
      Root.CloseNestedAppenders();

      ILogger[] currentLoggers = GetCurrentLoggers();

      foreach (Logger logger in currentLoggers.OfType<Logger>())
      {
        logger.CloseNestedAppenders();
      }

      // then, remove all appenders
      Root.RemoveAllAppenders();

      foreach (Logger logger in currentLoggers.OfType<Logger>())
      {
        logger.RemoveAllAppenders();
      }

      base.Shutdown();
    }

    /// <summary>
    /// Reset all values contained in this hierarchy instance to their default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reset all values contained in this hierarchy instance to their
    /// default.  This removes all appenders from all loggers, sets
    /// the level of all non-root loggers to <see langword="null"/>,
    /// sets their additivity flag to <see langword="true"/> and sets the level
    /// of the root logger to <see cref="Level.Debug"/>. Moreover,
    /// message disabling is set its default "off" value.
    /// </para>
    /// <para>
    /// Existing loggers are not removed. They are just reset.
    /// </para>
    /// <para>
    /// This method should be used sparingly and with care as it will
    /// block all logging until it is completed.
    /// </para>
    /// </remarks>
    public override void ResetConfiguration()
    {
      Root.Level = LevelMap.LookupWithDefault(Level.Debug);
      Threshold = LevelMap.LookupWithDefault(Level.All);

      Shutdown(); // nested locks are OK  

      foreach (Logger logger in GetCurrentLoggers().OfType<Logger>())
      {
        logger.Level = null;
        logger.Additivity = true;
      }

      base.ResetConfiguration();

      // Notify listeners
      OnConfigurationChanged(null);
    }

    /// <summary>
    /// Log the logEvent through this hierarchy.
    /// </summary>
    /// <param name="logEvent">the event to log</param>
    /// <remarks>
    /// <para>
    /// This method should not normally be used to log.
    /// The <see cref="ILog"/> interface should be used 
    /// for routine logging. This interface can be obtained
    /// using the <see cref="M:log4net.LogManager.GetLogger(string)"/> method.
    /// </para>
    /// <para>
    /// The <paramref name="logEvent" /> is delivered to the appropriate logger and
    /// that logger is then responsible for logging the event.
    /// </para>
    /// </remarks>
    public override void Log(LoggingEvent logEvent)
    {
      logEvent.EnsureNotNull();
      if (logEvent.LoggerName is not null)
      {
        GetLogger(logEvent.LoggerName, defaultFactory).Log(logEvent);
      }
    }

    /// <summary>
    /// Returns all the Appenders that are currently configured
    /// </summary>
    /// <returns>An array containing all the currently configured appenders</returns>
    /// <remarks>
    /// <para>
    /// Returns all the <see cref="log4net.Appender.IAppender"/> instances that are currently configured.
    /// All the loggers are searched for appenders. The appenders may also be containers
    /// for appenders and these are also searched for additional loggers.
    /// </para>
    /// <para>
    /// The list returned is unordered but does not contain duplicates.
    /// </para>
    /// </remarks>
    public override IAppender[] GetAppenders()
    {
      var appenderList = new HashSet<IAppender>();

      CollectAppenders(appenderList, Root);

      foreach (Logger logger in GetCurrentLoggers().OfType<Logger>())
      {
        CollectAppenders(appenderList, logger);
      }

      return appenderList.ToArray();
    }

    /// <summary>
    /// Collect the appenders from an <see cref="IAppenderAttachable"/>.
    /// The appender may also be a container.
    /// </summary>
    private static void CollectAppender(HashSet<IAppender> appenderList, IAppender appender)
    {
      if (appenderList.Add(appender) && appender is IAppenderAttachable container)
      {
        CollectAppenders(appenderList, container);
      }
    }

    /// <summary>
    /// Collect the appenders from an <see cref="IAppenderAttachable"/> container
    /// </summary>
    private static void CollectAppenders(HashSet<IAppender> appenderList, IAppenderAttachable container)
    {
      foreach (IAppender appender in container.Appenders)
      {
        CollectAppender(appenderList, appender);
      }
    }

    /// <summary>
    /// Initialize the log4net system using the specified appender
    /// </summary>
    /// <param name="appender">the appender to use to log all logging events</param>
    void IBasicRepositoryConfigurator.Configure(IAppender appender)
      => BasicRepositoryConfigure(appender);

    /// <summary>
    /// Initialize the log4net system using the specified appenders
    /// </summary>
    /// <param name="appenders">the appenders to use to log all logging events</param>
    void IBasicRepositoryConfigurator.Configure(params IAppender[] appenders)
      => BasicRepositoryConfigure(appenders);

    /// <summary>
    /// Initialize the log4net system using the specified appenders
    /// </summary>
    /// <param name="appenders">the appenders to use to log all logging events</param>
    /// <remarks>
    /// <para>
    /// This method provides the same functionality as the 
    /// <see cref="M:IBasicRepositoryConfigurator.Configure(IAppender)"/> method implemented
    /// on this object, but it is protected and therefore can be called by subclasses.
    /// </para>
    /// </remarks>
    protected void BasicRepositoryConfigure(params IAppender[] appenders)
    {
      var configurationMessages = new List<LogLog>();

      using (new LogLog.LogReceivedAdapter(configurationMessages))
      {
        foreach (IAppender appender in appenders)
        {
          Root.AddAppender(appender);
        }
      }

      Configured = true;

      ConfigurationMessages = configurationMessages;

      // Notify listeners
      OnConfigurationChanged(new ConfigurationChangedEventArgs(configurationMessages));
    }

    /// <summary>
    /// Initialize the log4net system using the specified config
    /// </summary>
    /// <param name="element">the element containing the root of the config</param>
    void IXmlRepositoryConfigurator.Configure(System.Xml.XmlElement element)
    {
      XmlRepositoryConfigure(element);
    }

    /// <summary>
    /// Initialize the log4net system using the specified config
    /// </summary>
    /// <param name="element">the element containing the root of the config</param>
    /// <remarks>
    /// <para>
    /// This method provides the same functionality as the 
    /// <see cref="M:IBasicRepositoryConfigurator.Configure(IAppender)"/> method implemented
    /// on this object, but it is protected and therefore can be called by subclasses.
    /// </para>
    /// </remarks>
    protected void XmlRepositoryConfigure(System.Xml.XmlElement element)
    {
      var configurationMessages = new List<LogLog>();

      using (new LogLog.LogReceivedAdapter(configurationMessages))
      {
        new XmlHierarchyConfigurator(this).Configure(element);
      }

      Configured = true;

      ConfigurationMessages = configurationMessages;

      // Notify listeners
      OnConfigurationChanged(new ConfigurationChangedEventArgs(configurationMessages));
    }

    /// <summary>
    /// Test if this hierarchy is disabled for the specified <see cref="Level"/>.
    /// </summary>
    /// <param name="level">The level to check against.</param>
    /// <returns>
    /// <see langword="true"/> if the repository is disabled for the level argument, <see langword="false"/> otherwise.
    /// </returns>
    /// <remarks>
    /// If this hierarchy has not been configured then this method will always return <see langword="true"/>.
    /// See also the <see cref="ILoggerRepository.Threshold"/> property.
    /// </remarks>
    public bool IsDisabled(Level level)
    {
      if (Configured)
      {
        return Threshold > level.EnsureNotNull();
      }
      // If not configured the hierarchy is effectively disabled
      return true;
    }

    /// <summary>
    /// Clear all logger definitions from the internal hashtable
    /// </summary>
    /// <remarks>
    /// <para>
    /// This call will clear all logger definitions from the internal
    /// hashtable. Invoking this method will irrevocably mess up the
    /// logger hierarchy.
    /// </para>
    /// <para>
    /// You should <b>really</b> know what you are doing before invoking this method.
    /// </para>
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Clear() => loggers.Clear();

    /// <summary>
    /// Returns a new logger instance named as the first parameter using
    /// <paramref name="factory"/>.
    /// </summary>
    /// <param name="name">The name of the logger to retrieve</param>
    /// <param name="factory">The factory that will make the new logger instance</param>
    /// <returns>The logger object with the name specified</returns>
    /// <remarks>
    /// <para>
    /// If a logger of that name already exists, then it will be
    /// returned. Otherwise, a new logger will be instantiated by the
    /// <paramref name="factory"/> parameter and linked with its existing
    /// ancestors as well as children.
    /// </para>
    /// </remarks>
    public Logger GetLogger(string name, ILoggerFactory factory)
    {
      name.EnsureNotNull();
      factory.EnsureNotNull();

      var key = new LoggerKey(name);

      const int maxRetries = 5;
      for (int i = 0; i < maxRetries; i++)
      {
        if (TryCreateLogger(key, factory) is Logger result)
        {
          return result;
        }
      }
      throw new LogException(
        $"GetLogger failed, because possibly too many threads are messing with creating the logger {name}!");
    }

    private Logger? TryCreateLogger(LoggerKey key, ILoggerFactory factory)
    {
      if (!loggers.TryGetValue(key, out object? node))
      {
        Logger newLogger = CreateLogger(key.Name);
        node = loggers.GetOrAdd(key, newLogger);
        if (node == newLogger)
        {
          RegisterLogger(newLogger);
        }
      }

      if (node is Logger logger)
      {
        return logger;
      }

      if (node is ProvisionNode provisionNode)
      {
        Logger newLogger = CreateLogger(key.Name);
        if (loggers.TryUpdate(key, newLogger, node))
        {
          UpdateChildren(provisionNode, newLogger);
          RegisterLogger(newLogger);
          return newLogger;
        }
        return null;
      }

      // It should be impossible to arrive here but let's keep the compiler happy.
      throw new LogException("TryCreateLogger failed, because a node is neither a Logger nor a ProvisionNode!");

      Logger CreateLogger(string name)
      {
        Logger result = factory.CreateLogger(this, name);
        result.Hierarchy = this;
        return result;
      }

      void RegisterLogger(Logger logger)
      {
        UpdateParents(logger);
        OnLoggerCreationEvent(logger);
      }
    }

    /// <summary>
    /// Sends a logger creation event to all registered listeners
    /// </summary>
    /// <param name="logger">The newly created logger</param>
    /// <remarks>
    /// Raises the logger creation event.
    /// </remarks>
    protected virtual void OnLoggerCreationEvent(Logger logger)
      => LoggerCreatedEvent?.Invoke(this, new(logger));

    /// <summary>
    /// Updates all the parents of the specified logger
    /// </summary>
    /// <param name="log">The logger to update the parents for</param>
    /// <remarks>
    /// <para>
    /// This method loops through all the <i>potential</i> parents of
    /// <paramref name="log"/>. There 3 possible cases:
    /// </para>
    /// <list type="number">
    ///    <item>
    ///      <term>No entry for the potential parent of <paramref name="log"/> exists</term>
    ///      <description>
    ///      We create a ProvisionNode for this potential 
    ///      parent and insert <paramref name="log"/> in that provision node.
    ///      </description>
    ///    </item>
    ///    <item>
    ///      <term>The entry is of type Logger for the potential parent.</term>
    ///      <description>
    ///      The entry is <paramref name="log"/>'s nearest existing parent. We 
    ///      update <paramref name="log"/>'s parent field with this entry. We also break from 
    ///      the loop because updating our parent's parent is our parent's 
    ///      responsibility.
    ///      </description>
    ///    </item>
    ///    <item>
    ///      <term>The entry is of type ProvisionNode for this potential parent.</term>
    ///      <description>
    ///      We add <paramref name="log"/> to the list of children for this potential parent.
    ///      </description>
    ///    </item>
    /// </list>
    /// </remarks>
    private void UpdateParents(Logger log)
    {
      string name = log.Name;
      int length = name.Length;
      bool parentFound = false;

      // if name = "w.x.y.z", loop through "w.x.y", "w.x" and "w", but not "w.x.y.z" 
      for (int i = name.LastIndexOf('.', length - 1); i >= 0; i = name.LastIndexOf('.', i - 1))
      {
        string substr = name.Substring(0, i);

        var key = new LoggerKey(substr);
        loggers.TryGetValue(key, out object? node);

        // Create a provision node for a future parent.
        if (node is null)
        {
          loggers[key] = new ProvisionNode(log);
        }
        else
        {
          if (node is Logger nodeLogger)
          {
            parentFound = true;
            log.Parent = nodeLogger;
            break; // no need to update the ancestors of the closest ancestor
          }

          if (node is ProvisionNode nodeProvisionNode)
          {
            nodeProvisionNode.Add(log);
          }
          else
          {
            LogLog.Error(declaringType, $"Unexpected object type [{node.GetType()}] in loggers.", new LogException());
          }
        }
        if (i == 0)
        {
          // logger name starts with a dot and we've hit the start
          break;
        }
      }

      // If we could not find any existing parents, then link with root.
      if (!parentFound)
      {
        log.Parent = Root;
      }
    }

    /// <summary>
    /// Replace a <see cref="ProvisionNode"/> with a <see cref="Logger"/> in the hierarchy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// We update the links for all the children that placed themselves
    /// in the provision node 'pn'. The second argument 'log' is a
    /// reference for the newly created Logger, parent of all the
    /// children in 'pn'.
    /// </para>
    /// <para>
    /// We loop on all the children 'c' in 'pn'.
    /// </para>
    /// <para>
    /// If the child 'c' has been already linked to a child of
    /// 'log' then there is no need to update 'c'.
    /// </para>
    /// <para>
    /// Otherwise, we set log's parent field to c's parent and set
    /// c's parent field to log.
    /// </para>
    /// </remarks>
    private static void UpdateChildren(ProvisionNode pn, Logger log)
    {
      foreach (Logger childLogger in pn)
      {
        // Unless this child already points to a correct (lower) parent,
        // make log.Parent point to childLogger.Parent and childLogger.Parent to log.
        if (childLogger.Parent is not null && !childLogger.Parent.Name.StartsWith(log.Name, StringComparison.Ordinal))
        {
          log.Parent = childLogger.Parent;
          childLogger.Parent = log;
        }
      }
    }

    /// <summary>
    /// Define or redefine a Level using the values in the <see cref="LevelEntry"/> argument
    /// </summary>
    /// <param name="levelEntry">the level values</param>
    /// <remarks>
    /// Supports setting levels via the configuration file.
    /// </remarks>
    internal void AddLevel(LevelEntry levelEntry)
    {
      levelEntry.EnsureNotNull();
      levelEntry.Name.EnsureNotNull();

      // Lookup replacement value
      if (levelEntry.Value == -1)
      {
        if (LevelMap[levelEntry.Name] is Level previousLevel)
        {
          levelEntry.Value = previousLevel.Value;
        }
        else
        {
          throw new InvalidOperationException($"Cannot redefine level [{levelEntry.Name}] because it is not defined in the LevelMap. To define the level supply the level value.");
        }
      }

      LevelMap.Add(levelEntry.Name, levelEntry.Value, levelEntry.DisplayName);
    }

    /// <summary>
    /// A class to hold the value, name and display name for a level
    /// </summary>
    internal sealed class LevelEntry
    {
      /// <summary>
      /// Value of the level
      /// </summary>
      /// <remarks>
      /// If the value is not set (defaults to -1) the value will be looked
      /// up for the current level with the same name.
      /// </remarks>
      public int Value { get; set; } = -1;

      /// <summary>
      /// Name of the level
      /// </summary>
      public string? Name { get; set; }

      /// <summary>
      /// Display name for the level
      /// </summary>
      public string? DisplayName { get; set; }

      /// <summary>
      /// Override <c>Object.ToString</c> to return sensible debug info
      /// </summary>
      /// <returns>string info about this object</returns>
      public override string ToString()
        => $"LevelEntry(Value={Value}, Name={Name}, DisplayName={DisplayName})";
    }

    /// <summary>
    /// Set a Property using the values in the <see cref="LevelEntry"/> argument
    /// </summary>
    /// <param name="propertyEntry">the property value</param>
    /// <remarks>
    /// Supports setting property values via the configuration file.
    /// </remarks>
    internal void AddProperty(PropertyEntry propertyEntry)
      => Properties[propertyEntry.Key.EnsureNotNull()] = propertyEntry.EnsureNotNull().Value;
  }
}