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
using System.Reflection;

using log4net.Util;

namespace log4net.Tests.Aot;

/// <summary>
/// Records what log4net can and cannot do when an application is published with Native AOT.
/// </summary>
/// <remarks>
/// <para>
/// The build runs this assembly twice, JIT compiled and published with <c>PublishAot</c>, so a
/// difference between the runs is caused by AOT rather than by the platform. The expected
/// differences are listed in <see cref="Probes"/>.
/// </para>
/// </remarks>
internal static class Program
{
  /// <summary>
  /// The environment variable the settings probe expects, set by the build before running.
  /// </summary>
  internal const string EnvironmentProbeKey = "log4net.AotEnvironmentProbe";

  private static int Main()
  {
    // The probes report their own failures, so log4net's internal error reporting is only noise
    // here, and a passing run that prints errors reads as a broken one.
    LogLog.EmitInternalMessages = false;

    bool isAot = !IsGetCallingAssemblySupported();
    string mode = isAot ? "AOT" : "JIT";
    Dictionary<string, string> expectedFailures
      = isAot ? Probes.ExpectedAotFailures : Probes.ExpectedJitFailures;

    Console.WriteLine($"log4net probes, {mode}");
    Console.WriteLine(new string('-', 100));

    List<string> regressions = [];
    List<string> unexpectedPasses = [];

    foreach (Probe probe in Probes.All())
    {
      string? failure = Run(probe);
      bool expectedToFail = expectedFailures.ContainsKey(probe.Key);

      if (failure is null)
      {
        Console.WriteLine($"  {"ok",-6} {probe.Key}");
        if (expectedToFail)
        {
          unexpectedPasses.Add(probe.Key);
        }
      }
      else
      {
        Console.WriteLine($"  {"FAIL",-6} {probe.Key}");
        Console.WriteLine($"         {failure}");
        if (expectedToFail)
        {
          Console.WriteLine($"         expected under {mode}: {expectedFailures[probe.Key]}");
        }
        else
        {
          regressions.Add($"{probe.Key}: {failure}");
        }
      }
    }

    Console.WriteLine(new string('-', 100));
    foreach (string regression in regressions)
    {
      Console.WriteLine($"REGRESSION  {regression}");
    }
    foreach (string unexpectedPass in unexpectedPasses)
    {
      Console.WriteLine($"NOW WORKING {unexpectedPass} is listed as an expected {mode} failure but passed. "
        + "Remove it from the list and update the Native AOT page in the manual.");
    }

    int problems = regressions.Count + unexpectedPasses.Count;
    Console.WriteLine(problems == 0
      ? $"{mode}: matches expectations"
      : $"{mode}: {problems} probe(s) did not match expectations");
    return problems == 0 ? 0 : 1;
  }

  /// <summary>
  /// Whether this process is running Native AOT.
  /// </summary>
  /// <remarks>
  /// <para>
  /// RuntimeFeature.IsDynamicCodeSupported cannot be used: setting
  /// <c>PublishAot</c> turns that switch off for an ordinary run of the same project too, so it
  /// reports AOT either way. <see cref="Assembly.GetCallingAssembly"/> is only unsupported when the
  /// application really was compiled ahead of time, which is also the capability log4net keys off.
  /// </para>
  /// </remarks>
  private static bool IsGetCallingAssemblySupported()
  {
    try
    {
      _ = Assembly.GetCallingAssembly();
      return true;
    }
    catch (PlatformNotSupportedException)
    {
      return false;
    }
  }

  private static string? Run(Probe probe)
  {
    try
    {
      probe.Run();
      return null;
    }
    catch (Exception e) when (e is not (OutOfMemoryException or StackOverflowException))
    {
      // A probe reports whatever it failed with, because letting the exception escape would lose
      // every later probe. The filter mirrors log4net's own Log4NetAssert.IsFatal, which is
      // internal and not worth linking in for two type checks.
      return $"{e.GetType().Name}: {e.Message.Split('\n')[0]}";
    }
  }
}
