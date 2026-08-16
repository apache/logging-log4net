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

namespace log4net.Tests.Aot;

/// <summary>
/// One named piece of log4net surface to exercise.
/// </summary>
/// <param name="Area">the grouping the probe belongs to</param>
/// <param name="Name">what the probe exercises</param>
/// <param name="Run">the probe itself, which throws to signal failure</param>
internal sealed record Probe(string Area, string Name, Action Run)
{
  /// <summary>
  /// Identifies the probe in the expected failure lists.
  /// </summary>
  internal string Key => $"{Area}/{Name}";
}
