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
using System.Globalization;
using System.Linq;
using System.Threading;
using log4net.Config;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Context;

/// <summary>
/// Used for internal unit testing the <see cref="ThreadContext"/> class.
/// </summary>
[TestFixture]
public class ThreadContextTest
{
  [TearDown]
  public void TearDown()
  {
    Utils.RemovePropertyFromAllContexts();
  }

  [Test]
  public void TestThreadPropertiesPattern()
  {
    StringAppender stringAppender = new StringAppender();
    stringAppender.Layout = new PatternLayout("%property{" + Utils.PROPERTY_KEY + "}");

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

    log1.Info("TestMessage");
    Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread properties value set");
    stringAppender.Reset();

    ThreadContext.Properties[Utils.PROPERTY_KEY] = "val1";

    log1.Info("TestMessage");
    Assert.AreEqual("val1", stringAppender.GetString(), "Test thread properties value set");
    stringAppender.Reset();

    ThreadContext.Properties.Remove(Utils.PROPERTY_KEY);

    log1.Info("TestMessage");
    Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread properties value removed");
    stringAppender.Reset();
  }

  [Test]
  public void TestThreadStackPattern()
  {
    StringAppender stringAppender = new StringAppender();
    stringAppender.Layout = new PatternLayout("%property{" + Utils.PROPERTY_KEY + "}");

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadStackPattern");

    log1.Info("TestMessage");
    Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread stack value set");
    stringAppender.Reset();

    using (ThreadContext.Stacks[Utils.PROPERTY_KEY].Push("val1"))
    {
      log1.Info("TestMessage");
      Assert.AreEqual("val1", stringAppender.GetString(), "Test thread stack value set");
      stringAppender.Reset();
    }

    log1.Info("TestMessage");
    Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread stack value removed");
    stringAppender.Reset();
  }

  [Test]
  public void TestThreadStackPattern2()
  {
    StringAppender stringAppender = new StringAppender();
    stringAppender.Layout = new PatternLayout("%property{" + Utils.PROPERTY_KEY + "}");

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadStackPattern");

    log1.Info("TestMessage");
    Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread stack value set");
    stringAppender.Reset();

    using (ThreadContext.Stacks[Utils.PROPERTY_KEY].Push("val1"))
    {
      log1.Info("TestMessage");
      Assert.AreEqual("val1", stringAppender.GetString(), "Test thread stack value set");
      stringAppender.Reset();

      using (ThreadContext.Stacks[Utils.PROPERTY_KEY].Push("val2"))
      {
        log1.Info("TestMessage");
        Assert.AreEqual("val1 val2", stringAppender.GetString(), "Test thread stack value pushed 2nd val");
        stringAppender.Reset();
      }
    }

    log1.Info("TestMessage");
    Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread stack value removed");
    stringAppender.Reset();
  }

  [Test]
  public void TestThreadStackPatternNullVal()
  {
    StringAppender stringAppender = new StringAppender();
    stringAppender.Layout = new PatternLayout("%property{" + Utils.PROPERTY_KEY + "}");

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadStackPattern");

    log1.Info("TestMessage");
    Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread stack value set");
    stringAppender.Reset();

    using (ThreadContext.Stacks[Utils.PROPERTY_KEY].Push(null))
    {
      log1.Info("TestMessage");
      Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread stack value set");
      stringAppender.Reset();
    }

    log1.Info("TestMessage");
    Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread stack value removed");
    stringAppender.Reset();
  }

  [Test]
  public void TestThreadStackPatternNullVal2()
  {
    StringAppender stringAppender = new StringAppender();
    stringAppender.Layout = new PatternLayout("%property{" + Utils.PROPERTY_KEY + "}");

    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
    BasicConfigurator.Configure(rep, stringAppender);

    ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadStackPattern");

    log1.Info("TestMessage");
    Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread stack value set");
    stringAppender.Reset();

    using (ThreadContext.Stacks[Utils.PROPERTY_KEY].Push("val1"))
    {
      log1.Info("TestMessage");
      Assert.AreEqual("val1", stringAppender.GetString(), "Test thread stack value set");
      stringAppender.Reset();

      using (ThreadContext.Stacks[Utils.PROPERTY_KEY].Push(null))
      {
        log1.Info("TestMessage");
        Assert.AreEqual("val1 ", stringAppender.GetString(), "Test thread stack value pushed null");
        stringAppender.Reset();
      }
    }

    log1.Info("TestMessage");
    Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread stack value removed");
    stringAppender.Reset();
  }

  [Test]
  public void TestBackgroundThreadContextProperty()
  {
    StringAppender stringAppender = new StringAppender();
    stringAppender.Layout = new PatternLayout("%property{DateTimeTodayToString}");

    string testBackgroundThreadContextPropertyRepository =
      "TestBackgroundThreadContextPropertyRepository" + Guid.NewGuid();
    ILoggerRepository rep = LogManager.CreateRepository(testBackgroundThreadContextPropertyRepository);
    BasicConfigurator.Configure(rep, stringAppender);

    Thread thread = new Thread(ExecuteBackgroundThread);
    thread.Start(testBackgroundThreadContextPropertyRepository);

    Thread.CurrentThread.Join(2000);
  }

  private static void ExecuteBackgroundThread(object? context)
  {
    string testBackgroundThreadContextPropertyRepository = (string)context!;
    ILog log = LogManager.GetLogger(testBackgroundThreadContextPropertyRepository, "ExecuteBackGroundThread");
    ThreadContext.Properties["DateTimeTodayToString"] = DateTime.Today.ToString(CultureInfo.InvariantCulture);

    log.Info("TestMessage");

    var hierarchyLoggingRepository = (Repository.Hierarchy.Hierarchy?)log.Logger.Repository;
    StringAppender stringAppender = (StringAppender)hierarchyLoggingRepository!.Root.Appenders[0];
    Assert.AreEqual(DateTime.Today.ToString(CultureInfo.InvariantCulture), stringAppender.GetString());
  }

  [Test]
  public void PropertiesShouldBeThreadSafe()
  {
    // Arrange
    var threads = new List<Thread>();
    var flags = new List<FlagContainer>();

    // Act
    for (var i = 0; i < Math.Max(64, 4 * Environment.ProcessorCount); i++)
    {
      var t = new Thread(SpinAndCheck);
      var flag = new FlagContainer();
      t.Start(flag);
      flags.Add(flag);
      threads.Add(t);
    }

    foreach (var t in threads)
    {
      t.Join();
    }

    Assert.IsTrue(flags.All(o => !o.Flag));
  }

  public class FlagContainer
  {
    public bool Flag { get; set; }
  }

  private void SpinAndCheck(object? obj)
  {
    var container = (FlagContainer)obj!;
    var threadid = Thread.CurrentThread.ManagedThreadId;
    for (var i = 0; i < 100000; i++)
    {
      ThreadContext.Properties["threadid"] = threadid;
      Thread.Sleep(0);
      if ((int?)ThreadContext.Properties["threadid"] != threadid)
      {
        container.Flag = true;
        break;
      }
    }
  }
}