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
using System.Linq;
using System.Text;

using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using log4net.Util;

using NUnit.Framework;

using PeanutButter.Utils;

namespace log4net.Tests.Config;

/// <summary>
/// Reconfiguring must not leave two <see cref="FileAppender"/> instances holding one file.
/// </summary>
[TestFixture]
[NonParallelizable]
public sealed class XmlConfiguratorReconfigurationTest
{
  /// <summary>
  /// Records every open and close call, so a test can assert their order.
  /// </summary>
  /// <remarks>Public because the configurator instantiates it by name.</remarks>
  public sealed class RecordingLock : FileAppender.LockingModelBase
  {
    /// <summary>Open and close calls across all instances, in order.</summary>
    internal static List<string> Calls { get; } = [];

    private string _tag = "?";

    /// <inheritdoc/>
    public override void ActivateOptions()
    { }

    /// <inheritdoc/>
    public override void OpenFile(string filename, bool append, Encoding encoding)
    {
      _tag = CurrentAppender?.Name ?? "?";
      Calls.Add($"open {_tag}");
    }

    /// <inheritdoc/>
    public override void CloseFile() => Calls.Add($"close {_tag}");

    /// <inheritdoc/>
    public override Stream? AcquireLock() => Stream.Null;

    /// <inheritdoc/>
    public override void ReleaseLock()
    { }

    /// <inheritdoc/>
    public override void OnClose()
    { }
  }

  /// <summary>Fails to activate, the way an appender does when a required option is missing.</summary>
  /// <remarks>Public because the configurator instantiates it by name.</remarks>
  public sealed class ThrowingAppender : AppenderSkeleton
  {
    /// <summary>Whether the configurator closed the appender it could not activate.</summary>
    internal static bool Closed { get; private set; }

    /// <summary>Clears the static state this appender records.</summary>
    internal static void Reset() => Closed = false;

    /// <inheritdoc/>
    public override void ActivateOptions() => throw new InvalidOperationException("no RemoteAddress");

    /// <inheritdoc/>
    protected override void OnClose() => Closed = true;

    /// <inheritdoc/>
    protected override void Append(LoggingEvent loggingEvent)
    { }
  }

  /// <summary>Fails to activate while holding an appender that a logger also holds directly.</summary>
  /// <remarks>Public because the configurator instantiates it by name.</remarks>
  public sealed class ThrowingForwarder : ForwardingAppender
  {
    /// <inheritdoc/>
    public override void ActivateOptions() => throw new InvalidOperationException("no target");
  }

  private ILoggerRepository? _repository;

  /// <summary>The call log is static, so it carries over between the tests in this fixture.</summary>
  [SetUp]
  public void SetUp()
  {
    RecordingLock.Calls.Clear();
    ThrowingAppender.Reset();
  }

  /// <summary>Closes the appenders, in case the test left the repository running.</summary>
  [TearDown]
  public void TearDown()
  {
    if (_repository is not null)
    {
      LogLog.ExecuteWithoutEmittingInternalMessages(_repository.Shutdown);
      _repository = null;
    }
  }

