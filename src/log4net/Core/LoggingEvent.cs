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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Principal;
using System.Threading;
using log4net.Repository;
using log4net.Util;

namespace log4net.Core;

/// <summary>
/// Portable data structure used by <see cref="LoggingEvent"/>
/// </summary>
/// <author>Nicko Cadell</author>
public struct LoggingEventData
{
  /// <summary>
  /// The logger name.
  /// </summary>
  public string? LoggerName;

  /// <summary>
  /// Level of logging event.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A null level produces varying results depending on the appenders in use.
  /// In many cases it is equivalent of <see cref="Level.All"/>, other times
  /// it is mapped to Debug or Info defaults.
  /// </para>
  /// <para>
  /// Level cannot be Serializable because it is a flyweight.
  /// Due to its special serialization it cannot be declared final either.
  /// </para>
  /// </remarks>
  public Level? Level;

  /// <summary>
  /// The application supplied message.
  /// </summary>
  public string? Message;

  /// <summary>
  /// Gets or sets the name of the thread in which this logging event was generated.
  /// </summary>
  public string? ThreadName;

  /// <summary>
  /// Gets or sets the UTC time the event was logged.
  /// </summary>
  public DateTime TimeStampUtc { get; set; }

  /// <summary>
  /// Location information for the caller.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Location information for the caller.
  /// </para>
  /// </remarks>
  public LocationInfo? LocationInfo;

  /// <summary>
  /// String representation of the user
  /// </summary>
  /// <remarks>
  /// <para>
  /// String representation of the user's windows name,
  /// like DOMAIN\username
  /// </para>
  /// </remarks>
  public string? UserName;

  /// <summary>
  /// String representation of the identity.
  /// </summary>
  /// <remarks>
  /// <para>
  /// String representation of the current thread's principal identity.
  /// </para>
  /// </remarks>
  public string? Identity;

  /// <summary>
  /// The string representation of the exception
  /// </summary>
  /// <remarks>
  /// <para>
  /// The string representation of the exception
  /// </para>
  /// </remarks>
  public string? ExceptionString;

  /// <summary>
  /// String representation of the AppDomain.
  /// </summary>
  /// <remarks>
  /// <para>
  /// String representation of the AppDomain.
  /// </para>
  /// </remarks>
  public string? Domain;

  /// <summary>
  /// Additional event specific properties
  /// </summary>
  /// <remarks>
  /// <para>
  /// A logger or an appender may attach additional
  /// properties to specific events. These properties
  /// have a string key and an object value.
  /// </para>
  /// </remarks>
  public PropertiesDictionary? Properties;
}

/// <summary>
/// The internal representation of logging events. 
/// </summary>
/// <remarks>
/// <para>
/// When an affirmative decision is made to log then a 
/// <see cref="LoggingEvent"/> instance is created. This instance 
/// is passed around to the different log4net components.
/// </para>
/// <para>
/// This class is of concern to those wishing to extend log4net.
/// </para>
/// <para>
/// Some of the values in instances of <see cref="LoggingEvent"/>
/// are considered volatile, that is the values are correct at the
/// time the event is delivered to appenders, but will not be consistent
/// at any time afterward. If an event is to be stored and then processed
/// at a later time these volatile values must be fixed by setting
/// <see cref="Fix"/>. There is a performance penalty
/// for incurred by calling <see cref="Fix"/> but it
/// is essential to maintain data consistency.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
/// <author>Douglas de la Torre</author>
/// <author>Daniel Cazzulino</author>
[Log4NetSerializable]
public class LoggingEvent : ILog4NetSerializable
{
  private static readonly Type declaringType = typeof(LoggingEvent);

