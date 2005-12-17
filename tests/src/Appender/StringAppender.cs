#region Copyright & License
//
// Copyright 2001-2005 The Apache Software Foundation
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

using System.Text;

using log4net.Core;
using log4net.Appender;

namespace log4net.Tests.Appender
{
	/// <summary>
	/// Write events to a string
	/// </summary>
	/// <author>Nicko Cadell</author>
	public class StringAppender : AppenderSkeleton
	{
		private StringBuilder m_buf = new StringBuilder();

		/// <summary>
		/// Initializes a new instance of the <see cref="StringAppender" /> class.
		/// </summary>
		public StringAppender()
		{
		}

		/// <summary>
		/// Get the string logged so far
		/// </summary>
		/// <returns></returns>
		public string GetString()
		{
			return m_buf.ToString();
		}

		/// <summary>
		/// Reset the string
		/// </summary>
		public void Reset()
		{
			m_buf.Length = 0;
		}

		/// <summary>
		/// </summary>
		/// <param name="loggingEvent">the event to log</param>
		override protected void Append(LoggingEvent loggingEvent) 
		{
			m_buf.Append(RenderLoggingEvent(loggingEvent));
		} 

		/// <summary>
		/// This appender requires a <see cref="Layout"/> to be set.
		/// </summary>
		/// <value><c>true</c></value>
		override protected bool RequiresLayout
		{
			get { return true; }
		}
	}
}
