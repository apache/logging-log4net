#region Copyright
//
// This framework is based on log4j see http://jakarta.apache.org/log4j
// Copyright (C) The Apache Software Foundation. All rights reserved.
//
// This software is published under the terms of the Apache Software
// License version 1.1, a copy of which has been included with this
// distribution in the LICENSE.txt file.
// 
#endregion

#define DEBUG

using log4net.Layout;
using log4net.Core;

namespace log4net.Appender
{
	/// <summary>
	/// Appends log events to the <see cref="System.Diagnostics.Debug"/> system.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The application configuration file can be used to control what listeners 
	/// are actually used. See the MSDN documentation for the 
	/// <see cref="System.Diagnostics.Debug"/> class for details on configuring the
	/// debug system.
	/// </para>
	/// <para>
	/// Events are written using the <see cref="System.Diagnostics.Debug.Write(string,string)"/>
	/// method. The event's logger name is passed as the value for the category name to the Write method.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public class DebugAppender : AppenderSkeleton
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DebugAppender" />.
		/// </summary>
		public DebugAppender()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DebugAppender" /> 
		/// with a specified layout.
		/// </summary>
		/// <param name="layout">The layout to use with this appender.</param>
		public DebugAppender(ILayout layout)
		{
			Layout = layout;
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets or sets a value that indicates whether the appender will 
		/// flush at the end of each write.
		/// </summary>
		/// <remarks>
		/// <para>The default behaviour is to flush at the end of each 
		/// write. If the option is set to<c>false</c>, then the underlying 
		/// stream can defer writing to physical medium to a later time. 
		/// </para>
		/// <para>
		/// Avoiding the flush operation at the end of each append results 
		/// in a performance gain of 10 to 20 percent. However, there is safety
		/// trade-off involved in skipping flushing. Indeed, when flushing is
		/// skipped, then it is likely that the last few log events will not
		/// be recorded on disk when the application exits. This is a high
		/// price to pay even for a 20% performance gain.
		/// </para>
		/// </remarks>
		public bool ImmediateFlush
		{
			get { return m_immediateFlush; }
			set { m_immediateFlush = value; }
		}

		#endregion Public Instance Properties

		#region Override implementation of AppenderSkeleton

		/// <summary>
		/// Overrides the parent method to close the default debug channel
		/// </summary>
		override protected void OnClose() 
		{
			System.Diagnostics.Debug.Close();
		}

		/// <summary>
		/// Writes the logging event to the <see cref="System.Diagnostics.Debug"/> system.
		/// </summary>
		/// <param name="loggingEvent">The event to log.</param>
		override protected void Append(LoggingEvent loggingEvent) 
		{
			//
			// Write the string to the Debug system
			//
			System.Diagnostics.Debug.Write(RenderLoggingEvent(loggingEvent), loggingEvent.LoggerName);
	 
			//
			// Flush the Debug system if needed
			//
			if (m_immediateFlush) 
			{
				System.Diagnostics.Debug.Flush();
			} 
		}

		/// <summary>
		/// This appender requires a <see cref="Layout"/> to be set.
		/// </summary>
		/// <value><c>true</c></value>
		override protected bool RequiresLayout
		{
			get { return true; }
		}

		#endregion Override implementation of AppenderSkeleton

		#region Private Instance Fields

		/// <summary>
		/// Immediate flush means that the underlying writer or output stream
		/// will be flushed at the end of each append operation.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Immediate flush is slower but ensures that each append request is 
		/// actually written. If <see cref="ImmediateFlush"/> is set to
		/// <c>false</c>, then there is a good chance that the last few
		/// logs events are not actually written to persistent media if and
		/// when the application crashes.
		/// </para>
		/// <para>
		/// The default value is <c>true</c>.</para>
		/// </remarks>
		private bool m_immediateFlush = true;

		#endregion Private Instance Fields
	}
}
