#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
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
using System.Windows.Forms;

using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Util;

namespace SampleAppendersApp.Appender
{
	/// <summary>
	/// Displays messages as message boxes
	/// </summary>
	/// <remarks>
	/// Displays each LoggingEvent as a MessageBox. The message box is UI modal
	/// and will block the calling thread until it is dismissed by the user.
	/// </remarks>
	public class MessageBoxAppender : AppenderSkeleton
	{
		private PatternLayout m_titleLayout;
		private LevelMapping m_levelMapping = new LevelMapping();

		public MessageBoxAppender()
		{
		}

		public void AddMapping(LevelIcon mapping)
		{
			m_levelMapping.Add(mapping);
		}

		public PatternLayout TitleLayout
		{
			get { return m_titleLayout; }
			set { m_titleLayout = value; }
		}

		override protected void Append(LoggingEvent loggingEvent) 
		{
			MessageBoxIcon messageBoxIcon = MessageBoxIcon.Information;

			LevelIcon levelIcon = m_levelMapping.Lookup(loggingEvent.Level) as LevelIcon;
			if (levelIcon != null)
			{
				// Prepend the Ansi Color code
				messageBoxIcon = levelIcon.Icon;
			}

			string message = RenderLoggingEvent(loggingEvent);

			string title = null;
			if (m_titleLayout == null)
			{
				title = "LoggingEvent: "+loggingEvent.Level.Name;
			}
			else
			{
				StringWriter titleWriter = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
				m_titleLayout.Format(titleWriter, loggingEvent);
				title = titleWriter.ToString();
			}

			MessageBox.Show(message, title, MessageBoxButtons.OK, messageBoxIcon);
		} 

		public override void ActivateOptions()
		{
			base.ActivateOptions();
			m_levelMapping.ActivateOptions();
		}

		public class LevelIcon : LevelMappingEntry
		{
			private MessageBoxIcon m_icon;

			public MessageBoxIcon Icon
			{
				get { return m_icon; }
				set { m_icon = value; }
			}
		}
	}
}
