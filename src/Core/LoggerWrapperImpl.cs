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

//using System;

namespace log4net.Core
{
	/// <summary>
	/// Implementation of the <see cref="ILoggerWrapper"/> interface.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class should be used as the base for all wrapper implementations.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public abstract class LoggerWrapperImpl : ILoggerWrapper
	{
		#region Protected Instance Constructors

		/// <summary>
		/// Constructs a new wrapper for the specified logger.
		/// </summary>
		/// <param name="logger">The logger to wrap.</param>
		protected LoggerWrapperImpl(ILogger logger) 
		{
			m_logger = logger;
		}

		#endregion Public Instance Constructors

		#region Implementation of ILoggerWrapper

		/// <summary>
		/// Gets the implementation behind this wrapper object.
		/// </summary>
		/// <value>
		/// The <see cref="ILogger"/> object that this object is implementing.
		/// </value>
		/// <remarks>
		/// <para>
		/// The <c>Logger</c> object may not be the same object as this object 
		/// because of logger decorators.
		/// </para>
		/// <para>
		/// This gets the actual underlying objects that is used to process
		/// the log events.
		/// </para>
		/// </remarks>
		virtual public ILogger Logger
		{
			get { return m_logger; }
		}

		#endregion

		#region Private Instance Fields

		/// <summary>
		/// The logger that this object is wrapping
		/// </summary>
		private readonly ILogger m_logger;  
 
		#endregion Private Instance Fields
	}
}
