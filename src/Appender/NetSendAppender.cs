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
using System.Globalization;
using System.Runtime.InteropServices;

using log4net.Util;
using log4net.Layout;
using log4net.Core;


namespace log4net.Appender 
{
	/// <summary>
	/// Logs entries by sending network messages using the 
	/// <see cref="NetMessageBufferSend" /> native function.
	/// </summary>
	/// <remarks>
	/// <para>
	/// You can send messages only to names that are active 
	/// on the network. If you send the message to a user name, 
	/// that user must be logged on and running the Messenger 
	/// service to receive the message.
	/// </para>
	/// <para>
	/// The receiver will get a top most window displaying the 
	/// messages one at a time, therefore this appender should 
	/// not be used to deliver a high volume of messages.
	/// </para>
	/// <para>
	/// The following table lists some possible uses for this appender :
	/// </para>
	/// <para>
	/// <list type="table">
	///     <listheader>
	///         <term>Action</term>
	///         <description>Property Value(s)</description>
	///     </listheader>
	///     <item>
	///         <term>Send a message to a user account on the local machine</term>
	///         <description>
	///             <para>
	///             <paramref name="Server"/> = &lt;name of the local machine&gt;
	///             </para>
	///             <para>
	///             <paramref name="Recipient"/> = &lt;user name&gt;
	///             </para>
	///         </description>
	///     </item>
	///     <item>
	///         <term>Send a message to a user account on a remote machine</term>
	///         <description>
	///             <para>
	///             <paramref name="Server"/> = &lt;name of the remote machine&gt;
	///             </para>
	///             <para>
	///             <paramref name="Recipient"/> = &lt;user name&gt;
	///             </para>
	///         </description>
	///     </item>
	///     <item>
	///         <term>Send a message to a domain user account</term>
	///         <description>
	///             <para>
	///             <paramref name="Server"/> = &lt;name of a domain controller | uninitialized&gt;
	///             </para>
	///             <para>
	///             <paramref name="Recipient"/> = &lt;user name&gt;
	///             </para>
	///         </description>
	///     </item>
	///     <item>
	///         <term>Send a message to all the names in a workgroup or domain</term>
	///         <description>
	///             <para>
	///             <paramref name="Recipient"/> = &lt;workgroup name | domain name&gt;*
	///             </para>
	///         </description>
	///     </item>
	///     <item>
	///         <term>Send a message from the local machine to a remote machine</term>
	///         <description>
	///             <para>
	///             <paramref name="Server"/> = &lt;name of the local machine | uninitialized&gt;
	///             </para>
	///             <para>
	///             <paramref name="Recipient"/> = &lt;name of the remote machine&gt;
	///             </para>
	///         </description>
	///     </item>
	/// </list>
	/// </para>
	/// <para>
	/// <b>Note :</b> security restrictions apply for sending 
	/// network messages, see <see cref="NetMessageBufferSend" /> 
	/// for more information.
	/// </para>
	/// </remarks>
	/// <example>
	/// <para>
	/// An example configuration section to log information 
	/// using this appender from the local machine, named 
	/// LOCAL_PC, to machine OPERATOR_PC :
	/// </para>
	/// <code>
	/// &lt;appender name="NetSendAppender_Operator" type="log4net.Appender.NetSendAppender, log4net"&gt;
	///     &lt;param name="Server" value="LOCAL_PC" /&gt;
	///     &lt;param name="Recipient" value="OPERATOR_PC" /&gt;
	///     &lt;layout type="log4net.Layout.PatternLayout, log4net"&gt;
	///         &lt;param name="ConversionPattern" value="%-5p %c [%x] - %m%n" /&gt;
	///     &lt;/layout&gt;
	/// &lt;/appender&gt;
	/// </code>
	/// </example>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class NetSendAppender : AppenderSkeleton 
	{
		#region Member Variables

		/// <summary>
		/// The DNS or NetBIOS name of the server on which the function is to execute.
		/// </summary>
		private string m_server;

		/// <summary>
		/// The sender of the network message.
		/// </summary>
		private string m_sender;

		/// <summary>
		/// The message alias to which the message should be sent.
		/// </summary>
		private string m_recipient;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes the appender.
		/// </summary>
		/// <remarks>
		/// The default constructor initializes all fields to their default values.
		/// </remarks>
		public NetSendAppender() 
		{
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the sender of the message.
		/// </summary>
		/// <value>
		/// The sender of the message.
		/// </value>
		/// <remarks>
		/// If this property is not specified, the message is sent from the local computer.
		/// </remarks>
		public string Sender 
		{
			get { return m_sender; }
			set { m_sender = value; }
		}

		/// <summary>
		/// Gets or sets the message alias to which the message should be sent.
		/// </summary>
		/// <value>
		/// The recipient of the message.
		/// </value>
		/// <remarks>
		/// This property should always be specified in order to send a message.
		/// </remarks>
		public string Recipient 
		{
			get { return m_recipient; }
			set { m_recipient = value; }
		}
		
		/// <summary>
		/// Gets or sets the DNS or NetBIOS name of the remote server on which the function is to execute.
		/// </summary>
		/// <value>
		/// DNS or NetBIOS name of the remote server on which the function is to execute.
		/// </value>
		/// <remarks>
		/// <para>
		/// For Windows NT 4.0 and earlier, the string should begin with \\.
		/// </para>
		/// <para>
		/// If this property is not specified, the local computer is used. 
		/// </para>
		/// </remarks>
		public string Server 
		{
			get { return m_server; }
			set { m_server = value; }
		}

		#endregion

		#region Implementation of IOptionHandler

		/// <summary>
		/// Initialise the appender based on the options set.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The appender will be ignored if no <see cref="Recipient" /> was specified.
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">The required property <see cref="Recipient" /> was not specified.</exception>
		public override void ActivateOptions()
		{
			base.ActivateOptions();
	
			if (this.Recipient == null) 
			{
				throw new ArgumentNullException("Recipient", "The required property 'Recipient' was not specified.");
			}
		}

		#endregion

		#region Override implementation of AppenderSkeleton

		/// <summary>
		/// This method is called by the <see cref="AppenderSkeleton.DoAppend"/> method.
		/// </summary>
		/// <param name="loggingEvent">The event to log.</param>
		/// <remarks>
		/// <para>
		/// Sends the event using a network message.
		/// </para>
		/// </remarks>
		protected override void Append(LoggingEvent loggingEvent) 
		{
			string renderedLoggingEvent = RenderLoggingEvent(loggingEvent);

			// Send the message
			int returnValue = NetMessageBufferSend(this.Server, this.Recipient, this.Sender, renderedLoggingEvent, renderedLoggingEvent.Length * Marshal.SystemDefaultCharSize);   

			// Log the error if the message could not be sent
			if (returnValue != 0) 
			{
				// Lookup the native error
				NativeError nativeError = NativeError.GetError(returnValue);

				// Handle the error over to the ErrorHandler
				ErrorHandler.Error(nativeError.ToString() + " (Params: Server=" + this.Server + ", Recipient=" + this.Recipient + ", Sender=" + this.Sender + ")");
			}
		}

		/// <summary>
		/// This appender requires a <see cref="Layout"/> to be set.
		/// </summary>
		/// <value><c>true</c></value>
		override protected bool RequiresLayout
		{
			get { return true; }
		}

		#endregion

		#region Stubs For Native Function Calls

		/// <summary>
		/// Sends a buffer of information to a registered message alias.
		/// </summary>
		/// <param name="serverName">The DNS or NetBIOS name of the server on which the function is to execute.</param>
		/// <param name="msgName">The message alias to which the message buffer should be sent</param>
		/// <param name="fromName">The originator of the message.</param>
		/// <param name="buffer">The message text.</param>
		/// <param name="bufferSize">The length, in bytes, of the message text.</param>
		/// <remarks>
		/// <para>
		/// The following restrictions apply for sending network messages :
		/// </para>
		/// <para>
		/// <list type="table">
		///     <listheader>
		///         <term>Platform</term>
		///         <description>Requirements</description>
		///     </listheader>
		///     <item>
		///         <term>Windows NT</term>
		///         <description>
		///             <para>
		///             No special group membership is required to send a network message.
		///             </para>
		///             <para>
		///             Admin, Accounts, Print, or Server Operator group membership is required to 
		///             successfully send a network message on a remote server.
		///             </para>
		///         </description>
		///     </item>
		///     <item>
		///         <term>Windows 2000 or later</term>
		///         <description>
		///             <para>
		///             If you send a message on a domain controller that is running Active Directory, 
		///             access is allowed or denied based on the access control list (ACL) for the securable 
		///             object. The default ACL permits only Domain Admins and Account Operators to send a network message. 
		///             </para>
		///             <para>
		///             On a member server or workstation, only Administrators and Server Operators can send a network message. 
		///             </para>
		///         </description>
		///     </item>
		/// </list>
		/// </para>
		/// <para>
		/// For more information see <a href="http://msdn.microsoft.com/library/default.asp?url=/library/en-us/netmgmt/netmgmt/security_requirements_for_the_network_management_functions.asp">Security Requirements for the Network Management Functions</a>.
		/// </para>
		/// </remarks>
		/// <returns>
		/// <para>
		/// If the function succeeds, the return value is zero.
		/// </para>
		/// </returns>
		[DllImport("netapi32.dll", SetLastError=true)] 
		protected static extern int NetMessageBufferSend(	   
			[MarshalAs(UnmanagedType.LPWStr)] string serverName,
			[MarshalAs(UnmanagedType.LPWStr)] string msgName,
			[MarshalAs(UnmanagedType.LPWStr)] string fromName,
			[MarshalAs(UnmanagedType.LPWStr)] string buffer,
			int bufferSize);

		#endregion
	}
}