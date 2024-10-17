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
    var selector = new DefaultRepositorySelector(typeof(MockLoggerRepository));
    int numCreatedCallbacks = 0;
    selector.LoggerRepositoryCreatedEvent += (_, _) => numCreatedCallbacks++;

    Assert.IsFalse(selector.ExistsRepository(RepositoryName));
    ILoggerRepository[] allRepositories = selector.GetAllRepositories();
    Assert.AreEqual(0, allRepositories.Length);
    Assert.AreEqual(0, numCreatedCallbacks);
    Assert.Throws<LogException>(() => selector.GetRepository(RepositoryName));

    ILoggerRepository logRep = selector.CreateRepository(RepositoryName, typeof(MockLoggerRepository));
    Assert.IsTrue(selector.ExistsRepository(RepositoryName));
    allRepositories = selector.GetAllRepositories();
    Assert.AreEqual(1, allRepositories.Length);
    Assert.AreSame(logRep, allRepositories[0]);
    Assert.AreEqual(1, numCreatedCallbacks);
    Assert.IsInstanceOf<MockLoggerRepository>(logRep);
    ILoggerRepository rep2 = selector.GetRepository(RepositoryName);
    Assert.AreSame(logRep, rep2);

    try
    {
      selector.CreateRepository(RepositoryName, typeof(MockLoggerRepository));
      Assert.Fail("Should have thrown exception on redefinition.");
    }
    catch (LogException logEx)
    {
      Assert.IsTrue(logEx.Message.Contains("already defined"));
    }
  }

  [Test]
  public void CreateRepository_AssemblyAndType_NoReadAssemblyAttributes()
  {
    var selector = new DefaultRepositorySelector(typeof(MockLoggerRepository));
    int numCreatedCallbacks = 0;
    selector.LoggerRepositoryCreatedEvent += (_, _) => numCreatedCallbacks++;

    ILoggerRepository logRep = selector.CreateRepository(typeof(MockLoggerRepository).Assembly,
      typeof(MockLoggerRepository), RepositoryName, readAssemblyAttributes: false);
    Assert.IsTrue(selector.ExistsRepository(RepositoryName));
    ILoggerRepository[] allRepositories = selector.GetAllRepositories();
    Assert.AreEqual(1, allRepositories.Length);
    Assert.AreSame(logRep, allRepositories[0]);
    Assert.AreEqual(1, numCreatedCallbacks);
    Assert.IsInstanceOf<MockLoggerRepository>(logRep);
  }

  [Test]
  public void CreateRepository_AssemblyWithNullType_NoReadAssemblyAttributes()
  {
    var selector = new DefaultRepositorySelector(typeof(MockLoggerRepository));

    ILoggerRepository logRep = selector.CreateRepository(typeof(MockLoggerRepository2).Assembly, repositoryType: null,
      RepositoryName, readAssemblyAttributes: false);
    Assert.IsInstanceOf<MockLoggerRepository>(logRep, "Should have instantiated the default logger type specified in the selector constructor");
  }

  [Test]
  public void CreateRepository_AssemblyWithNullType_ReadAssemblyAttributes()
  {
    var selector = new DefaultRepositorySelector(typeof(MockLoggerRepository));

    ILoggerRepository logRep = selector.CreateRepository(typeof(MockLoggerRepository2).Assembly, repositoryType: null,
      RepositoryName, readAssemblyAttributes: true);
    Assert.IsInstanceOf<MockLoggerRepository>(logRep, "Should have instantiated default logger type");
    Assert.AreEqual("MyRepository", logRep.Name);
  }

  [Test]
  public void CreateRepositoryAndAlias()
  {
    var selector = new DefaultRepositorySelector(typeof(MockLoggerRepository));
    int numCreatedCallbacks = 0;
    selector.LoggerRepositoryCreatedEvent += (_, _) => numCreatedCallbacks++;

    ILoggerRepository logRep = selector.CreateRepository(RepositoryName, typeof(MockLoggerRepository));
    selector.AliasRepository("alias1", logRep);

    var otherTypeLogRep = new MockLoggerRepository2();
    Assert.Throws<InvalidOperationException>(() => selector.AliasRepository("alias1", otherTypeLogRep));
    Assert.Throws<InvalidOperationException>(() => selector.AliasRepository(RepositoryName, otherTypeLogRep));

    Assert.IsTrue(selector.ExistsRepository(RepositoryName));
    Assert.IsFalse(selector.ExistsRepository("alias1"));
    ILoggerRepository[] allRepositories = selector.GetAllRepositories();
    Assert.AreEqual(1, allRepositories.Length);
    Assert.AreSame(logRep, allRepositories[0]);
    Assert.AreEqual(1, numCreatedCallbacks);
  }
}

internal class MockLoggerRepository : ILoggerRepository
{
  public MockLoggerRepository()
  {
    PluginMap = new(this);
  }

  public string Name { get; set; } = nameof(MockLoggerRepository);
  public RendererMap RendererMap { get; } = new();
  public PluginMap PluginMap { get; }
  public LevelMap LevelMap { get; } = new();
  public Level Threshold { get; set; } = Level.All;
  public ILogger Exists(string name)
  {
    throw new NotImplementedException();
  }

  public ILogger[] GetCurrentLoggers()
  {
    throw new NotImplementedException();
  }

  public ILogger GetLogger(string name)
  {
    throw new NotImplementedException();
  }

  public void Shutdown()
  {
  }

  public void ResetConfiguration()
  {
  }

  public void Log(LoggingEvent logEvent)
  {
  }
  public bool Configured { get; set; }
  public ICollection ConfigurationMessages { get; set; } = new List<string>();
#pragma warning disable CS0067  // Unused event
  public event LoggerRepositoryShutdownEventHandler? ShutdownEvent;
  public event LoggerRepositoryConfigurationResetEventHandler? ConfigurationReset;
  public event LoggerRepositoryConfigurationChangedEventHandler? ConfigurationChanged;
#pragma warning restore CS0067
  public PropertiesDictionary Properties { get; } = new();
  public IAppender[] GetAppenders()
  {
    throw new NotImplementedException();
  }
}

internal sealed class MockLoggerRepository2 : MockLoggerRepository;
