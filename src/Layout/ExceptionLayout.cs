#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
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

using System;
using System.IO;
using System.Text;

using log4net.Util;
using log4net.Core;

namespace log4net.Layout
{
	/// <summary>
	/// A Layout that renders only the Exception text from the logging event
	/// </summary>
	/// <remarks>
	/// <para>A Layout that renders only the Exception text from the logging event</para>
	/// <para>This Layout should only be used with appenders that utilise multiple
	/// layouts (e.g. <see cref="log4net.Appender.AdoNetAppender"/>).</para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class ExceptionLayout : LayoutSkeleton
	{
		#region Constructors

		/// <summary>
		/// Constructs a ExceptionLayout
		/// </summary>
		/// <remarks>
		/// </remarks>
		public ExceptionLayout()
		{
		}

		#endregion
  
		#region Implementation of IOptionHandler

		/// <summary>
		/// Does not do anything as options become effective immediately.
		/// </summary>
		override public void ActivateOptions() 
		{
			// nothing to do.
		}

		#endregion

		#region Override implementation of LayoutSkeleton

		/// <summary>
		/// The ExceptionLayout only handles the exception. Thus, it returns <c>false</c>.
		/// </summary>
		/// <value>
		/// The ExceptionLayout only handles the exception. Thus, it returns <c>false</c>.
		/// </value>
		/// <remarks>
		/// The ExceptionLayout only handles the exception. Thus, it returns <c>false</c>.
		/// </remarks>
		override public bool IgnoresException
		{
			get { return false; }
		}

		/// <summary>
		/// Gets the exception text from the logging event
		/// </summary>
		/// <param name="writer">The TextWriter to write the formatted event to</param>
		/// <param name="loggingEvent">the event being logged</param>
		override public void Format(TextWriter writer, LoggingEvent loggingEvent) 
		{
			if (loggingEvent == null)
			{
				throw new ArgumentNullException("loggingEvent");
			}

			writer.Write(loggingEvent.GetExceptionStrRep());
		}

		#endregion
	}
}
