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
	/// A very simple layout
	/// </summary>
	/// <remarks>
	/// SimpleLayout consists of the level of the log statement,
	/// followed by " - " and then the log message itself. For example,
	/// <code>
	/// DEBUG - Hello world
	/// </code>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class SimpleLayout : LayoutSkeleton
	{
		#region Constants

//		/// <summary>
//		/// Initial buffer size
//		/// </summary>
//  		protected const int BUF_SIZE = 256;
//
//		/// <summary>
//		/// Maximum buffer size before it is recycled
//		/// </summary>
//		protected const int MAX_CAPACITY = 1024;

		#endregion

		#region Member Variables
  
//		/// <summary>
//		/// output buffer appended to when Format() is invoked
//		/// </summary>
//		private StringBuilder m_sbuf = new StringBuilder(BUF_SIZE);
  
		#endregion

		#region Constructors

		/// <summary>
		/// Constructs a SimpleLayout
		/// </summary>
		/// <remarks>
		/// </remarks>
		public SimpleLayout()
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
		/// The SimpleLayout does not handle the exception contained within
		/// LoggingEvents. Thus, it returns <c>true</c>.
		/// </summary>
		override public bool IgnoresException
		{
			get { return true; }
		}

		/// <summary>
		/// Produces a formatted string.
		/// </summary>
		/// <param name="loggingEvent">the event being logged</param>
		/// <param name="writer">The TextWriter to write the formatted event to</param>
		override public void Format(TextWriter writer, LoggingEvent loggingEvent) 
		{
			if (loggingEvent == null)
			{
				throw new ArgumentNullException("loggingEvent");
			}

			writer.Write(loggingEvent.Level.Name);
			writer.Write(" - ");
			loggingEvent.WriteRenderedMessage(writer);
			writer.WriteLine();
		}

		#endregion
	}
}
