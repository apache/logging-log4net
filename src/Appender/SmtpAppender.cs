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

// .NET Compact Framework 1.0 has no support for System.Web.Mail
// SSCLI 1.0 has no support for System.Web.Mail
#if !NETCF && !SSCLI

using System;
using System.IO;
using System.Web.Mail;

using log4net.Layout;
using log4net.Core;
using log4net.Util;

namespace log4net.Appender
{
	/// <summary>
	/// Send an e-mail when a specific logging event occurs, typically on errors 
	/// or fatal errors.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The number of logging events delivered in this e-mail depend on
	/// the value of <see cref="BufferingAppenderSkeleton.BufferSize"/> option. The
	/// <see cref="SmtpAppender"/> keeps only the last
	/// <see cref="BufferingAppenderSkeleton.BufferSize"/> logging events in its 
	/// cyclic buffer. This keeps memory requirements at a reasonable level while 
	/// still delivering useful application context.
	/// </para>
	/// <para>
	/// This appender sets the <c>log4net:HostName</c> property in the 
	/// <see cref="LoggingEvent.Properties"/> collection to the name of 
	/// the machine on which the event is logged.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class SmtpAppender : BufferingAppenderSkeleton
	{
		#region Public Instance Constructors

		/// <summary>
		/// </summary>
		public SmtpAppender()
		{	
		}

		#endregion // Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets or sets a semicolon-delimited list of recipient e-mail addresses.
		/// </summary>
		/// <value>
		/// A semicolon-delimited list of e-mail addresses.
		/// </value>
		public string To 
		{
			get { return m_to; }
			set { m_to = value; }
		}

		/// <summary>
		/// Gets or sets the e-mail address of the sender.
		/// </summary>
		/// <value>
		/// The e-mail address of the sender.
		/// </value>
		public string From 
		{
			get { return m_from; }
			set { m_from = value; }
		}

		/// <summary>
		/// Gets or sets the subject line of the e-mail message.
		/// </summary>
		/// <value>
		/// The subject line of the e-mail message.
		/// </value>
		public string Subject 
		{
			get { return m_subject; }
			set { m_subject = value; }
		}
  
		/// <summary>
		/// Gets or sets the name of the SMTP relay mail server to use to send 
		/// the e-mail messages.
		/// </summary>
		/// <value>
		/// The name of the e-mail relay server. If SmtpServer is not set, the 
		/// name of the local SMTP server is used.
		/// </value>
		public string SmtpHost
		{
			get { return m_smtpHost; }
			set { m_smtpHost = value; }
		}

		/// <summary>
		/// Obsolete
		/// </summary>
		/// <remarks>
		/// Use the BufferingAppenderSkeleton Fix methods instead 
		/// </remarks>
		[Obsolete("Use the BufferingAppenderSkeleton Fix methods")]
		public bool LocationInfo
		{
			get { return false; }
			set { ; }
		}

		#endregion // Public Instance Properties

		#region Override implementation of BufferingAppenderSkeleton

		/// <summary>
		/// Sends the contents of the cyclic buffer as an e-mail message.
		/// </summary>
		/// <param name="events">The logging events to send.</param>
		override protected void SendBuffer(LoggingEvent[] events) 
		{
			// Note: this code already owns the monitor for this
			// appender. This frees us from needing to synchronize on 'cb'.
			try 
			{	  
				StringWriter writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);

				string t = Layout.Header;
				if (t != null)
				{
					writer.Write(t);
				}


				string hostName = SystemInfo.HostName;

				for(int i = 0; i < events.Length; i++) 
				{
					// Set the hostname property
					if (events[i].Properties[LoggingEvent.HostNameProperty] == null)
					{
						events[i].Properties[LoggingEvent.HostNameProperty] = hostName;
					}

					// Render the event and append the text to the buffer
					RenderLoggingEvent(writer, events[i]);
				}

				t = Layout.Footer;
				if (t != null)
				{
					writer.Write(t);
				}

				MailMessage mailMessage = new MailMessage();
				mailMessage.Body = writer.ToString();
				mailMessage.From = m_from;
				mailMessage.To = m_to;
				mailMessage.Subject = m_subject;

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

		#endregion // Override implementation of BufferingAppenderSkeleton

		#region Override implementation of AppenderSkeleton

		/// <summary>
		/// This appender requires a <see cref="Layout"/> to be set.
		/// </summary>
		/// <value><c>true</c></value>
		override protected bool RequiresLayout
		{
			get { return true; }
		}

		#endregion // Override implementation of AppenderSkeleton

		#region Private Instance Fields

		private string m_to;
		private string m_from;
		private string m_subject;
		private string m_smtpHost;

		#endregion // Private Instance Fields
	}
}

#endif // !NETCF && !SSCLI