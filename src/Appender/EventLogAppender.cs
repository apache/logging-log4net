#region Copyright & License
//
// Copyright 2001-2005 The Apache Software Foundation
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

// MONO 1.0 Beta mcs does not like #if !A && !B && !C syntax

// .NET Compact Framework 1.0 has no support for EventLog
#if !NETCF 
// SSCLI 1.0 has no support for EventLog
#if !SSCLI

using System;
using System.Diagnostics;
using System.Globalization;

using log4net.Util;
using log4net.Layout;
using log4net.Core;

namespace log4net.Appender
{
	/// <summary>
	/// Writes events to the system event log.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The <c>EventID</c> of the event log entry can be
	/// set using the <c>EventLogEventID</c> property (<see cref="LoggingEvent.Properties"/>)
	/// on the <see cref="LoggingEvent"/>.
	/// </para>
	/// <para>
	/// There is a limit of 32K characters for an event log message
	/// </para>
	/// <para>
	/// When configuring the EventLogAppender a mapping can be
	/// specified to map a logging level to an event log entry type. For example:
	/// </para>
	/// <code lang="XML">
	/// &lt;mapping&gt;
	/// 	&lt;level value="ERROR" /&gt;
	/// 	&lt;eventLogEntryType value="Error" /&gt;
	/// &lt;/mapping&gt;
	/// &lt;mapping&gt;
	/// 	&lt;level value="DEBUG" /&gt;
	/// 	&lt;eventLogEntryType value="Information" /&gt;
	/// &lt;/mapping&gt;
	/// </code>
	/// <para>
	/// The Level is the standard log4net logging level and eventLogEntryType can be any value
	/// from the <see cref="EventLogEntryType"/> enum, i.e.:
	/// <list type="bullet">
	/// <item><term>Error</term><description>an error event</description></item>
	/// <item><term>Warning</term><description>a warning event</description></item>
	/// <item><term>Information</term><description>an informational event</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	/// <author>Aspi Havewala</author>
	/// <author>Douglas de la Torre</author>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	/// <author>Thomas Voss</author>
	public class EventLogAppender : AppenderSkeleton
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="EventLogAppender" /> class.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Default constructor.
		/// </para>
		/// </remarks>
		public EventLogAppender()
		{
			m_applicationName	= System.Threading.Thread.GetDomain().FriendlyName;
			m_logName			= "Application";	// Defaults to application log
			m_machineName		= ".";	// Only log on the local machine
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EventLogAppender" /> class
		/// with the specified <see cref="ILayout" />.
		/// </summary>
		/// <param name="layout">The <see cref="ILayout" /> to use with this appender.</param>
		/// <remarks>
		/// <para>
		/// Obsolete constructor.
		/// </para>
		/// </remarks>
		[Obsolete("Instead use the default constructor and set the Layout property")]
		public EventLogAppender(ILayout layout) : this()
		{
			Layout = layout;
		}

		#endregion // Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// The name of the log where messages will be stored.
		/// </summary>
		/// <value>
		/// The string name of the log where messages will be stored.
		/// </value>
		/// <remarks>
		/// <para>This is the name of the log as it appears in the Event Viewer
		/// tree. The default value is to log into the <c>Application</c>
		/// log, this is where most applications write their events. However
		/// if you need a separate log for your application (or applications)
		/// then you should set the <see cref="LogName"/> appropriately.</para>
		/// <para>This should not be used to distinguish your event log messages
		/// from those of other applications, the <see cref="ApplicationName"/>
		/// property should be used to distinguish events. This property should be 
		/// used to group together events into a single log.
		/// </para>
		/// </remarks>
		public string LogName
		{
			get { return m_logName; }
			set { m_logName = value; }
		}

		/// <summary>
		/// Property used to set the Application name.  This appears in the
		/// event logs when logging.
		/// </summary>
		/// <value>
		/// The string used to distinguish events from different sources.
		/// </value>
		/// <remarks>
		/// Sets the event log source property.
		/// </remarks>
		public string ApplicationName
		{
			get { return m_applicationName; }
			set { m_applicationName = value; }
		}

		/// <summary>
		/// This property is used to return the name of the computer to use
		/// when accessing the event logs.  Currently, this is the current
		/// computer, denoted by a dot "."
		/// </summary>
		/// <value>
		/// The string name of the machine holding the event log that 
		/// will be logged into.
		/// </value>
		/// <remarks>
		/// This property cannot be changed. It is currently set to '.'
		/// i.e. the local machine. This may be changed in future.
		/// </remarks>
		public string MachineName
		{
			get { return m_machineName; }
			set { /* Currently we do not allow the machine name to be changed */; }
		}

		/// <summary>
		/// Add a mapping of level to <see cref="EventLogEntryType"/> - done by the config file
		/// </summary>
		/// <param name="mapping">The mapping to add</param>
		/// <remarks>
		/// <para>
		/// Add a <see cref="Level2EventLogEntryType"/> mapping to this appender.
		/// Each mapping defines the event log entry type for a level.
		/// </para>
		/// </remarks>
		public void AddMapping(Level2EventLogEntryType mapping)
		{
			m_levelMapping.Add(mapping);
		}

		/// <summary>
		/// Gets or sets the <see cref="SecurityContext"/> used to write to the EventLog.
		/// </summary>
		/// <value>
		/// The <see cref="SecurityContext"/> used to write to the EventLog.
		/// </value>
		/// <remarks>
		/// <para>
		/// The system security context used to write to the EventLog.
		/// </para>
		/// <para>
		/// Unless a <see cref="SecurityContext"/> specified here for this appender
		/// the <see cref="SecurityContextProvider.DefaultProvider"/> is queried for the
		/// security context to use. The default behavior is to use the security context
		/// of the current thread.
		/// </para>
		/// </remarks>
		public SecurityContext SecurityContext 
		{
			get { return m_securityContext; }
			set { m_securityContext = value; }
		}

		#endregion // Public Instance Properties

		#region Implementation of IOptionHandler

		/// <summary>
		/// Initialize the appender based on the options set
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is part of the <see cref="IOptionHandler"/> delayed object
		/// activation scheme. The <see cref="ActivateOptions"/> method must 
		/// be called on this object after the configuration properties have
		/// been set. Until <see cref="ActivateOptions"/> is called this
		/// object is in an undefined state and must not be used. 
		/// </para>
		/// <para>
		/// If any of the configuration properties are modified then 
		/// <see cref="ActivateOptions"/> must be called again.
		/// </para>
		/// </remarks>
		override public void ActivateOptions() 
		{
			base.ActivateOptions();

			if (m_securityContext == null)
			{
				m_securityContext = SecurityContextProvider.DefaultProvider.CreateSecurityContext(this);
			}

			bool sourceAlreadyExists = false;
			string currentLogName = null;

			using(SecurityContext.Impersonate(this))
			{
				sourceAlreadyExists = EventLog.SourceExists(m_applicationName);
				if (sourceAlreadyExists)
				{
					currentLogName = EventLog.LogNameFromSourceName(m_applicationName, m_machineName);
				}
			}

			if (sourceAlreadyExists && currentLogName != m_logName)
			{
				LogLog.Debug("EventLogAppender: Changing event source [" + m_applicationName + "] from log [" + currentLogName + "] to log [" + m_logName + "]");
			}
			else if (!sourceAlreadyExists)
			{
				LogLog.Debug("EventLogAppender: Creating event source Source [" + m_applicationName + "] in log " + m_logName + "]");
			}

			string registeredLogName = null;

			using(SecurityContext.Impersonate(this))
			{
				if (sourceAlreadyExists && currentLogName != m_logName)
				{
					//
					// Re-register this to the current application if the user has changed
					// the application / logfile association
					//
					EventLog.DeleteEventSource(m_applicationName, m_machineName);
					CreateEventSource(m_applicationName, m_logName, m_machineName);

					registeredLogName = EventLog.LogNameFromSourceName(m_applicationName, m_machineName);
				}
				else if (!sourceAlreadyExists)
				{
					CreateEventSource(m_applicationName, m_logName, m_machineName);

					registeredLogName = EventLog.LogNameFromSourceName(m_applicationName, m_machineName);
				}
			}

			m_levelMapping.ActivateOptions();

			LogLog.Debug("EventLogAppender: Source [" + m_applicationName + "] is registered to log [" + registeredLogName + "]");		
		}

		#endregion // Implementation of IOptionHandler

		/// <summary>
		/// Create an event log source
		/// </summary>
		/// <remarks>
		/// Uses different API calls under NET_2_0
		/// </remarks>
		private static void CreateEventSource(string source, string logName, string machineName)
		{
#if NET_2_0
			EventSourceCreationData eventSourceCreationData = new EventSourceCreationData(source, logName);
			eventSourceCreationData.MachineName = machineName;
			EventLog.CreateEventSource(eventSourceCreationData);
#else
			EventLog.CreateEventSource(source, logName, machineName);
#endif
		}
 

		#region Override implementation of AppenderSkeleton

		/// <summary>
		/// This method is called by the <see cref="AppenderSkeleton.DoAppend(LoggingEvent)"/>
		/// method. 
		/// </summary>
		/// <param name="loggingEvent">the event to log</param>
		/// <remarks>
		/// <para>Writes the event to the system event log using the 
		/// <see cref="ApplicationName"/>.</para>
		/// 
		/// <para>If the event has an <c>EventID</c> property (see <see cref="LoggingEvent.Properties"/>)
		/// set then this integer will be used as the event log event id.</para>
		/// 
		/// <para>
		/// There is a limit of 32K characters for an event log message
		/// </para>
		/// </remarks>
		override protected void Append(LoggingEvent loggingEvent) 
		{
			//
			// Write the resulting string to the event log system
			//
			int eventID = 0;

			// Look for the EventLogEventID property
			object eventIDPropertyObj = loggingEvent.LookupProperty("EventID");
			if (eventIDPropertyObj != null)
			{
				if (eventIDPropertyObj is int)
				{
					eventID = (int)eventIDPropertyObj;
				}
				else
				{
					string eventIDPropertyString = eventIDPropertyObj as string;
					if (eventIDPropertyString != null && eventIDPropertyString.Length > 0)
					{
						// Read the string property into a number
						int intVal;
						if (SystemInfo.TryParse(eventIDPropertyString, out intVal))
						{
							eventID = intVal;
						}
						else
						{
							ErrorHandler.Error("Unable to parse event ID property [" + eventIDPropertyString + "].");
						}
					}
				}
			}

			// Write to the event log
			try
			{
				string eventTxt = RenderLoggingEvent(loggingEvent);

				// There is a limit of 32K characters for an event log message
				if (eventTxt.Length > 32000)
				{
					eventTxt = eventTxt.Substring(0, 32000);
				}

				EventLogEntryType entryType = GetEntryType(loggingEvent.Level);

				using(SecurityContext.Impersonate(this))
				{
					EventLog.WriteEntry(m_applicationName, eventTxt, entryType, eventID);
				}
			}
			catch(Exception ex)
			{
				ErrorHandler.Error("Unable to write to event log [" + m_logName + "] using source [" + m_applicationName + "]", ex);
			}
		} 

		/// <summary>
		/// This appender requires a <see cref="Layout"/> to be set.
		/// </summary>
		/// <value><c>true</c></value>
		/// <remarks>
		/// <para>
		/// This appender requires a <see cref="Layout"/> to be set.
		/// </para>
		/// </remarks>
		override protected bool RequiresLayout
		{
			get { return true; }
		}

		#endregion // Override implementation of AppenderSkeleton

		#region Protected Instance Methods

		/// <summary>
		/// Get the equivalent <see cref="EventLogEntryType"/> for a <see cref="Level"/> <paramref name="p"/>
		/// </summary>
		/// <param name="level">the Level to convert to an EventLogEntryType</param>
		/// <returns>The equivalent <see cref="EventLogEntryType"/> for a <see cref="Level"/> <paramref name="p"/></returns>
		/// <remarks>
		/// Because there are fewer applicable <see cref="EventLogEntryType"/>
		/// values to use in logging levels than there are in the 
		/// <see cref="Level"/> this is a one way mapping. There is
		/// a loss of information during the conversion.
		/// </remarks>
		virtual protected EventLogEntryType GetEntryType(Level level)
		{
			// see if there is a specified lookup.
			Level2EventLogEntryType entryType = m_levelMapping.Lookup(level) as Level2EventLogEntryType;
			if (entryType != null)
			{
				return entryType.EventLogEntryType;
			}

			// Use default behavior

			if (level >= Level.Error) 
			{
				return EventLogEntryType.Error;
			}
			else if (level == Level.Warn) 
			{
				return EventLogEntryType.Warning;
			} 

			// Default setting
			return EventLogEntryType.Information;
		}

		#endregion // Protected Instance Methods

		#region Private Instance Fields

		/// <summary>
		/// The log name is the section in the event logs where the messages
		/// are stored.
		/// </summary>
		private string m_logName;

		/// <summary>
		/// Name of the application to use when logging.  This appears in the
		/// application column of the event log named by <see cref="m_logName"/>.
		/// </summary>
		private string m_applicationName;

		/// <summary>
		/// The name of the machine which holds the event log. This is
		/// currently only allowed to be '.' i.e. the current machine.
		/// </summary>
		private string m_machineName;

		/// <summary>
		/// Mapping from level object to EventLogEntryType
		/// </summary>
		private LevelMapping m_levelMapping = new LevelMapping();

		/// <summary>
		/// The security context to use for privileged calls
		/// </summary>
		private SecurityContext m_securityContext;

		#endregion // Private Instance Fields

		#region Level2EventLogEntryType LevelMapping Entry

		/// <summary>
		/// A class to act as a mapping between the level that a logging call is made at and
		/// the color it should be displayed as.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Defines the mapping between a level and its event log entry type.
		/// </para>
		/// </remarks>
		public class Level2EventLogEntryType : LevelMappingEntry
		{
			private EventLogEntryType m_entryType;

			/// <summary>
			/// The <see cref="EventLogEntryType"/> for this entry
			/// </summary>
			/// <remarks>
			/// <para>
			/// Required property.
			/// The <see cref="EventLogEntryType"/> for this entry
			/// </para>
			/// </remarks>
			public EventLogEntryType EventLogEntryType
			{
				get { return m_entryType; }
				set { m_entryType = value; }
			}
		}

		#endregion // LevelColors LevelMapping Entry
	}
}

#endif // !SSCLI
#endif // !NETCF
