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

namespace log4net.Core
{
	/// <summary>
	/// Implementation of <see cref="ILog"/> wrapper interface.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This implementation of the <see cref="ILog"/> interface
	/// forwards to the <see cref="ILogger"/> held by the base class.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class LogImpl : LoggerWrapperImpl, ILog
	{
		#region Public Instance Constructors

		/// <summary>
		/// Construct a new wrapper for the specified logger.
		/// </summary>
		/// <param name="logger">The logger to wrap.</param>
		public LogImpl(ILogger logger) : base(logger)
		{
			this.m_fullName = this.GetType().FullName;
		}

		#endregion Public Instance Constructors

		#region Protected Instance Properties

		/// <summary>
		/// Gets the fully qualified classname of the logger.
		/// </summary>
		/// <value>
		/// The fully qualified classname of the logger.
		/// </value>
		protected string FullName 
		{
			get { return this.m_fullName; }
		}

		#endregion Protected Instance Properties

		#region Implementation of ILog

		/// <summary>
		/// Logs a message object with the <see cref="Level.Debug"/> level.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <remarks>
		/// <para>
		/// This method first checks if this logger is <c>DEBUG</c>
		/// enabled by comparing the level of this logger with the 
		/// <see cref="Level.Debug"/> level. If this logger is
		/// <c>DEBUG</c> enabled, then it converts the message object
		/// (passed as parameter) to a string by invoking the appropriate
		/// <see cref="log4net.ObjectRenderer.IObjectRenderer"/>. It then 
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
		virtual public void Debug(object message) 
		{
			Logger.Log(m_fullName, Level.Debug, message, null);
		}

		/// <summary>
		/// Logs a message object with the <c>DEBUG</c> level including
		/// the stack trace of the <see cref="Exception"/> <paramref name="t"/> passed
		/// as a parameter.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <param name="t">The exception to log, including its stack trace.</param>
		/// <remarks>
		/// See the <see cref="Debug(object)"/> form for more detailed information.
		/// </remarks>
		/// <seealso cref="Debug(object)"/>
		virtual public void Debug(object message, Exception t) 
		{
			Logger.Log(m_fullName, Level.Debug, message, t);
		}

		/// <summary>
		/// Logs a message object with the <see cref="Level.Info"/> level.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <remarks>
		/// <para>
		/// This method first checks if this logger is <c>INFO</c>
		/// enabled by comparing the level of this logger with the 
		/// <see cref="Level.Info"/> level. If this logger is
		/// <c>INFO</c> enabled, then it converts the message object
		/// (passed as parameter) to a string by invoking the appropriate
		/// <see cref="log4net.ObjectRenderer.IObjectRenderer"/>. It then 
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
		virtual public void Info(object message) 
		{
			Logger.Log(m_fullName, Level.Info, message, null);
		}
  
		/// <summary>
		/// Logs a message object with the <c>INFO</c> level including
		/// the stack trace of the <see cref="Exception"/> <paramref name="t"/> 
		/// passed as a parameter.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <param name="t">The exception to log, including its stack trace.</param>
		/// <remarks>
		/// See the <see cref="Info(object)"/> form for more detailed information.
		/// </remarks>
		/// <seealso cref="Info(object)"/>
		virtual public void Info(object message, Exception t) 
		{
			Logger.Log(m_fullName, Level.Info, message, t);
		}

		/// <summary>
		/// Logs a message object with the <see cref="Level.Warn"/> level.
		/// </summary>
		/// <param name="message">the message object to log</param>
		/// <remarks>
		/// <para>
		/// This method first checks if this logger is <c>WARN</c>
		/// enabled by comparing the level of this logger with the 
		/// <see cref="Level.Warn"/> level. If this logger is
		/// <c>WARN</c> enabled, then it converts the message object
		/// (passed as parameter) to a string by invoking the appropriate
		/// <see cref="log4net.ObjectRenderer.IObjectRenderer"/>. It then 
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
		virtual public void Warn(object message) 
		{
			Logger.Log(m_fullName, Level.Warn, message, null);
		}
  
		/// <summary>
		/// Logs a message object with the <c>WARN</c> level including
		/// the stack trace of the <see cref="Exception"/> <paramref name="t"/> 
		/// passed as a parameter.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <param name="t">The exception to log, including its stack trace.</param>
		/// <remarks>
		/// See the <see cref="Warn(object)"/> form for more detailed information.
		/// </remarks>
		/// <seealso cref="Warn(object)"/>
		virtual public void Warn(object message, Exception t) 
		{
			Logger.Log(m_fullName, Level.Warn, message, t);
		}

		/// <summary>
		/// Logs a message object with the <see cref="Level.Error"/> level.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <remarks>
		/// <para>
		/// This method first checks if this logger is <c>ERROR</c>
		/// enabled by comparing the level of this logger with the 
		/// <see cref="Level.Error"/> level. If this logger is
		/// <c>ERROR</c> enabled, then it converts the message object
		/// (passed as parameter) to a string by invoking the appropriate
		/// <see cref="log4net.ObjectRenderer.IObjectRenderer"/>. It then 
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
		virtual public void Error(object message) 
		{
			Logger.Log(m_fullName, Level.Error, message, null);
		}

		/// <summary>
		/// Logs a message object with the <c>ERROR</c> level including
		/// the stack trace of the <see cref="Exception"/> <paramref name="t"/> 
		/// passed as a parameter.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <param name="t">The exception to log, including its stack trace.</param>
		/// <remarks>
		/// See the <see cref="Error(object)"/> form for more detailed information.
		/// </remarks>
		/// <seealso cref="Error(object)"/>
		virtual public void Error(object message, Exception t) 
		{
			Logger.Log(m_fullName, Level.Error, message, t);
		}

		/// <summary>
		/// Logs a message object with the <see cref="Level.Fatal"/> level.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <remarks>
		/// <para>
		/// This method first checks if this logger is <c>FATAL</c>
		/// enabled by comparing the level of this logger with the 
		/// <see cref="Level.Fatal"/> level. If this logger is
		/// <c>FATAL</c> enabled, then it converts the message object
		/// (passed as parameter) to a string by invoking the appropriate
		/// <see cref="log4net.ObjectRenderer.IObjectRenderer"/>. It then 
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
		virtual public void Fatal(object message) 
		{
			Logger.Log(m_fullName, Level.Fatal, message, null);
		}
  
		/// <summary>
		/// Logs a message object with the <c>FATAL</c> level including
		/// the stack trace of the <see cref="Exception"/> <paramref name="t"/> 
		/// passed as a parameter.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <param name="t">The exception to log, including its stack trace.</param>
		/// <remarks>
		/// See the <see cref="Fatal(object)"/> form for more detailed information.
		/// </remarks>
		/// <seealso cref="Fatal(object)"/>
		virtual public void Fatal(object message, Exception t) 
		{
			Logger.Log(m_fullName, Level.Fatal, message, t);
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
		/// <code>
		/// log.Debug("This is entry number: " + i );
		/// </code>
		/// <para>
		/// You incur the cost constructing the message, concatenation in
		/// this case, regardless of whether the message is logged or not.
		/// </para>
		/// <para>
		/// If you are worried about speed, then you should write:
		/// </para>
		/// <code>
		/// if (log.IsDebugEnabled())
		/// { 
		///	 log.Debug("This is entry number: " + i );
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
		virtual public bool IsDebugEnabled
		{
			get { return Logger.IsEnabledFor(Level.Debug); }
		}
  
		/// <summary>
		/// Checks if this logger is enabled for the <c>INFO</c> level.
		/// </summary>
		/// <value>
		/// <c>true</c> if this logger is enabled for <c>INFO</c> events,
		/// <c>false</c> otherwise.
		/// </value>
		/// <remarks>
		/// See <see cref="IsDebugEnabled"/> for more information and examples 
		/// of using this method.
		/// </remarks>
		/// <seealso cref="LogImpl.IsDebugEnabled"/>
		virtual public bool IsInfoEnabled
		{
			get { return Logger.IsEnabledFor(Level.Info); }
		}

		/// <summary>
		/// Checks if this logger is enabled for the <c>WARN</c> level.
		/// </summary>
		/// <value>
		/// <c>true</c> if this logger is enabled for <c>WARN</c> events,
		/// <c>false</c> otherwise.
		/// </value>
		/// <remarks>
		/// See <see cref="IsDebugEnabled"/> for more information and examples 
		/// of using this method.
		/// </remarks>
		/// <seealso cref="ILog.IsDebugEnabled"/>
		virtual public bool IsWarnEnabled
		{
			get { return Logger.IsEnabledFor(Level.Warn); }
		}

		/// <summary>
		/// Checks if this logger is enabled for the <c>ERROR</c> level.
		/// </summary>
		/// <value>
		/// <c>true</c> if this logger is enabled for <c>ERROR</c> events,
		/// <c>false</c> otherwise.
		/// </value>
		/// <remarks>
		/// See <see cref="IsDebugEnabled"/> for more information and examples of using this method.
		/// </remarks>
		/// <seealso cref="ILog.IsDebugEnabled"/>
		virtual public bool IsErrorEnabled
		{
			get { return Logger.IsEnabledFor(Level.Error); }
		}

		/// <summary>
		/// Checks if this logger is enabled for the <c>FATAL</c> level.
		/// </summary>
		/// <value>
		/// <c>true</c> if this logger is enabled for <c>FATAL</c> events,
		/// <c>false</c> otherwise.
		/// </value>
		/// <remarks>
		/// See <see cref="IsDebugEnabled"/> for more information and examples of using this method.
		/// </remarks>
		/// <seealso cref="ILog.IsDebugEnabled"/>
		virtual public bool IsFatalEnabled
		{
			get { return Logger.IsEnabledFor(Level.Fatal); }
		}

		#endregion Implementation of ILog

		#region Private Instance Fields

		/// <summary>
		/// The fully qualified name of the Logger class.
		/// </summary>
		private readonly string m_fullName;

		#endregion Private Instance Fields
	}
}
