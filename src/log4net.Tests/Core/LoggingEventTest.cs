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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using log4net.Core;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Core;

[TestFixture]
public sealed class LoggingEventTest
{
  // net8 deprecates BinaryFormatter used in testing.
#if NET462_OR_GREATER
  private static readonly DateTime localTime
    = new(2000, 7, 1, 0, 0, 0, 0, CultureInfo.InvariantCulture.Calendar, DateTimeKind.Local);

  [Test]
  public void SerializeDeserialize_BinaryFormatter()
  {
    Utils.InconclusiveOnMono();
    DateTime timestamp = localTime.ToUniversalTime();
    LoggingEvent ev = new(new()
    {
      LoggerName = "aLogger",
      Level = Level.Log4Net_Debug,
      Message = "aMessage",
      ThreadName = "aThread",
      TimeStampUtc = timestamp,
      LocationInfo = new LocationInfo(GetType()),
      UserName = "aUser",
      Identity = "anIdentity",
      ExceptionString = "anException",
      Domain = "aDomain",
      Properties = new PropertiesDictionary { ["foo"] = "bar" },
    });

    BinaryFormatter formatter = new();
    using MemoryStream stream = new();
    formatter.Serialize(stream, ev);
    stream.Position = 0;
    LoggingEvent ev2 = (LoggingEvent)formatter.Deserialize(stream);

    Assert.AreEqual("aLogger", ev2.LoggerName);
    Assert.AreEqual(Level.Log4Net_Debug, ev2.Level);
    Assert.IsNull(ev2.MessageObject);
    Assert.AreEqual("aMessage", ev2.RenderedMessage);
    Assert.AreEqual("aThread", ev2.ThreadName);
    Assert.AreEqual(timestamp, ev2.TimeStampUtc);
    Assert.IsNotNull(ev2.LocationInfo);
    Assert.AreEqual("System.RuntimeMethodHandle", ev2.LocationInfo!.ClassName);
    Assert.AreEqual("InvokeMethod", ev2.LocationInfo!.MethodName);
    Assert.IsNull(ev2.LocationInfo!.FileName);
    Assert.AreEqual("0", ev2.LocationInfo!.LineNumber);
    Assert.AreEqual("aUser", ev2.UserName);
    Assert.AreEqual("anIdentity", ev2.Identity);
    Assert.IsNull(ev2.ExceptionObject);
    Assert.AreEqual("anException", ev2.GetExceptionString());
    Assert.AreEqual("aDomain", ev2.Domain);
    Assert.AreEqual(1, ev.Properties.Count);
    Assert.AreEqual("bar", ev2.Properties["foo"]);
  }

  /// <summary>
  /// Loads and validates the cached serialized v2 event data from the log4net2-SerializeEvent directory.
  /// </summary>
  [Test]
  public void DeserializeV2()
  {
    Utils.InconclusiveOnMono();
    const string datPath = @"..\..\..\..\integration-testing\log4net2-SerializeEvent\SerializeV2Event.dat";
    using Stream stream = File.OpenRead(datPath);
    BinaryFormatter formatter = new();
    LoggingEvent ev = (LoggingEvent)formatter.Deserialize(stream);
    Assert.IsNotNull(ev);

    Assert.AreEqual("aLogger", ev!.LoggerName);
    Assert.AreEqual(Level.Log4Net_Debug, ev.Level);
    Assert.IsNull(ev.MessageObject);
    Assert.AreEqual("aMessage", ev.RenderedMessage);
    Assert.AreEqual("aThread", ev.ThreadName);
    Assert.IsNotNull(ev.LocationInfo);
    Assert.AreEqual("?", ev.LocationInfo!.ClassName);
    Assert.AreEqual("?", ev.LocationInfo!.MethodName);
    Assert.AreEqual("?", ev.LocationInfo!.FileName);
    Assert.AreEqual("?", ev.LocationInfo!.LineNumber);
    Assert.AreEqual("aUser", ev.UserName);
    Assert.AreEqual("anIdentity", ev.Identity);
    Assert.IsNull(ev.ExceptionObject);
    Assert.AreEqual("anException", ev.GetExceptionString());
    Assert.AreEqual("aDomain", ev.Domain);
    Assert.AreEqual(1, ev.Properties.Count);
    Assert.AreEqual("bar", ev.Properties["foo"]);
    Assert.AreEqual(localTime.ToUniversalTime(), ev.TimeStampUtc);
  }
#endif // NET462_OR_GREATER

  /// <summary>
  /// Tests <see cref="LoggingEvent.ReviseThreadName"/>
  /// </summary>
  [Test]
  public void ReviseThreadNameTest()
  {
    Assert.AreEqual("PoolBoy", ReviseThreadName("PoolBoy"));
    AssertIsCurrentThreadId(ReviseThreadName(null));
    AssertIsCurrentThreadId(ReviseThreadName(string.Empty));
    AssertIsCurrentThreadId(ReviseThreadName(".NET ThreadPool Worker"));
    AssertIsCurrentThreadId(ReviseThreadName(".NET TP Worker"));
    AssertIsCurrentThreadId(ReviseThreadName(".NET Long Running Task"));

    static string ReviseThreadName(string? name)
    {
      return (string)typeof(LoggingEvent).GetMethod(nameof(ReviseThreadName),
        BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [name])!;
    }

    static void AssertIsCurrentThreadId(string name)
      => Assert.AreEqual(name, SystemInfo.CurrentThreadId.ToString(CultureInfo.InvariantCulture));
  }
}