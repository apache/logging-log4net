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

using log4net.Layout;
using log4net.Core;

namespace log4net.Appender
{
	/// <summary>
	/// Implements an Appender for test purposes that counts the
	/// number of output calls to <see cref="Append" />.
	/// </summary>
	/// <remarks>
	/// This appender is used in the unit tests.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class CountingAppender : AppenderSkeleton
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="CountingAppender" /> class.
		/// </summary>
		public CountingAppender()
		{
			m_counter = 0;
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Returns the number of times <see cref="Append" /> has been called.
		/// </summary>
		/// <value>
		/// The number of times <see cref="Append" /> has been called.
		/// </value>
		public int Counter
		{
			get { return m_counter; }
		}

		#endregion Public Instance Properties

		#region Override implementation of AppenderSkeleton

		/// <summary>
		/// Registers how many times the method has been called.
		/// </summary>
		/// <param name="logEvent">The logging event.</param>
		override protected void Append(LoggingEvent logEvent)
		{
			m_counter++;
		}

		#endregion Override implementation of AppenderSkeleton

		#region Private Instance Fields

		/// <summary>
		/// The number of times <see cref="Append" /> has been called.
		/// </summary>
		private int m_counter;

		#endregion Private Instance Fields
	}
}
