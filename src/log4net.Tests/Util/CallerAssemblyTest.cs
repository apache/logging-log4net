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

using System.Reflection;
using System.Runtime.CompilerServices;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Util;

/// <summary>
/// Tests for <see cref="CallerAssembly"/>, the guard that keeps the
/// <see cref="Assembly.GetCallingAssembly()"/> based overloads usable under Native AOT.
/// </summary>
/// <remarks>
/// <para>
/// The AOT half of the behaviour cannot be covered here - these tests always run on a JIT
/// runtime, where <see cref="CallerAssembly.IsSupported"/> is <see langword="true"/> and
/// <see cref="CallerAssembly.Fallback"/> is never consulted. What they do cover is that the
/// guard stays inert on a JIT runtime, so that no call site silently starts attributing
/// loggers to the entry assembly instead of the caller.
/// </para>
/// </remarks>
[TestFixture]
public class CallerAssemblyTest
{
  /// <summary>
  /// The probe recognises a runtime that does implement
  /// <see cref="Assembly.GetCallingAssembly()"/>, so the guard stays out of the way everywhere
  /// except Native AOT. A false negative here would silently move every logger to the entry
  /// assembly's repository.
  /// </summary>
  [Test]
  public void IsSupportedOnAJitRuntime() => Assert.That(CallerAssembly.IsSupported, Is.True);

  /// <summary>
  /// There is always a replacement assembly to attribute a call to, even though the entry
  /// assembly is <see langword="null"/> in a host without a managed entry point.
  /// </summary>
  [Test]
  public void FallbackIsAvailable() => Assert.That(CallerAssembly.Fallback, Is.Not.Null);

  /// <summary>
  /// The guard has to leave <see cref="Assembly.GetCallingAssembly()"/> in the method whose
  /// caller is wanted, so a call from this assembly still resolves to this assembly.
  /// </summary>
  [Test]
  public void GuardedCallStillReportsTheCallersAssembly()
    => Assert.That(GuardedCallingAssembly(), Is.SameAs(typeof(CallerAssemblyTest).Assembly));

  /// <summary>
  /// Stands in for a public log4net entry point. Inlining is suppressed because it would
  /// hand <see cref="Assembly.GetCallingAssembly()"/> a different frame - the same effect
  /// that makes the release build of <see cref="SystemInfoTest"/> unable to assert on an
  /// exact assembly.
  /// </summary>
  [MethodImpl(MethodImplOptions.NoInlining)]
  private static Assembly GuardedCallingAssembly()
    => CallerAssembly.IsSupported ? Assembly.GetCallingAssembly() : CallerAssembly.Fallback;
}