  /// <summary>
  /// Configuring the live repository again is what the file watcher does on every change. The
  /// incoming appender used to open the file while the outgoing one still held it.
  /// </summary>
  [Test]
  public void ReconfiguringClosesTheOutgoingAppenderBeforeOpeningTheNewOne()
  {
    using AutoTempFolder folder = new();
    FileInfo configFile = new(Path.Combine(folder.Path, "log.config"));
    _repository = LogManager.CreateRepository(Guid.NewGuid().ToString());

    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      Write(configFile, folder, "first");
      XmlConfigurator.Configure(_repository, configFile);

      Write(configFile, folder, "second");
      XmlConfigurator.Configure(_repository, configFile);

      _repository.Shutdown();
      _repository = null;
    });

    Assert.That(RecordingLock.Calls,
      Is.EqualTo(new[] { "open first", "close first", "open second", "close second" }));
  }

  /// <summary>
  /// Activation runs after the logger has taken the appender, so a failure has to be caught and
  /// the appender taken back off the logger rather than escaping the configuration pass.
  /// </summary>
  [Test]
  public void AnAppenderThatCannotActivateIsDetachedAndClosed()
  {
    using AutoTempFolder folder = new();
    FileInfo configFile = new(Path.Combine(folder.Path, "log.config"));
    _repository = LogManager.CreateRepository(Guid.NewGuid().ToString());

    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      WriteThrowing(configFile);
      XmlConfigurator.Configure(_repository, configFile);
    });

    Assert.That(ThrowingAppender.Closed, Is.True);
    Assert.That(_repository.GetAppenders(), Is.Empty);
  }

  /// <summary>
  /// The appenders are activated in one loop, so a failure in it must not cost the appenders
  /// behind it their activation.
  /// </summary>
  [Test]
  public void AFailedActivationDoesNotStopTheOnesBehindIt()
  {
    using AutoTempFolder folder = new();
    FileInfo configFile = new(Path.Combine(folder.Path, "log.config"));
    _repository = LogManager.CreateRepository(Guid.NewGuid().ToString());

    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      WriteThrowingThenWorking(configFile, folder);
      XmlConfigurator.Configure(_repository, configFile);
    });

    Assert.That(RecordingLock.Calls, Is.EqualTo(new[] { "open working" }));
    Assert.That(_repository.GetAppenders().Select(a => a.Name), Is.EqualTo(new[] { "working" }));
  }

  /// <summary>
  /// Closing a container closes what it holds, so discarding one that failed to activate must not
  /// take down an appender a logger still holds directly.
  /// </summary>
  [Test]
  public void DiscardingAContainerLeavesItsChildrenOpen()
  {
    using AutoTempFolder folder = new();
    FileInfo configFile = new(Path.Combine(folder.Path, "log.config"));
    _repository = LogManager.CreateRepository(Guid.NewGuid().ToString());

    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      WriteThrowingForwarder(configFile, folder);
      XmlConfigurator.Configure(_repository, configFile);
    });

    Assert.That(RecordingLock.Calls, Is.EqualTo(new[] { "open working" }));
    Assert.That(_repository.GetAppenders().Select(a => a.Name), Is.EqualTo(new[] { "working" }));
  }

  /// <summary>Writes a configuration whose single appender throws on activation.</summary>
  private static void WriteThrowing(FileInfo configFile)
  {
    using (StreamWriter writer = configFile.CreateText())
    {
      writer.Write("""
        <log4net>
          <appender name="throwing" type="log4net.Tests.Config.XmlConfiguratorReconfigurationTest+ThrowingAppender, log4net.Tests">
            <layout type="log4net.Layout.PatternLayout">
              <conversionPattern value="%message%newline" />
            </layout>
          </appender>
          <root>
            <level value="ALL" />
            <appender-ref ref="throwing" />
          </root>
        </log4net>
        """);
    }

    configFile.Refresh();
  }

  /// <summary>Writes a configuration whose first appender throws and whose second one works.</summary>
  private static void WriteThrowingThenWorking(FileInfo configFile, AutoTempFolder folder)
  {
    using (StreamWriter writer = configFile.CreateText())
    {
      writer.Write($"""
        <log4net>
          <appender name="throwing" type="log4net.Tests.Config.XmlConfiguratorReconfigurationTest+ThrowingAppender, log4net.Tests">
            <layout type="log4net.Layout.PatternLayout">
              <conversionPattern value="%message%newline" />
            </layout>
          </appender>
          <appender name="working" type="log4net.Appender.FileAppender">
            <file value="{Path.Combine(folder.Path, "log.log")}" />
            <lockingModel type="log4net.Tests.Config.XmlConfiguratorReconfigurationTest+RecordingLock, log4net.Tests" />
            <layout type="log4net.Layout.PatternLayout">
              <conversionPattern value="%message%newline" />
            </layout>
          </appender>
          <root>
            <level value="ALL" />
            <appender-ref ref="throwing" />
            <appender-ref ref="working" />
          </root>
        </log4net>
        """);
    }

    configFile.Refresh();
  }

  /// <summary>Writes a configuration whose failing forwarder holds the appender root holds.</summary>
  private static void WriteThrowingForwarder(FileInfo configFile, AutoTempFolder folder)
  {
    using (StreamWriter writer = configFile.CreateText())
    {
      writer.Write($"""
        <log4net>
          <appender name="working" type="log4net.Appender.FileAppender">
            <file value="{Path.Combine(folder.Path, "log.log")}" />
            <lockingModel type="log4net.Tests.Config.XmlConfiguratorReconfigurationTest+RecordingLock, log4net.Tests" />
            <layout type="log4net.Layout.PatternLayout">
              <conversionPattern value="%message%newline" />
            </layout>
          </appender>
          <appender name="forwarder" type="log4net.Tests.Config.XmlConfiguratorReconfigurationTest+ThrowingForwarder, log4net.Tests">
            <appender-ref ref="working" />
          </appender>
          <root>
            <level value="ALL" />
            <appender-ref ref="working" />
            <appender-ref ref="forwarder" />
          </root>
        </log4net>
        """);
    }

    configFile.Refresh();
  }

  /// <summary>Writes a configuration naming its single appender after the pass.</summary>
  private static void Write(FileInfo configFile, AutoTempFolder folder, string appenderName)
  {
    using (StreamWriter writer = configFile.CreateText())
    {
      writer.Write($"""
        <log4net>
          <appender name="{appenderName}" type="log4net.Appender.FileAppender">
            <file value="{Path.Combine(folder.Path, "log.log")}" />
            <lockingModel type="log4net.Tests.Config.XmlConfiguratorReconfigurationTest+RecordingLock, log4net.Tests" />
            <layout type="log4net.Layout.PatternLayout">
              <conversionPattern value="%message%newline" />
            </layout>
          </appender>
          <root>
            <level value="ALL" />
            <appender-ref ref="{appenderName}" />
          </root>
        </log4net>
        """);
    }

    configFile.Refresh();
  }
}
