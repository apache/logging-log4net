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

using log4net.Core;
using log4net.Appender;

namespace log4net.Util;

/// <summary>
/// A straightforward implementation of the <see cref="IAppenderAttachable"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// This is the default implementation of the <see cref="IAppenderAttachable"/>
/// interface. Implementors of the <see cref="IAppenderAttachable"/> interface
/// should aggregate an instance of this type.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
public class AppenderAttachedImpl : IAppenderAttachable
{
  /// <summary>
  /// Constructor
  /// </summary>
  /// <remarks>
  /// <para>
  /// Initializes a new instance of the <see cref="AppenderAttachedImpl"/> class.
  /// </para>
  /// </remarks>
  public AppenderAttachedImpl()
  {
  }

  /// <summary>
  /// Append on on all attached appenders.
  /// </summary>
  /// <param name="loggingEvent">The event being logged.</param>
  /// <returns>The number of appenders called.</returns>
  /// <remarks>
  /// <para>
  /// Calls the <see cref="IAppender.DoAppend" /> method on all 
  /// attached appenders.
  /// </para>
  /// </remarks>
  public int AppendLoopOnAppenders(LoggingEvent loggingEvent)
  {
    if (loggingEvent is null)
    {
      throw new ArgumentNullException(nameof(loggingEvent));
    }

    // appenderList is null when empty
    if (_appenderList is null)
    {
      return 0;
    }

    _appenderArray ??= _appenderList.ToArray();

    foreach (IAppender appender in _appenderArray)
    {
      try
      {
        appender.DoAppend(loggingEvent);
      }
      catch (Exception e) when (!e.IsFatal())
      {
        LogLog.Error(_declaringType, $"Failed to append to appender [{appender.Name}]", e);
      }
    }
    return _appenderList.Count;
  }

  /// <summary>
  /// Append on on all attached appenders.
  /// </summary>
  /// <param name="loggingEvents">The array of events being logged.</param>
  /// <returns>The number of appenders called.</returns>
  /// <remarks>
  /// <para>
  /// Calls the <see cref="IAppender.DoAppend" /> method on all 
  /// attached appenders.
  /// </para>
  /// </remarks>
  public int AppendLoopOnAppenders(LoggingEvent[] loggingEvents)
  {
    if (loggingEvents is null)
    {
      throw new ArgumentNullException(nameof(loggingEvents));
    }
    if (loggingEvents.Length == 0)
    {
      throw new ArgumentException($"{nameof(loggingEvents)} array must not be empty", nameof(loggingEvents));
    }
    if (loggingEvents.Length == 1)
    {
      // Fall back to single event path
      return AppendLoopOnAppenders(loggingEvents[0]);
    }

    // appenderList is null when empty
    if (_appenderList is null)
    {
      return 0;
    }

    _appenderArray ??= _appenderList.ToArray();

    foreach (IAppender appender in _appenderArray)
    {
      try
      {
        CallAppend(appender, loggingEvents);
      }
      catch (Exception e) when (!e.IsFatal())
      {
        LogLog.Error(_declaringType, $"Failed to append to appender [{appender.Name}]", e);
      }
    }
    return _appenderList.Count;
  }

  /// <summary>
  /// Calls the DoAppende method on the <see cref="IAppender"/> with 
  /// the <see cref="LoggingEvent"/> objects supplied.
  /// </summary>
  /// <param name="appender">The appender</param>
  /// <param name="loggingEvents">The events</param>
  /// <remarks>
  /// <para>
  /// If the <paramref name="appender" /> supports the <see cref="IBulkAppender"/>
  /// interface then the <paramref name="loggingEvents" /> will be passed 
  /// through using that interface. Otherwise the <see cref="LoggingEvent"/>
  /// objects in the array will be passed one at a time.
  /// </para>
  /// </remarks>
  private static void CallAppend(IAppender appender, LoggingEvent[] loggingEvents)
  {
    if (appender is IBulkAppender bulkAppender)
    {
      bulkAppender.DoAppend(loggingEvents);
    }
    else
    {
      foreach (LoggingEvent loggingEvent in loggingEvents)
      {
        appender.DoAppend(loggingEvent);
      }
    }
  }

