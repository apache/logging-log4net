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

using System.Web;

using log4net.Layout;
using log4net.Core;

namespace log4net.Appender 
{
	/// <summary>
	/// <para>
	/// Appends log events to the ASP.NET <see cref="System.Web.TraceContext"/> system.
	/// </para>
	/// </summary>
	/// <remarks>
	/// <para>
	/// Diagnostic information and tracing messages that you specify are appended to the output 
	/// of the page that is sent to the requesting browser. Optionally, you can view this information
	/// from a separate trace viewer (Trace.axd) that displays trace information for every page in a 
	/// given application.
	/// </para>
	/// <para>
	/// Trace statements are processed and displayed only when tracing is enabled. You can control 
	/// whether tracing is displayed to a page, to the trace viewer, or both.
	/// </para>
	/// <para>
	/// The logging event is passed to the <see cref="System.Web.TraceContext.Write"/> or 
	/// <see cref="System.Web.TraceContext.Warn"/> method depending on the level of the logging event.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class AspNetTraceAppender : AppenderSkeleton 
	{
		#region Public Instances Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="AspNetTraceAppender" /> class.
		/// </summary>
		public AspNetTraceAppender() 
		{
		}

		#endregion Public Instances Constructors

		#region Override implementation of AppenderSkeleton

		/// <summary>
		/// Actual writing occurs here.
		/// <para>Most subclasses of <see cref="AppenderSkeleton"/> will need to 
		/// override this method.</para>
		/// </summary>
		/// <param name="loggingEvent">the event to log</param>
		override protected void Append(LoggingEvent loggingEvent) 
		{
			// check if log4net is running in the context of an ASP.NET application
			if (HttpContext.Current != null) 
			{
				// check if tracing is enabled for the current context
				if (HttpContext.Current.Trace.IsEnabled) 
				{
					if (loggingEvent.Level >= Level.Warn) 
					{
						HttpContext.Current.Trace.Warn(loggingEvent.LoggerName, RenderLoggingEvent(loggingEvent));
					}
					else 
					{
						HttpContext.Current.Trace.Write(loggingEvent.LoggerName, RenderLoggingEvent(loggingEvent));
					}
				}
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
	}
}
