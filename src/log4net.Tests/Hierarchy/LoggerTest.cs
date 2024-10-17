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
  private static string _msg = "M";

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
    var a1 = new CountingAppender { Name = "testAppender1" };
    _log.AddAppender(a1);

    IEnumerator enumAppenders = ((IEnumerable)_log.Appenders).GetEnumerator();
    try
    {
      Assert.IsTrue(enumAppenders.MoveNext());
      var aHat = (CountingAppender?)enumAppenders.Current;
      Assert.IsNotNull(aHat);
      Assert.AreEqual(a1, aHat);
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
    var a1 = new CountingAppender { Name = "testAppender2.1" };
    var a2 = new CountingAppender { Name = "testAppender2.2" };

    _log = (Logger)LogManager.GetLogger("test").Logger;
    _log.AddAppender(a1);
    _log.AddAppender(a2);
    Assert.AreEqual(2, _log.Appenders.Count);

    var aHat = (CountingAppender?)_log.GetAppender(a1.Name);
    Assert.AreEqual(a1, aHat);

    aHat = (CountingAppender?)_log.GetAppender(a2.Name);
    Assert.IsNotNull(aHat);
    Assert.AreEqual(a2, aHat);

    // By name.
    IAppender? removedAppender = _log.RemoveAppender("testAppender2.1");
    Assert.AreSame(a1, removedAppender);
    Assert.AreEqual(1, _log.Appenders.Count);

    IEnumerator enumAppenders = ((IEnumerable)_log.Appenders).GetEnumerator();
    try
    {
      Assert.IsTrue(enumAppenders.MoveNext());
      aHat = (CountingAppender?)enumAppenders.Current;
      Assert.IsNotNull(aHat);
      Assert.AreSame(a2, aHat);
      Assert.IsFalse(enumAppenders.MoveNext());
    }
    finally
    {
      if (enumAppenders is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }

    aHat = (CountingAppender?)_log.GetAppender(a2.Name);
    Assert.IsNotNull(aHat);
    Assert.AreSame(a2, aHat);

    // By appender.
    removedAppender = _log.RemoveAppender(a2);
    Assert.AreSame(a2, removedAppender);
    Assert.AreEqual(0, _log.Appenders.Count);

    enumAppenders = ((IEnumerable)_log.Appenders).GetEnumerator();
    try
    {
      Assert.IsFalse(enumAppenders.MoveNext());
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
    var ca = new CountingAppender();

    a.AddAppender(ca);
    Assert.IsNotNull(a.Repository);
    a.Repository!.Configured = true;

    Assert.AreEqual(0, ca.Counter);
    ab.Log(Level.Debug, _msg, null);
    Assert.AreEqual(1, ca.Counter);
    ab.Log(Level.Info, _msg, null);
    Assert.AreEqual(2, ca.Counter);
    ab.Log(Level.Warn, _msg, null);
    Assert.AreEqual(3, ca.Counter);
    ab.Log(Level.Error, _msg, null);
    Assert.AreEqual(4, ca.Counter);
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

    var ca1 = new CountingAppender();
    var ca2 = new CountingAppender();

    a.AddAppender(ca1);
    abc.AddAppender(ca2);
    Assert.IsNotNull(a.Repository);
    a.Repository!.Configured = true;

    Assert.AreEqual(0, ca1.Counter);
    Assert.AreEqual(0, ca2.Counter);

    ab.Log(Level.Debug, _msg, null);
    Assert.AreEqual(1, ca1.Counter);
    Assert.AreEqual(0, ca2.Counter);

    abc.Log(Level.Debug, _msg, null);
    Assert.AreEqual(2, ca1.Counter);
    Assert.AreEqual(1, ca2.Counter);

    x.Log(Level.Debug, _msg, null);
    Assert.AreEqual(2, ca1.Counter);
    Assert.AreEqual(1, ca2.Counter);
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

    var caRoot = new CountingAppender();
    var caA = new CountingAppender();
    var caAbc = new CountingAppender();

    root.AddAppender(caRoot);
    a.AddAppender(caA);
    abc.AddAppender(caAbc);
    Assert.IsNotNull(a.Repository);
    a.Repository!.Configured = true;

    Assert.AreEqual(0, caRoot.Counter);
    Assert.AreEqual(0, caA.Counter);
    Assert.AreEqual(0, caAbc.Counter);

    ab.Additivity = false;

    a.Log(Level.Debug, _msg, null);
    Assert.AreEqual(1, caRoot.Counter);
    Assert.AreEqual(1, caA.Counter);
    Assert.AreEqual(0, caAbc.Counter);

    ab.Log(Level.Debug, _msg, null);
    Assert.AreEqual(1, caRoot.Counter);
    Assert.AreEqual(1, caA.Counter);
    Assert.AreEqual(0, caAbc.Counter);

    abc.Log(Level.Debug, _msg, null);
    Assert.AreEqual(1, caRoot.Counter);
    Assert.AreEqual(1, caA.Counter);
    Assert.AreEqual(1, caAbc.Counter);
  }

  /// <summary>
  /// Test the ability to disable a level of message
  /// </summary>
  [Test]
  public void TestDisable1()
  {
    var caRoot = new CountingAppender();
    Logger root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;
    root.AddAppender(caRoot);

    Repository.Hierarchy.Hierarchy h = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository());
    h.Threshold = Level.Info;
    h.Configured = true;

    Assert.AreEqual(0, caRoot.Counter);

    root.Log(Level.Debug, _msg, null);
    Assert.AreEqual(0, caRoot.Counter);
    root.Log(Level.Info, _msg, null);
    Assert.AreEqual(1, caRoot.Counter);
    root.Log(Level.Warn, _msg, null);
    Assert.AreEqual(2, caRoot.Counter);
    root.Log(Level.Warn, _msg, null);
    Assert.AreEqual(3, caRoot.Counter);

    h.Threshold = Level.Warn;
    root.Log(Level.Debug, _msg, null);
    Assert.AreEqual(3, caRoot.Counter);
    root.Log(Level.Info, _msg, null);
    Assert.AreEqual(3, caRoot.Counter);
    root.Log(Level.Warn, _msg, null);
    Assert.AreEqual(4, caRoot.Counter);
    root.Log(Level.Error, _msg, null);
    Assert.AreEqual(5, caRoot.Counter);
    root.Log(Level.Error, _msg, null);
    Assert.AreEqual(6, caRoot.Counter);

    h.Threshold = Level.Off;
    root.Log(Level.Debug, _msg, null);
    Assert.AreEqual(6, caRoot.Counter);
    root.Log(Level.Info, _msg, null);
    Assert.AreEqual(6, caRoot.Counter);
    root.Log(Level.Warn, _msg, null);
    Assert.AreEqual(6, caRoot.Counter);
    root.Log(Level.Error, _msg, null);
    Assert.AreEqual(6, caRoot.Counter);
    root.Log(Level.Fatal, _msg, null);
    Assert.AreEqual(6, caRoot.Counter);
    root.Log(Level.Fatal, _msg, null);
    Assert.AreEqual(6, caRoot.Counter);
  }

  /// <summary>
  /// Tests the Exists method of the Logger class
  /// </summary>
  [Test]
  public void TestExists()
  {
    ILog a = LogManager.GetLogger("a");
    Assert.IsNotNull(a);
    ILog aB = LogManager.GetLogger("a.b");
    Assert.IsNotNull(aB);
    ILog aBc = LogManager.GetLogger("a.b.c");
    Assert.IsNotNull(aBc);

    ILog? t = LogManager.Exists("xx");
    Assert.IsNull(t);
    t = LogManager.Exists("a");
    Assert.AreSame(a, t);
    t = LogManager.Exists("a.b");
    Assert.AreSame(aB, t);
    t = LogManager.Exists("a.b.c");
    Assert.AreSame(aBc, t);
  }

  /// <summary>
  /// Tests the chained level for a hierarchy
  /// </summary>
  [Test]
  public void TestHierarchy1()
  {
    var h = new Repository.Hierarchy.Hierarchy
    {
      Root =
      {
        Level = Level.Error
      }
    };

    Logger a0 = (Logger)h.GetLogger("a");
    Assert.AreEqual("a", a0.Name);
    Assert.IsNull(a0.Level);
    Assert.AreSame(Level.Error, a0.EffectiveLevel);

    Logger a1 = (Logger)h.GetLogger("a");
    Assert.AreSame(a0, a1);
  }
}
