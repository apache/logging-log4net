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
using System.Net;
using System.Xml;

using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using log4net.Util;

namespace log4net.Tests.Aot;

/// <summary>
/// The log4net surface this project exercises.
/// </summary>
/// <remarks>
/// <para>
/// Each probe is run identically whether the assembly was JIT compiled or published with Native
/// AOT, so a difference between the two runs is an AOT effect rather than a platform one.
/// </para>
/// </remarks>
internal static class Probes
{
  /// <summary>
  /// Probes that are expected to fail under Native AOT, with the reason.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Keep it honest: an unlisted failure and a listed probe that starts passing both fail the run.
  /// </para>
  /// </remarks>
  internal static readonly Dictionary<string, string> ExpectedAotFailures = new(StringComparer.Ordinal)
  {
    ["config/XmlConfigurator.Configure(element)"]
      = "appender and layout types are named as strings in XML, so the trimmer removes them",
    ["settings/appSettings from app.config"]
      = "System.Configuration cannot initialize once trimmed; environment variables stand in",
  };

  /// <summary>
  /// Probes that are expected to fail when the assembly is JIT compiled.
  /// </summary>
  internal static readonly Dictionary<string, string> ExpectedJitFailures = new(StringComparer.Ordinal)
  {
    ["settings/appSettings from environment"]
      = "the environment only stands in once the configuration system is known to be unavailable",
  };

  /// <summary>
  /// Every probe, in the order they are run.
  /// </summary>
  /// <returns>the probes</returns>
  internal static IEnumerable<Probe> All()
  {
    MemoryAppender memory = Configure();
    return
    [
      .. Core(memory),
      .. Configuration(),
      .. Each("appender", Appenders(), Activate),
      .. Each("layout", Layouts(), layout => Render(layout)),
      .. Patterns(),
      .. Each("filter", Filters(), Activate),
      .. Settings(),
      .. Shutdown(),
    ];
  }

  /// <summary>
  /// Wires up the appender the core probes assert against.
  /// </summary>
  private static MemoryAppender Configure()
  {
    MemoryAppender memory = new()
    {
      Layout = new PatternLayout("%level %logger %message"),
      Threshold = Level.All,
    };
    memory.ActivateOptions();
    BasicConfigurator.Configure(memory);
    return memory;
  }

  private static IEnumerable<Probe> Core(MemoryAppender memory)
  {
    yield return new("core", "GetLogger(Type)", () => LogManager.GetLogger(typeof(Probes)));
    yield return new("core", "GetLogger(string)", () => LogManager.GetLogger("by.name"));
    yield return new("core", "GetRepository()", () => LogManager.GetRepository());
    yield return new("core", "Exists", () => LogManager.Exists("by.name"));
    yield return new("core", "GetCurrentLoggers", () => LogManager.GetCurrentLoggers());
    yield return new("core", "CreateRepository(name)", () => LogManager.CreateRepository("probe-repository"));
    yield return new("core", "event reaches appender", () =>
    {
      memory.Clear();
      LogManager.GetLogger(typeof(Probes)).Info("hello");
      Require(memory.GetEvents().Length == 1, "the event did not reach the appender");
    });
    yield return new("core", "level lookup by name", () =>
    {
      Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
      Require(hierarchy.LevelMap["WARN"] is not null, "WARN is not in the level map");
    });
    yield return new("core", "context properties", () =>
    {
      ThreadContext.Properties["thread"] = 1;
      GlobalContext.Properties["global"] = 2;
      LogicalThreadContext.Properties["logical"] = 3;
    });
  }

  private static IEnumerable<Probe> Configuration()
  {
    yield return new("config", "BasicConfigurator.Configure(appender)", () => BasicConfigurator.Configure(new MemoryAppender()));
    yield return new("config", "XmlConfigurator.Configure(element)", () =>
    {
      XmlDocument document = new();
      document.LoadXml("""
        <log4net>
          <appender name="probe" type="log4net.Appender.MemoryAppender" />
          <root><level value="ALL" /><appender-ref ref="probe" /></root>
        </log4net>
        """);
      ILoggerRepository repository = LogManager.CreateRepository("xml-repository");
      XmlConfigurator.Configure(repository, document.DocumentElement!);
      Require(repository.GetAppenders().Length > 0, "no appenders were created from the XML");
    });
  }