  /// <summary>
  /// Attaches an appender.
  /// </summary>
  /// <param name="newAppender">The appender to add.</param>
  /// <remarks>
  /// <para>
  /// If the appender is already in the list it won't be added again.
  /// </para>
  /// </remarks>
  public void AddAppender(IAppender newAppender)
  {
    // Null values for newAppender parameter are strictly forbidden.
    if (newAppender is null)
    {
      throw new ArgumentNullException(nameof(newAppender));
    }

    _appenderArray = null;
    _appenderList ??= new AppenderCollection(1);
    if (!_appenderList.Contains(newAppender))
    {
      _appenderList.Add(newAppender);
    }
  }

  /// <summary>
  /// Gets all attached appenders.
  /// </summary>
  /// <returns>
  /// A collection of attached appenders, or <c>null</c> if there
  /// are no attached appenders.
  /// </returns>
  /// <remarks>
  /// <para>
  /// The read only collection of all currently attached appenders.
  /// </para>
  /// </remarks>
  public AppenderCollection Appenders
  {
    get
    {
      if (_appenderList is null)
      {
        // We must always return a valid collection
        return AppenderCollection.EmptyCollection;
      }
      else
      {
        return AppenderCollection.ReadOnly(_appenderList);
      }
    }
  }

  /// <summary>
  /// Gets an attached appender with the specified name.
  /// </summary>
  /// <param name="name">The name of the appender to get.</param>
  /// <returns>
  /// The appender with the name specified, or <c>null</c> if no appender with the
  /// specified name is found.
  /// </returns>
  /// <remarks>
  /// <para>
  /// Lookup an attached appender by name.
  /// </para>
  /// </remarks>
  public IAppender? GetAppender(string? name)
  {
    if (_appenderList is not null && name is not null)
    {
      foreach (IAppender appender in _appenderList)
      {
        if (name == appender.Name)
        {
          return appender;
        }
      }
    }
    return null;
  }

  /// <summary>
  /// Removes all attached appenders.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Removes and closes all attached appenders
  /// </para>
  /// </remarks>
  public void RemoveAllAppenders()
  {
    if (_appenderList is not null)
    {
      foreach (IAppender appender in _appenderList)
      {
        try
        {
          appender.Close();
        }
        catch (Exception e) when (!e.IsFatal())
        {
          LogLog.Error(_declaringType, $"Failed to Close appender [{appender.Name}]", e);
        }
      }
      _appenderList = null;
      _appenderArray = null;
    }
  }

  /// <summary>
  /// Removes the specified appender from the list of attached appenders.
  /// </summary>
  /// <param name="appender">The appender to remove.</param>
  /// <returns>The appender removed from the list</returns>
  /// <remarks>
  /// <para>
  /// The appender removed is not closed.
  /// If you are discarding the appender you must call
  /// <see cref="IAppender.Close"/> on the appender removed.
  /// </para>
  /// </remarks>
  public IAppender? RemoveAppender(IAppender? appender)
  {
    if (appender is not null && _appenderList is not null)
    {
      _appenderList.Remove(appender);
      if (_appenderList.Count == 0)
      {
        _appenderList = null;
      }
      _appenderArray = null;
    }
    return appender;
  }

  /// <summary>
  /// Removes the appender with the specified name from the list of appenders.
  /// </summary>
  /// <param name="name">The name of the appender to remove.</param>
  /// <returns>The appender removed from the list</returns>
  /// <remarks>
  /// <para>
  /// The appender removed is not closed.
  /// If you are discarding the appender you must call
  /// <see cref="IAppender.Close"/> on the appender removed.
  /// </para>
  /// </remarks>
  public IAppender? RemoveAppender(string name) => RemoveAppender(GetAppender(name));

  /// <summary>
  /// List of appenders
  /// </summary>
  private AppenderCollection? _appenderList;

  /// <summary>
  /// Array of appenders, used to cache the appenderList
  /// </summary>
  private IAppender[]? _appenderArray;

  /// <summary>
  /// The fully qualified type of the AppenderAttachedImpl class.
  /// </summary>
  /// <remarks>
  /// Used by the internal logger to record the Type of the
  /// log message.
  /// </remarks>
  private static readonly Type _declaringType = typeof(AppenderAttachedImpl);
}