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
using System.Net;
using MailKit.Security;
using MimeKit;

namespace log4net.Ext.Mail.Appender;

/// <summary>
/// The slice of the MailKit SMTP client API used by <see cref="SmtpAppender"/>.
/// </summary>
/// <remarks>
/// <para>
/// This interface exists so that <see cref="SmtpAppender"/> can be unit tested without
/// talking to a real SMTP server. <see cref="MailKitSmtpTransport"/> is the production
/// implementation and simply forwards to <see cref="MailKit.Net.Smtp.SmtpClient"/>.
/// </para>
/// <para>
/// The members mirror their MailKit counterparts, so an implementation that wraps
/// <see cref="MailKit.Net.Smtp.SmtpClient"/> needs no translation logic.
/// </para>
/// </remarks>
public interface ISmtpTransport : IDisposable
{
  /// <summary>
  /// Gets a value indicating whether the transport is connected to a server.
  /// </summary>
  bool IsConnected { get; }

  /// <summary>
  /// Gets a value indicating whether the transport has been authenticated.
  /// </summary>
  bool IsAuthenticated { get; }

  /// <summary>
  /// Connects to the SMTP server at <paramref name="host"/> and <paramref name="port"/>.
  /// </summary>
  /// <param name="host">The name or address of the SMTP server.</param>
  /// <param name="port">The port the SMTP server is listening on.</param>
  /// <param name="secureSocketOptions">The transport security to use.</param>
  void Connect(string host, int port, SecureSocketOptions secureSocketOptions);

  /// <summary>
  /// Authenticates using the supplied <paramref name="credentials"/> and whichever
  /// SASL mechanism the server and client agree on.
  /// </summary>
  /// <param name="credentials">The credentials to authenticate with.</param>
  void Authenticate(ICredentials credentials);

  /// <summary>
  /// Authenticates using an explicit SASL <paramref name="mechanism"/>.
  /// </summary>
  /// <param name="mechanism">The SASL mechanism to authenticate with.</param>
  void Authenticate(SaslMechanism mechanism);

  /// <summary>
  /// Sends the specified <paramref name="message"/>.
  /// </summary>
  /// <param name="message">The message to send.</param>
  void Send(MimeMessage message);

  /// <summary>
  /// Disconnects from the SMTP server.
  /// </summary>
  /// <param name="quit">
  /// <see langword="true"/> to send the <c>QUIT</c> command before disconnecting.
  /// </param>
  void Disconnect(bool quit);
}
