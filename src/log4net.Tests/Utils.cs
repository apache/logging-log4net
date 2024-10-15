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
using NUnit.Framework;

namespace log4net.Tests;

/// <summary>
/// Utilities for testing
/// </summary>
public static class Utils
{
  /// <summary>
  /// Is the mono runtime used
  /// </summary>
  internal static bool IsMono { get; } = Type.GetType("Mono.Runtime") is not null;

  /// <summary>
  /// Skips the current test when run under mono
  /// </summary>
  internal static void InconclusiveOnMono()
  {
    if (IsMono)
    {
      Assert.Inconclusive("mono has a different behaviour");
    }
  }

  /// <summary>
  /// Sample property key
  /// </summary>
  internal const string PROPERTY_KEY = "prop1";

  /// <summary>
  /// Remove the <see cref="PROPERTY_KEY"/> from alle log4net contexts
  /// </summary>
  internal static void RemovePropertyFromAllContexts()
  {
    GlobalContext.Properties.Remove(PROPERTY_KEY);
    ThreadContext.Properties.Remove(PROPERTY_KEY);
    LogicalThreadContext.Properties.Remove(PROPERTY_KEY);
  }
}
