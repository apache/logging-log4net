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

namespace log4net.Util;

/// <summary>
/// A class to hold the key and data for a property set in the config file
/// </summary>
public class PropertyEntry
{
  /// <summary>
  /// Property Key
  /// </summary>
  public string? Key { get; set; }

  /// <summary>
  /// Property Value
  /// </summary>
  public object? Value { get; set; }

  /// <summary>
  /// Override <c>Object.ToString</c> to return sensible debug info
  /// </summary>
  /// <returns>string info about this object</returns>
  public override string ToString() => $"PropertyEntry(Key={Key}, Value={Value})";
}
