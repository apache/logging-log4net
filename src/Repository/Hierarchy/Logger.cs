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
using System.Collections;

using log4net.Appender;
using log4net.Util;
using log4net.Core;

namespace log4net.Repository.Hierarchy
{
	/// <summary>
	/// Internal class used to provide implementation of <see cref="ILog"/>
	/// interface. Applications should use <see cref="LogManager"/> to get
	/// logger instances.
	/// </summary>
	/// <remarks>
	/// This is one of the central class' in the log4net implementation. One of the
	/// distinctive features of log4net are hierarchical loggers and their
	/// evaluation.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	/// <author>Aspi Havewala</author>
	/// <author>Douglas de la Torre</author>
	public class Logger : IAppenderAttachable, ILogger
	{
		#region Protected Instance Constructors

		/// <summary>
		/// This constructor created a new <see cref="Logger" /> instance and
		/// sets its name.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Loggers are constructed by <see cref="ILoggerFactory"/> 
		/// objects. See <see cref="DefaultLoggerFactory"/> for the default
		/// logger creator.
		/// </para>
		/// </remarks>
		/// <param name="name">The name of the <see cref="Logger" />.</param>
		protected Logger(string name) 
		{
#if NETCF
			// NETCF: String.Intern causes Native Exception
			m_name = name;
#else
			m_name = string.Intern(name);
#endif
		}

