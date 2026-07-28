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

using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace log4net.Ext.Mail.Appender;

/// <summary>
/// The default <see cref="ISmtpTransport"/> implementation, backed by
/// <see cref="MailKit.Net.Smtp.SmtpClient"/>.
/// </summary>
public sealed class MailKitSmtpTransport : ISmtpTransport
{
  private readonly SmtpClient _client = new();

  /// <inheritdoc/>
  public bool IsConnected => _client.IsConnected;

  /// <inheritdoc/>
  public bool IsAuthenticated => _client.IsAuthenticated;

  /// <inheritdoc/>
  public void Connect(string host, int port, SecureSocketOptions secureSocketOptions)
    => _client.Connect(host, port, secureSocketOptions);

  /// <inheritdoc/>
  public void Authenticate(ICredentials credentials) => _client.Authenticate(credentials);

  /// <inheritdoc/>
  public void Authenticate(SaslMechanism mechanism) => _client.Authenticate(mechanism);

  /// <inheritdoc/>
  public void Send(MimeMessage message) => _client.Send(message);

  /// <inheritdoc/>
  public void Disconnect(bool quit) => _client.Disconnect(quit);

  /// <inheritdoc/>
  public void Dispose() => _client.Dispose();
}
