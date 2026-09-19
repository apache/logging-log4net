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
using System.Text;

using log4net.Appender;
using log4net.Config;
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

  private ILoggerRepository? _repository;

  /// <summary>The call log is static, so it carries over between the tests in this fixture.</summary>
  [SetUp]
  public void SetUp() => RecordingLock.Calls.Clear();

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
