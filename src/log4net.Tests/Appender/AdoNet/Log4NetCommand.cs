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
using System.Collections.Generic;
using System.Data;

namespace log4net.Tests.Appender.AdoNet;

internal sealed class Log4NetCommand : IDbCommand
{
  /// <summary>
  /// Initializes a new instance and records it as the <see cref="MostRecentInstance"/>.
  /// </summary>
  public Log4NetCommand()
  {
    MostRecentInstance = this;

    Parameters = new Log4NetParameterCollection();
  }

  /// <inheritdoc/>
  public void Dispose()
  {
    // empty
  }

  /// <inheritdoc/>
  public IDbTransaction? Transaction { get; set; }

  /// <inheritdoc/>
  public int ExecuteNonQuery()
  {
    string? payload = null;
    foreach (object? parameter in Parameters)
    {
      if (parameter is IDataParameter { Value: string value })
      {
        payload = value;
        break;
      }
    }
    payload ??= CommandText;

    if (ExceptionTrigger is not null
        && payload?.IndexOf(ExceptionTrigger, StringComparison.Ordinal) >= 0)
    {
      throw new InvalidOperationException($"Simulated database rejection of [{payload}]");
    }

    ExecuteNonQueryCount++;
    if (payload is not null)
    {
      ExecutedPayloads.Add(payload);
    }
    return 0;
  }

  /// <summary>
  /// The number of successful <see cref="ExecuteNonQuery"/> calls on this instance.
  /// </summary>
  public int ExecuteNonQueryCount { get; private set; }

  /// <summary>
  /// When set, <see cref="ExecuteNonQuery"/> throws for every command whose payload
  /// contains this string, simulating a database that rejects specific content.
  /// </summary>
  public static string? ExceptionTrigger { get; set; }

  /// <summary>
  /// The payload - the first string parameter value, or the command text when there are no
  /// parameters - of every successful <see cref="ExecuteNonQuery"/> across all instances.
  /// </summary>
  public static List<string> ExecutedPayloads { get; } = [];

  /// <inheritdoc/>
  public IDbDataParameter CreateParameter() => new Log4NetParameter();

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
  /// <inheritdoc/>
  public string? CommandText { get; set; }
#pragma warning restore CS8766

  /// <inheritdoc/>
  public CommandType CommandType { get; set; }

  /// <inheritdoc/>
  public void Prepare()
  {
    // empty
  }

  /// <inheritdoc/>
  public IDataParameterCollection Parameters { get; }

  /// <summary>
  /// The most recently constructed instance, so that a test can inspect what the appender used.
  /// </summary>
  public static Log4NetCommand? MostRecentInstance { get; private set; }

  /// <inheritdoc/>
  public void Cancel() => throw new NotImplementedException();

  /// <inheritdoc/>
  public IDataReader ExecuteReader() => throw new NotImplementedException();

  /// <inheritdoc/>
  public IDataReader ExecuteReader(CommandBehavior behavior) => throw new NotImplementedException();

  /// <inheritdoc/>
  public object ExecuteScalar() => throw new NotImplementedException();

  /// <inheritdoc/>
  public IDbConnection? Connection
  {
    get => throw new NotImplementedException();
    set => throw new NotImplementedException();
  }

  /// <inheritdoc/>
  public int CommandTimeout
  {
    get => throw new NotImplementedException();
    set => throw new NotImplementedException();
  }

  /// <inheritdoc/>
  public UpdateRowSource UpdatedRowSource
  {
    get => throw new NotImplementedException();
    set => throw new NotImplementedException();
  }
}
