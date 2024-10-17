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

public class Log4NetParameter : IDbDataParameter
{
#pragma warning disable CS8766  // nullability difference from interface - seems to vary by framework.
  public string? ParameterName { get; set; } = string.Empty;
#pragma warning restore CS8766

  public byte Precision { get; set; }

  public byte Scale { get; set; }

  public int Size { get; set; }

  public DbType DbType { get; set; }

  public object? Value { get; set; }

  public ParameterDirection Direction
  {
    get => throw new NotImplementedException();
    set => throw new NotImplementedException();
  }

  public bool IsNullable => throw new NotImplementedException();

#pragma warning disable CS8767  // nullability difference from interface - seems to vary by framework.
  public string SourceColumn
  {
    get => throw new NotImplementedException();
    set => throw new NotImplementedException();
  }
#pragma warning restore CS8766

  public DataRowVersion SourceVersion
  {
    get => throw new NotImplementedException();
    set => throw new NotImplementedException();
  }
}
