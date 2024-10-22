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

using log4net.Appender;
using log4net.Util;
using log4net.Core;

namespace log4net.Repository.Hierarchy;

/// <summary>
/// Implementation of <see cref="ILogger"/> used by <see cref="Hierarchy"/>
/// </summary>
/// <remarks>
/// <para>
/// Internal class used to provide implementation of <see cref="ILogger"/>
/// interface. Applications should use <see cref="LogManager"/> to get
/// logger instances.
/// </para>
/// <para>
/// This is one of the central classes in the log4net implementation. One of the
/// distinctive features of log4net are hierarchical loggers and their
/// evaluation. The <see cref="Hierarchy"/> organizes the <see cref="Logger"/>
/// instances into a rooted tree hierarchy.
/// </para>
/// <para>
/// The <see cref="Logger"/> class is abstract. Only concrete subclasses of
/// <see cref="Logger"/> can be created. The <see cref="ILoggerFactory"/>
/// is used to create instances of this type for the <see cref="Hierarchy"/>.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
/// <author>Aspi Havewala</author>
/// <author>Douglas de la Torre</author>
public abstract class Logger : IAppenderAttachable, ILogger
{
  /// <summary>
  /// The fully qualified type of the Logger class.
  /// </summary>
  private static readonly Type _declaringType = typeof(Logger);

  /// <summary>
  /// The parent of this logger.
  /// </summary>
  /// <remarks>
  /// <para>
  /// All loggers have at least one ancestor which is the root logger.
  /// </para>
  /// </remarks>
  private Logger? _parent;

  /// <summary>
  /// Loggers need to know what Hierarchy they are in.
  /// </summary>
  private Hierarchy? _hierarchy;

  /// <summary>
  /// Helper implementation of the <see cref="IAppenderAttachable"/> interface
  /// </summary>
  private AppenderAttachedImpl? _appenderAttachedImpl;

  /// <summary>
  /// Lock to protect AppenderAttachedImpl variable appenderAttachedImpl
  /// </summary>
  private readonly ReaderWriterLock _appenderLock = new();

  /// <summary>
  /// This constructor created a new <see cref="Logger" /> instance and
  /// sets its name.
  /// </summary>
  /// <param name="name">The name of the <see cref="Logger" />.</param>
  /// <remarks>
  /// <para>
  /// This constructor is protected and designed to be used by
  /// a subclass that is not abstract.
  /// </para>
  /// <para>
  /// Loggers are constructed by <see cref="ILoggerFactory"/> 
  /// objects. See <see cref="DefaultLoggerFactory"/> for the default
  /// logger creator.
  /// </para>
  /// </remarks>
  protected Logger(string name)
    => Name = string.Intern(name);

  /// <summary>
  /// Gets or sets the parent logger in the hierarchy.
  /// </summary>
  /// <value>
  /// The parent logger in the hierarchy.
  /// </value>
  /// <remarks>
  /// <para>
  /// Part of the Composite pattern that makes the hierarchy.
  /// The hierarchy is parent linked rather than child linked.
  /// </para>
  /// </remarks>
  public virtual Logger? Parent
  {
    get => _parent;
    set => _parent = value;
  }

  /// <summary>
  /// Gets or sets a value indicating if child loggers inherit their parent's appenders.
  /// </summary>
  /// <value>
  /// <see langword="true"/> if child loggers inherit their parent's appenders.
  /// </value>
  /// <remarks>
  /// <para>
  /// Additivity is set to <see langword="true"/> by default, that is children inherit
  /// the appenders of their ancestors by default. If this variable is
  /// set to <see langword="false"/> then the appenders found in the
  /// ancestors of this logger are not used. However, the children
  /// of this logger will inherit its appenders, unless the children
  /// have their additivity flag set to <see langword="false"/> too. See
  /// the user manual for more details.
  /// </para>
  /// </remarks>
  public virtual bool Additivity { get; set; } = true;

  /// <summary>
  /// Gets the effective level for this logger.
  /// </summary>
  /// <returns>The nearest level in the logger hierarchy.</returns>
  /// <remarks>
  /// <para>
  /// Starting from this logger, searches the logger hierarchy for a
  /// non-null level and returns it. Otherwise, returns the level of the
  /// root logger.
  /// </para>
  /// <para>The Logger class is designed so that this method executes as
  /// quickly as possible.</para>
  /// </remarks>
  public virtual Level EffectiveLevel
  {
    get
    {
      for (Logger? c = this; c is not null; c = c._parent)
      {
        if (c.Level is Level level)
        {
          return level;
        }
      }
      return null!; // If reached will cause an NullPointerException.
    }
  }

