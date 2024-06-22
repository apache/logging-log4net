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
using System.Threading;
using log4net.Appender;
using log4net.Core;
using log4net.Util;

namespace SampleAppendersApp.Appender
{
  /// <summary>
  /// Appender that forwards LoggingEvents asynchronously
  /// </summary>
  /// <remarks>
  /// This appender forwards LoggingEvents to a list of attached appenders.
  /// The events are forwarded asynchronously using the ThreadPool.
  /// This allows the calling thread to be released quickly, however it does
  /// not guarantee the ordering of events delivered to the attached appenders.
  /// </remarks>
  public sealed class AsyncAppender : IAppender, IBulkAppender, IOptionHandler, IAppenderAttachable
  {
    private readonly object syncRoot = new();

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public void ActivateOptions()
    { }

    /// <inheritdoc/>
    public FixFlags Fix { get; set; } = FixFlags.All;

    /// <inheritdoc/>
    public void Close()
    {
      // Remove all the attached appenders
      lock (syncRoot)
        appenderAttachedImpl?.RemoveAllAppenders();
    }

    /// <inheritdoc/>
    public void DoAppend(LoggingEvent loggingEvent)
    {
      ArgumentNullException.ThrowIfNull(loggingEvent);
      loggingEvent.Fix = Fix;
      ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncAppend), loggingEvent);
    }

    /// <inheritdoc/>
    public void DoAppend(LoggingEvent[] loggingEvents)
    {
      ArgumentNullException.ThrowIfNull(loggingEvents);
      foreach (LoggingEvent loggingEvent in loggingEvents)
        loggingEvent.Fix = Fix;
      ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncAppend), loggingEvents);
    }

    private void AsyncAppend(object? state)
    {
      if (appenderAttachedImpl != null)
      {
        if (state is LoggingEvent loggingEvent)
          appenderAttachedImpl.AppendLoopOnAppenders(loggingEvent);
        else if (state is LoggingEvent[] loggingEvents)
          appenderAttachedImpl.AppendLoopOnAppenders(loggingEvents);
      }
    }

    #region IAppenderAttachable Members

    /// <inheritdoc/>
    public void AddAppender(IAppender appender)
    {
      ArgumentNullException.ThrowIfNull(appender);
      lock (syncRoot)
        (appenderAttachedImpl ??= new()).AddAppender(appender);
    }

    /// <inheritdoc/>
    public AppenderCollection Appenders
    {
      get
      {
        lock (syncRoot)
          return appenderAttachedImpl?.Appenders ?? AppenderCollection.EmptyCollection;
      }
    }

    /// <inheritdoc/>
    public IAppender? GetAppender(string name)
    {
      lock (syncRoot)
      {
        if (appenderAttachedImpl is null || name is null)
          return null;

        return appenderAttachedImpl.GetAppender(name);
      }
    }

    /// <inheritdoc/>
    public void RemoveAllAppenders()
    {
      lock (syncRoot)
        if (appenderAttachedImpl is not null)
        {
          appenderAttachedImpl.RemoveAllAppenders();
          appenderAttachedImpl = null;
        }
    }

    /// <inheritdoc/>
    public IAppender? RemoveAppender(IAppender appender)
    {
      lock (syncRoot)
        if (appender is not null && appenderAttachedImpl is not null)
          return appenderAttachedImpl.RemoveAppender(appender);
      return null;
    }

    /// <inheritdoc/>
    public IAppender? RemoveAppender(string name)
    {
      lock (syncRoot)
        if (name is not null && appenderAttachedImpl is not null)
          return appenderAttachedImpl.RemoveAppender(name);
      return null;
    }

    #endregion

    private AppenderAttachedImpl? appenderAttachedImpl;
  }
}
