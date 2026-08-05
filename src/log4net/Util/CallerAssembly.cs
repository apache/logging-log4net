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
using System.Reflection;

namespace log4net.Util;

/// <summary>
/// Support for the <see cref="Assembly.GetCallingAssembly()"/> calls that select the
/// <see cref="Repository.ILoggerRepository"/> of the caller.
/// </summary>
/// <remarks>
/// <para>
/// Native AOT does not implement <see cref="Assembly.GetCallingAssembly()"/> - it throws
/// <see cref="PlatformNotSupportedException"/> unconditionally, because the stack frames it
/// would have to walk no longer exist after compilation. Callers therefore have to test
/// <see cref="IsSupported"/> and use <see cref="Fallback"/> instead.
/// </para>
/// <para>
/// The test cannot be hidden behind a helper that calls <see cref="Assembly.GetCallingAssembly()"/>
/// itself: the calling assembly of such a helper is log4net, not the assembly that called log4net.
/// <see cref="Assembly.GetCallingAssembly()"/> has to stay in the public method whose caller is
/// wanted, so this class only supplies the flag and the replacement value.
/// </para>
/// </remarks>
internal static class CallerAssembly
{
  /// <summary>
  /// Whether <see cref="Assembly.GetCallingAssembly()"/> works on the current runtime.
  /// </summary>
  internal static bool IsSupported { get; } = Probe();

  /// <summary>
  /// The assembly to attribute a call to when <see cref="IsSupported"/> is <see langword="false"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The entry assembly is the closest available stand-in: an application published with
  /// Native AOT is self-contained, so its loggers would almost always have ended up in the
  /// entry assembly's repository anyway. Hosts without a managed entry point fall back to
  /// log4net itself, which yields the default repository.
  /// </para>
  /// </remarks>
  internal static Assembly Fallback { get; } = Assembly.GetEntryAssembly() ?? typeof(CallerAssembly).Assembly;

  private static bool Probe()
  {
    try
    {
      return Assembly.GetCallingAssembly() is not null;
    }
    catch (PlatformNotSupportedException)
    {
      return false;
    }
  }
}