  private static IEnumerable<Probe> Patterns()
  {
    yield return new("layout", "PatternLayout with every built-in converter", () =>
    {
      PatternLayout layout = new("%a %c %C %d %F %l %L %m %M %n %p %P %r %t %u %w %x %X %utcdate %exception %stacktrace %stacktracedetail");
      Require(Render(layout).Length > 0, "the layout rendered nothing");
    });
    yield return new("layout", "PatternString", () =>
      Require(new PatternString("%processid %appdomain %date{yyyy}").Format().Length > 0, "the pattern rendered nothing"));
  }

  private static IEnumerable<Probe> Settings()
  {
    yield return new("settings", "appSettings from app.config", () =>
      Require(SystemInfo.GetAppSetting("log4net.AotProbe") == "from-config", "the key was not read from app.config"));
    yield return new("settings", "appSettings from environment", () =>
      Require(SystemInfo.GetAppSetting(Program.EnvironmentProbeKey) == "from-environment", "the key was not read from the environment"));
  }

  private static IEnumerable<Probe> Shutdown()
  {
    yield return new("shutdown", "Flush", () => LogManager.Flush(1000));
    yield return new("shutdown", "Shutdown", LogManager.Shutdown);
  }

  /// <summary>
  /// One probe per entry, each creating its subject and handing it to <paramref name="check"/>.
  /// </summary>
  private static IEnumerable<Probe> Each<T>(string area, Dictionary<string, Func<T>> subjects, Action<T> check)
  {
    foreach (KeyValuePair<string, Func<T>> subject in subjects)
    {
      yield return new(area, subject.Key, () => check(subject.Value()));
    }
  }

  private static Dictionary<string, Func<IAppender>> Appenders() => new(StringComparer.Ordinal)
  {
    ["ConsoleAppender"] = () => new ConsoleAppender { Layout = new SimpleLayout() },
    ["MemoryAppender"] = () => new MemoryAppender(),
    ["DebugAppender"] = () => new DebugAppender { Layout = new SimpleLayout() },
    ["TraceAppender"] = () => new TraceAppender { Layout = new SimpleLayout() },
    ["ForwardingAppender"] = () => new ForwardingAppender(),
    ["BufferingForwardingAppender"] = () => new BufferingForwardingAppender { BufferSize = 2 },
    ["FileAppender"] = () => new FileAppender { File = TempFile("aot-probe-file.log"), Layout = new SimpleLayout() },
    ["RollingFileAppender"] = () => new RollingFileAppender { File = TempFile("aot-probe-roll.log"), Layout = new SimpleLayout() },
    ["UdpAppender"] = () => new UdpAppender { RemoteAddress = IPAddress.Loopback, RemotePort = 9999, Layout = new SimpleLayout() },
    ["AnsiColorTerminalAppender"] = () => new AnsiColorTerminalAppender { Layout = new SimpleLayout() },
  };

  private static Dictionary<string, Func<ILayout>> Layouts() => new(StringComparer.Ordinal)
  {
    ["SimpleLayout"] = () => new SimpleLayout(),
    ["PatternLayout"] = () => new PatternLayout("%level %logger %message%newline"),
    ["ExceptionLayout"] = () => new ExceptionLayout(),
    ["XmlLayout"] = () => new XmlLayout(),
  };

  private static Dictionary<string, Func<IFilter>> Filters() => new(StringComparer.Ordinal)
  {
    ["LevelRangeFilter"] = () => new LevelRangeFilter { LevelMin = Level.Debug, LevelMax = Level.Fatal },
    ["LevelMatchFilter"] = () => new LevelMatchFilter { LevelToMatch = Level.Info },
    ["StringMatchFilter"] = () => new StringMatchFilter { StringToMatch = "probe" },
    ["PropertyFilter"] = () => new PropertyFilter { Key = "probe", StringToMatch = "value" },
    ["DenyAllFilter"] = () => new DenyAllFilter(),
  };

  private static string TempFile(string name) => Path.Combine(Path.GetTempPath(), name);

  private static void Activate(object option) => (option as IOptionHandler)?.ActivateOptions();

  private static string Render(ILayout layout)
  {
    Activate(layout);
    using StringWriter writer = new();
    layout.Format(writer, new(new()
    {
      Level = Level.Info,
      LoggerName = "probe",
      Message = "message",
      TimeStampUtc = DateTime.UtcNow,
    }));
    return writer.ToString();
  }

  private static void Require(bool condition, string message)
  {
    if (!condition)
    {
      throw new InvalidOperationException(message);
    }
  }
}
