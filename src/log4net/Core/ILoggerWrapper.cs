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

namespace log4net.Core;

/// <summary>
/// Base interface for all wrappers
/// </summary>
/// <remarks>
/// <para>
/// Base interface for all wrappers.
/// </para>
/// <para>
/// All wrappers must implement this interface.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
public interface ILoggerWrapper
{
  /// <summary>
  /// Gets the <see cref="ILogger"/> object that implements this object.
  /// </summary>
  /// <value>
  /// </value>
  /// <remarks>
  /// <para>
  /// The <c>Logger</c> object may not  be the same object as this object because of logger decorators.
  /// This gets the actual underlying objects that is used to process the log events.
  /// </para>
  /// </remarks>
  ILogger Logger { get; }
}
