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

using log4net.Appender;
using log4net.Core;

namespace log4net.Tests.Appender;

/// <summary>
/// Implements an Appender for test purposes that counts the
/// number of output calls to <see cref="Append" />.
/// </summary>
/// <remarks>
/// This appender is used in the unit tests.
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
public class CountingAppender : AppenderSkeleton
{
  /// <summary>
  /// Returns the number of times <see cref="Append" /> has been called.
  /// </summary>
  /// <value>
  /// The number of times <see cref="Append" /> has been called.
  /// </value>
  public int Counter { get; private set; }

  /// <summary>
  /// Reset the counter to zero
  /// </summary>
  public void ResetCounter()
  {
    Counter = 0;
  }

  /// <summary>
  /// Registers how many times the method has been called.
  /// </summary>
  /// <param name="logEvent">The logging event.</param>
  protected override void Append(LoggingEvent logEvent)
  {
    Counter++;
  }
}