  /// <summary>
  /// Initializes a new instance of the <see cref="LoggingEvent" /> class
  /// from the supplied parameters.
  /// </summary>
  /// <param name="callerStackBoundaryDeclaringType">The declaring type of the method that is
  /// the stack boundary into the logging system for this call.</param>
  /// <param name="repository">The repository this event is logged in.</param>
  /// <param name="loggerName">The name of the logger of this event.</param>
  /// <param name="level">
  /// The level of this event.
  /// A null level produces varying results depending on the appenders in use.
  /// In many cases it is equivalent of <see cref="Level.All"/>, other times
  /// it is mapped to Debug or Info defaults.
  /// </param>
  /// <param name="message">The message of this event.</param>
  /// <param name="exception">The exception for this event.</param>
  /// <remarks>
  /// <para>
  /// Except <see cref="TimeStamp"/>, <see cref="Level"/> and <see cref="LoggerName"/>, 
  /// all fields of <see cref="LoggingEvent"/> are lazily filled when actually needed. Set
  /// <see cref="Fix"/> to cache all data locally to prevent inconsistencies.
  /// </para>
  /// <para>This method is called by the log4net framework
  /// to create a logging event.
  /// </para>
  /// </remarks>
  public LoggingEvent(
      Type? callerStackBoundaryDeclaringType,
      ILoggerRepository? repository,
      string? loggerName,
      Level? level,
      object? message,
      Exception? exception)
  {
    this.callerStackBoundaryDeclaringType = callerStackBoundaryDeclaringType;
    this.message = message;
    Repository = repository;
    ExceptionObject = exception;

    data.LoggerName = loggerName;
    data.Level = level;

    // Store the event creation time
    data.TimeStampUtc = DateTime.UtcNow;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="LoggingEvent" /> class 
  /// using specific data.
  /// </summary>
  /// <param name="callerStackBoundaryDeclaringType">The declaring type of the method that is
  /// the stack boundary into the logging system for this call.</param>
  /// <param name="repository">The repository this event is logged in.</param>
  /// <param name="data">Data used to initialize the logging event.</param>
  /// <param name="fixedData">The fields in the <paranref name="data"/> struct that have already been fixed.</param>
  /// <remarks>
  /// <para>
  /// This constructor is provided to allow a <see cref="LoggingEvent" />
  /// to be created independently of the log4net framework. This can
  /// be useful if you require a custom serialization scheme.
  /// </para>
  /// <para>
  /// Use the <see cref="M:GetLoggingEventData(FixFlags)"/> method to obtain an 
  /// instance of the <see cref="LoggingEventData"/> class.
  /// </para>
  /// <para>
  /// The <paramref name="fixedData"/> parameter should be used to specify which fields in the
  /// <paramref name="data"/> struct have been preset. Fields not specified in the <paramref name="fixedData"/>
  /// will be captured from the environment if requested or fixed.
  /// </para>
  /// </remarks>
  public LoggingEvent(
      Type? callerStackBoundaryDeclaringType,
      ILoggerRepository? repository,
      LoggingEventData data,
      FixFlags fixedData)
  {
    this.callerStackBoundaryDeclaringType = callerStackBoundaryDeclaringType;
    Repository = repository;

    this.data = data;
    fixFlags = fixedData;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="LoggingEvent" /> class 
  /// using specific data.
  /// </summary>
  /// <param name="callerStackBoundaryDeclaringType">The declaring type of the method that is
  /// the stack boundary into the logging system for this call.</param>
  /// <param name="repository">The repository this event is logged in.</param>
  /// <param name="data">Data used to initialize the logging event.</param>
  /// <remarks>
  /// <para>
  /// This constructor is provided to allow a <see cref="LoggingEvent" />
  /// to be created independently of the log4net framework. This can
  /// be useful if you require a custom serialization scheme.
  /// </para>
  /// <para>
  /// Use the <see cref="M:GetLoggingEventData(FixFlags)"/> method to obtain an 
  /// instance of the <see cref="LoggingEventData"/> class.
  /// </para>
  /// <para>
  /// This constructor sets this objects <see cref="Fix"/> flags to <see cref="FixFlags.All"/>,
  /// this assumes that all the data relating to this event is passed in via the <paramref name="data"/>
  /// parameter and no other data should be captured from the environment.
  /// </para>
  /// </remarks>
  public LoggingEvent(
      Type? callerStackBoundaryDeclaringType,
      ILoggerRepository? repository,
      LoggingEventData data)
    : this(callerStackBoundaryDeclaringType, repository, data, FixFlags.All)
  {
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="LoggingEvent" /> class 
  /// using specific data.
  /// </summary>
  /// <param name="data">Data used to initialize the logging event.</param>
  /// <remarks>
  /// <para>
  /// This constructor is provided to allow a <see cref="LoggingEvent" />
  /// to be created independently of the log4net framework. This can
  /// be useful if you require a custom serialization scheme.
  /// </para>
  /// <para>
  /// Use the <see cref="M:GetLoggingEventData(FixFlags)"/> method to obtain an 
  /// instance of the <see cref="LoggingEventData"/> class.
  /// </para>
  /// <para>
  /// This constructor sets this objects <see cref="Fix"/> flags to <see cref="FixFlags.All"/>,
  /// this assumes that all the data relating to this event is passed in via the <paramref name="data"/>
  /// parameter and no other data should be captured from the environment.
  /// </para>
  /// </remarks>
  public LoggingEvent(LoggingEventData data) : this(null, null, data)
  {
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="LoggingEvent" /> class.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This constructor is provided to allow deserialization using System.Text.Json
  /// or Newtonsoft.Json.
  /// </para>
  /// <para>
  /// Use the <see cref="M:GetLoggingEventData(FixFlags)"/> method to obtain an 
  /// instance of the <see cref="LoggingEventData"/> class.
  /// </para>
  /// <para>
  /// This constructor sets this objects <see cref="Fix"/> flags to <see cref="FixFlags.None"/>.
  /// </para>
  /// </remarks>
  public LoggingEvent() : this(null, null, new LoggingEventData(), FixFlags.None)
  {
  }

  /// <summary>
  /// Serialization constructor
  /// </summary>
  /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data.</param>
  /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
  /// <remarks>
  /// <para>
  /// Initializes a new instance of the <see cref="LoggingEvent" /> class 
  /// with serialized data.
  /// </para>
  /// </remarks>
  protected LoggingEvent(SerializationInfo info, StreamingContext context)
  {
    data.LoggerName = info.GetString("LoggerName");

    // Note we are deserializing the whole level object. That is the
    // name and the value. This value is correct for the source 
    // hierarchy but may not be for the target hierarchy that this
    // event may be re-logged into. If it is to be re-logged it may
    // be necessary to re-lookup the level based only on the name.
    data.Level = info.GetValue("Level", typeof(Level)) as Level;

    data.Message = info.GetString("Message");
    data.ThreadName = info.GetString("ThreadName");

    // Favor the newer serialization tag 'TimeStampUtc' while supporting the obsolete format from pre-3.0.
    try
    {
      data.TimeStampUtc = info.GetDateTime("TimeStampUtc");
    }
    catch (SerializationException)
    {
      data.TimeStampUtc = info.GetDateTime("TimeStamp").ToUniversalTime();
    }

    data.LocationInfo = info.GetValue("LocationInfo", typeof(LocationInfo)) as LocationInfo;
    data.UserName = info.GetString("UserName");
    data.ExceptionString = info.GetString("ExceptionString");
    data.Properties = info.GetValue("Properties", typeof(PropertiesDictionary)) as PropertiesDictionary;
    data.Domain = info.GetString("Domain");
    data.Identity = info.GetString("Identity");

    // We have restored all the values of this instance, i.e. all the values are fixed
    // Set the fix flags otherwise the data values may be overwritten from the current environment.
    fixFlags = FixFlags.All;
  }

  /// <summary>
  /// Gets the time when the current process started.
  /// </summary>
  /// <value>
  /// This is the time when this process started.
  /// </value>
  /// <remarks>
  /// <para>
  /// The TimeStamp is stored internally in UTC and converted to the local time zone for this computer.
  /// </para>
  /// <para>
  /// Tries to get the start time for the current process.
  /// Failing that it returns the time of the first call to
  /// this property.
  /// </para>
  /// <para>
  /// Note that AppDomains may be loaded and unloaded within the
  /// same process without the process terminating and therefore
  /// without the process start time being reset.
  /// </para>
  /// </remarks>
  public static DateTime StartTime => SystemInfo.ProcessStartTimeUtc.ToLocalTime();

  /// <summary>
  /// Gets the UTC time when the current process started.
  /// </summary>
  /// <value>
  /// This is the UTC time when this process started.
  /// </value>
  /// <remarks>
  /// <para>
  /// Tries to get the start time for the current process.
  /// Failing that it returns the time of the first call to
  /// this property.
  /// </para>
  /// <para>
  /// Note that AppDomains may be loaded and unloaded within the
  /// same process without the process terminating and therefore
  /// without the process start time being reset.
  /// </para>
  /// </remarks>
  public static DateTime StartTimeUtc => SystemInfo.ProcessStartTimeUtc;

  /// <summary>
  /// Gets the <see cref="Level" /> of the logging event.
  /// A null level produces varying results depending on the appenders in use.
  /// In many cases it is equivalent of <see cref="Level.All"/>, other times
  /// it is mapped to Debug or Info defaults.
  /// </summary>
  public Level? Level => data.Level;

  /// <summary>
  /// Gets the time of the logging event.
  /// </summary>
  /// <value>
  /// The time of the logging event.
  /// </value>
  /// <remarks>
  /// <para>
  /// The TimeStamp is stored in UTC and converted to the local time zone for this computer.
  /// </para>
  /// </remarks>
  public DateTime TimeStamp => data.TimeStampUtc.ToLocalTime();

  /// <summary>
  /// Gets UTC the time of the logging event.
  /// </summary>
  /// <value>
  /// The UTC time of the logging event.
  /// </value>
  public DateTime TimeStampUtc => data.TimeStampUtc;

  /// <summary>
  /// Gets the name of the logger that logged the event.
  /// </summary>
  public string? LoggerName => data.LoggerName;

  /// <summary>
  /// Gets the location information for this logging event.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The collected information is cached for future use.
  /// </para>
  /// <para>
  /// See the <see cref="LocationInfo"/> class for more information on
  /// supported frameworks and the different behavior in Debug and
  /// Release builds.
  /// </para>
  /// </remarks>
  public LocationInfo? LocationInformation
  {
    get
    {
      if (data.LocationInfo is null && cacheUpdatable)
      {
        data.LocationInfo = new LocationInfo(callerStackBoundaryDeclaringType);
      }

      return data.LocationInfo;
    }
  }

  /// <summary>
  /// Gets the message object used to initialize this event.
  /// </summary>
  /// <value>
  /// The message object used to initialize this event.
  /// </value>
  /// <remarks>
  /// <para>
  /// Gets the message object used to initialize this event.
  /// Note that this event may not have a valid message object.
  /// If the event is serialized the message object will not 
  /// be transferred. To get the text of the message the
  /// <see cref="RenderedMessage"/> property must be used 
  /// not this property.
  /// </para>
  /// <para>
  /// If there is no defined message object for this event then
  /// null will be returned.
  /// </para>
  /// </remarks>
  public object? MessageObject
  {
    get { return message; }
    protected set { message = value; }
  }

  /// <summary>
  /// Gets the exception object used to initialize this event.
  /// </summary>
  /// <value>
  /// The exception object used to initialize this event.
  /// </value>
  /// <remarks>
  /// <para>
  /// Gets the exception object used to initialize this event.
  /// Note that this event may not have a valid exception object.
  /// If the event is serialized the exception object will not 
  /// be transferred. To get the text of the exception the
  /// <see cref="GetExceptionString"/> method must be used 
  /// not this property.
  /// </para>
  /// <para>
  /// If there is no defined exception object for this event then
  /// null will be returned.
  /// </para>
  /// </remarks>
  public Exception? ExceptionObject { get; }

  /// <summary>
  /// The <see cref="ILoggerRepository"/> that this event was created in.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The <see cref="ILoggerRepository"/> that this event was created in.
  /// </para>
  /// </remarks>
  public ILoggerRepository? Repository { get; private set; }

  /// <summary>
  /// Ensure that the repository is set.
  /// </summary>
  /// <param name="repository">the value for the repository</param>
  internal void EnsureRepository(ILoggerRepository? repository)
  {
    if (repository is not null)
    {
      Repository = repository;
    }
  }

  /// <summary>
  /// Gets the message, rendered through the <see cref="ILoggerRepository.RendererMap" />.
  /// </summary>
  /// <value>
  /// The message rendered through the <see cref="ILoggerRepository.RendererMap" />.
  /// </value>
  /// <remarks>
  /// <para>
  /// The collected information is cached for future use.
  /// </para>
  /// </remarks>
  public virtual string? RenderedMessage
  {
    get
    {
      if (data.Message is null && cacheUpdatable)
      {
        if (message is null)
        {
          data.Message = string.Empty;
        }
        else if (message is string s)
        {
          data.Message = s;
        }
        else if (Repository is not null)
        {
          data.Message = Repository.RendererMap.FindAndRender(message);
        }
        else
        {
          // Very last resort
          data.Message = message.ToString();
        }
      }

      return data.Message;
    }
  }

  /// <summary>
  /// Write the rendered message to a TextWriter
  /// </summary>
  /// <param name="writer">the writer to write the message to</param>
  /// <remarks>
  /// <para>
  /// Unlike the <see cref="RenderedMessage"/> property this method
  /// does store the message data in the internal cache. Therefore 
  /// if called only once this method should be faster than the
  /// <see cref="RenderedMessage"/> property, however if the message is
  /// to be accessed multiple times then the property will be more efficient.
  /// </para>
  /// </remarks>
  public virtual void WriteRenderedMessage(TextWriter writer)
  {
    if (data.Message is not null)
    {
      writer.Write(data.Message);
    }
    else
    {
      if (message is not null)
      {
        if (message is string s)
        {
          writer.Write(s);
        }
        else if (Repository is not null)
        {
          Repository.RendererMap.FindAndRender(message, writer);
        }
        else
        {
          // Very last resort
          writer.Write(message.ToString());
        }
      }
    }
  }

  /// <summary>
  /// Gets the name of the current thread.  
  /// </summary>
  /// <value>
  /// The name of the current thread, or the thread ID when 
  /// the name is not available.
  /// </value>
  /// <remarks>
  /// <para>
  /// The collected information is cached for future use.
  /// </para>
  /// </remarks>
  public string? ThreadName
  {
    get
    {
      if (data.ThreadName is null && cacheUpdatable)
      {
        data.ThreadName = ReviseThreadName(Thread.CurrentThread.Name);
      }

      return data.ThreadName;
    }
  }

  /// <summary>
  /// Returns a 'meaningful' name for the thread (or its Id)
  /// </summary>
  /// <param name="threadName">Name</param>
  /// <returns>Meaningful name</returns>
  private static string ReviseThreadName(string? threadName)
  {
    // '.NET ThreadPool Worker' appears as a default thread name in the .NET 6-7 thread pool.
    // '.NET TP Worker' is the default thread name in the .NET 8+ thread pool.
    // '.NET Long Running Task' is used for long running tasks
    // Prefer the numeric thread ID instead.
    if (threadName is string { Length: > 0 } name
        && !name.StartsWith(".NET ", StringComparison.Ordinal))
    {
      return name;
    }
    // The thread name is not available or unsuitable, therefore we use the ManagedThreadId.
    try
    {
      return SystemInfo.CurrentThreadId.ToString(NumberFormatInfo.InvariantInfo);
    }
    catch (SecurityException)
    {
      // This security exception will occur if the caller does not have 
      // some undefined set of SecurityPermission flags.
      LogLog.Debug(declaringType,
        "Security exception while trying to get current thread ID. Error Ignored. Empty thread name.");

      // As a last resort use the hash code of the Thread object
      return Thread.CurrentThread.GetHashCode().ToString(CultureInfo.InvariantCulture);
    }
  }

  /// <summary>
  /// Gets the name of the current user.
  /// </summary>
  /// <value>
  /// The name of the current user, or <c>NOT AVAILABLE</c> when the 
  /// underlying runtime has no support for retrieving the name of the 
  /// current user.
  /// </value>
  /// <remarks>
  /// <para>
  /// Calls <c>WindowsIdentity.GetCurrent().Name</c> to get the name of
  /// the current windows user.
  /// </para>
  /// <para>
  /// To improve performance, we could cache the string representation of 
  /// the name, and reuse that as long as the identity stayed constant.  
  /// Once the identity changed, we would need to re-assign and re-render 
  /// the string.
  /// </para>
  /// <para>
  /// However, the <c>WindowsIdentity.GetCurrent()</c> call seems to 
  /// return different objects every time, so the current implementation 
  /// doesn't do this type of caching.
  /// </para>
  /// <para>
  /// Timing for these operations:
  /// </para>
  /// <list type="table">
  ///   <listheader>
  ///     <term>Method</term>
  ///     <description>Results</description>
  ///   </listheader>
  ///   <item>
  ///      <term><c>WindowsIdentity.GetCurrent()</c></term>
  ///      <description>10000 loops, 00:00:00.2031250 seconds</description>
  ///   </item>
  ///   <item>
  ///      <term><c>WindowsIdentity.GetCurrent().Name</c></term>
  ///      <description>10000 loops, 00:00:08.0468750 seconds</description>
  ///   </item>
  /// </list>
  /// <para>
  /// This means we could speed things up almost 40 times by caching the 
  /// value of the <c>WindowsIdentity.GetCurrent().Name</c> property, since 
  /// this takes (8.04-0.20) = 7.84375 seconds.
  /// </para>
  /// </remarks>
  public string UserName =>
      data.UserName ??= TryGetCurrentUserName() ?? SystemInfo.NotAvailableText;

  private string? TryGetCurrentUserName()
  {
    if (_platformDoesNotSupportWindowsIdentity)
    {
      // we've already received one PlatformNotSupportedException
      // and it's highly unlikely that will change
      return Environment.UserName;
    }

    try
    {
      return _cachedWindowsIdentityUserName ??=
          TryReadWindowsIdentityUserName();
    }
    catch (PlatformNotSupportedException)
    {
      _platformDoesNotSupportWindowsIdentity = true;
      return Environment.UserName;
    }
    catch (SecurityException)
    {
      // This security exception will occur if the caller does not have 
      // some undefined set of SecurityPermission flags.
      LogLog.Debug(
          declaringType,
          "Security exception while trying to get current windows identity. Error Ignored."
      );
      return Environment.UserName;
    }
    catch
    {
      return null;
    }
  }

  private string? _cachedWindowsIdentityUserName;
  private static string TryReadWindowsIdentityUserName()
  {
#if !NET462_OR_GREATER
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      return string.Empty;
    }
#endif
    using var identity = WindowsIdentity.GetCurrent();
    return identity?.Name ?? string.Empty;
  }

  private static bool _platformDoesNotSupportWindowsIdentity;

  /// <summary>
  /// Gets the identity of the current thread principal.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Calls <c>System.Threading.Thread.CurrentPrincipal.Identity.Name</c> to get
  /// the name of the current thread principal.
  /// </para>
  /// </remarks>
  public string? Identity
  {
    get
    {
      if (data.Identity is null && cacheUpdatable)
      {
        try
        {
          if (Thread.CurrentPrincipal?.Identity?.Name is string name)
          {
            data.Identity = name;
          }
          else
          {
            data.Identity = string.Empty;
          }
        }
        catch (ObjectDisposedException)
        {
          // This exception will occur if Thread.CurrentPrincipal.Identity is not null but
          // the getter of the property Name tries to access disposed objects.
          // Seen to happen on IIS 7 or greater with windows authentication.
          LogLog.Debug(declaringType,
              "Object disposed exception while trying to get current thread principal. Error Ignored. Empty identity name.");

          data.Identity = string.Empty;
        }
        catch (SecurityException)
        {
          // This security exception will occur if the caller does not have 
          // some undefined set of SecurityPermission flags.
          LogLog.Debug(declaringType,
              "Security exception while trying to get current thread principal. Error Ignored. Empty identity name.");

          data.Identity = string.Empty;
        }
      }

      return data.Identity;
    }
  }

  /// <summary>
  /// Gets the AppDomain friendly name.
  /// </summary>
  public string? Domain
  {
    get
    {
      if (data.Domain is null && cacheUpdatable)
      {
        data.Domain = SystemInfo.ApplicationFriendlyName;
      }

      return data.Domain;
    }
  }

  /// <summary>
  /// Additional event specific properties.
  /// </summary>
  /// <value>
  /// Additional event specific properties.
  /// </value>
  /// <remarks>
  /// <para>
  /// A logger or an appender may attach additional
  /// properties to specific events. These properties
  /// have a string key and an object value.
  /// </para>
  /// <para>
  /// This property is for events that have been added directly to
  /// this event. The aggregate properties (which include these
  /// event properties) can be retrieved using <see cref="LookupProperty"/>
  /// and <see cref="GetProperties"/>.
  /// </para>
  /// <para>
  /// Once the properties have been fixed <see cref="Fix"/> this property
  /// returns the combined cached properties. This ensures that updates to
  /// this property are always reflected in the underlying storage. When
  /// returning the combined properties there may be more keys in the
  /// Dictionary than expected.
  /// </para>
  /// </remarks>
  public PropertiesDictionary Properties
  {
    get
    {
      // If we have cached properties then return that otherwise changes will be lost
      if (data.Properties is not null)
      {
        return data.Properties;
      }

      eventProperties ??= [];
      return eventProperties;
    }
  }

  /// <summary>
  /// Gets the fixed fields in this event, or on set, fixes fields specified in the value.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Fields will not be fixed if they have previously been fixed.
  /// It is not possible to 'unfix' a field.
  /// </para>
  /// </remarks>
  public FixFlags Fix
  {
    get => fixFlags;
    set => FixVolatileData(value);
  }

  /// <summary>
  /// Serializes this object into the <see cref="SerializationInfo" /> provided.
  /// </summary>
  /// <param name="info">The <see cref="SerializationInfo" /> to populate with data.</param>
  /// <param name="context">The destination for this serialization.</param>
  /// <remarks>
  /// <para>
  /// The data in this event must be fixed before it can be serialized.
  /// </para>
  /// <para>
  /// The <see cref="Fix"/> property must be set during the
  /// <see cref="log4net.Appender.IAppender.DoAppend"/> method call if this event 
  /// is to be used outside that method.
  /// </para>
  /// </remarks>
  [SecurityCritical]
  [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand,
      SerializationFormatter = true)]
  public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
  {
    // The caller must set Fix before this object can be serialized.

    info.AddValue("LoggerName", data.LoggerName);
    info.AddValue("Level", data.Level);
    info.AddValue("Message", data.Message);
    info.AddValue("ThreadName", data.ThreadName);

    // Serialize UTC->local time for backward compatibility with obsolete 'TimeStamp' property.
    info.AddValue("TimeStamp", data.TimeStampUtc.ToLocalTime());

    // Also add the UTC time under its own serialization tag.
    info.AddValue("TimeStampUtc", data.TimeStampUtc);

    info.AddValue("LocationInfo", data.LocationInfo);
    info.AddValue("UserName", data.UserName);
    info.AddValue("ExceptionString", data.ExceptionString);
    info.AddValue("Properties", data.Properties);
    info.AddValue("Domain", data.Domain);
    info.AddValue("Identity", data.Identity);
  }

  /// <summary>
  /// Gets the portable data for this <see cref="LoggingEvent" />.
  /// </summary>
  /// <returns>The <see cref="LoggingEventData"/> for this event.</returns>
  /// <remarks>
  /// <para>
  /// A new <see cref="LoggingEvent"/> can be constructed using a
  /// <see cref="LoggingEventData"/> instance.
  /// </para>
  /// <para>
  /// Does a <see cref="FixFlags.Partial"/> fix of the data
  /// in the logging event before returning the event data.
  /// </para>
  /// </remarks>
  public LoggingEventData GetLoggingEventData()
  {
    return GetLoggingEventData(FixFlags.Partial);
  }

  /// <summary>
  /// Gets the portable data for this <see cref="LoggingEvent" />.
  /// </summary>
  /// <param name="fixFlags">The set of data to ensure is fixed in the LoggingEventData</param>
  /// <returns>The <see cref="LoggingEventData"/> for this event.</returns>
  /// <remarks>
  /// <para>
  /// A new <see cref="LoggingEvent"/> can be constructed using a
  /// <see cref="LoggingEventData"/> instance.
  /// </para>
  /// </remarks>
  public LoggingEventData GetLoggingEventData(FixFlags fixFlags)
  {
    Fix = fixFlags;
    return data;
  }

  /// <summary>
  /// Returns this event's exception's rendered using the 
  /// <see cref="ILoggerRepository.RendererMap" />.
  /// </summary>
  /// <returns>
  /// This event's exception's rendered using the <see cref="ILoggerRepository.RendererMap" />.
  /// </returns>
  /// <remarks>
  /// <para>
  /// Returns this event's exception's rendered using the 
  /// <see cref="ILoggerRepository.RendererMap" />.
  /// </para>
  /// </remarks>
  public string? GetExceptionString()
  {
    if (data.ExceptionString is null && cacheUpdatable)
    {
      if (ExceptionObject is not null)
      {
        if (Repository is not null)
        {
          // Render exception using the repositories renderer map
          data.ExceptionString = Repository.RendererMap.FindAndRender(ExceptionObject);
        }
        else
        {
          // Very last resort
          data.ExceptionString = ExceptionObject.ToString();
        }
      }
      else
      {
        data.ExceptionString = string.Empty;
      }
    }

    return data.ExceptionString;
  }

  /// <summary>
  /// Fix the fields specified by the <see cref="FixFlags"/> parameter
  /// </summary>
  /// <param name="flags">the fields to fix</param>
  /// <remarks>
  /// <para>
  /// Only fields specified in the <paramref name="flags"/> will be fixed.
  /// Fields will not be fixed if they have previously been fixed.
  /// It is not possible to 'unfix' a field.
  /// </para>
  /// </remarks>
  protected virtual void FixVolatileData(FixFlags flags)
  {
    // Unlock the cache so that new values can be stored
    // This may not be ideal if we are no longer in the correct context
    // and someone calls fix. 
    cacheUpdatable = true;

    // determine the flags that we are actually fixing
    FixFlags updateFlags = (flags ^ fixFlags) & flags;

    if (updateFlags > 0)
    {
      if ((updateFlags & FixFlags.Message) != 0)
      {
        // Force the message to be rendered
        _ = RenderedMessage;

        fixFlags |= FixFlags.Message;
      }

      if ((updateFlags & FixFlags.ThreadName) != 0)
      {
        // Grab the thread name
        _ = ThreadName;

        fixFlags |= FixFlags.ThreadName;
      }

      if ((updateFlags & FixFlags.LocationInfo) != 0)
      {
        // Force the location information to be loaded
        _ = LocationInformation;

        fixFlags |= FixFlags.LocationInfo;
      }

      if ((updateFlags & FixFlags.UserName) != 0)
      {
        // Grab the user name
        _ = UserName;

        fixFlags |= FixFlags.UserName;
      }

      if ((updateFlags & FixFlags.Domain) != 0)
      {
        // Grab the domain name
        _ = Domain;

        fixFlags |= FixFlags.Domain;
      }

      if ((updateFlags & FixFlags.Identity) != 0)
      {
        // Grab the identity
        _ = Identity;

        fixFlags |= FixFlags.Identity;
      }

      if ((updateFlags & FixFlags.Exception) != 0)
      {
        // Force the exception text to be loaded
        _ = GetExceptionString();

        fixFlags |= FixFlags.Exception;
      }

      if ((updateFlags & FixFlags.Properties) != 0)
      {
        CacheProperties();

        fixFlags |= FixFlags.Properties;
      }
    }

    // Finally lock everything we've cached.
    cacheUpdatable = false;
  }

  private void CreateCompositeProperties()
  {
    var compositeProperties = new CompositeProperties();

    if (eventProperties is not null)
    {
      compositeProperties.Add(eventProperties);
    }
    var logicalThreadProperties = LogicalThreadContext.Properties.GetProperties(false);
    if (logicalThreadProperties is not null)
    {
      compositeProperties.Add(logicalThreadProperties);
    }
    if (ThreadContext.Properties.GetProperties(false) is PropertiesDictionary threadProperties)
    {
      compositeProperties.Add(threadProperties);
    }

    // TODO: Add Repository Properties

    // event properties
    bool shouldFixUserName = (fixFlags & FixFlags.UserName) != 0;
    bool shouldFixIdentity = (fixFlags & FixFlags.Identity) != 0;
    if (shouldFixIdentity || shouldFixUserName)
    {
      var eventProperties = new PropertiesDictionary();
      if (shouldFixUserName)
      {
        eventProperties[UserNameProperty] = UserName;
      }

      if (shouldFixIdentity)
      {
        eventProperties[IdentityProperty] = Identity;
      }

      compositeProperties.Add(eventProperties);
    }

    compositeProperties.Add(GlobalContext.Properties.GetReadOnlyProperties());
    this.compositeProperties = compositeProperties;
  }

  private void CacheProperties()
  {
    if (data.Properties is null && cacheUpdatable)
    {
      if (compositeProperties is null)
      {
        CreateCompositeProperties();
      }

      var flattenedProperties = compositeProperties!.Flatten();

      var fixedProperties = new PropertiesDictionary();

      // Validate properties
      foreach (KeyValuePair<string, object?> entry in flattenedProperties)
      {
        object? val = entry.Value;

        // Fix any IFixingRequired objects
        if (entry.Value is IFixingRequired fixingRequired)
        {
          val = fixingRequired.GetFixedObject();
        }

        // Strip keys with null values
        if (val is not null)
        {
          fixedProperties[entry.Key] = val;
        }
      }

      data.Properties = fixedProperties;
    }
  }

  /// <summary>
  /// Looks up a composite property in this event
  /// </summary>
  /// <param name="key">the key for the property to lookup</param>
  /// <returns>the value for the property</returns>
  /// <remarks>
  /// <para>
  /// This event has composite properties that combine properties from
  /// several different contexts in the following order:
  /// <list type="definition">
  ///    <item>
  ///     <term>this event's properties</term>
  ///     <description>
  ///     This event has <see cref="Properties"/> that can be set. These 
  ///     properties are specific to this event only.
  ///     </description>
  ///   </item>
  ///   <item>
  ///     <term>the thread properties</term>
  ///     <description>
  ///     The <see cref="ThreadContext.Properties"/> that are set on the current
  ///     thread. These properties are shared by all events logged on this thread.
  ///     </description>
  ///   </item>
  ///   <item>
  ///     <term>the global properties</term>
  ///     <description>
  ///     The <see cref="GlobalContext.Properties"/> that are set globally. These 
  ///     properties are shared by all the threads in the AppDomain.
  ///     </description>
  ///   </item>
  /// </list>
  /// </para>
  /// </remarks>
  public object? LookupProperty(string key)
  {
    if (data.Properties is not null)
    {
      return data.Properties[key];
    }

    if (compositeProperties is null)
    {
      CreateCompositeProperties();
    }

    return compositeProperties![key];
  }

  /// <summary>
  /// Get all the composite properties in this event
  /// </summary>
  /// <returns>the <see cref="PropertiesDictionary"/> containing all the properties</returns>
  /// <remarks>
  /// <para>
  /// See <see cref="LookupProperty"/> for details of the composite properties 
  /// stored by the event.
  /// </para>
  /// <para>
  /// This method returns a single <see cref="PropertiesDictionary"/> containing all the
  /// properties defined for this event.
  /// </para>
  /// </remarks>
  public PropertiesDictionary GetProperties()
  {
    if (data.Properties is not null)
    {
      return data.Properties;
    }

    if (compositeProperties is null)
    {
      CreateCompositeProperties();
    }

    return compositeProperties!.Flatten();
  }

  /// <summary>
  /// The internal logging event data.
  /// </summary>
  private LoggingEventData data;

  /// <summary>
  /// Location information for the caller.
  /// </summary>
  public LocationInfo? LocationInfo => data.LocationInfo;

  /// <summary>
  /// The internal logging event data.
  /// </summary>
  private CompositeProperties? compositeProperties;

  /// <summary>
  /// The internal logging event data.
  /// </summary>
  private PropertiesDictionary? eventProperties;

  /// <summary>
  /// The fully qualified Type of the calling 
  /// logger class in the stack frame (i.e. the declaring type of the method).
  /// </summary>
  private readonly Type? callerStackBoundaryDeclaringType;

  /// <summary>
  /// The application supplied message of logging event.
  /// </summary>
  private object? message;

  /// <summary>
  /// The fix state for this event
  /// </summary>
  /// <remarks>
  /// These flags indicate which fields have been fixed.
  /// Not serialized.
  /// </remarks>
  private FixFlags fixFlags = FixFlags.None;

  /// <summary>
  /// Indicated that the internal cache is updateable (ie not fixed)
  /// </summary>
  /// <remarks>
  /// This is a separate flag to fixFlags as it allows incremental fixing and simpler
  /// changes in the caching strategy.
  /// </remarks>
  private bool cacheUpdatable = true;

  /// <summary>
  /// The key into the Properties map for the host name value.
  /// </summary>
  public const string HostNameProperty = "log4net:HostName";

  /// <summary>
  /// The key into the Properties map for the thread identity value.
  /// </summary>
  public const string IdentityProperty = "log4net:Identity";

  /// <summary>
  /// The key into the Properties map for the user name value.
  /// </summary>
  public const string UserNameProperty = "log4net:UserName";
}