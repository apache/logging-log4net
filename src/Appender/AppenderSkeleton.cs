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

using System;
using System.IO;

using log4net.Filter;
using log4net.Util;
using log4net.Layout;
using log4net.Core;

namespace log4net.Appender
{
	/// <summary>
	/// Abstract base class implementation of <see cref="IAppender"/>. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class provides the code for common functionality, such 
	/// as support for threshold filtering and support for general filters.
	/// </para>
	/// <para>
	/// Appenders can also implement the <see cref="IOptionHandler"/> interface. Therefore
	/// they would require that the <see cref="IOptionHandler.ActivateOptions()"/> method
	/// be called after the appenders properties have been configured.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public abstract class AppenderSkeleton : IAppender, IOptionHandler
	{
		#region Protected Instance Constructors

		/// <summary>
		/// Default constructor
		/// </summary>
		/// <remarks>
		/// <para>Empty default constructor</para>
		/// </remarks>
		protected AppenderSkeleton()
		{
			m_errorHandler = new OnlyOnceErrorHandler(this.GetType().Name);
		}

		#endregion Protected Instance Constructors

		#region Finalizer

		/// <summary>
		/// Finalizes this appender by calling the implementation's 
		/// <see cref="AppenderSkeleton.Close"/> method.
		/// </summary>
		~AppenderSkeleton() 
		{
			// An appender might be closed then garbage collected. 
			// There is no point in closing twice.
			if (!m_closed) 
			{
				LogLog.Debug("AppenderSkeleton: Finalizing appender named ["+m_name+"].");
				Close();
			}
		}

		#endregion Finalizer

		#region Public Instance Properties

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
		/// Gets or sets the <see cref="IErrorHandler"/> for this appender.
		/// </summary>
		/// <value>The <see cref="IErrorHandler"/> of the appender</value>
		/// <remarks>
		/// <para>
		/// The <see cref="AppenderSkeleton"/> provides a default 
		/// implementation for the <see cref="ErrorHandler"/> property. 
		/// </para>
		/// </remarks>
		virtual public IErrorHandler ErrorHandler 
		{
			get { return this.m_errorHandler; }
			set 
			{
				lock(this) 
				{
					if (value == null) 
					{
						// We do not throw exception here since the cause is probably a
						// bad config file.
						LogLog.Warn("AppenderSkeleton: You have tried to set a null error-handler.");
					} 
					else 
					{
						m_errorHandler = value;
					}
				}
			}
		}

		/// <summary>
		/// The filter chain.
		/// </summary>
		/// <value>The head of the filter chain filter chain.</value>
		/// <remarks>
		/// <para>
		/// Returns the head Filter. The Filters are organized in a linked list
		/// and so all Filters on this Appender are available through the result.
		/// </para>
		/// </remarks>
		virtual public IFilter FilterHead
		{
			get { return m_headFilter; }
		}

		/// <summary>
		/// Gets or sets the <see cref="ILayout"/> for this appender.
		/// </summary>
		/// <value>The layout of the appender.</value>
		/// <remarks>
		/// <para>
		/// See <see cref="RequiresLayout"/> for more information.
		/// </para>
		/// </remarks>
		/// <seealso cref="RequiresLayout"/>
		virtual public ILayout Layout 
		{
			get { return m_layout; }
			set { m_layout = value; }
		}

		#endregion

		#region Implementation of IOptionHandler

		/// <summary>
		/// Initialise the appender based on the options set
		/// </summary>
		virtual public void ActivateOptions() 
		{
		}

		#endregion Implementation of IOptionHandler

		#region Implementation of IAppender

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
		/// Closes the appender and release resources.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Release any resources allocated within the appender such as file handles, 
		/// network connections, etc.
		/// </para>
		/// <para>
		/// It is a programming error to append to a closed appender.
		/// </para>
		/// <para>
		/// This method cannot be overridden by subclasses. This method 
		/// delegates the closing of the appender to the <see cref="OnClose"/>
		/// method which must be overridden in the subclass.
		/// </para>
		/// </remarks>
		public void Close()
		{
			// This lock prevents the appender being closed while it is still appending
			lock(this)
			{
				if (!m_closed)
				{
					OnClose();
					m_closed = true;
				}
			}
		}

		/// <summary>
		/// Performs threshold checks and invokes filters before 
		/// delegating actual logging to the subclasses specific 
		/// <see cref="AppenderSkeleton.Append"/> method.
		/// </summary>
		/// <param name="loggingEvent">The event to log.</param>
		/// <remarks>
		/// <para>
		/// This method cannot be overridden by derived classes. A
		/// derived class should override the <see cref="Append"/> method
		/// which is called by this method.
		/// </para>
		/// <para>
		/// The implementation of this method is as follows:
		/// </para>
		/// <para>
		/// <list type="bullet">
		///		<item>
		///			<description>
		///			Checks that the severity of the <paramref name="loggingEvent"/>
		///			is greater than or equal to the <see cref="Threshold"/> of this
		///			appender.</description>
		///		</item>
		///		<item>
		///			<description>
		///			Checks that the <see cref="Filter"/> chain accepts the 
		///			<paramref name="loggingEvent"/>.
		///			</description>
		///		</item>
		///		<item>
		///			<description>
		///			Calls <see cref="PreAppendCheck()"/> and checks that 
		///			it returns <c>true</c>.</description>
		///		</item>
		/// </list>
		/// </para>
		/// <para>
		/// If all of the above steps succeed then the <paramref name="loggingEvent"/>
		/// will be passed to the abstract <see cref="Append"/> method.
		/// </para>
		/// </remarks>
		public void DoAppend(LoggingEvent loggingEvent) 
		{
			// This lock is absolutely critical for correct formatting
			// of the message in a multi-threaded environment.  Without
			// this, the message may be broken up into elements from
			// multiple thread contexts (like get the wrong thread ID).

			lock(this)
			{
				if (m_closed)
				{
					ErrorHandler.Error("Attempted to append to closed appender named ["+m_name+"].");
					return;
				}

				// prevent re-entry
				if (m_recursiveGuard)
				{
					return;
				}

				try
				{
					m_recursiveGuard = true;

					if (!IsAsSevereAsThreshold(loggingEvent.Level)) 
					{
						return;
					}

					IFilter f = this.FilterHead;

					while(f != null) 
					{
						switch(f.Decide(loggingEvent)) 
						{
							case FilterDecision.Deny: 
								return;		// Return without appending

							case FilterDecision.Accept:
								f = null;	// Break out of the loop
								break;

							case FilterDecision.Neutral:
								f = f.Next;	// Move to next filter
								break;
						}
					}

					if (PreAppendCheck())
					{
						this.Append(loggingEvent);
					}
				}
				catch(Exception ex)
				{
					ErrorHandler.Error("Failed in DoAppend", ex);
				}
				finally
				{
					m_recursiveGuard = false;
				}
			}
		}

		#endregion Implementation of IAppender

		#region Public Instance Methods

		/// <summary>
		/// Adds a filter to the end of the filter chain.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The Filters are organized in a linked list.
		/// </para>
		/// <para>
		/// Setting this property causes the new filter to be pushed onto the 
		/// back of the filter chain.
		/// </para>
		/// </remarks>
		virtual public void AddFilter(IFilter filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException("filter param nust not be null");
			}

			if (m_headFilter == null) 
			{
				m_headFilter = m_tailFilter = filter;
			} 
			else 
			{
				m_tailFilter.Next = filter;
				m_tailFilter = filter;	
			}
		}

