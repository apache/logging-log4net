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

namespace log4net.Ext.Mail.Appender;

/// <summary>
/// How <see cref="SmtpAppender"/> secures its connection to the SMTP server.
/// </summary>
/// <remarks>
/// <para>
/// This mirrors the transport security modes of the underlying mail library without exposing its
/// types, so that the appender's configuration surface stays CLS compliant.
/// </para>
/// </remarks>
public enum SmtpTransportSecurity
{
  /// <summary>
  /// The connection is not encrypted.
  /// </summary>
  None,

  /// <summary>
  /// Transport security is required, and the mechanism follows the port: implicit TLS on port 465,
  /// <c>STARTTLS</c> on every other port.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Connecting fails when the server does not offer transport security, rather than continuing
  /// unencrypted. This is what <see cref="SmtpAppender.EnableSsl"/> selects and is the right
  /// choice unless the server does something unusual.
  /// </para>
  /// </remarks>
  Required,

  /// <summary>
  /// The session is encrypted before the SMTP greeting, without <c>STARTTLS</c>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Use this for a server that expects implicit TLS on a port other than 465, which
  /// <see cref="Required"/> would try to negotiate with <c>STARTTLS</c> instead.
  /// </para>
  /// </remarks>
  ImplicitTls,

  /// <summary>
  /// <c>STARTTLS</c> is required, whatever the port.
  /// </summary>
  StartTls,

  /// <summary>
  /// <c>STARTTLS</c> is used when the server advertises it, and the connection continues
  /// unencrypted when it does not.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is opportunistic: an attacker who can modify the traffic can remove the server's
  /// <c>STARTTLS</c> advertisement and the session then proceeds in plaintext, exposing the
  /// credentials and the log content. Only choose it for a network where that is acceptable.
  /// </para>
  /// </remarks>
  StartTlsWhenAvailable
}
