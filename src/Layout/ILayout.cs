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

using log4net;
using log4net.Core;

namespace log4net.Layout
{
	/// <summary>
	/// Interface implemented by layout objects
	/// </summary>
	/// <remarks>
	/// <para>An <see cref="ILayout"/> object is used to format a <see cref="LoggingEvent"/>
	/// as text. The <see cref="Format(LoggingEvent)"/> method is called by an
	/// appender to transform the <see cref="LoggingEvent"/> into a string.</para>
	/// 
	/// <para>The layout can also supply <see cref="Header"/> and <see cref="Footer"/>
	/// text that is appender before any events and after all the events respectively.</para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public interface ILayout
	{
		/// <summary>
		/// Implement this method to create your own layout format.
		/// </summary>
		/// <param name="loggingEvent">The event to format</param>
		/// <returns>returns the formatted event</returns>
		/// <remarks>
		/// <para>This method is called by an appender to format
		/// the <paramref name="loggingEvent"/> as a string.</para>
		/// </remarks>
		[Obsolete("Use Format(TextWriter,LoggingEvent)")]
		string Format(LoggingEvent loggingEvent);

		/// <summary>
		/// Implement this method to create your own layout format.
		/// </summary>
		/// <param name="writer">The TextWriter to write the formatted event to</param>
		/// <param name="loggingEvent">The event to format</param>
		/// <remarks>
		/// <para>This method is called by an appender to format
		/// the <paramref name="loggingEvent"/> as text.</para>
		/// </remarks>
		void Format(TextWriter writer, LoggingEvent loggingEvent);

		/// <summary>
		/// The content type output by this layout. 
		/// </summary>
		/// <value>The content type</value>
		/// <remarks>
		/// <para>The content type output by this layout.</para>
		/// <para>This is a MIME type e.g. <c>"text/plain"</c>.</para>
		/// </remarks>
		string ContentType { get; }

		/// <summary>
		/// The header for the layout format.
		/// </summary>
		/// <value>the layout header</value>
		/// <remarks>
		/// <para>The Header text will be appended before any logging events
		/// are formatted and appended.</para>
		/// </remarks>
		string Header { get; }

		/// <summary>
		/// The footer for the layout format.
		/// </summary>
		/// <value>the layout footer</value>
		/// <remarks>
		/// <para>The Footer text will be appended after all the logging events
		/// have been formatted and appended.</para>
		/// </remarks>
		string Footer { get; }

		/// <summary>
		/// Flag indicating if this layout handle exceptions
		/// </summary>
		/// <value><c>false</c> if this layout handles exceptions</value>
		/// <remarks>
		/// <para>If this layout handles the exception object contained within
		/// <see cref="LoggingEvent"/>, then the layout should return
		/// <c>false</c>. Otherwise, if the layout ignores the exception
		/// object, then the layout should return <c>true</c>.</para>
		/// </remarks>
		bool IgnoresException { get; }
	}
}