		/// <summary>
		/// Clears the filter list for this appender.
		/// </summary>
		virtual public void ClearFilters()
		{
			m_headFilter = m_tailFilter = null;
		}

		#endregion Public Instance Methods

		#region Protected Instance Methods

		/// <summary>
		/// Checks if the message level is below this appender's threshold.
		/// </summary>
		/// <param name="level"><see cref="Level"/> to test against.</param>
		/// <remarks>
		/// <para>
		/// If there is no threshold set, then the return value is always <c>true</c>.
		/// </para>
		/// </remarks>
		/// <returns>
		/// <c>true</c> if the <paramref name="level"/> meets the <see cref="Threshold"/> 
		/// requirements of this appender.
		/// </returns>
		virtual protected bool IsAsSevereAsThreshold(Level level) 
		{
			return ((m_threshold == null) || level >= m_threshold);
		}

		/// <summary>
		/// Is called when the appender is closed. Derived classes should override 
		/// this method if resources need to be released.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Releases any resources allocated within the appender such as file handles, 
		/// network connections, etc.
		/// </para>
		/// <para>
		/// It is a programming error to append to a closed appender.
		/// </para>
		/// </remarks>
		virtual protected void OnClose() 
		{
			// Do nothing by default
		}

		/// <summary>
		/// Subclasses of <see cref="AppenderSkeleton"/> should implement this method 
		/// to perform actual logging.
		/// </summary>
		/// <param name="loggingEvent">The event to append.</param>
		/// <remarks>
		/// <para>
		/// A subclass must implement this method to perform
		/// logging of the <paramref name="loggingEvent"/>.
		/// </para>
		/// <para>This method will be called by <see cref="AppenderSkeleton.DoAppend"/>
		/// if all the conditions listed for that method are met.
		/// </para>
		/// <para>
		/// To restrict the logging of events in the appender
		/// override the <see cref="PreAppendCheck()"/> method.
		/// </para>
		/// </remarks>
		abstract protected void Append(LoggingEvent loggingEvent);

