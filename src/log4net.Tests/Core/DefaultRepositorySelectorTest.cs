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
using System.Collections;
using System.Collections.Generic;
using log4net.Appender;
using log4net.Core;
using log4net.ObjectRenderer;
using log4net.Plugin;
using log4net.Repository;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Core;

[TestFixture]
public class DefaultRepositorySelectorTest
{
  private const string RepositoryName = "MyRepository";

  [Test]
  public void CreateRepository_ExplicitType()
  {
    DefaultRepositorySelector selector = new(typeof(MockLoggerRepository));
    int numCreatedCallbacks = 0;
    selector.LoggerRepositoryCreatedEvent += (_, _) => numCreatedCallbacks++;

    Assert.That(selector.ExistsRepository(RepositoryName), Is.False);
    ILoggerRepository[] allRepositories = selector.GetAllRepositories();
    Assert.That(allRepositories, Is.Empty);
    Assert.That(numCreatedCallbacks, Is.EqualTo(0));
    Assert.That(() => selector.GetRepository(RepositoryName), Throws.TypeOf<LogException>());

    ILoggerRepository logRep = selector.CreateRepository(RepositoryName, typeof(MockLoggerRepository));
    Assert.That(selector.ExistsRepository(RepositoryName));
    allRepositories = selector.GetAllRepositories();
    Assert.That(allRepositories, Has.Length.EqualTo(1));
    Assert.That(allRepositories[0], Is.SameAs(logRep));
    Assert.That(numCreatedCallbacks, Is.EqualTo(1));
    Assert.That(logRep, Is.InstanceOf<MockLoggerRepository>());
    ILoggerRepository rep2 = selector.GetRepository(RepositoryName);
    Assert.That(rep2, Is.SameAs(logRep));

    Assert.That(() => selector.CreateRepository(RepositoryName, typeof(MockLoggerRepository)),
      Throws.TypeOf<LogException>().With.Message.Contains("already defined"),
      "Should have thrown exception on redefinition.");
  }

  [Test]
  public void CreateRepository_AssemblyAndType_NoReadAssemblyAttributes()
  {
    DefaultRepositorySelector selector = new(typeof(MockLoggerRepository));
    int numCreatedCallbacks = 0;
    selector.LoggerRepositoryCreatedEvent += (_, _) => numCreatedCallbacks++;

    ILoggerRepository logRep = selector.CreateRepository(typeof(MockLoggerRepository).Assembly,
      typeof(MockLoggerRepository), RepositoryName, readAssemblyAttributes: false);
    Assert.That(selector.ExistsRepository(RepositoryName));
    ILoggerRepository[] allRepositories = selector.GetAllRepositories();
    Assert.That(allRepositories, Has.Length.EqualTo(1));
    Assert.That(allRepositories[0], Is.SameAs(logRep));
    Assert.That(numCreatedCallbacks, Is.EqualTo(1));
    Assert.That(logRep, Is.InstanceOf<MockLoggerRepository>());
  }

  [Test]
  public void CreateRepository_AssemblyWithNullType_NoReadAssemblyAttributes()
  {
    DefaultRepositorySelector selector = new(typeof(MockLoggerRepository));

    ILoggerRepository logRep = selector.CreateRepository(typeof(MockLoggerRepository2).Assembly, repositoryType: null,
      RepositoryName, readAssemblyAttributes: false);
    Assert.That(logRep, Is.InstanceOf<MockLoggerRepository>(), "Should have instantiated the default logger type specified in the selector constructor");
  }

  [Test]
  public void CreateRepository_AssemblyWithNullType_ReadAssemblyAttributes()
  {
    DefaultRepositorySelector selector = new(typeof(MockLoggerRepository));

    ILoggerRepository logRep = selector.CreateRepository(typeof(MockLoggerRepository2).Assembly, repositoryType: null,
      RepositoryName, readAssemblyAttributes: true);
    Assert.That(logRep, Is.InstanceOf<MockLoggerRepository>(), "Should have instantiated default logger type");
    Assert.That(logRep.Name, Is.EqualTo("MyRepository"));
  }

  [Test]
  public void CreateRepositoryAndAlias()
  {
    DefaultRepositorySelector selector = new(typeof(MockLoggerRepository));
    int numCreatedCallbacks = 0;
    selector.LoggerRepositoryCreatedEvent += (_, _) => numCreatedCallbacks++;

    ILoggerRepository logRep = selector.CreateRepository(RepositoryName, typeof(MockLoggerRepository));
    selector.AliasRepository("alias1", logRep);

    MockLoggerRepository2 otherTypeLogRep = new();
    Assert.That(() => selector.AliasRepository("alias1", otherTypeLogRep), Throws.InvalidOperationException);
    Assert.That(() => selector.AliasRepository(RepositoryName, otherTypeLogRep), Throws.InvalidOperationException);

    Assert.That(selector.ExistsRepository(RepositoryName));
    Assert.That(selector.ExistsRepository("alias1"), Is.False);
    ILoggerRepository[] allRepositories = selector.GetAllRepositories();
    Assert.That(allRepositories, Has.Length.EqualTo(1));
    Assert.That(allRepositories[0], Is.SameAs(logRep));
    Assert.That(numCreatedCallbacks, Is.EqualTo(1));
  }
}

internal class MockLoggerRepository : ILoggerRepository
{
  public MockLoggerRepository() => PluginMap = new(this);

  public string Name { get; set; } = nameof(MockLoggerRepository);
  public RendererMap RendererMap { get; } = new();
  public PluginMap PluginMap { get; }
  public LevelMap LevelMap { get; } = new();
  public Level Threshold { get; set; } = Level.All;
  public ILogger Exists(string name) => throw new NotImplementedException();

  public ILogger[] GetCurrentLoggers() => throw new NotImplementedException();

  public ILogger GetLogger(string name) => throw new NotImplementedException();

  public void Shutdown()
  { }

  public void ResetConfiguration()
  { }

  public void Log(LoggingEvent logEvent)
  { }

  public bool Configured { get; set; }
  public ICollection ConfigurationMessages { get; set; } = new List<string>();
#pragma warning disable CS0067  // Unused event
  public event LoggerRepositoryShutdownEventHandler? ShutdownEvent;
  public event LoggerRepositoryConfigurationResetEventHandler? ConfigurationReset;
  public event LoggerRepositoryConfigurationChangedEventHandler? ConfigurationChanged;
#pragma warning restore CS0067
  public PropertiesDictionary Properties { get; } = [];
  public IAppender[] GetAppenders() => throw new NotImplementedException();
}

internal sealed class MockLoggerRepository2 : MockLoggerRepository;
