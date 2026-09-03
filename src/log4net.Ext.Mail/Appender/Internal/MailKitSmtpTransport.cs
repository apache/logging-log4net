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
using System.Threading;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace log4net.Ext.Mail.Appender.Internal;

/// <summary>
/// The default <see cref="ISmtpTransport"/> implementation, backed by
/// <see cref="MailKit.Net.Smtp.SmtpClient"/>.
/// </summary>
internal sealed class MailKitSmtpTransport : ISmtpTransport
{
  private readonly SmtpClient _client = new();

  /// <inheritdoc/>
  public int Timeout
  {
    get => _client.Timeout;
    set => _client.Timeout = value;
  }

  /// <inheritdoc/>
  public bool IsConnected => _client.IsConnected;

  /// <inheritdoc/>
  public bool IsAuthenticated => _client.IsAuthenticated;

  /// <inheritdoc/>
  public void Connect(string host, int port, SecureSocketOptions secureSocketOptions,
    CancellationToken cancellationToken)
    => _client.Connect(host, port, secureSocketOptions, cancellationToken);

  /// <inheritdoc/>
  public void Authenticate(ICredentials credentials, CancellationToken cancellationToken)
    => _client.Authenticate(credentials, cancellationToken);

  /// <inheritdoc/>
  public void Authenticate(SaslMechanism mechanism, CancellationToken cancellationToken)
    => _client.Authenticate(mechanism, cancellationToken);

  /// <inheritdoc/>
  public void Send(MimeMessage message, CancellationToken cancellationToken)
    => _client.Send(message, cancellationToken);

  /// <inheritdoc/>
  public void Disconnect(bool quit) => _client.Disconnect(quit);

  /// <inheritdoc/>
  public void Dispose() => _client.Dispose();
}
