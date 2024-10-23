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
using System.Globalization;

using log4net.Repository;
using log4net.Util;

namespace log4net.Core;

/// <summary>
/// Implementation of <see cref="ILog"/> wrapper interface.
/// </summary>
/// <remarks>
/// <para>
/// This implementation of the <see cref="ILog"/> interface
/// forwards to the <see cref="ILogger"/> held by the base class.
/// </para>
/// <para>
/// This logger has methods to allow the caller to log at the following
/// levels:
/// </para>
/// <list type="definition">
///   <item>
///     <term>DEBUG</term>
///     <description>
///     The <see cref="Debug(object)"/> and <see cref="DebugFormat(string, object[])"/> methods log messages
///     at the <c>DEBUG</c> level. That is the level with that name defined in the
///     repositories <see cref="ILoggerRepository.LevelMap"/>. The default value
///     for this level is <see cref="Level.Debug"/>. The <see cref="IsDebugEnabled"/>
///     property tests if this level is enabled for logging.
///     </description>
///   </item>
///   <item>
///     <term>INFO</term>
///     <description>
///     The <see cref="Info(object)"/> and <see cref="InfoFormat(string, object[])"/> methods log messages
///     at the <c>INFO</c> level. That is the level with that name defined in the
///     repositories <see cref="ILoggerRepository.LevelMap"/>. The default value
///     for this level is <see cref="Level.Info"/>. The <see cref="IsInfoEnabled"/>
///     property tests if this level is enabled for logging.
///     </description>
///   </item>
///   <item>
///     <term>WARN</term>
///     <description>
///     The <see cref="Warn(object)"/> and <see cref="WarnFormat(string, object[])"/> methods log messages
///     at the <c>WARN</c> level. That is the level with that name defined in the
///     repositories <see cref="ILoggerRepository.LevelMap"/>. The default value
///     for this level is <see cref="Level.Warn"/>. The <see cref="IsWarnEnabled"/>
///     property tests if this level is enabled for logging.
///     </description>
///   </item>
///   <item>
///     <term>ERROR</term>
///     <description>
///     The <see cref="Error(object)"/> and <see cref="ErrorFormat(string, object[])"/> methods log messages
///     at the <c>ERROR</c> level. That is the level with that name defined in the
///     repositories <see cref="ILoggerRepository.LevelMap"/>. The default value
///     for this level is <see cref="Level.Error"/>. The <see cref="IsErrorEnabled"/>
///     property tests if this level is enabled for logging.
///     </description>
///   </item>
///   <item>
///     <term>FATAL</term>
///     <description>
///     The <see cref="Fatal(object)"/> and <see cref="FatalFormat(string, object[])"/> methods log messages
///     at the <c>FATAL</c> level. That is the level with that name defined in the
///     repositories <see cref="ILoggerRepository.LevelMap"/>. The default value
///     for this level is <see cref="Level.Fatal"/>. The <see cref="IsFatalEnabled"/>
///     property tests if this level is enabled for logging.
///     </description>
///   </item>
/// </list>
/// <para>
/// The values for these levels and their semantic meanings can be changed by 
/// configuring the <see cref="ILoggerRepository.LevelMap"/> for the repository.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public class LogImpl : LoggerWrapperImpl, ILog
{
  /// <summary>
  /// Construct a new wrapper for the specified logger.
  /// </summary>
  /// <param name="logger">The logger to wrap.</param>
  /// <remarks>
  /// <para>
  /// Construct a new wrapper for the specified logger.
  /// </para>
  /// </remarks>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2214:Do not call overridable methods in constructors")]
  public LogImpl(ILogger logger) 
    : base(logger)
  {
    if (logger.EnsureNotNull().Repository is ILoggerRepository repository)
    {
      // Listen for changes to the repository
      repository.ConfigurationChanged += LoggerRepositoryConfigurationChanged;

      // load the current levels
      ReloadLevels(repository);
    }
  }

  /// <summary>
  /// Virtual method called when the configuration of the repository changes
  /// </summary>
  /// <param name="repository">the repository holding the levels</param>
  /// <remarks>
  /// <para>
  /// Virtual method called when the configuration of the repository changes
  /// </para>
  /// </remarks>
  protected virtual void ReloadLevels(ILoggerRepository repository)
  {
    LevelMap levelMap = repository.EnsureNotNull().LevelMap;

    _levelDebug = levelMap.LookupWithDefault(Level.Debug);
    _levelInfo = levelMap.LookupWithDefault(Level.Info);
    _levelWarn = levelMap.LookupWithDefault(Level.Warn);
    _levelError = levelMap.LookupWithDefault(Level.Error);
    _levelFatal = levelMap.LookupWithDefault(Level.Fatal);
  }

  /// <summary>
  /// Logs a message object with the <c>DEBUG</c> level.
  /// </summary>
  /// <param name="message">The message object to log.</param>
  /// <remarks>
  /// <para>
  /// This method first checks if this logger is <c>DEBUG</c>
  /// enabled by comparing the level of this logger with the 
  /// <c>DEBUG</c> level. If this logger is
  /// <c>DEBUG</c> enabled, then it converts the message object
  /// (passed as parameter) to a string by invoking the appropriate
  /// <see cref="ObjectRenderer.IObjectRenderer"/>. It then 
  /// proceeds to call all the registered appenders in this logger 
  /// and also higher in the hierarchy depending on the value of the 
  /// additivity flag.
  /// </para>
  /// <para>
  /// <b>WARNING</b> Note that passing an <see cref="Exception"/> 
  /// to this method will print the name of the <see cref="Exception"/> 
  /// but no stack trace. To print a stack trace use the 
  /// <see cref="Debug(object,Exception)"/> form instead.
  /// </para>
  /// </remarks>
  public virtual void Debug(object? message) 
    => Logger.Log(_thisDeclaringType, _levelDebug, message, null);

  /// <summary>
  /// Logs a message object with the <c>DEBUG</c> level
  /// </summary>
  /// <param name="message">The message object to log.</param>
  /// <param name="exception">The exception to log, including its stack trace.</param>
  /// <remarks>
  /// <para>
  /// Logs a message object with the <c>DEBUG</c> level including
  /// the stack trace of the <see cref="Exception"/> <paramref name="exception"/> passed
  /// as a parameter.
  /// </para>
  /// <para>
  /// See the <see cref="Debug(object)"/> form for more detailed information.
  /// </para>
  /// </remarks>
  /// <seealso cref="Debug(object)"/>
  public virtual void Debug(object? message, Exception? exception) 
    => Logger.Log(_thisDeclaringType, _levelDebug, message, exception);

  /// <summary>
  /// Logs a formatted message string with the <c>DEBUG</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="args">An Object array containing zero or more objects to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="DebugFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Debug(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void DebugFormat(string format, params object?[]? args)
  {
    if (IsDebugEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelDebug, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, args ?? _oneNullArgs), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>DEBUG</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="DebugFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Debug(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void DebugFormat(string format, object? arg0)
  {
    if (IsDebugEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelDebug, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>DEBUG</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <param name="arg1">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="DebugFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Debug(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void DebugFormat(string format, object? arg0, object? arg1)
  {
    if (IsDebugEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelDebug, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0, arg1]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>DEBUG</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <param name="arg1">An Object to format</param>
  /// <param name="arg2">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="DebugFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Debug(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void DebugFormat(string format, object? arg0, object? arg1, object? arg2)
  {
    if (IsDebugEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelDebug, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0, arg1, arg2]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>DEBUG</c> level.
  /// </summary>
  /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific formatting information</param>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="args">An Object array containing zero or more objects to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Debug(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void DebugFormat(IFormatProvider? provider, string format, params object?[]? args)
  {
    if (IsDebugEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelDebug, 
        new SystemStringFormat(provider, format, args ?? _oneNullArgs), null);
    }
  }

  /// <summary>
  /// Logs a message object with the <c>INFO</c> level.
  /// </summary>
  /// <param name="message">The message object to log.</param>
  /// <remarks>
  /// <para>
  /// This method first checks if this logger is <c>INFO</c>
  /// enabled by comparing the level of this logger with the 
  /// <c>INFO</c> level. If this logger is
  /// <c>INFO</c> enabled, then it converts the message object
  /// (passed as parameter) to a string by invoking the appropriate
  /// <see cref="ObjectRenderer.IObjectRenderer"/>. It then 
  /// proceeds to call all the registered appenders in this logger 
  /// and also higher in the hierarchy depending on the value of 
  /// the additivity flag.
  /// </para>
  /// <para>
  /// <b>WARNING</b> Note that passing an <see cref="Exception"/> 
  /// to this method will print the name of the <see cref="Exception"/> 
  /// but no stack trace. To print a stack trace use the 
  /// <see cref="Info(object,Exception)"/> form instead.
  /// </para>
  /// </remarks>
  public virtual void Info(object? message) 
    => Logger.Log(_thisDeclaringType, _levelInfo, message, null);

  /// <summary>
  /// Logs a message object with the <c>INFO</c> level.
  /// </summary>
  /// <param name="message">The message object to log.</param>
  /// <param name="exception">The exception to log, including its stack trace.</param>
  /// <remarks>
  /// <para>
  /// Logs a message object with the <c>INFO</c> level including
  /// the stack trace of the <see cref="Exception"/> <paramref name="exception"/> 
  /// passed as a parameter.
  /// </para>
  /// <para>
  /// See the <see cref="Info(object)"/> form for more detailed information.
  /// </para>
  /// </remarks>
  /// <seealso cref="Info(object)"/>
  public virtual void Info(object? message, Exception? exception) 
    => Logger.Log(_thisDeclaringType, _levelInfo, message, exception);

  /// <summary>
  /// Logs a formatted message string with the <c>INFO</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="args">An Object array containing zero or more objects to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="InfoFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Info(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void InfoFormat(string format, params object?[]? args)
  {
    if (IsInfoEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelInfo, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, args ?? _oneNullArgs), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>INFO</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="InfoFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Info(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void InfoFormat(string format, object? arg0)
  {
    if (IsInfoEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelInfo, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>INFO</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <param name="arg1">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="InfoFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Info(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void InfoFormat(string format, object? arg0, object? arg1)
  {
    if (IsInfoEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelInfo,
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0, arg1]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>INFO</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <param name="arg1">An Object to format</param>
  /// <param name="arg2">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="InfoFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Info(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void InfoFormat(string format, object? arg0, object? arg1, object? arg2)
  {
    if (IsInfoEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelInfo, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0, arg1, arg2]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>INFO</c> level.
  /// </summary>
  /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific formatting information</param>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="args">An Object array containing zero or more objects to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Info(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void InfoFormat(IFormatProvider? provider, string format, params object?[]? args)
  {
    if (IsInfoEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelInfo, 
        new SystemStringFormat(provider, format, args ?? _oneNullArgs), null);
    }
  }

  /// <summary>
  /// Logs a message object with the <c>WARN</c> level.
  /// </summary>
  /// <param name="message">the message object to log</param>
  /// <remarks>
  /// <para>
  /// This method first checks if this logger is <c>WARN</c>
  /// enabled by comparing the level of this logger with the 
  /// <c>WARN</c> level. If this logger is
  /// <c>WARN</c> enabled, then it converts the message object
  /// (passed as parameter) to a string by invoking the appropriate
  /// <see cref="ObjectRenderer.IObjectRenderer"/>. It then 
  /// proceeds to call all the registered appenders in this logger and 
  /// also higher in the hierarchy depending on the value of the 
  /// additivity flag.
  /// </para>
  /// <para>
  /// <b>WARNING</b> Note that passing an <see cref="Exception"/> to this
  /// method will print the name of the <see cref="Exception"/> but no
  /// stack trace. To print a stack trace use the 
  /// <see cref="Warn(object,Exception)"/> form instead.
  /// </para>
  /// </remarks>
  public virtual void Warn(object? message) 
    => Logger.Log(_thisDeclaringType, _levelWarn, message, null);

  /// <summary>
  /// Logs a message object with the <c>WARN</c> level
  /// </summary>
  /// <param name="message">The message object to log.</param>
  /// <param name="exception">The exception to log, including its stack trace.</param>
  /// <remarks>
  /// <para>
  /// Logs a message object with the <c>WARN</c> level including
  /// the stack trace of the <see cref="Exception"/> <paramref name="exception"/> 
  /// passed as a parameter.
  /// </para>
  /// <para>
  /// See the <see cref="Warn(object)"/> form for more detailed information.
  /// </para>
  /// </remarks>
  /// <seealso cref="Warn(object)"/>
  public virtual void Warn(object? message, Exception? exception) 
    => Logger.Log(_thisDeclaringType, _levelWarn, message, exception);

  /// <summary>
  /// Logs a formatted message string with the <c>WARN</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="args">An Object array containing zero or more objects to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="WarnFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Warn(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void WarnFormat(string format, params object?[]? args)
  {
    if (IsWarnEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelWarn, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, args ?? _oneNullArgs), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>WARN</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="WarnFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Warn(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void WarnFormat(string format, object? arg0)
  {
    if (IsWarnEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelWarn, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>WARN</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <param name="arg1">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="WarnFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Warn(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void WarnFormat(string format, object? arg0, object? arg1)
  {
    if (IsWarnEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelWarn, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0, arg1]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>WARN</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <param name="arg1">An Object to format</param>
  /// <param name="arg2">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="WarnFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Warn(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void WarnFormat(string format, object? arg0, object? arg1, object? arg2)
  {
    if (IsWarnEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelWarn, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0, arg1, arg2]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>WARN</c> level.
  /// </summary>
  /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific formatting information</param>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="args">An Object array containing zero or more objects to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Warn(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void WarnFormat(IFormatProvider? provider, string format, params object?[]? args)
  {
    if (IsWarnEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelWarn, 
        new SystemStringFormat(provider, format, args ?? _oneNullArgs), null);
    }
  }

  /// <summary>
  /// Logs a message object with the <c>ERROR</c> level.
  /// </summary>
  /// <param name="message">The message object to log.</param>
  /// <remarks>
  /// <para>
  /// This method first checks if this logger is <c>ERROR</c>
  /// enabled by comparing the level of this logger with the 
  /// <c>ERROR</c> level. If this logger is
  /// <c>ERROR</c> enabled, then it converts the message object
  /// (passed as parameter) to a string by invoking the appropriate
  /// <see cref="ObjectRenderer.IObjectRenderer"/>. It then 
  /// proceeds to call all the registered appenders in this logger and 
  /// also higher in the hierarchy depending on the value of the 
  /// additivity flag.
  /// </para>
  /// <para>
  /// <b>WARNING</b> Note that passing an <see cref="Exception"/> to this
  /// method will print the name of the <see cref="Exception"/> but no
  /// stack trace. To print a stack trace use the 
  /// <see cref="Error(object,Exception)"/> form instead.
  /// </para>
  /// </remarks>
  public virtual void Error(object? message)
    => Logger.Log(_thisDeclaringType, _levelError, message, null);

  /// <summary>
  /// Logs a message object with the <c>ERROR</c> level
  /// </summary>
  /// <param name="message">The message object to log.</param>
  /// <param name="exception">The exception to log, including its stack trace.</param>
  /// <remarks>
  /// <para>
  /// Logs a message object with the <c>ERROR</c> level including
  /// the stack trace of the <see cref="Exception"/> <paramref name="exception"/> 
  /// passed as a parameter.
  /// </para>
  /// <para>
  /// See the <see cref="Error(object)"/> form for more detailed information.
  /// </para>
  /// </remarks>
  /// <seealso cref="Error(object)"/>
  public virtual void Error(object? message, Exception? exception) 
    => Logger.Log(_thisDeclaringType, _levelError, message, exception);

  /// <summary>
  /// Logs a formatted message string with the <c>ERROR</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="args">An Object array containing zero or more objects to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="ErrorFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Error(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void ErrorFormat(string format, params object?[]? args)
  {
    if (IsErrorEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelError, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, args ?? _oneNullArgs), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>ERROR</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="ErrorFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Error(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void ErrorFormat(string format, object? arg0)
  {
    if (IsErrorEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelError, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>ERROR</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <param name="arg1">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="ErrorFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Error(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void ErrorFormat(string format, object? arg0, object? arg1)
  {
    if (IsErrorEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelError, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0, arg1]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>ERROR</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <param name="arg1">An Object to format</param>
  /// <param name="arg2">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="ErrorFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Error(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void ErrorFormat(string format, object? arg0, object? arg1, object? arg2)
  {
    if (IsErrorEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelError, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0, arg1, arg2]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>ERROR</c> level.
  /// </summary>
  /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific formatting information</param>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="args">An Object array containing zero or more objects to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Error(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void ErrorFormat(IFormatProvider? provider, string format, params object?[]? args)
  {
    if (IsErrorEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelError, 
        new SystemStringFormat(provider, format, args ?? _oneNullArgs), null);
    }
  }

  /// <summary>
  /// Logs a message object with the <c>FATAL</c> level.
  /// </summary>
  /// <param name="message">The message object to log.</param>
  /// <remarks>
  /// <para>
  /// This method first checks if this logger is <c>FATAL</c>
  /// enabled by comparing the level of this logger with the 
  /// <c>FATAL</c> level. If this logger is
  /// <c>FATAL</c> enabled, then it converts the message object
  /// (passed as parameter) to a string by invoking the appropriate
  /// <see cref="ObjectRenderer.IObjectRenderer"/>. It then 
  /// proceeds to call all the registered appenders in this logger and 
  /// also higher in the hierarchy depending on the value of the 
  /// additivity flag.
  /// </para>
  /// <para>
  /// <b>WARNING</b> Note that passing an <see cref="Exception"/> to this
  /// method will print the name of the <see cref="Exception"/> but no
  /// stack trace. To print a stack trace use the 
  /// <see cref="Fatal(object,Exception)"/> form instead.
  /// </para>
  /// </remarks>
  public virtual void Fatal(object? message) 
    => Logger.Log(_thisDeclaringType, _levelFatal, message, null);

  /// <summary>
  /// Logs a message object with the <c>FATAL</c> level
  /// </summary>
  /// <param name="message">The message object to log.</param>
  /// <param name="exception">The exception to log, including its stack trace.</param>
  /// <remarks>
  /// <para>
  /// Logs a message object with the <c>FATAL</c> level including
  /// the stack trace of the <see cref="Exception"/> <paramref name="exception"/> 
  /// passed as a parameter.
  /// </para>
  /// <para>
  /// See the <see cref="Fatal(object)"/> form for more detailed information.
  /// </para>
  /// </remarks>
  /// <seealso cref="Fatal(object)"/>
  public virtual void Fatal(object? message, Exception? exception) 
    => Logger.Log(_thisDeclaringType, _levelFatal, message, exception);

  /// <summary>
  /// Logs a formatted message string with the <c>FATAL</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="args">An Object array containing zero or more objects to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="FatalFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Fatal(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void FatalFormat(string format, params object?[]? args)
  {
    if (IsFatalEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelFatal,
        new SystemStringFormat(CultureInfo.InvariantCulture, format, args ?? _oneNullArgs), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>FATAL</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="FatalFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Fatal(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void FatalFormat(string format, object? arg0)
  {
    if (IsFatalEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelFatal, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>FATAL</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <param name="arg1">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="FatalFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Fatal(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void FatalFormat(string format, object? arg0, object? arg1)
  {
    if (IsFatalEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelFatal, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0, arg1]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>FATAL</c> level.
  /// </summary>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="arg0">An Object to format</param>
  /// <param name="arg1">An Object to format</param>
  /// <param name="arg2">An Object to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// The string is formatted using the <see cref="CultureInfo.InvariantCulture"/>
  /// format provider. To specify a localized provider use the
  /// <see cref="FatalFormat(IFormatProvider,string,object[])"/> method.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Fatal(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void FatalFormat(string format, object? arg0, object? arg1, object? arg2)
  {
    if (IsFatalEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelFatal, 
        new SystemStringFormat(CultureInfo.InvariantCulture, format, [arg0, arg1, arg2]), null);
    }
  }

  /// <summary>
  /// Logs a formatted message string with the <c>FATAL</c> level.
  /// </summary>
  /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific formatting information</param>
  /// <param name="format">A String containing zero or more format items</param>
  /// <param name="args">An Object array containing zero or more objects to format</param>
  /// <remarks>
  /// <para>
  /// The message is formatted using the <see cref="String.Format(IFormatProvider, string, object[])"/> method. See
  /// <c>String.Format</c> for details of the syntax of the format string and the behavior
  /// of the formatting.
  /// </para>
  /// <para>
  /// This method does not take an <see cref="Exception"/> object to include in the
  /// log event. To pass an <see cref="Exception"/> use one of the <see cref="Fatal(object)"/>
  /// methods instead.
  /// </para>
  /// </remarks>
  public virtual void FatalFormat(IFormatProvider? provider, string format, params object?[]? args)
  {
    if (IsFatalEnabled)
    {
      Logger.Log(_thisDeclaringType, _levelFatal, 
        new SystemStringFormat(provider, format, args ?? _oneNullArgs), null);
    }
  }

  /// <summary>
  /// Checks if this logger is enabled for the <c>DEBUG</c>
  /// level.
  /// </summary>
  /// <value>
  /// <c>true</c> if this logger is enabled for <c>DEBUG</c> events,
  /// <c>false</c> otherwise.
  /// </value>
  /// <remarks>
  /// <para>
  /// This function is intended to lessen the computational cost of
  /// disabled log debug statements.
  /// </para>
  /// <para>
  /// For some <c>log</c> Logger object, when you write:
  /// </para>
  /// <code lang="C#">
  /// log.Debug("This is entry number: " + i );
  /// </code>
  /// <para>
  /// You incur the cost constructing the message, concatenation in
  /// this case, regardless of whether the message is logged or not.
  /// </para>
  /// <para>
  /// If you are worried about speed, then you should write:
  /// </para>
  /// <code lang="C#">
  /// if (log.IsDebugEnabled())
  /// { 
  ///   log.Debug("This is entry number: " + i );
  /// }
  /// </code>
  /// <para>
  /// This way you will not incur the cost of parameter
  /// construction if debugging is disabled for <c>log</c>. On
  /// the other hand, if the <c>log</c> is debug enabled, you
  /// will incur the cost of evaluating whether the logger is debug
  /// enabled twice. Once in <c>IsDebugEnabled</c> and once in
  /// the <c>Debug</c>.  This is an insignificant overhead
  /// since evaluating a logger takes about 1% of the time it
  /// takes to actually log.
  /// </para>
  /// </remarks>
  public virtual bool IsDebugEnabled => Logger.IsEnabledFor(_levelDebug);

  /// <summary>
  /// Checks if this logger is enabled for the <c>INFO</c> level.
  /// </summary>
  /// <value>
  /// <c>true</c> if this logger is enabled for <c>INFO</c> events,
  /// <c>false</c> otherwise.
  /// </value>
  /// <remarks>
  /// <para>
  /// See <see cref="IsDebugEnabled"/> for more information and examples 
  /// of using this method.
  /// </para>
  /// </remarks>
  /// <seealso cref="IsDebugEnabled"/>
  public virtual bool IsInfoEnabled => Logger.IsEnabledFor(_levelInfo);

  /// <summary>
  /// Checks if this logger is enabled for the <c>WARN</c> level.
  /// </summary>
  /// <value>
  /// <c>true</c> if this logger is enabled for <c>WARN</c> events,
  /// <c>false</c> otherwise.
  /// </value>
  /// <remarks>
  /// <para>
  /// See <see cref="IsDebugEnabled"/> for more information and examples 
  /// of using this method.
  /// </para>
  /// </remarks>
  /// <seealso cref="ILog.IsDebugEnabled"/>
  public virtual bool IsWarnEnabled => Logger.IsEnabledFor(_levelWarn);

  /// <summary>
  /// Checks if this logger is enabled for the <c>ERROR</c> level.
  /// </summary>
  /// <value>
  /// <c>true</c> if this logger is enabled for <c>ERROR</c> events,
  /// <c>false</c> otherwise.
  /// </value>
  /// <remarks>
  /// <para>
  /// See <see cref="IsDebugEnabled"/> for more information and examples of using this method.
  /// </para>
  /// </remarks>
  /// <seealso cref="ILog.IsDebugEnabled"/>
  public virtual bool IsErrorEnabled => Logger.IsEnabledFor(_levelError);

  /// <summary>
  /// Checks if this logger is enabled for the <c>FATAL</c> level.
  /// </summary>
  /// <value>
  /// <c>true</c> if this logger is enabled for <c>FATAL</c> events,
  /// <c>false</c> otherwise.
  /// </value>
  /// <remarks>
  /// <para>
  /// See <see cref="IsDebugEnabled"/> for more information and examples of using this method.
  /// </para>
  /// </remarks>
  /// <seealso cref="ILog.IsDebugEnabled"/>
  public virtual bool IsFatalEnabled => Logger.IsEnabledFor(_levelFatal);

  /// <summary>
  /// Event handler for the <see cref="ILoggerRepository.ConfigurationChanged"/> event
  /// </summary>
  /// <param name="sender">the repository</param>
  /// <param name="e">Empty</param>
  private void LoggerRepositoryConfigurationChanged(object sender, EventArgs e)
  {
    if (sender is ILoggerRepository repository)
    {
      ReloadLevels(repository);
    }
  }

  /// <summary>
  /// The fully qualified name of this declaring type not the type of any subclass.
  /// </summary>
  private static readonly Type _thisDeclaringType = typeof(LogImpl);

  /// <summary>
  /// Used to ensure 'params object?[]?' arguments that receive a null are converted
  /// to an array of one null value so that 'XxxFormat("{0}", null)' will work correctly.
  /// Overloads like 'XxxFormat(message, object? arg0)' are not matched by the compiler in this case.
  /// </summary>
  private static readonly object?[] _oneNullArgs = [null];

  private Level _levelDebug = Level.Debug;
  private Level _levelInfo = Level.Info;
  private Level _levelWarn = Level.Warn;
  private Level _levelError = Level.Error;
  private Level _levelFatal = Level.Fatal;
}