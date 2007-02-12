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
using System.Text;
using log4net.Appender;
using log4net.Util;
using log4net.Layout;
using log4net.Core;

namespace SampleAppendersApp.Appender
{
	/// <summary>
	/// Appender that writes to a file named using a pattern
	/// </summary>
	/// <remarks>
	/// The file to write to is selected for each event using a
	/// PatternLayout specified in the File property. This allows
	/// each LoggingEvent to be written to a file based on properties
	/// of the event.
	/// The output file is opened to write each LoggingEvent as it arrives
	/// and closed afterwards.
	/// </remarks>
	public class PatternFileAppender : AppenderSkeleton
	{
		public PatternFileAppender()
		{
		}

		public PatternLayout File
		{
			get { return m_filePattern; }
			set { m_filePattern = value; }
		}

		public Encoding Encoding
		{
			get { return m_encoding; }
			set { m_encoding = value; }
		}

		public SecurityContext SecurityContext 
		{
			get { return m_securityContext; }
			set { m_securityContext = value; }
		}

		override public void ActivateOptions() 
		{	
			base.ActivateOptions();

			if (m_securityContext == null)
			{
				m_securityContext = SecurityContextProvider.DefaultProvider.CreateSecurityContext(this);
			}
		}

		override protected void Append(LoggingEvent loggingEvent) 
		{
			try
			{
				// Render the file name
				StringWriter stringWriter = new StringWriter();
				m_filePattern.Format(stringWriter, loggingEvent);
				string fileName = stringWriter.ToString();

				fileName = SystemInfo.ConvertToFullPath(fileName);

				FileStream fileStream = null;

				using(m_securityContext.Impersonate(this))
				{
					// Ensure that the directory structure exists
					string directoryFullName = Path.GetDirectoryName(fileName);

					// Only create the directory if it does not exist
					// doing this check here resolves some permissions failures
					if (!Directory.Exists(directoryFullName))
					{
						Directory.CreateDirectory(directoryFullName);
					}

					// Open file stream while impersonating
					fileStream = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read);
				}

				if (fileStream != null)
				{
					using(StreamWriter streamWriter = new StreamWriter(fileStream, m_encoding))
					{
						RenderLoggingEvent(streamWriter, loggingEvent);
					}

					fileStream.Close();
				}
			}
			catch(Exception ex)
			{
				ErrorHandler.Error("Failed to append to file", ex);
			}
		}

		private PatternLayout m_filePattern = null;
		private Encoding m_encoding = Encoding.Default;
		private SecurityContext m_securityContext;
	}
}
