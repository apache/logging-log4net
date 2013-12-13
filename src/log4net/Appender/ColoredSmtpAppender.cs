using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;
using log4net.Appender;
using log4net.Core;
using log4net.Util;

namespace log4net.Appender
{
    /// <summary>
    /// Appends colorful logging events to the email html content
    /// </summary>
    /// <remarks>
    /// <para>
    /// When configuring the colored console appender, mappings should be
    /// specified to map logging levels to colors and css styles. For example:
    /// </para>
    /// <code lang="XML" escaped="true">
    ///	<mapping>
    ///		<level value="ERROR" />
    ///		<foreColor value="Red" />
    ///		<backColor value="White" />
    ///     <style value="font-size: larger;font-weight: bold" />
    ///	</mapping>
    ///	<mapping>
    ///		<level value="WARN" />
    ///		<foreColor value="#FFFF00" />
    ///	</mapping>
    ///	<mapping>
    ///		<level value="INFO" />
    ///		<foreColor value="#000" />
    ///	</mapping>
    ///	<mapping>
    ///		<level value="DEBUG" />
    ///		<foreColor value="#eee" />
    ///	</mapping>
    /// </code>
    /// <para>
    /// The Level is the standard log4net logging level while
    /// ForeColor and BackColor are the values of CSS color value
    /// Style is the CSS style.
    /// </para>
    /// </remarks>
    /// <author>Devin Zeng</author>
    public class ColoredSmtpAppender : SmtpAppender
    {
        private LevelMapping m_levelMapping = new LevelMapping();

        protected override void SendBuffer(LoggingEvent[] events)
        {
            // Note: this code already owns the monitor for this
            // appender. This frees us from needing to synchronize again.
            try
            {
                StringWriter writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);

                string t = Layout.Header;
                if (t != null)
                {
                    writer.Write(t);
                }

                for (int i = 0; i < events.Length; i++)
                {
                    // Render the event and append the text to the buffer
                    RenderColoredLoggingEvent(writer, events[i]);
                }

                t = Layout.Footer;
                if (t != null)
                {
                    writer.Write(t);
                }

                SendEmail(writer.ToString());
            }
            catch (Exception e)
            {
                ErrorHandler.Error("Error occurred while sending e-mail notification.", e);
            }
        }
        /// <summary>
        /// Based on loggingEvent configration, render the html formatted log content.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="loggingEvent"></param>
        protected void RenderColoredLoggingEvent(TextWriter writer, LoggingEvent loggingEvent)
        {
            if (Layout == null)
            {
                throw new InvalidOperationException("A layout must be set");
            }
            if (Layout.IgnoresException)
            {
                string exceptionStr = loggingEvent.GetExceptionString();
                if (exceptionStr != null && exceptionStr.Length > 0)
                {
                    writer.WriteLine(HtmlMessage(exceptionStr));
                }
            }
            writer.WriteLine(BuildHtmlLoggingMessage(loggingEvent));

        }
        /// <summary>
        /// Build loggingEvent html format message
        /// </summary>
        /// <param name="loggingEvent"></param>
        /// <returns>Log content</returns>
        private string BuildHtmlLoggingMessage(LoggingEvent loggingEvent)
        {
            StringBuilder style = new StringBuilder();
            LevelColors levelColors = m_levelMapping.Lookup(loggingEvent.Level) as LevelColors;
            if (levelColors != null)
            {
                // if the backColor has been explicitly set
                if (levelColors.HasBackColor)
                {
                    style.Append("background-color:" + levelColors.BackColor + ";");
                }
                // if the foreColor has been explicitly set
                if (levelColors.HasForeColor)
                {
                    style.Append("color:" + levelColors.ForeColor + ";");
                }
                if (!String.IsNullOrEmpty(levelColors.Style))
                {
                    style.Append(levelColors.Style);
                }
            }
            var css = style.ToString();
            string strLoggingMessage = RenderLoggingEvent(loggingEvent);
            return HtmlMessage(strLoggingMessage, css);
        }

        private string HtmlMessage(string strLoggingMessage, string css = "")
        {
            return "<span style='white-space:pre;" + css + "'>" + HttpUtility.HtmlEncode(strLoggingMessage) + "</span>";
        }

