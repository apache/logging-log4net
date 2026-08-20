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
using System.IO;
using System.Reflection;

using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Util;

namespace log4net.Tests.Aot;

/// <summary>
/// Records what log4net does in a process that hosts the runtime natively, where there is no entry
/// assembly for the configuration system to derive the config file path from (issue #162).
/// </summary>
/// <remarks>
/// <para>
/// This cannot be one of <see cref="Probes"/>: the configuration system caches its initialization,
/// and log4net reads its first application setting from a static constructor, so by the time any
/// probe runs the outcome is already decided. The check therefore owns the process from its first
/// statement, which is why the runner invokes it instead of the probe list.
/// </para>
/// <para>
/// Removing the entry assembly is what a native host does to the process, and reflecting onto
/// <c>Assembly.SetEntryAssembly</c> is the only way to arrive there without one. It is a CoreCLR
/// internal, so the check reports itself as not applicable rather than failing where it is absent.
/// </para>
/// <para>
/// The build runs this JIT compiled only. Native AOT trims the configuration system away, so there
/// the first setting read fails as a missing constructor before it can fail for want of an entry
/// assembly: the published executable passes this check even with the fix for #162 reverted, which
/// would be a green that cannot fail for the reason the check exists.
/// </para>
/// </remarks>
internal static class NativeHostCheck
{
  /// <summary>
  /// The argument that selects this check instead of the probe list.
  /// </summary>
  internal const string Argument = "--no-entry-assembly";

  /// <summary>
  /// A setting that only app.config carries, so reading it proves whether the configuration system
  /// was really bypassed rather than answering from a cache.
  /// </summary>
  private const string ConfigOnlyKey = "log4net.AotProbe";

  /// <summary>
  /// Removes the entry assembly, starts log4net, and reports what it wrote while doing so.
  /// </summary>
  /// <returns>0 if log4net started without reporting an error, otherwise 1</returns>
  internal static int Run()
  {
    Console.WriteLine("log4net probes, no entry assembly");
    Console.WriteLine(new string('-', 100));

    if (RemoveTheEntryAssembly() is string unavailable)
    {
      Console.WriteLine($"  {"n/a",-6} settings/no error without an entry assembly");
      Console.WriteLine($"         {unavailable}");
      Console.WriteLine(new string('-', 100));
      Console.WriteLine("no entry assembly: not applicable here");
      return 0;
    }

    // log4net reports an unreadable configuration system on Console.Error, from a static
    // constructor, so the writer has to be in place before anything touches log4net at all.
    TextWriter console = Console.Error;
    using StringWriter emitted = new();
    Console.SetError(emitted);
    List<string> failures = [];
    try
    {
      Environment.SetEnvironmentVariable(Program.EnvironmentProbeKey, "from-environment");
      Check(failures, SystemInfo.GetAppSetting(Program.EnvironmentProbeKey) == "from-environment",
        "an application setting was not read from the environment");
      Check(failures, SystemInfo.GetAppSetting(ConfigOnlyKey) is null,
        $"{ConfigOnlyKey} was answered from app.config, so this check exercised nothing");
      Check(failures, Log() == 1, "the event did not reach the appender");
    }
    catch (Exception e) when (e is not (OutOfMemoryException or StackOverflowException))
    {
      failures.Add($"{e.GetType().Name}: {e.Message}");
    }
    finally
    {
      Console.SetError(console);
    }

    // The symptom of #162 is a stack trace for every setting log4net reads, in an application whose
    // configuration is fine. An absent configuration system is a property of the host, so it
    // belongs at debug level, and nothing here turns internal debugging on.
    string output = emitted.ToString();
    Check(failures, !output.Contains("log4net:ERROR", StringComparison.Ordinal),
      "log4net reported an error while reading its application settings");

    foreach (string failure in failures)
    {
      Console.WriteLine($"  {"FAIL",-6} settings/no error without an entry assembly");
      Console.WriteLine($"         {failure}");
    }
    if (failures.Count == 0)
    {
      Console.WriteLine($"  {"ok",-6} settings/no error without an entry assembly");
    }
    else if (output.Length > 0)
    {
      Console.WriteLine("what log4net wrote:");
      Console.WriteLine(output);
    }

    Console.WriteLine(new string('-', 100));
    Console.WriteLine(failures.Count == 0
      ? "no entry assembly: log4net started without reporting an error"
      : $"no entry assembly: {failures.Count} check(s) did not match expectations");
    return failures.Count == 0 ? 0 : 1;
  }

  /// <summary>
  /// Makes this process look like a natively hosted one.
  /// </summary>
  /// <returns>null once there is no entry assembly, otherwise why that cannot be arranged</returns>
  private static string? RemoveTheEntryAssembly()
  {
    MethodInfo? setEntryAssembly = typeof(Assembly)
      .GetMethod("SetEntryAssembly", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    if (setEntryAssembly is null)
    {
      return "Assembly.SetEntryAssembly is not available, so the entry assembly cannot be removed";
    }
    try
    {
      setEntryAssembly.Invoke(null, [null]);
    }
    catch (Exception e) when (e is not (OutOfMemoryException or StackOverflowException))
    {
      return $"Assembly.SetEntryAssembly rejected the call: {e.InnerException?.Message ?? e.Message}";
    }
    return Assembly.GetEntryAssembly() is null
      ? null
      : "Assembly.SetEntryAssembly left an entry assembly in place";
  }

  /// <summary>
  /// Starts log4net the way an application would, which is what reads the application settings.
  /// </summary>
  /// <returns>the number of events that reached the appender</returns>
  private static int Log()
  {
    MemoryAppender memory = new()
    {
      Layout = new PatternLayout("%level %logger %message"),
      Threshold = Level.All,
    };
    memory.ActivateOptions();
    BasicConfigurator.Configure(memory);
    LogManager.GetLogger(typeof(NativeHostCheck)).Info("hello from a host without an entry assembly");
    return memory.GetEvents().Length;
  }

  private static void Check(List<string> failures, bool condition, string message)
  {
    if (!condition)
    {
      failures.Add(message);
    }
  }
}