		/// <summary>
		/// Called before <see cref="Append"/> as a precondition.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method is called by <see cref="AppenderSkeleton.DoAppend"/>
		/// before the call to the abstract <see cref="Append"/> method.
		/// </para>
		/// <para>
		/// This method can be overridden in a subclass to extend the checks 
		/// made before the event is passed to the <see cref="Append"/> method.
		/// </para>
		/// <para>
		/// A subclass should ensure that they delegate this call to
		/// this base class if it is overridden.
		/// </para>
		/// </remarks>
		/// <returns><c>true</c> if the call to <see cref="Append"/> should proceed.</returns>
		virtual protected bool PreAppendCheck()
		{
			if ((m_layout == null) && RequiresLayout)
			{
				ErrorHandler.Error("AppenderSkeleton: No layout set for the appender named ["+m_name+"].");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Renders the <see cref="LoggingEvent"/> to a string.
		/// </summary>
		/// <param name="loggingEvent">The event to render.</param>
		/// <returns>The event rendered as a string.</returns>
		/// <remarks>
		/// <para>
		/// Helper method to render a <see cref="LoggingEvent"/> to 
		/// a string. This appender must have a <see cref="Layout"/>
		/// set to render the <paramref name="loggingEvent"/> to 
		/// a string.
		/// </para>
		/// <para>If there is exception data in the logging event and 
		/// the layout does not process the exception, this method 
		/// will append the exception text to the rendered string.
		/// </para>
		/// <para>
		/// Where possible use the alternative version of this method
		/// <see cref="RenderLoggingEvent(TextWriter,LoggingEvent)"/>.
		/// That method streams the rendering onto an existing Writer
		/// which can give better performace if the caller already has
		/// a <see cref="TextWriter"/> open and ready for writing.
		/// </para>
		/// </remarks>
		protected string RenderLoggingEvent(LoggingEvent loggingEvent)
		{
			// Create the render writer on first use
			if (m_renderWriter == null)
			{
				m_renderWriter = new ReusableStringWriter(System.Globalization.CultureInfo.InvariantCulture);
			}

			// Reset the writer so we can reuse it
			m_renderWriter.Reset(c_renderBufferMaxCapacity, c_renderBufferSize);

			RenderLoggingEvent(m_renderWriter, loggingEvent);
			return m_renderWriter.ToString();
		}

		/// <summary>
		/// Renders the <see cref="LoggingEvent"/> to a string.
		/// </summary>
		/// <param name="loggingEvent">The event to render.</param>
		/// <param name="writer">The TextWriter to write the formatted event to</param>
		/// <remarks>
		/// <para>
		/// Helper method to render a <see cref="LoggingEvent"/> to 
		/// a string. This appender must have a <see cref="Layout"/>
		/// set to render the <paramref name="loggingEvent"/> to 
		/// a string.
		/// </para>
		/// <para>If there is exception data in the logging event and 
		/// the layout does not process the exception, this method 
		/// will append the exception text to the rendered string.
		/// </para>
		/// <para>
		/// Use this method in preference to <see cref="RenderLoggingEvent(LoggingEvent)"/>
		/// where possible. If, however, the caller needs to render the event
		/// to a string then <see cref="RenderLoggingEvent(LoggingEvent)"/> does
		/// provide an efficent mecanisum for doing so.
		/// </para>
		/// </remarks>
		protected void RenderLoggingEvent(TextWriter writer, LoggingEvent loggingEvent)
		{
			if (m_layout == null) 
			{
				throw new InvalidOperationException("A layout must be set");
			}

			if (m_layout.IgnoresException) 
			{
				string exceptionStr = loggingEvent.GetExceptionStrRep();
				if (exceptionStr != null && exceptionStr.Length > 0) 
				{
					// render the event and the exception
					m_layout.Format(writer, loggingEvent);
					writer.WriteLine(exceptionStr);
				}
				else 
				{
					// there is no exception to render
					m_layout.Format(writer, loggingEvent);
				}
			}
			else 
			{
				// The layout will render the exception
				m_layout.Format(writer, loggingEvent);
			}
		}

		/// <summary>
		/// Tests if this appender requires a <see cref="Layout"/> to be set.
		/// </summary>
		/// <remarks>
		/// <para>
		/// In the rather exceptional case, where the appender 
		/// implementation admits a layout but can also work without it, 
		/// then the appender should return <c>true</c>.
		/// </para>
		/// <para>
		/// This default implementation always returns <c>true</c>.
		/// </para>
		/// </remarks>
		/// <returns>
		/// <c>true</c> if the appender requires a layout object, otherwise <c>false</c>.
		/// </returns>
		virtual protected bool RequiresLayout
		{
			get { return false; }
		}

		#endregion

		#region Private Instance Fields

		/// <summary>
		/// The layout of this appender.
		/// </summary>
		/// <remarks>
		/// See <see cref="Layout"/> for more information.
		/// </remarks>
		private ILayout m_layout;

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
		private IErrorHandler m_errorHandler;

		/// <summary>
		/// The first filter in the filter chain.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Set to <c>null</c> initially.
		/// </para>
		/// <para>
		/// See <see cref="Filter"/> for more information.
		/// </para>
		/// </remarks>
		private IFilter m_headFilter;

		/// <summary>
		/// The last filter in the filter chain.
		/// </summary>
		/// <remarks>
		/// See <see cref="Filter"/> for more information.
		/// </remarks>
		private IFilter m_tailFilter;

		/// <summary>
		/// Flag indicating if this appender is closed.
		/// </summary>
		/// <remarks>
		/// See <see cref="Close"/> for more information.
		/// </remarks>
		private bool m_closed = false;

		/// <summary>
		/// The guard prevents an appender from repeatedly calling its own DoAppend method
		/// </summary>
		private bool m_recursiveGuard = false;

		/// <summary>
		/// StringWriter used to render events
		/// </summary>
		private ReusableStringWriter m_renderWriter = null;

		#endregion Private Instance Fields

		#region Constants

		/// <summary>
		/// Initial buffer size
		/// </summary>
		private const int c_renderBufferSize = 256;

		/// <summary>
		/// Maximum buffer size before it is recycled
		/// </summary>
		private const int c_renderBufferMaxCapacity = 1024;

		#endregion
	}
}
