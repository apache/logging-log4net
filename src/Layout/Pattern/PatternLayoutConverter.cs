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
using System.Text;
using System.IO;
using System.Collections;

using log4net.Core;
using log4net.Util;
using log4net.Repository;

namespace log4net.Layout.Pattern
{
	/// <summary>
	/// Abstract class that provides the formatting functionality that 
	/// derived classes need.
	/// </summary>
	/// <remarks>
	/// Conversion specifiers in a conversion patterns are parsed to
	/// individual PatternConverters. Each of which is responsible for
	/// converting a logging event in a converter specific manner.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public abstract class PatternLayoutConverter : PatternConverter
	{
		#region Protected Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="PatternLayoutConverter" /> class.
		/// </summary>
		protected PatternLayoutConverter() 
		{  
		}

		#endregion Protected Instance Constructors

		#region Protected Abstract Methods

		/// <summary>
		/// Derived pattern converters must override this method in order to
		/// convert conversion specifiers in the correct way.
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="loggingEvent">The <see cref="LoggingEvent" /> on which the pattern converter should be executed.</param>
		abstract protected void Convert(TextWriter writer, LoggingEvent loggingEvent);

		#endregion Protected Abstract Methods

		#region Protected Methods

		/// <summary>
		/// Derived pattern converters must override this method in order to
		/// convert conversion specifiers in the correct way.
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="state">The state object on which the pattern converter should be executed.</param>
		override protected void Convert(TextWriter writer, object state)
		{
			if (state is LoggingEvent)
			{
				Convert(writer, (LoggingEvent)state);
			}
			else
			{
				throw new ArgumentException("state must be a LoggingEvent", "state");
			}
		}

		#endregion Protected Methods
	}
}
