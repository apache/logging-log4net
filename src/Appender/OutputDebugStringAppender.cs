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

using System.Runtime.InteropServices;

using log4net.Layout;
using log4net.Core;

namespace log4net.Appender
{
	/// <summary>
	/// Appends log events to the OutputDebugString system.
	/// </summary>
	/// <remarks>
	/// <para>
	/// OutputDebugStringAppender appends log events to the
	/// OutputDebugString system.
	/// </para>
	/// <para>
	/// The string is passed to the native <c>OutputDebugString</c> 
	/// function.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class OutputDebugStringAppender : AppenderSkeleton
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="OutputDebugStringAppender" /> class.
		/// </summary>
		public OutputDebugStringAppender()
		{
		}

		#endregion Public Instance Constructors

		#region Override implementation of AppenderSkeleton

		/// <summary>
		/// This method is called by the <see cref="AppenderSkeleton.DoAppend"/>
		/// method. 
		/// </summary>
		/// <param name="loggingEvent">the event to log</param>
		override protected void Append(LoggingEvent loggingEvent) 
		{
			OutputDebugString(RenderLoggingEvent(loggingEvent));
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

		#region Protected Static Methods

		/// <summary>
		/// Stub for OutputDebugString native method
		/// </summary>
		/// <param name="message">the string to output</param>
#if NETCF
		[DllImport("CoreDll.dll")]
#else
		[DllImport("Kernel32.dll")]
#endif
		protected static extern void OutputDebugString(string message);

		#endregion Protected Static Methods
	}
}
