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
		#region Constructors

		/// <summary>
		/// Constructs a SimpleLayout
		/// </summary>
		/// <remarks>
		/// </remarks>
		public SimpleLayout()
		{
			IgnoresException = true;
		}

		#endregion
  
		#region Implementation of IOptionHandler

		/// <summary>
		/// Initialize layout options
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is part of the <see cref="IOptionHandler"/> delayed object
		/// activation scheme. The <see cref="ActivateOptions"/> method must 
		/// be called on this object after the configuration properties have
		/// been set. Until <see cref="ActivateOptions"/> is called this
		/// object is in an undefined state and must not be used. 
		/// </para>
		/// <para>
		/// If any of the configuration properties are modified then 
		/// <see cref="ActivateOptions"/> must be called again.
		/// </para>
		/// </remarks>
		override public void ActivateOptions() 
		{
			// nothing to do.
		}

		#endregion

		#region Override implementation of LayoutSkeleton

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

			writer.Write(loggingEvent.Level.DisplayName);
			writer.Write(" - ");
			loggingEvent.WriteRenderedMessage(writer);
			writer.WriteLine();
		}

		#endregion
	}
}
