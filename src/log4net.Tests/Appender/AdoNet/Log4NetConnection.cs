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

namespace log4net.Tests.Appender.AdoNet;

internal sealed class Log4NetConnection : IDbConnection
{
  private bool _open;

  public Log4NetConnection() => MostRecentInstance = this;

  public void Close() => _open = false;

  public ConnectionState State => _open ? ConnectionState.Open : ConnectionState.Closed;

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
  public string? ConnectionString { get; set; }
#pragma warning restore CS8766

  public IDbTransaction BeginTransaction() => new Log4NetTransaction();

  public IDbCommand CreateCommand() => new Log4NetCommand();

  public void Open() => _open = true;

  public static Log4NetConnection? MostRecentInstance { get; private set; }

  public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();

  public void ChangeDatabase(string databaseName) => throw new NotImplementedException();

  public int ConnectionTimeout => throw new NotImplementedException();

  public string Database => throw new NotImplementedException();

  public void Dispose() => throw new NotImplementedException();
}