        /// <summary>
        /// Allow HTML content
        /// </summary>
        /// <param name="messageBody"></param>
        protected override void SendEmail(string messageBody)
        {
            // .NET 2.0 has a new API for SMTP email System.Net.Mail
            // This API supports credentials and multiple hosts correctly.
            // The old API is deprecated.

            // Create and configure the smtp client
            SmtpClient smtpClient = new SmtpClient();
            if (!String.IsNullOrEmpty(SmtpHost))
            {
                smtpClient.Host = SmtpHost;
            }
            smtpClient.Port = Port;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.EnableSsl = EnableSsl;

            if (Authentication == SmtpAuthentication.Basic)
            {
                // Perform basic authentication
                smtpClient.Credentials = new System.Net.NetworkCredential(Username, Password);
            }
            else if (Authentication == SmtpAuthentication.Ntlm)
            {
                // Perform integrated authentication (NTLM)
                smtpClient.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
            }

            using (MailMessage mailMessage = new MailMessage())
            {
                mailMessage.Body = messageBody;
                mailMessage.IsBodyHtml = true;
                mailMessage.BodyEncoding = BodyEncoding;
                mailMessage.From = new MailAddress(From);
                mailMessage.To.Add(To);
                if (!String.IsNullOrEmpty(Cc))
                {
                    mailMessage.CC.Add(Cc);
                }
                if (!String.IsNullOrEmpty(Bcc))
                {
                    mailMessage.Bcc.Add(Bcc);
                }
                if (!String.IsNullOrEmpty(ReplyTo))
                {
                    // .NET 4.0 warning CS0618: 'System.Net.Mail.MailMessage.ReplyTo' is obsolete:
                    // 'ReplyTo is obsoleted for this type.  Please use ReplyToList instead which can accept multiple addresses. http://go.microsoft.com/fwlink/?linkid=14202'
#if !FRAMEWORK_4_0_OR_ABOVE
                    mailMessage.ReplyTo = new MailAddress(ReplyTo);
#else
                    mailMessage.ReplyToList.Add(new MailAddress(ReplyTo));
#endif
                }
                mailMessage.Subject = Subject;
                mailMessage.SubjectEncoding = SubjectEncoding;
                mailMessage.Priority = Priority;

                // TODO: Consider using SendAsync to send the message without blocking. This would be a change in
                // behaviour compared to .NET 1.x. We would need a SendCompletedCallback to log errors.
                smtpClient.Send(mailMessage);
            }
        }

        /// <summary>
        /// Add a mapping of level to color - done by the config file
        /// </summary>
        /// <param name="mapping">The mapping to add</param>
        /// <remarks>
        /// <para>
        /// Add a <see cref="LevelColors"/> mapping to this appender.
        /// Each mapping defines the foreground and background colors
        /// for a level.
        /// </para>
        /// </remarks>
        public void AddMapping(LevelColors mapping)
        {
            m_levelMapping.Add(mapping);
        }

        /// <summary>
        /// Initialize the options for this appender
        /// </summary>
        /// <remarks>
        /// <para>
        /// Initialize the level to color mappings set on this appender.
        /// </para>
        /// </remarks>
        public override void ActivateOptions()
        {
            base.ActivateOptions();
            m_levelMapping.ActivateOptions();
        }

        /// <summary>
        /// Copy from ManagedColoredConsoleAppender, add Style propertity
        /// </summary>
        public class LevelColors : LevelMappingEntry
        {
            /// <summary>
            /// The mapped foreground color for the specified level
            /// </summary>
            /// <remarks>
            /// <para>
            /// Required property.
            /// The mapped foreground color for the specified level.
            /// </para>
            /// </remarks>
            public string ForeColor
            {
                get { return (this.foreColor); }
                // Keep a flag that the color has been set
                // and is no longer the default.
                set
                {
                    this.foreColor = value;
                    this.hasForeColor = true;
                }
            }

            private string foreColor;
            private bool hasForeColor;

            internal bool HasForeColor
            {
                get
                {
                    return hasForeColor;
                }
            }
            /// <summary>
            /// CSS style
            /// </summary>
            public string Style
            { get; set; }

            /// <summary>
            /// The mapped background color for the specified level
            /// </summary>
            /// <remarks>
            /// <para>
            /// Required property.
            /// The mapped background color for the specified level.
            /// </para>
            /// </remarks>
            public string BackColor
            {
                get { return (this.backColor); }
                // Keep a flag that the color has been set
                // and is no longer the default.
                set
                {
                    this.backColor = value;
                    this.hasBackColor = true;
                }
            }

            private string backColor;
            private bool hasBackColor;

            internal bool HasBackColor
            {
                get
                {
                    return hasBackColor;
                }
            }
        }
    }
}
