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
using System.Management.Instrumentation;

// This is the WMI namespace for event objects in this assembly
[assembly: Instrumented("root/log4net")]

namespace log4net.Appender
{
	/// <summary>
	/// <see cref="WmiAppender"/> fires instrumented events for each <see cref="LoggingEvent"/>
	/// </summary>
	/// <remarks>
	/// <para>
	/// This appender fires Windows Management Instrumentation (WMI) events for
	/// each <see cref="LoggingEvent"/>.
	/// </para>
	/// <para>
	/// By default this appender fires <see cref="WmiLoggingEvent"/> objects, however 
	/// this can be overridden by specifying a custom <see cref="Layout"/> or by setting
	/// the <see cref="LoggingEvent"/>.<see cref="LoggingEvent.MessageObject"/> to an
	/// <see cref="IEvent"/> instance.
	/// </para>
	/// <para>
	/// This assembly must be registered with WMI. Use the <c>InstallUtil</c> tool
	/// shipped with the .NET framework to install this assembly. This will register
	/// the <c>root/log4net</c> WMI namespace.
	/// </para>
	/// </remarks>
	public sealed class WmiAppender : IAppender, IOptionHandler
	{
		#region Private Instance Fields

		/// <summary>
		/// The layout of this appender.
		/// </summary>
		/// <remarks>
		/// See <see cref="Layout"/> for more information.
		/// </remarks>
		private WmiLayout m_layout;

		/// <summary>
		/// The name of this appender.
		/// </summary>
		/// <remarks>
		/// See <see cref="Name"/> for more information.
		/// </remarks>
		private string m_name;

		/// <summary>
		/// The level threshold of this appender.
		/// </summary>
		/// <remarks>
		/// <para>
		/// There is no level threshold filtering by default.
		/// </para>
		/// <para>
		/// See <see cref="Threshold"/> for more information.
		/// </para>
		/// </remarks>
		private Level m_threshold;

		/// <summary>
		/// It is assumed and enforced that errorHandler is never null.
		/// </summary>
		/// <remarks>
		/// <para>
		/// It is assumed and enforced that errorHandler is never null.
		/// </para>
		/// <para>
		/// See <see cref="ErrorHandler"/> for more information.
		/// </para>
		/// </remarks>
		private IErrorHandler m_errorHandler = new log4net.Util.OnlyOnceErrorHandler("WmiAppender");

		#endregion

		#region Public Instance Properties

		/// <summary>
		/// Gets or sets the name of this appender.
		/// </summary>
		/// <value>The name of the appender.</value>
		/// <remarks>
		/// <para>
		/// The name uniquely identifies the appender.
		/// </para>
		/// </remarks>
		public string Name 
		{
			get { return m_name; }
			set { m_name = value; }
		}

		/// <summary>
		/// Gets or sets the threshold <see cref="Level"/> of this appender.
		/// </summary>
		/// <value>
		/// The threshold <see cref="Level"/> of the appender. 
		/// </value>
		/// <remarks>
		/// <para>
		/// All log events with lower level than the threshold level are ignored 
		/// by the appender.
		/// </para>
		/// <para>
		/// In configuration files this option is specified by setting the
		/// value of the <see cref="Threshold"/> option to a level
		/// string, such as "DEBUG", "INFO" and so on.
		/// </para>
		/// </remarks>
		public Level Threshold 
		{
			get { return m_threshold; }
			set { m_threshold = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="WmiLayout"/> for this appender.
		/// </summary>
		/// <value>The layout of the appender.</value>
		/// <remarks>
		/// <para>
		/// The <see cref="WmiLayout"/> to use to format the
		/// <see cref="LoggingEvent"/> as an <see cref="IEvent"/>.
		/// </para>
		/// </remarks>
		public WmiLayout Layout
		{
			get { return m_layout; }
			set { m_layout = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="IErrorHandler"/> for this appender.
		/// </summary>
		/// <value>The <see cref="IErrorHandler"/> of the appender</value>
		/// <remarks>
		/// <para>
		/// The default value is a <see cref="OnlyOnceErrorHandler"/>.
		/// </para>
		/// </remarks>
		public IErrorHandler ErrorHandler 
		{
			get { return m_errorHandler; }
			set 
			{
				if (value == null) 
				{
					// We do not throw exception here since the cause is probably a
					// bad config file.
					log4net.Util.LogLog.Warn(GetType(), "WmiAppender: You have tried to set a null error-handler.");
				} 
				else 
				{
					m_errorHandler = value;
				}
			}
		}

		#endregion Public Instance Properties

		/// <summary>
		/// Activate this appender
		/// </summary>
		/// <remarks>
		/// <para>
		/// If a <see cref="Layout"/> has not been specified then this
		/// method will create a default <see cref="WmiLayout"/> instance.
		/// </para>
		/// </remarks>
		public void ActivateOptions() 
		{
			if (m_layout == null)
			{
				m_layout = new WmiLayout();
			}
		}

		/// <summary>
		/// Close this appender
		/// </summary>
		public void Close()
		{
		}

		/// <summary>
		/// Process a <see cref="LoggingEvent"/>
		/// </summary>
		/// <param name="loggingEvent">the <see cref="LoggingEvent"/> containing the data</param>
		/// <remarks>
		/// <para>
		/// Uses the <see cref="Layout"/> to format the <paramref name="loggingEvent"/>
		/// as an <see cref="IEvent"/>. This <see cref="IEvent"/> is then fired.
		/// </para>
		/// </remarks>
		public void DoAppend(LoggingEvent loggingEvent)
		{
			if (loggingEvent == null)
			{
				throw new ArgumentNullException("loggingEvent");
			}

			try
			{
				if (IsAsSevereAsThreshold(loggingEvent.Level))
				{
					IEvent instrumentationEvent = m_layout.Format(loggingEvent);
					if (instrumentationEvent != null)
					{
						instrumentationEvent.Fire();
					}
				}
			}
			catch(Exception ex)
			{
				ErrorHandler.Error("Failed in DoAppend", ex);
			}
			catch
			{
				// Catch handler for non System.Exception types
				ErrorHandler.Error("Failed in DoAppend (unknown exception)");
			}
		}

		/// <summary>
		/// Checks if the message level is below this appender's threshold.
		/// </summary>
		private bool IsAsSevereAsThreshold(Level level) 
		{
			return ((m_threshold == null) || level >= m_threshold);
		}
	}
}
