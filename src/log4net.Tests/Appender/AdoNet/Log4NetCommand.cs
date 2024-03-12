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

#nullable enable

namespace log4net.Tests.Appender.AdoNet
{
  public class Log4NetCommand : IDbCommand
  {
    public Log4NetCommand()
    {
      MostRecentInstance = this;

      Parameters = new Log4NetParameterCollection();
    }

    public void Dispose()
    {
      // empty
    }

    public IDbTransaction? Transaction { get; set; }

    public int ExecuteNonQuery()
    {
      ExecuteNonQueryCount++;
      return 0;
    }

    public int ExecuteNonQueryCount { get; private set; }

    public IDbDataParameter CreateParameter()
    {
      return new Log4NetParameter();
    }

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public string? CommandText { get; set; }
#pragma warning restore CS8766

    public CommandType CommandType { get; set; }

    public void Prepare()
    {
      // empty
    }

    public IDataParameterCollection Parameters { get; }

    public static Log4NetCommand? MostRecentInstance { get; private set; }

    public void Cancel()
    {
      throw new NotImplementedException();
    }

    public IDataReader ExecuteReader()
    {
      throw new NotImplementedException();
    }

    public IDataReader ExecuteReader(CommandBehavior behavior)
    {
      throw new NotImplementedException();
    }

    public object ExecuteScalar()
    {
      throw new NotImplementedException();
    }

    public IDbConnection? Connection
    {
      get => throw new NotImplementedException();
      set => throw new NotImplementedException();
    }

    public int CommandTimeout
    {
      get => throw new NotImplementedException();
      set => throw new NotImplementedException();
    }

    public UpdateRowSource UpdatedRowSource
    {
      get => throw new NotImplementedException();
      set => throw new NotImplementedException();
    }
  }
}
