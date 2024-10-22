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
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Tests.Appender;

using NUnit.Framework;

namespace log4net.Tests.Hierarchy;

/// <summary>
/// Used for internal unit testing the <see cref="Logger"/> class.
/// </summary>
/// <remarks>
/// Internal unit test. Uses the NUnit test harness.
/// </remarks>
[TestFixture]
public sealed class LoggerTest
{
  private Logger? _log;

  // A short message.
  private const string TestLogMessage = "M";

  /// <summary>
  /// Any steps that happen after each test go here
  /// </summary>
  [TearDown]
  public void TearDown()
  {
    // Regular users should not use the clear method lightly!
    LogManager.GetRepository().ResetConfiguration();
    LogManager.GetRepository().Shutdown();
    ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Clear();
  }

  /// <summary>
  /// Add an appender and see if it can be retrieved.
  /// </summary>
  [Test]
  public void TestAppender1()
  {
    _log = (Logger)LogManager.GetLogger("test").Logger;
    CountingAppender a1 = new() { Name = "testAppender1" };
    _log.AddAppender(a1);

    IEnumerator enumAppenders = ((IEnumerable)_log.Appenders).GetEnumerator();
    try
    {
      Assert.That(enumAppenders.MoveNext());
      CountingAppender? aHat = (CountingAppender?)enumAppenders.Current;
      Assert.That(aHat, Is.Not.Null);
      Assert.That(aHat, Is.EqualTo(a1));
    }
    finally
    {
      if (enumAppenders is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }

  /// <summary>
  /// Add an appender X, Y, remove X and check if Y is the only
  /// remaining appender.
  /// </summary>
  [Test]
  public void TestAppender2()
  {
    CountingAppender a1 = new() { Name = "testAppender2.1" };
    CountingAppender a2 = new() { Name = "testAppender2.2" };

    _log = (Logger)LogManager.GetLogger("test").Logger;
    _log.AddAppender(a1);
    _log.AddAppender(a2);
    Assert.That(_log.Appenders, Has.Count.EqualTo(2));

    CountingAppender? aHat = (CountingAppender?)_log.GetAppender(a1.Name);
    Assert.That(aHat, Is.EqualTo(a1));

    aHat = (CountingAppender?)_log.GetAppender(a2.Name);
    Assert.That(aHat, Is.Not.Null);
    Assert.That(aHat, Is.EqualTo(a2));

    // By name.
    IAppender? removedAppender = _log.RemoveAppender("testAppender2.1");
    Assert.That(removedAppender, Is.SameAs(a1));
    Assert.That(_log.Appenders, Has.Count.EqualTo(1));

    IEnumerator enumAppenders = ((IEnumerable)_log.Appenders).GetEnumerator();
    try
    {
      Assert.That(enumAppenders.MoveNext());
      aHat = (CountingAppender?)enumAppenders.Current;
      Assert.That(aHat, Is.Not.Null);
      Assert.That(aHat, Is.SameAs(a2));
      Assert.That(enumAppenders.MoveNext(), Is.False);
    }
    finally
    {
      if (enumAppenders is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }

    aHat = (CountingAppender?)_log.GetAppender(a2.Name);
    Assert.That(aHat, Is.Not.Null);
    Assert.That(aHat, Is.SameAs(a2));

    // By appender.
    removedAppender = _log.RemoveAppender(a2);
    Assert.That(removedAppender, Is.SameAs(a2));
    Assert.That(_log.Appenders, Is.Empty);

    enumAppenders = ((IEnumerable)_log.Appenders).GetEnumerator();
    try
    {
      Assert.That(enumAppenders.MoveNext(), Is.False);
    }
    finally
    {
      if (enumAppenders is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }

  /// <summary>
  /// Test if logger a.b inherits its appender from a.
  /// </summary>
  [Test]
  public void TestAdditivity1()
  {
    Logger a = (Logger)LogManager.GetLogger("a").Logger;
    Logger ab = (Logger)LogManager.GetLogger("a.b").Logger;
    CountingAppender ca = new();

    a.AddAppender(ca);
    Assert.That(a.Repository, Is.Not.Null);
    a.Repository.Configured = true;

    Assert.That(ca.Counter, Is.EqualTo(0));
    ab.Log(Level.Debug, TestLogMessage, null);
    Assert.That(ca.Counter, Is.EqualTo(1));
    ab.Log(Level.Info, TestLogMessage, null);
    Assert.That(ca.Counter, Is.EqualTo(2));
    ab.Log(Level.Warn, TestLogMessage, null);
    Assert.That(ca.Counter, Is.EqualTo(3));
    ab.Log(Level.Error, TestLogMessage, null);
    Assert.That(ca.Counter, Is.EqualTo(4));
  }

  /// <summary>
  /// Test multiple additivity.
  /// </summary>
  [Test]
  public void TestAdditivity2()
  {
    Logger a = (Logger)LogManager.GetLogger("a").Logger;
    Logger ab = (Logger)LogManager.GetLogger("a.b").Logger;
    Logger abc = (Logger)LogManager.GetLogger("a.b.c").Logger;
    Logger x = (Logger)LogManager.GetLogger("x").Logger;

    CountingAppender ca1 = new();
    CountingAppender ca2 = new();

    a.AddAppender(ca1);
    abc.AddAppender(ca2);
    Assert.That(a.Repository, Is.Not.Null);
    a.Repository.Configured = true;

    Assert.That(ca1.Counter, Is.EqualTo(0));
    Assert.That(ca2.Counter, Is.EqualTo(0));

    ab.Log(Level.Debug, TestLogMessage, null);
    Assert.That(ca1.Counter, Is.EqualTo(1));
    Assert.That(ca2.Counter, Is.EqualTo(0));

    abc.Log(Level.Debug, TestLogMessage, null);
    Assert.That(ca1.Counter, Is.EqualTo(2));
    Assert.That(ca2.Counter, Is.EqualTo(1));

    x.Log(Level.Debug, TestLogMessage, null);
    Assert.That(ca1.Counter, Is.EqualTo(2));
    Assert.That(ca2.Counter, Is.EqualTo(1));
  }

  /// <summary>
  /// Test additivity flag.
  /// </summary>
  [Test]
  public void TestAdditivity3()
  {
    Logger root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;
    Logger a = (Logger)LogManager.GetLogger("a").Logger;
    Logger ab = (Logger)LogManager.GetLogger("a.b").Logger;
    Logger abc = (Logger)LogManager.GetLogger("a.b.c").Logger;

    CountingAppender caRoot = new();
    CountingAppender caA = new();
    CountingAppender caAbc = new();

    root.AddAppender(caRoot);
    a.AddAppender(caA);
    abc.AddAppender(caAbc);
    Assert.That(a.Repository, Is.Not.Null);
    a.Repository.Configured = true;

    Assert.That(caRoot.Counter, Is.EqualTo(0));
    Assert.That(caA.Counter, Is.EqualTo(0));
    Assert.That(caAbc.Counter, Is.EqualTo(0));

    ab.Additivity = false;

    a.Log(Level.Debug, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(1));
    Assert.That(caA.Counter, Is.EqualTo(1));
    Assert.That(caAbc.Counter, Is.EqualTo(0));

    ab.Log(Level.Debug, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(1));
    Assert.That(caA.Counter, Is.EqualTo(1));
    Assert.That(caAbc.Counter, Is.EqualTo(0));

    abc.Log(Level.Debug, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(1));
    Assert.That(caA.Counter, Is.EqualTo(1));
    Assert.That(caAbc.Counter, Is.EqualTo(1));
  }

  /// <summary>
  /// Test the ability to disable a level of message
  /// </summary>
  [Test]
  public void TestDisable1()
  {
    CountingAppender caRoot = new();
    Logger root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;
    root.AddAppender(caRoot);

    Repository.Hierarchy.Hierarchy h = (Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
    h.Threshold = Level.Info;
    h.Configured = true;

    Assert.That(caRoot.Counter, Is.EqualTo(0));

    root.Log(Level.Debug, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(0));
    root.Log(Level.Info, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(1));
    root.Log(Level.Warn, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(2));
    root.Log(Level.Warn, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(3));

    h.Threshold = Level.Warn;
    root.Log(Level.Debug, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(3));
    root.Log(Level.Info, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(3));
    root.Log(Level.Warn, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(4));
    root.Log(Level.Error, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(5));
    root.Log(Level.Error, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(6));

    h.Threshold = Level.Off;
    root.Log(Level.Debug, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(6));
    root.Log(Level.Info, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(6));
    root.Log(Level.Warn, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(6));
    root.Log(Level.Error, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(6));
    root.Log(Level.Fatal, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(6));
    root.Log(Level.Fatal, TestLogMessage, null);
    Assert.That(caRoot.Counter, Is.EqualTo(6));
  }

  /// <summary>
  /// Tests the Exists method of the Logger class
  /// </summary>
  [Test]
  public void TestExists()
  {
    ILog a = LogManager.GetLogger("a");
    Assert.That(a, Is.Not.Null);
    ILog aB = LogManager.GetLogger("a.b");
    Assert.That(aB, Is.Not.Null);
    ILog aBc = LogManager.GetLogger("a.b.c");
    Assert.That(aBc, Is.Not.Null);

    ILog? t = LogManager.Exists("xx");
    Assert.That(t, Is.Null);
    t = LogManager.Exists("a");
    Assert.That(t, Is.SameAs(a));
    t = LogManager.Exists("a.b");
    Assert.That(t, Is.SameAs(aB));
    t = LogManager.Exists("a.b.c");
    Assert.That(t, Is.SameAs(aBc));
  }

  /// <summary>
  /// Tests the chained level for a hierarchy
  /// </summary>
  [Test]
  public void TestHierarchy1()
  {
    Repository.Hierarchy.Hierarchy h = new() { Root = { Level = Level.Error } };

    Logger a0 = (Logger)h.GetLogger("a");
    Assert.That(a0.Name, Is.EqualTo("a"));
    Assert.That(a0.Level, Is.Null);
    Assert.That(a0.EffectiveLevel, Is.SameAs(Level.Error));

    Logger a1 = (Logger)h.GetLogger("a");
    Assert.That(a1, Is.SameAs(a0));
  }
}
