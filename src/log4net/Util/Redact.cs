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
using System.Collections.Generic;
using System.Data.Common;

namespace log4net.Util;

/// <summary>
/// Keeps secrets out of the messages log4net writes about its own configuration.
/// </summary>
internal static class Redact
{
  /// <summary>
  /// Reduces a connection string to the keywords that identify the server.
  /// </summary>
  /// <param name="connectionString">The connection string to redact.</param>
  /// <returns>The other values replaced, or <see cref="RedactedValue"/> if it could not be parsed.</returns>
  /// <remarks>
  /// <para>
  /// An allowlist, because hiding known secret keywords misses `Extended Properties`: it nests a
  /// whole connection string that the parser returns as one opaque value.
  /// </para>
  /// </remarks>
  internal static string ConnectionString(string connectionString)
  {
    if (string.IsNullOrEmpty(connectionString))
    {
      return connectionString;
    }

    try
    {
      DbConnectionStringBuilder builder = new() { ConnectionString = connectionString };

      List<string> keys = [];
      foreach (string key in builder.Keys)
      {
        keys.Add(key);
      }

      foreach (string key in keys)
      {
        if (!DiagnosticKeywords.Contains(key))
        {
          builder[key] = RedactedValue;
        }
      }

      return builder.ConnectionString;
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // The connection string could not be parsed - which is likely, given that it just failed
      // to connect - so redact all of it rather than risk echoing a password.
      LogLog.Debug(_declaringType, "Could not parse the connection string in order to redact it", e);
      return RedactedValue;
    }
  }

  /// <summary>
  /// Hides a configured value whose parameter name says it is a secret.
  /// </summary>
  /// <param name="name">The configured parameter name.</param>
  /// <param name="value">The value about to be logged.</param>
  /// <returns>The value, <see cref="RedactedValue"/>, or a redacted connection string.</returns>
  internal static object? Value(string name, object? value)
  {
    if (value is null)
    {
      return null;
    }

    // EndsWith, not Contains: ConnectionType and ConnectionStringName are diagnostics, not secrets.
    if (name.EndsWith("connectionstring", StringComparison.OrdinalIgnoreCase))
    {
      return ConnectionString(value.ToString() ?? string.Empty);
    }

    foreach (string secret in SecretNames)
    {
      if (name.IndexOf(secret, StringComparison.OrdinalIgnoreCase) >= 0)
      {
        return RedactedValue;
      }
    }

    return value;
  }

  /// <summary>
  /// Keywords whose values are kept: they name the server and account, not the credentials.
  /// </summary>
  private static readonly HashSet<string> DiagnosticKeywords = new(StringComparer.OrdinalIgnoreCase)
  {
    "provider", "driver", "data source", "server", "address", "addr", "network address",
    "initial catalog", "database", "port", "user id", "uid", "user", "username",
    "integrated security", "trusted_connection", "encrypt", "timeout", "connect timeout",
    "connection timeout", "application name", "workstation id", "pooling",
  };

  /// <summary>
  /// Parameter name fragments that mark a value as a secret.
  /// </summary>
  private static readonly string[] SecretNames =
    ["password", "pwd", "passphrase", "secret", "token", "credential", "apikey"];

  /// <summary>
  /// Stands in for a value withheld from a diagnostic message.
  /// </summary>
  internal const string RedactedValue = "*****";

  private static readonly Type _declaringType = typeof(Redact);
}
