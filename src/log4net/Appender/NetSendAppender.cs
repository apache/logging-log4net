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
using System.Runtime.InteropServices;

using log4net.Util;
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
  ///             <see cref="NetSendAppender.Server"/> = &lt;name of the local machine&gt;
  ///             </para>
  ///             <para>
  ///             <see cref="NetSendAppender.Recipient"/> = &lt;user name&gt;
  ///             </para>
  ///         </description>
  ///     </item>
  ///     <item>
  ///         <term>Send a message to a user account on a remote machine</term>
  ///         <description>
  ///             <para>
  ///             <see cref="NetSendAppender.Server"/> = &lt;name of the remote machine&gt;
  ///             </para>
  ///             <para>
  ///             <see cref="NetSendAppender.Recipient"/> = &lt;user name&gt;
  ///             </para>
  ///         </description>
  ///     </item>
  ///     <item>
  ///         <term>Send a message to a domain user account</term>
  ///         <description>
  ///             <para>
  ///             <see cref="NetSendAppender.Server"/> = &lt;name of a domain controller | uninitialized&gt;
  ///             </para>
  ///             <para>
  ///             <see cref="NetSendAppender.Recipient"/> = &lt;user name&gt;
  ///             </para>
  ///         </description>
  ///     </item>
  ///     <item>
  ///         <term>Send a message to all the names in a workgroup or domain</term>
  ///         <description>
  ///             <para>
  ///             <see cref="NetSendAppender.Recipient"/> = &lt;workgroup name | domain name&gt;*
  ///             </para>
  ///         </description>
  ///     </item>
  ///     <item>
  ///         <term>Send a message from the local machine to a remote machine</term>
  ///         <description>
  ///             <para>
  ///             <see cref="NetSendAppender.Server"/> = &lt;name of the local machine | uninitialized&gt;
  ///             </para>
  ///             <para>
  ///             <see cref="NetSendAppender.Recipient"/> = &lt;name of the remote machine&gt;
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
  /// <code lang="XML" escaped="true">
  /// <appender name="NetSendAppender_Operator" type="log4net.Appender.NetSendAppender">
  ///     <server value="LOCAL_PC" />
  ///     <recipient value="OPERATOR_PC" />
  ///     <layout type="log4net.Layout.PatternLayout" value="%-5p %c [%x] - %m%n" />
  /// </appender>
  /// </code>
  /// </example>
  /// <author>Nicko Cadell</author>
  /// <author>Gert Driesen</author>
  public class NetSendAppender : AppenderSkeleton
  {
    /// <summary>
    /// Initializes the appender.
    /// </summary>
    /// <remarks>
    /// The default constructor initializes all fields to their default values.
    /// </remarks>
    public NetSendAppender()
    {
    }

    /// <summary>
    /// Gets or sets the sender of the message.
    /// </summary>
    /// <value>
    /// The sender of the message.
    /// </value>
    /// <remarks>
    /// If this property is not specified, the message is sent from the local computer.
    /// </remarks>
    public string? Sender { get; set; }

    /// <summary>
    /// Gets or sets the message alias to which the message should be sent.
    /// </summary>
    /// <value>
    /// The recipient of the message.
    /// </value>
    /// <remarks>
    /// This property should always be specified in order to send a message.
    /// </remarks>
    public string? Recipient { get; set; }

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
    public string? Server { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="SecurityContext"/> used to call the NetSend method.
    /// </summary>
    /// <value>
    /// The <see cref="SecurityContext"/> used to call the NetSend method.
    /// </value>
    /// <remarks>
    /// <para>
    /// Unless a <see cref="SecurityContext"/> specified here for this appender
    /// the <see cref="SecurityContextProvider.DefaultProvider"/> is queried for the
    /// security context to use. The default behavior is to use the security context
    /// of the current thread.
    /// </para>
    /// </remarks>
    public SecurityContext? SecurityContext { get; set; }

    /// <summary>
    /// Initialize the appender based on the options set.
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
    /// <para>
    /// The appender will be ignored if no <see cref="Recipient" /> was specified.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">The required property <see cref="Recipient" /> was not specified.</exception>
    public override void ActivateOptions()
    {
      base.ActivateOptions();

      if (Recipient is null)
      {
        throw new ArgumentNullException(nameof(Recipient), "The required property 'Recipient' was not specified.");
      }

      SecurityContext ??= SecurityContextProvider.DefaultProvider.CreateSecurityContext(this);
    }

    /// <summary>
    /// This method is called by the <see cref="M:AppenderSkeleton.DoAppend(LoggingEvent)"/> method.
    /// </summary>
    /// <param name="loggingEvent">The event to log.</param>
    /// <remarks>
    /// <para>
    /// Sends the event using a network message.
    /// </para>
    /// </remarks>
    [System.Security.SecuritySafeCritical]
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, UnmanagedCode = true)]
    protected override void Append(LoggingEvent loggingEvent)
    {
      NativeError? nativeError = null;

      // Render the event in the callers security context
      string renderedLoggingEvent = RenderLoggingEvent(loggingEvent);

      using (SecurityContext?.Impersonate(this))
      {
        // Send the message
        int returnValue = NetMessageBufferSend(Server, Recipient!, Sender, renderedLoggingEvent, renderedLoggingEvent.Length * Marshal.SystemDefaultCharSize);

        // Log the error if the message could not be sent
        if (returnValue != 0)
        {
          // Lookup the native error
          nativeError = NativeError.GetError(returnValue);
        }
      }

      if (nativeError is not null)
      {
        // Handle the error over to the ErrorHandler
        ErrorHandler.Error($"{nativeError} (Params: Server={Server}, Recipient={Recipient}, Sender={Sender})");
      }
    }

    /// <summary>
    /// This appender requires a <see cref="Layout"/> to be set.
    /// </summary>
    protected override bool RequiresLayout => true;

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
    /// The following restrictions apply for sending network messages:
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
    [DllImport("netapi32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    protected static extern int NetMessageBufferSend(
      [MarshalAs(UnmanagedType.LPWStr)] string? serverName,
      [MarshalAs(UnmanagedType.LPWStr)] string msgName,
      [MarshalAs(UnmanagedType.LPWStr)] string? fromName,
      [MarshalAs(UnmanagedType.LPWStr)] string buffer,
      int bufferSize);
  }
}