		#endregion Protected Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets or sets the parent logger in the hierarchy.
		/// </summary>
		/// <value>
		/// The parent logger in the hierarchy.
		/// </value>
		/// <remarks>
		/// Part of the Composite pattern that makes the hierarchy.
		/// </remarks>
		virtual public Logger Parent
		{
			get { return m_parent; }
			set { m_parent = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating if child loggers inherit their parent's appenders.
		/// </summary>
		/// <value>
		/// <c>true</c> if child loggers inherit their parent's appenders.
		/// </value>
		/// <remarks>
		/// Additivity is set to <c>true</c> by default, that is children inherit
		/// the appenders of their ancestors by default. If this variable is
		/// set to <c>false</c> then the appenders found in the
		/// ancestors of this logger are not used. However, the children
		/// of this logger will inherit its appenders, unless the children
		/// have their additivity flag set to <c>false</c> too. See
		/// the user manual for more details.
		/// </remarks>
		virtual public bool Additivity
		{
			get { return m_additive; }
			set { m_additive = value; }
		}

		/// <summary>
		/// Gets the effective level for this logger.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Starting from this logger, searches the logger hierarchy for a
		/// non-null level and returns it. Otherwise, returns the level of the
		/// root logger.
		/// </para>
		/// <para>The Logger class is designed so that this method executes as
		/// quickly as possible.</para>
		/// </remarks>
		/// <returns>The nearest level in the logger hierarchy.</returns>
		virtual public Level EffectiveLevel
		{
			get 
			{
				for(Logger c = this; c != null; c = c.m_parent) 
				{
					if (c.m_level != null) 
					{
						return c.m_level;
					}
				}
				return null; // If reached will cause an NullPointerException.
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="Hierarchy"/> where this 
		/// <c>Logger</c> instance is attached to.
		/// </summary>
		/// <value>The hierarchy that this logger belongs to.</value>
		virtual public Hierarchy Hierarchy
		{
			get { return m_hierarchy; }
			set { m_hierarchy = value; }
		}

		/// <summary>
		/// Gets or sets the assigned <see cref="Level"/>, if any, for this Logger.  
		/// </summary>
		/// <value>
		/// The <see cref="Level"/> of this logger.
		/// </value>
		/// <remarks>
		/// The assigned <see cref="Level"/> can be <c>null</c>.
		/// </remarks>
		virtual public Level Level
		{
			get { return m_level; }
			set { m_level = value; }
		}

		#endregion Public Instance Properties

		#region Implementation of IAppenderAttachable

		/// <summary>
		/// Add <paramref name="newAppender"/> to the list of appenders of this
		/// Logger instance.
		/// </summary>
		/// <param name="newAppender">An appender to add to this logger</param>
		/// <remarks>
		/// Add <paramref name="newAppender"/> to the list of appenders of this
		/// Logger instance.
		/// <para>If <paramref name="newAppender"/> is already in the list of
		/// appenders, then it won't be added again.</para>
		/// </remarks>
		virtual public void AddAppender(IAppender newAppender) 
		{
			if (newAppender == null)
			{
				throw new ArgumentNullException("newAppender");
			}

			m_appenderLock.AcquireWriterLock();
			try
			{
				if (m_aai == null) 
				{
					m_aai = new log4net.Util.AppenderAttachedImpl();
				}
				m_aai.AddAppender(newAppender);
			}
			finally
			{
				m_appenderLock.ReleaseWriterLock();
			}
		}

		/// <summary>
		/// Get the appenders contained in this logger as an 
		/// <see cref="System.Collections.ICollection"/>.
		/// </summary>
		/// <remarks>
		/// Get the appenders contained in this logger as an 
		/// <see cref="System.Collections.ICollection"/>. If no appenders 
		/// can be found, then a <see cref="EmptyCollection"/> is returned.
		/// </remarks>
		/// <returns>A collection of the appenders in this logger</returns>
		virtual public AppenderCollection Appenders 
		{
			get
			{
				m_appenderLock.AcquireReaderLock();
				try
				{
					if (m_aai == null)
					{
						return AppenderCollection.EmptyCollection;
					}
					else 
					{
						return m_aai.Appenders;
					}
				}
				finally
				{
					m_appenderLock.ReleaseReaderLock();
				}
			}
		}

		/// <summary>
		/// Look for the appender named as <c>name</c>
		/// </summary>
		/// <param name="name">The name of the appender to lookup</param>
		/// <returns>The appender with the name specified, or <c>null</c>.</returns>
		/// <remarks>
		/// Returns the named appender, or null if the appender is not found.
		/// </remarks>
		virtual public IAppender GetAppender(string name) 
		{
			m_appenderLock.AcquireReaderLock();
			try
			{
				if (m_aai == null || name == null)
				{
					return null;
				}

				return m_aai.GetAppender(name);
			}
			finally
			{
				m_appenderLock.ReleaseReaderLock();
			}
		}

		/// <summary>
		/// Remove all previously added appenders from this Logger instance.
		/// </summary>
		/// <remarks>
		/// Remove all previously added appenders from this Logger instance.
		/// <para>This is useful when re-reading configuration information.</para>
		/// </remarks>
		virtual public void RemoveAllAppenders() 
		{
			m_appenderLock.AcquireWriterLock();
			try
			{
				if (m_aai != null) 
				{
					m_aai.RemoveAllAppenders();
					m_aai = null;
				}
			}
			finally
			{
				m_appenderLock.ReleaseWriterLock();
			}
		}

		/// <summary>
		/// Remove the appender passed as parameter form the list of appenders.
		/// </summary>
		/// <param name="appender">The appender to remove</param>
		/// <remarks>
		/// Remove the appender passed as parameter form the list of appenders.
		/// </remarks>
		virtual public void RemoveAppender(IAppender appender) 
		{
			m_appenderLock.AcquireWriterLock();
			try
			{
				if (appender != null && m_aai != null) 
				{
					m_aai.RemoveAppender(appender);
				}
			}
			finally
			{
				m_appenderLock.ReleaseWriterLock();
			}
		}

		/// <summary>
		/// Remove the appender passed as parameter form the list of appenders.
		/// </summary>
		/// <param name="name">The name of the appender to remove</param>
		/// <remarks>
		/// Remove the named appender passed as parameter form the list of appenders.
		/// </remarks>
		virtual public void RemoveAppender(string name) 
		{
			m_appenderLock.AcquireWriterLock();
			try
			{
				if (name != null && m_aai != null)
				{
					m_aai.RemoveAppender(name);
				}
			}
			finally
			{
				m_appenderLock.ReleaseWriterLock();
			}
		}
  
		#endregion

		#region Implementation of ILogger

		/// <summary>
		/// Gets the logger name.
		/// </summary>
		/// <value>
		/// The name of the logger.
		/// </value>
		virtual public string Name
		{
			get { return m_name; }
		}

		/// <summary>
		/// This generic form is intended to be used by wrappers.
		/// </summary>
		/// <param name="callerFullName">The wrapper class' fully qualified class name.</param>
		/// <param name="level">The level of the message to be logged.</param>
		/// <param name="message">The message object to log.</param>
		/// <param name="t">The exception to log, including its stack trace.</param>
		/// <remarks>
		/// Generate a logging event for the specified <paramref name="level"/> using
		/// the <paramref name="message"/> and <paramref name="t"/>.
		/// </remarks>
		virtual public void Log(string callerFullName, Level level, object message, Exception t) 
		{
			if (IsEnabledFor(level))
			{
				ForcedLog((callerFullName != null) ? callerFullName : ThisClassFullName , level, message, t);
			}
		}

		/// <summary>
		/// This is the most generic printing method. 
		/// This generic form is intended to be used by wrappers
		/// </summary>
		/// <param name="logEvent">The event being logged.</param>
		/// <remarks>
		/// Logs the logging event specified.
		/// </remarks>
		virtual public void Log(LoggingEvent logEvent) 
		{
			if (logEvent == null)
			{
				throw new ArgumentNullException("logEvent");
			}

			if (IsEnabledFor(logEvent.Level))
			{
				ForcedLog(logEvent);
			}
		}

		/// <summary>
		/// Checks if this logger is enabled for a given <see cref="Level"/> passed as parameter.
		/// </summary>
		/// <param name="level">The level to check.</param>
		/// <returns>
		/// <c>true</c> if this logger is enabled for <c>level</c>, otherwise <c>false</c>.
		/// </returns>
		virtual public bool IsEnabledFor(Level level) 
		{
			if (m_hierarchy.IsDisabled(level))
			{
				return false;
			}
			return level >= this.EffectiveLevel;
		}

		/// <summary>
		/// Gets the <see cref="ILoggerRepository"/> where this 
		/// <c>Logger</c> instance is attached to.
		/// </summary>
		/// <value>The <see cref="ILoggerRepository"/> that this logger belongs to.</value>
		public ILoggerRepository Repository
		{ 
			get { return m_hierarchy; }
		}

  		#endregion Implementation of ILogger

		/// <summary>
		/// Call the appenders in the hierarchy starting at
		/// <c>this</c>.  If no appenders could be found, emit a
		/// warning.
		/// </summary>
		/// <remarks>
		/// This method calls all the appenders inherited from the
		/// hierarchy circumventing any evaluation of whether to log or not
		/// to log the particular log request.
		/// </remarks>
		/// <param name="loggingEvent">The event to log.</param>
		virtual protected void CallAppenders(LoggingEvent loggingEvent) 
		{
			if (loggingEvent == null)
			{
				throw new ArgumentNullException("loggingEvent");
			}

			int writes = 0;

			for(Logger c=this; c != null; c=c.m_parent) 
			{
				// Protected against simultaneous call to addAppender, removeAppender,...
				c.m_appenderLock.AcquireReaderLock();
				try
				{
					if (c.m_aai != null) 
					{
						writes += c.m_aai.AppendLoopOnAppenders(loggingEvent);
					}
				}
				finally
				{
					c.m_appenderLock.ReleaseReaderLock();
				}

				if (!c.m_additive) 
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
			if (!m_hierarchy.EmittedNoAppenderWarning && writes == 0) 
			{
				LogLog.Debug("Logger: No appenders could be found for logger [" + Name + "] repository [" + Repository.Name + "]");
				LogLog.Debug("Logger: Please initialize the log4net system properly.");
				try
				{
					LogLog.Debug("Logger:    Current AppDomain context information: ");
					LogLog.Debug("Logger:       BaseDirectory   : " + SystemInfo.ApplicationBaseDirectory);
#if !NETCF
					LogLog.Debug("Logger:       FriendlyName    : " + AppDomain.CurrentDomain.FriendlyName);
					LogLog.Debug("Logger:       DynamicDirectory: " + AppDomain.CurrentDomain.DynamicDirectory);
#endif
				}
				catch(System.Security.SecurityException)
				{
					// Insufficent permissions to display info from the AppDomain
				}
				m_hierarchy.EmittedNoAppenderWarning = true;
			}
		}

		/// <summary>
		/// Closes all attached appenders implementing the IAppenderAttachable interface.
		/// </summary>
		/// <remarks>
		/// Used to ensure that the appenders are correctly shutdown.
		/// </remarks>
		virtual public void CloseNestedAppenders() 
		{
			m_appenderLock.AcquireWriterLock();
			try
			{
				if (m_aai != null)
				{
					AppenderCollection appenders = m_aai.Appenders;
					foreach(IAppender appender in appenders)
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
				m_appenderLock.ReleaseWriterLock();
			}
		}

		/// <summary>
		/// This is the most generic printing method. This generic form is intended to be used by wrappers
		/// </summary>
		/// <param name="level">The level of the message to be logged.</param>
		/// <param name="message">The message object to log.</param>
		/// <param name="t">The exception to log, including its stack trace.</param>
		/// <remarks>
		/// Generate a logging event for the specified <paramref name="level"/> using
		/// the <paramref name="message"/>.
		/// </remarks>
		virtual public void Log(Level level, object message, Exception t) 
		{
			if (IsEnabledFor(level))
			{
				ForcedLog(ThisClassFullName, level, message, t);
			}
		}

		/// <summary>
		/// Creates a new logging event and logs the event without further checks.
		/// </summary>
		/// <param name="callerFullName">The wrapper class' fully qualified class name.</param>
		/// <param name="level">The level of the message to be logged.</param>
		/// <param name="message">The message object to log.</param>
		/// <param name="t">The exception to log, including its stack trace.</param>
		/// <remarks>
		/// Generates a logging event and delivers it to the attached
		/// appenders.
		/// </remarks>
		virtual protected void ForcedLog(string callerFullName, Level level, object message, Exception t) 
		{
			CallAppenders(new LoggingEvent(callerFullName, this.Hierarchy, this.Name, level, message, t));
		}

		/// <summary>
		/// Creates a new logging event and logs the event without further checks.
		/// </summary>
		/// <param name="logEvent">The event being logged.</param>
		/// <remarks>
		/// Delivers the logging event to the attached appenders.
		/// </remarks>
		virtual protected void ForcedLog(LoggingEvent logEvent) 
		{
			CallAppenders(logEvent);
		}

		#region Private Static Fields

		/// <summary>
		/// The fully qualified name of the Logger class.
		/// </summary>
		private readonly static string ThisClassFullName = typeof(Logger).FullName;

		#endregion Private Static Fields

		#region Private Instance Fields

		/// <summary>
		/// The name of this logger.
		/// </summary>
		private readonly string m_name;  

		/// <summary>
		/// The assigned level of this logger. 
		/// </summary>
		/// <remarks>
		/// The <c>level</c> variable need not be 
		/// assigned a value in which case it is inherited 
		/// form the hierarchy.
		/// </remarks>
		private Level m_level;

		/// <summary>
		/// The parent of this logger.
		/// </summary>
		/// <remarks>
		/// The parent of this logger. All loggers have at least one ancestor which is the root logger.
		/// </remarks>
		private Logger m_parent;

		/// <summary>
		/// Loggers need to know what Hierarchy they are in.
		/// </summary>
		/// <remarks>
		/// Loggers need to know what Hierarchy they are in.
		/// The hierarchy that this logger is a member of is stored
		/// here.
		/// </remarks>
		private Hierarchy m_hierarchy;

		/// <summary>
		/// Helper implementation of the <see cref="IAppenderAttachable"/> interface
		/// </summary>
		private log4net.Util.AppenderAttachedImpl m_aai;

		/// <summary>
		/// Flag indicating if child loggers inherit their parents appenders
		/// </summary>
		/// <remarks>
		/// Additivity is set to true by default, that is children inherit
		/// the appenders of their ancestors by default. If this variable is
		/// set to <c>false</c> then the appenders found in the
		/// ancestors of this logger are not used. However, the children
		/// of this logger will inherit its appenders, unless the children
		/// have their additivity flag set to <c>false</c> too. See
		/// the user manual for more details.
		/// </remarks>
		private bool m_additive = true;

		/// <summary>
		/// Lock to protect AppenderAttachedImpl variable m_aai
		/// </summary>
		private readonly ReaderWriterLock m_appenderLock = new ReaderWriterLock();
  
		#endregion
	}
}
