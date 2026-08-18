/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace log4net.Tests.Appender.AdoNet;

[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Reflection")]
internal sealed class Log4NetConnection : IDbConnection
{
  private bool _open;

  /// <summary>
  /// Initializes a new instance and records it as the <see cref="MostRecentInstance"/>.
  /// </summary>
  public Log4NetConnection() => MostRecentInstance = this;

  /// <inheritdoc/>
  public void Close() => _open = false;

  /// <inheritdoc/>
  public ConnectionState State => _open ? ConnectionState.Open : ConnectionState.Closed;

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
  /// <inheritdoc/>
  public string? ConnectionString { get; set; }
#pragma warning restore CS8766

  /// <inheritdoc/>
  public IDbTransaction BeginTransaction() => new Log4NetTransaction();

  /// <inheritdoc/>
  public IDbCommand CreateCommand() => new Log4NetCommand();

  /// <inheritdoc/>
  public void Open()
  {
    if (FailOnOpen)
    {
      throw new InvalidOperationException("Simulated failure to open the connection");
    }
    _open = true;
  }

  /// <summary>
  /// When set, <see cref="Open"/> throws, simulating a connection that cannot be established.
  /// </summary>
  public static bool FailOnOpen { get; set; }

  /// <summary>
  /// The most recently constructed instance, so that a test can inspect what the appender used.
  /// </summary>
  public static Log4NetConnection? MostRecentInstance { get; private set; }

  /// <inheritdoc/>
  public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();

  /// <inheritdoc/>
  public void ChangeDatabase(string databaseName) => throw new NotImplementedException();

  /// <inheritdoc/>
  public int ConnectionTimeout => throw new NotImplementedException();

  /// <inheritdoc/>
  public string Database => throw new NotImplementedException();

  /// <inheritdoc/>
  public void Dispose() => throw new NotImplementedException();
}
