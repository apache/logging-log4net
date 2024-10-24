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
  private static readonly DateTime _localTime
    = new(2000, 7, 1, 0, 0, 0, 0, CultureInfo.InvariantCulture.Calendar, DateTimeKind.Local);

  [Test]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2300:Do not use insecure deserializer BinaryFormatter")]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2301:Do not use insecure deserializer BinaryFormatter")]
  public void SerializeDeserialize_BinaryFormatter()
  {
    Utils.InconclusiveOnMono();
    DateTime timestamp = _localTime.ToUniversalTime();
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

    Assert.That(ev2.LoggerName, Is.EqualTo("aLogger"));
    Assert.That(ev2.Level, Is.EqualTo(Level.Log4Net_Debug));
    Assert.That(ev2.MessageObject, Is.Null);
    Assert.That(ev2.RenderedMessage, Is.EqualTo("aMessage"));
    Assert.That(ev2.ThreadName, Is.EqualTo("aThread"));
    Assert.That(ev2.TimeStampUtc, Is.EqualTo(timestamp));
    Assert.That(ev2.LocationInfo, Is.Not.Null);
    Assert.That(ev2.LocationInfo!.ClassName, Is.EqualTo("System.RuntimeMethodHandle"));
    Assert.That(ev2.LocationInfo!.MethodName, Is.EqualTo("InvokeMethod"));
    Assert.That(ev2.LocationInfo!.FileName, Is.Null);
    Assert.That(ev2.LocationInfo!.LineNumber, Is.EqualTo("0"));
    Assert.That(ev2.UserName, Is.EqualTo("aUser"));
    Assert.That(ev2.Identity, Is.EqualTo("anIdentity"));
    Assert.That(ev2.ExceptionObject, Is.Null);
    Assert.That(ev2.GetExceptionString(), Is.EqualTo("anException"));
    Assert.That(ev2.Domain, Is.EqualTo("aDomain"));
    Assert.That(ev.Properties, Has.Count.EqualTo(1));
    Assert.That(ev2.Properties["foo"], Is.EqualTo("bar"));
  }

  /// <summary>
  /// Loads and validates the cached serialized v2 event data from the log4net2-SerializeEvent directory.
  /// </summary>
  [Test]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2300:Do not use insecure deserializer BinaryFormatter")]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2301:Do not use insecure deserializer BinaryFormatter")]
  public void DeserializeV2()
  {
    Utils.InconclusiveOnMono();
    const string datPath = @"..\..\..\..\integration-testing\log4net2-SerializeEvent\SerializeV2Event.dat";
    using Stream stream = File.OpenRead(datPath);
    BinaryFormatter formatter = new();
    LoggingEvent ev = (LoggingEvent)formatter.Deserialize(stream);
    Assert.That(ev, Is.Not.Null);

    Assert.That(ev!.LoggerName, Is.EqualTo("aLogger"));
    Assert.That(ev.Level, Is.EqualTo(Level.Log4Net_Debug));
    Assert.That(ev.MessageObject, Is.Null);
    Assert.That(ev.RenderedMessage, Is.EqualTo("aMessage"));
    Assert.That(ev.ThreadName, Is.EqualTo("aThread"));
    Assert.That(ev.LocationInfo, Is.Not.Null);
    Assert.That(ev.LocationInfo!.ClassName, Is.EqualTo("?"));
    Assert.That(ev.LocationInfo!.MethodName, Is.EqualTo("?"));
    Assert.That(ev.LocationInfo!.FileName, Is.EqualTo("?"));
    Assert.That(ev.LocationInfo!.LineNumber, Is.EqualTo("?"));
    Assert.That(ev.UserName, Is.EqualTo("aUser"));
    Assert.That(ev.Identity, Is.EqualTo("anIdentity"));
    Assert.That(ev.ExceptionObject, Is.Null);
    Assert.That(ev.GetExceptionString(), Is.EqualTo("anException"));
    Assert.That(ev.Domain, Is.EqualTo("aDomain"));
    Assert.That(ev.Properties, Has.Count.EqualTo(1));
    Assert.That(ev.Properties["foo"], Is.EqualTo("bar"));
    Assert.That(ev.TimeStampUtc, Is.EqualTo(_localTime.ToUniversalTime()));
  }
#endif // NET462_OR_GREATER

  /// <summary>
  /// Tests <see cref="LoggingEvent.ReviseThreadName"/>
  /// </summary>
  [Test]
  public void ReviseThreadNameTest()
  {
    Assert.That(ReviseThreadName("PoolBoy"), Is.EqualTo("PoolBoy"));
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
      => Assert.That(SystemInfo.CurrentThreadId.ToString(CultureInfo.InvariantCulture), Is.EqualTo(name));
  }
}