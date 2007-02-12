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
using System.Web.Mail;

using log4net.Layout;
using log4net.Core;
using log4net.Appender;

namespace SampleAppendersApp.Appender
{
	/// <summary>
	/// Simple mail appender that sends individual messages
	/// </summary>
	/// <remarks>
	/// This SimpleSmtpAppender sends each LoggingEvent received as a
	/// separate mail message.
	/// The mail subject line can be specified using a pattern layout.
	/// </remarks>
	public class SimpleSmtpAppender : AppenderSkeleton
	{
		public SimpleSmtpAppender()
		{	
		}

		public string To 
		{
			get { return m_to; }
			set { m_to = value; }
		}

		public string From 
		{
			get { return m_from; }
			set { m_from = value; }
		}

		public PatternLayout Subject 
		{
			get { return m_subjectLayout; }
			set { m_subjectLayout = value; }
		}
  
		public string SmtpHost
		{
			get { return m_smtpHost; }
			set { m_smtpHost = value; }
		}

		#region Override implementation of AppenderSkeleton

		override protected void Append(LoggingEvent loggingEvent) 
		{
			try 
			{	  
				StringWriter writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);

				string t = Layout.Header;
				if (t != null)
				{
					writer.Write(t);
				}

				// Render the event and append the text to the buffer
				RenderLoggingEvent(writer, loggingEvent);

				t = Layout.Footer;
				if (t != null)
				{
					writer.Write(t);
				}

				MailMessage mailMessage = new MailMessage();
				mailMessage.Body = writer.ToString();
				mailMessage.From = m_from;
				mailMessage.To = m_to;

				if (m_subjectLayout == null)
				{
					mailMessage.Subject = "Missing Subject Layout";
				}
				else
				{
					StringWriter subjectWriter = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
					m_subjectLayout.Format(subjectWriter, loggingEvent);
					mailMessage.Subject = subjectWriter.ToString();
				}

				if (m_smtpHost != null && m_smtpHost.Length > 0)
				{
					SmtpMail.SmtpServer = m_smtpHost;
				}

				SmtpMail.Send(mailMessage);
			} 
			catch(Exception e) 
			{
				ErrorHandler.Error("Error occurred while sending e-mail notification.", e);
			}		
		}

		override protected bool RequiresLayout
		{
			get { return true; }
		}

		#endregion // Override implementation of AppenderSkeleton

		private string m_to;
		private string m_from;
		private PatternLayout m_subjectLayout;
		private string m_smtpHost;
	}
}