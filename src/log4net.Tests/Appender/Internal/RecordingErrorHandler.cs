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

using log4net.Core;

namespace log4net.Tests.Appender.Internal;

/// <summary>
/// Collects what an appender reports instead of letting it reach the console, so a test can
/// assert on it and a provoked error adds no noise to the suite output.
/// </summary>
internal sealed class RecordingErrorHandler : IErrorHandler
{
  /// <summary>Reported messages, in the order they were reported.</summary>
  internal List<string> Messages { get; } = [];

  /// <inheritdoc/>
  public void Error(string message) => Messages.Add(message);

  /// <inheritdoc/>
  public void Error(string message, Exception e) => Messages.Add(message);

  /// <inheritdoc/>
  public void Error(string message, Exception? e, ErrorCode errorCode) => Messages.Add(message);
}