  /// <summary>
  /// Gets or sets the <see cref="Hierarchy"/> where this <see cref="Logger"/> instance is attached to.
  /// </summary>
  public virtual Hierarchy? Hierarchy
  {
    get => _hierarchy;
    set => _hierarchy = value;
  }

  /// <summary>
  /// Gets or sets the assigned <see cref="Level"/> for this Logger.  
  /// </summary>
  public virtual Level? Level { get; set; }

  /// <summary>
  /// Add <paramref name="appender"/> to the list of appenders of this
  /// Logger instance.
  /// </summary>
  /// <param name="appender">An appender to add to this logger</param>
  /// <remarks>
  /// <para>
  /// If <paramref name="appender"/> is already in the list of
  /// appenders, then it won't be added again.
  /// </para>
  /// </remarks>
  public virtual void AddAppender(IAppender appender)
  {
    appender.EnsureNotNull();

    _appenderLock.AcquireWriterLock();
    try
    {
      _appenderAttachedImpl ??= new();
      _appenderAttachedImpl.AddAppender(appender);
    }
    finally
    {
      _appenderLock.ReleaseWriterLock();
    }
  }

  /// <summary>
  /// Get the appenders contained in this logger as an 
  /// <see cref="System.Collections.ICollection"/>.
  /// </summary>
  /// <returns>
  /// A collection of the appenders in this logger. If no appenders 
  /// can be found, then a <see cref="EmptyCollection"/> is returned.
  /// </returns>
  public virtual AppenderCollection Appenders
  {
    get
    {
      _appenderLock.AcquireReaderLock();
      try
      {
        if (_appenderAttachedImpl is null)
        {
          return AppenderCollection.EmptyCollection;
        }
        return _appenderAttachedImpl.Appenders;
      }
      finally
      {
        _appenderLock.ReleaseReaderLock();
      }
    }
  }

  /// <summary>
  /// Look for the appender named as <paramref name="name"/>
  /// </summary>
  /// <param name="name">The name of the appender to lookup</param>
  /// <returns>The appender with the name specified, or <see langword="null"/>.</returns>
  public virtual IAppender? GetAppender(string? name)
  {
    _appenderLock.AcquireReaderLock();
    try
    {
      if (_appenderAttachedImpl is null || name is null)
      {
        return null;
      }

      return _appenderAttachedImpl.GetAppender(name);
    }
    finally
    {
      _appenderLock.ReleaseReaderLock();
    }
  }

  /// <summary>
  /// Removes all previously added appenders from this Logger instance.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is useful when re-reading configuration information.
  /// </para>
  /// </remarks>
  public virtual void RemoveAllAppenders()
  {
    _appenderLock.AcquireWriterLock();
    try
    {
      if (_appenderAttachedImpl is not null)
      {
        _appenderAttachedImpl.RemoveAllAppenders();
        _appenderAttachedImpl = null;
      }
    }
    finally
    {
      _appenderLock.ReleaseWriterLock();
    }
  }

  /// <summary>
  /// Remove the appender passed as parameter form the list of appenders.
  /// </summary>
  /// <param name="appender">The appender to remove</param>
  /// <returns>The appender removed from the list</returns>
  /// <remarks>
  /// <para>
  /// The appender removed is not closed.
  /// If you are discarding the appender you must call
  /// <see cref="IAppender.Close"/> on the appender removed.
  /// </para>
  /// </remarks>
  public virtual IAppender? RemoveAppender(IAppender? appender)
  {
    _appenderLock.AcquireWriterLock();
    try
    {
      if (appender is not null && _appenderAttachedImpl is not null)
      {
        return _appenderAttachedImpl.RemoveAppender(appender);
      }
    }
    finally
    {
      _appenderLock.ReleaseWriterLock();
    }
    return null;
  }

  /// <summary>
  /// Remove the appender passed as parameter form the list of appenders.
  /// </summary>
  /// <param name="name">The name of the appender to remove</param>
  /// <returns>The appender removed from the list</returns>
  /// <remarks>
  /// <para>
  /// The appender removed is not closed.
  /// If you are discarding the appender you must call
  /// <see cref="IAppender.Close"/> on the appender removed.
  /// </para>
  /// </remarks>
  public virtual IAppender? RemoveAppender(string? name)
  {
    _appenderLock.AcquireWriterLock();
    try
    {
      if (name is not null && _appenderAttachedImpl is not null)
      {
        return _appenderAttachedImpl.RemoveAppender(name);
      }
    }
    finally
    {
      _appenderLock.ReleaseWriterLock();
    }
    return null;
  }

  /// <summary>
  /// Gets the logger name.
  /// </summary>
  public virtual string Name { get; }

  /// <summary>
  /// Generates a logging event for the specified <paramref name="level"/> using
  /// the <paramref name="message"/> and <paramref name="exception"/>.
  /// </summary>
  /// <param name="callerStackBoundaryDeclaringType">The declaring type of the method that is
  /// the stack boundary into the logging system for this call.</param>
  /// <param name="level">The level of the message to be logged.</param>
  /// <param name="message">The message object to log.</param>
  /// <param name="exception">The exception to log, including its stack trace.</param>
  /// <remarks>
  /// <para>
  /// This generic form is intended to be used by wrappers.
  /// </para>
  /// <para>
  /// This method must not throw any exception to the caller.
  /// </para>
  /// </remarks>
  public virtual void Log(Type? callerStackBoundaryDeclaringType, Level? level, object? message, Exception? exception)
  {
    try
    {
      if (IsEnabledFor(level))
      {
        ForcedLog(callerStackBoundaryDeclaringType ?? _declaringType, level, message, exception);
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      LogLog.Error(_declaringType, "Exception while logging", e);
    }
  }

  /// <summary>
  /// Logs the specified logging event through this logger.
  /// </summary>
  /// <param name="logEvent">The event being logged.</param>
  /// <remarks>
  /// <para>
  /// This is the most generic printing method that is intended to be used 
  /// by wrappers.
  /// </para>
  /// <para>
  /// This method must not throw any exception to the caller.
  /// </para>
  /// </remarks>
  public virtual void Log(LoggingEvent? logEvent)
  {
    try
    {
      if (logEvent is not null)
      {
        if (IsEnabledFor(logEvent.Level))
        {
          ForcedLog(logEvent);
        }
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      LogLog.Error(_declaringType, "Exception while logging", e);
    }
  }

  /// <summary>
  /// Checks if this logger is enabled for a given <see cref="Level"/> passed as parameter.
  /// </summary>
  /// <param name="level">The level to check.</param>
  /// <returns>
  /// <see langword="true"/> if this logger is enabled for <paramref name="level"/>,
  /// otherwise <see langword="false"/>.
  /// </returns>
  /// <remarks>
  /// <para>
  /// This method must not throw any exception to the caller.
  /// </para>
  /// </remarks>
  public virtual bool IsEnabledFor(Level? level)
  {
    try
    {
      if (level is not null)
      {
        if (_hierarchy is not null && _hierarchy.IsDisabled(level))
        {
          return false;
        }
        return level >= EffectiveLevel;
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      LogLog.Error(_declaringType, "Exception while logging", e);
    }
    return false;
  }

  /// <summary>
  /// Gets the <see cref="ILoggerRepository"/> where this 
  /// <see cref="Logger"/> instance is attached to.
  /// </summary>
  public ILoggerRepository? Repository => _hierarchy;

  /// <summary>
  /// Deliver the <see cref="LoggingEvent"/> to the attached appenders.
  /// </summary>
  /// <param name="loggingEvent">The event to log.</param>
  /// <remarks>
  /// <para>
  /// Call the appenders in the hierarchy starting at <see langword="this"/>.
  /// If no appenders could be found, emit a warning.
  /// </para>
  /// <para>
  /// This method calls all the appenders inherited from the
  /// hierarchy circumventing any evaluation of whether to log or not
  /// to log the particular log request.
  /// </para>
  /// </remarks>
  protected virtual void CallAppenders(LoggingEvent loggingEvent)
  {
    loggingEvent.EnsureNotNull();

    int writes = 0;

    for (Logger? c = this; c is not null; c = c._parent)
    {
      if (c._appenderAttachedImpl is not null)
      {
        // Protected against simultaneous call to addAppender, removeAppender,...
        c._appenderLock.AcquireReaderLock();
        try
        {
          if (c._appenderAttachedImpl is not null)
          {
            writes += c._appenderAttachedImpl.AppendLoopOnAppenders(loggingEvent);
          }
        }
        finally
        {
          c._appenderLock.ReleaseReaderLock();
        }
      }

      if (!c.Additivity)
      {
        break;
      }
    }

    // No appenders in hierarchy, warn user only once.
    //
    // Note that by including the AppDomain values for the currently running
    // thread, it becomes much easier to see which application the warning
    // is from, which is especially helpful in a multi-AppDomain environment
    // (like IIS with multiple VDIRS).  Without this, it can be difficult
    // or impossible to determine which .config file is missing appender
    // definitions.
    //
    if (_hierarchy is not null && !_hierarchy.EmittedNoAppenderWarning && writes == 0)
    {
      _hierarchy.EmittedNoAppenderWarning = true;
      LogLog.Debug(_declaringType, $"No appenders could be found for logger [{Name}] repository [{Repository?.Name}]");
      LogLog.Debug(_declaringType, "Please initialize the log4net system properly.");
      try
      {
        LogLog.Debug(_declaringType, "    Current AppDomain context information: ");
        LogLog.Debug(_declaringType, "       BaseDirectory   : " + SystemInfo.ApplicationBaseDirectory);
        LogLog.Debug(_declaringType, "       FriendlyName    : " + AppDomain.CurrentDomain.FriendlyName);
        LogLog.Debug(_declaringType, "       DynamicDirectory: " + AppDomain.CurrentDomain.DynamicDirectory);
      }
      catch (System.Security.SecurityException)
      {
        // Insufficient permissions to display info from the AppDomain
      }
    }
  }

  /// <summary>
  /// Closes all attached appenders implementing the <see cref="IAppenderAttachable"/> interface.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Used to ensure that the appenders are correctly shutdown.
  /// </para>
  /// </remarks>
  public virtual void CloseNestedAppenders()
  {
    _appenderLock.AcquireWriterLock();
    try
    {
      if (_appenderAttachedImpl is not null)
      {
        AppenderCollection appenders = _appenderAttachedImpl.Appenders;
        foreach (IAppender appender in appenders)
        {
          if (appender is IAppenderAttachable)
          {
            appender.Close();
          }
        }
      }
    }
    finally
    {
      _appenderLock.ReleaseWriterLock();
    }
  }

  /// <summary>
  /// This is the most generic printing method. This generic form is intended to be used by wrappers
  /// </summary>
  /// <param name="level">The level of the message to be logged.</param>
  /// <param name="message">The message object to log.</param>
  /// <param name="exception">The exception to log, including its stack trace.</param>
  /// <remarks>
  /// <para>
  /// Generate a logging event for the specified <paramref name="level"/> using
  /// the <paramref name="message"/>.
  /// </para>
  /// </remarks>
  public virtual void Log(Level level, object? message, Exception? exception)
  {
    if (IsEnabledFor(level))
    {
      ForcedLog(_declaringType, level, message, exception);
    }
  }

  /// <summary>
  /// Creates a new logging event and logs the event without further checks.
  /// </summary>
  /// <param name="callerStackBoundaryDeclaringType">The declaring type of the method that is
  /// the stack boundary into the logging system for this call.</param>
  /// <param name="level">The level of the message to be logged.</param>
  /// <param name="message">The message object to log.</param>
  /// <param name="exception">The exception to log, including its stack trace.</param>
  /// <remarks>
  /// <para>
  /// Generates a logging event and delivers it to the attached
  /// appenders.
  /// </para>
  /// </remarks>
  protected virtual void ForcedLog(Type callerStackBoundaryDeclaringType, Level? level,
    object? message, Exception? exception)
  {
    CallAppenders(new LoggingEvent(callerStackBoundaryDeclaringType, Hierarchy, Name, level,
      message, exception));
  }

  /// <summary>
  /// Creates a new logging event and logs the event without further checks.
  /// </summary>
  /// <param name="logEvent">The event being logged.</param>
  /// <remarks>
  /// <para>
  /// Delivers the logging event to the attached appenders.
  /// </para>
  /// </remarks>
  protected virtual void ForcedLog(LoggingEvent logEvent)
  {
    // The logging event may not have been created by this logger
    // the Repository may not be correctly set on the event. This
    // is required for the appenders to correctly lookup renderers etc...
    logEvent.EnsureNotNull().EnsureRepository(Hierarchy);

    CallAppenders(logEvent);
  }
}