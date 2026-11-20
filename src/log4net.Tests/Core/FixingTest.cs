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
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using log4net.Tests.Appender;
using log4net.Util;

using NUnit.Framework;

using static log4net.Tests.TestThreads;

namespace log4net.Tests.Core;

/// <summary>
/// Tests for <see cref="LoggingEvent.Fix"/> and the fields it captures.
/// </summary>
[TestFixture]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types")]
public class FixingTest
{
  /// <summary>
  /// The name of the repository the events under test belong to.
  /// </summary>
  private const string TestRepository = "Test Repository";

  /// <summary>
  /// Creates the repository the events under test belong to, and names the thread, so that
  /// <see cref="LoggingEvent.ThreadName"/> has something stable to capture.
  /// </summary>
  [OneTimeSetUp]
  public void CreateRepository()
  {
    bool exists = false;
    Repository.ILoggerRepository[] repositories = LogManager.GetAllRepositories();
    if (repositories is not null)
    {
      foreach (Repository.ILoggerRepository r in repositories)
      {
        if (r.Name == TestRepository)
        {
          exists = true;
          break;
        }
      }
    }
    if (!exists)
    {
      LogManager.CreateRepository(TestRepository);
    }

    // write-once
    if (Thread.CurrentThread.Name is null)
    {
      Thread.CurrentThread.Name = "Log4Net Test thread";
    }
  }

  /// <summary>
  /// <see cref="FixFlags.All"/> has to contain every other flag, so that fixing everything does not
  /// quietly leave a field out when a new flag is added.
  /// </summary>
  [Test]
  public void AllContainsEveryFlag()
  {
    // Arrange
    // Act
    var allFlags = Enum.GetValues(typeof(FixFlags)).Cast<FixFlags>()
      .Except([FixFlags.None])
      .ToArray();
    // Assert
    foreach (var flag in allFlags)
    {
      Assert.That(FixFlags.All & flag, Is.EqualTo(flag), $"FixFlags.All does not contain {flag}");
    }
  }

  /// <summary>
  /// A newly created event has nothing fixed yet.
  /// </summary>
  [Test]
  public void TestUnfixedValues()
  {
    LoggingEventData loggingEventData = BuildStandardEventData();

    // LoggingEvents occur at distinct points in time
    LoggingEvent loggingEvent = new(
      loggingEventData.LocationInfo?.GetType(),
      LogManager.GetRepository(TestRepository),
      loggingEventData.LoggerName,
      loggingEventData.Level,
      loggingEventData.Message,
      new("This is the exception"));

    AssertExpectedLoggingEvent(loggingEvent, loggingEventData);

    Assert.That(loggingEvent.Fix, Is.EqualTo(FixFlags.None), "Fixed Fields is incorrect");
  }

  /// <summary>
  /// Fixing with <see cref="FixFlags.All"/> reports every field as fixed.
  /// </summary>
  [Test]
  public void TestAllFixedValues()
  {
    LoggingEventData loggingEventData = BuildStandardEventData();

    // LoggingEvents occur at distinct points in time
    LoggingEvent loggingEvent = new(
      loggingEventData.LocationInfo?.GetType(),
      LogManager.GetRepository(TestRepository),
      loggingEventData.LoggerName,
      loggingEventData.Level,
      loggingEventData.Message,
      new("This is the exception"));

    AssertExpectedLoggingEvent(loggingEvent, loggingEventData);

    loggingEvent.Fix = FixFlags.All;

    Assert.That(loggingEvent.Fix, Is.EqualTo(FixFlags.LocationInfo | FixFlags.UserName | FixFlags.Identity | FixFlags.Partial | FixFlags.Message | FixFlags.ThreadName | FixFlags.Exception | FixFlags.Domain | FixFlags.Properties), "Fixed Fields is incorrect");
  }

  /// <summary>
  /// Fixing with <see cref="FixFlags.None"/> leaves the event unfixed.
  /// </summary>
  [Test]
  public void TestNoFixedValues()
  {
    LoggingEventData loggingEventData = BuildStandardEventData();

    // LoggingEvents occur at distinct points in time
    LoggingEvent loggingEvent = new(
      loggingEventData.LocationInfo?.GetType(),
      LogManager.GetRepository(TestRepository),
      loggingEventData.LoggerName,
      loggingEventData.Level,
      loggingEventData.Message,
      new("This is the exception"));

    AssertExpectedLoggingEvent(loggingEvent, loggingEventData);

    loggingEvent.Fix = FixFlags.None;

    Assert.That(loggingEvent.Fix, Is.EqualTo(FixFlags.None), "Fixed Fields is incorrect");
  }

  /// <summary>
  /// Without impersonation the user name is the process identity, which is the same whichever
  /// thread asks, so resolving it after the event has been fixed still gives the right answer. An
  /// event fixed without <see cref="FixFlags.UserName"/> must therefore keep reporting it.
  /// </summary>
  [Test]
  public void UserNameIsStillResolvedAfterFixingWithoutImpersonation()
  {
    string expected = CreateEvent().UserName;
    LoggingEvent loggingEvent = CreateEvent();

    // Partial deliberately leaves UserName out, being the documented setting for avoiding its cost.
    loggingEvent.Fix = FixFlags.Partial;

    Assert.That(loggingEvent.Fix & FixFlags.UserName, Is.EqualTo(FixFlags.None));
    Assert.That(loggingEvent.UserName, Is.EqualTo(expected));
    Assert.That(loggingEvent.UserName, Is.Not.EqualTo(SystemInfo.NotAvailableText));
  }

  /// <summary>
  /// Fixing with <see cref="FixFlags.None"/> skips the whole of FixVolatileData but still locks the
  /// cache, so the user name has to survive that path too.
  /// </summary>
  [Test]
  public void UserNameIsStillResolvedAfterFixingNothing()
  {
    string expected = CreateEvent().UserName;
    LoggingEvent loggingEvent = CreateEvent();

    loggingEvent.Fix = FixFlags.None;

    Assert.That(loggingEvent.UserName, Is.EqualTo(expected));
  }

  /// <summary>
  /// Fixing an event with UserName still has to capture it, on the thread that logged the event.
  /// </summary>
  [Test]
  public void UserNameIsCapturedWhenItIsFixed()
  {
    LoggingEvent loggingEvent = CreateEvent();

    string expected = loggingEvent.UserName;
    loggingEvent.Fix = FixFlags.All;

    Assert.That(loggingEvent.UserName, Is.EqualTo(expected));
    Assert.That(loggingEvent.UserName, Is.Not.EqualTo(SystemInfo.NotAvailableText));
  }

  /// <summary>
  /// A redundant Fix must not reopen the cache: a thread reading a field the event never captured
  /// would otherwise store its own principal in it. 500000 calls, the defect hit within 63000.
  /// </summary>
  [Test]
  public void ARedundantFixDoesNotLetAnotherThreadCacheItsIdentity()
  {
    LoggingEvent loggingEvent = CreateEvent();
    // Partial leaves Identity out, so it stays null unless the cache reopens.
    loggingEvent.Fix = FixFlags.Partial;

    bool stop = false;
    long spins = 0;
    TaskCompletionSource<bool> running = new();
    Thread reader = new(() =>
    {
      Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity("Intruder"), null);
      running.TrySetResult(true);
      while (!Volatile.Read(ref stop) && loggingEvent.Identity is null)
      {
        Interlocked.Increment(ref spins);
      }
    })
    { IsBackground = true };
    reader.Start();
    Assert.That(running.Task.Wait(TimeSpan.FromSeconds(10)), Is.True);

    for (int i = 0; i < 500_000; i++)
    {
      loggingEvent.Fix = FixFlags.Partial;
    }
    Volatile.Write(ref stop, true);
    Assert.That(reader.Join(TimeSpan.FromSeconds(10)), Is.True);

    // A reader that never got scheduled would pass the identity assertion for the wrong reason.
    Assert.That(Interlocked.Read(ref spins), Is.GreaterThan(0));
    Assert.That(loggingEvent.Identity, Is.Null);
  }

  /// <summary>
  /// A fix that has something to do must not reopen the cache either, or a reading thread caches its own principal.
  /// </summary>
  [Test]
  public void AFixWithSomethingToDoDoesNotLetAnotherThreadCacheItsIdentity()
  {
    LoggingEvent? current = null;
    bool stop = false;
    string? leaked = null;
    long spins = 0;
    TaskCompletionSource<bool> running = new();
    Thread reader = new(() =>
    {
      Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity("Intruder"), null);
      running.TrySetResult(true);
      while (!Volatile.Read(ref stop) && leaked is null)
      {
        leaked = Volatile.Read(ref current)?.Identity;
        Interlocked.Increment(ref spins);
      }
    })
    { IsBackground = true };
    reader.Start();
    Assert.That(running.Task.Wait(TimeSpan.FromSeconds(10)), Is.True);

    for (int i = 0; i < 200_000 && Volatile.Read(ref leaked) is null; i++)
    {
      LoggingEvent loggingEvent = CreateEvent();
      loggingEvent.Fix = FixFlags.None;
      Volatile.Write(ref current, loggingEvent);
      loggingEvent.Fix = FixFlags.Message;
    }
    Volatile.Write(ref stop, true);
    Assert.That(reader.Join(TimeSpan.FromSeconds(10)), Is.True);

    Assert.That(Interlocked.Read(ref spins), Is.GreaterThan(0));
    Assert.That(leaked, Is.Null);
  }

  /// <summary>
  /// A buffering appender's fix used to lock the event for every later appender of the same call.
  /// </summary>
  [TestCase(FixFlags.None)]
  [TestCase(FixFlags.Message)]
  [TestCase(FixFlags.Partial)]
  public void ALaterAppenderOfTheSameCallSeesEveryField(FixFlags bufferFix)
  {
    LoggingEvent? seen = null;
    string? identity = null, threadName = null, domain = null, message = null, exception = null;
    LocationInfo? location = null;
    ILogger logger = CreateLogger(CreateBuffer(bufferFix), new Probe(e =>
    {
      seen = e;
      identity = e.Identity;
      threadName = e.ThreadName;
      domain = e.Domain;
      message = e.RenderedMessage;
      exception = e.GetExceptionString();
      location = e.LocationInformation;
    }));

    RunAs("Alice", () => Log(logger));

    Assert.That(seen, Is.Not.Null);
    Assert.That(identity, Is.EqualTo("Alice"));
    Assert.That(threadName, Is.EqualTo("Alice"));
    Assert.That(domain, Is.Not.Null.And.Not.Empty);
    Assert.That(message, Is.EqualTo("message"));
    Assert.That(exception, Does.Contain("boom"));
    Assert.That(location, Is.Not.Null);
  }

  /// <summary>
  /// A value a later appender read lands in the event, so another thread reads the logging thread's value.
  /// </summary>
  [Test]
  public void AValueReadDuringDeliveryTravelsInTheEvent()
  {
    Probe probe = new(e => _ = e.Identity);
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.Partial), probe);
    RunAs("Alice", () => Log(logger));

    string? identity = "unset";
    RunAs("Intruder", () => identity = probe.Events[0].Identity);

    Assert.That(identity, Is.EqualTo("Alice"));
  }

  /// <summary>
  /// After the delivery the lock holds, on the logging thread under another principal and on any other thread.
  /// </summary>
  [Test]
  public void TheLockHoldsAfterTheDelivery()
  {
    Probe probe = new();
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.Partial), probe);
    string? sameThread = "unset";
    LocationInfo? location = null;
    RunAs("Alice", () =>
    {
      Log(logger);
      Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity("Mallory"), null);
      sameThread = probe.Events[0].Identity;
      location = probe.Events[0].LocationInformation;
    });
    string? otherThread = "unset";
    RunAs("Intruder", () => otherThread = probe.Events[0].Identity);

    Assert.That(sameThread, Is.Null);
    Assert.That(location, Is.Null);
    Assert.That(otherThread, Is.Null);
  }

  /// <summary>
  /// Only the delivering thread may fill the cache while the event is delivered.
  /// </summary>
  [Test]
  public void AnotherThreadReadingDuringTheDeliveryGetsNull()
  {
    string? other = "unset", own = "unset";
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.Partial), new Probe(e =>
    {
      RunAs("Intruder", () => other = e.Identity);
      own = e.Identity;
    }));

    RunAs("Alice", () => Log(logger));

    Assert.That(other, Is.Null);
    Assert.That(own, Is.EqualTo("Alice"));
  }

  /// <summary>
  /// A fixed event logged again on another thread does not reopen the cache for that thread.
  /// </summary>
  [Test]
  public void AFixedEventLoggedAgainOnAnotherThreadStaysLocked()
  {
    Probe probe = new();
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.Partial), probe);
    RunAs("Alice", () => Log(logger));
    string? identity = "unset";
    ILogger second = CreateLogger(new Probe(e => identity = e.Identity));

    RunAs("Bob", () => second.Log(probe.Events[0]));

    Assert.That(identity, Is.Null);
  }

  /// <summary>
  /// Logging from inside an appender, a new event or the same one again, does not end the outer delivery.
  /// </summary>
  [TestCase(true)]
  [TestCase(false)]
  public void NestedLoggingDoesNotEndTheOuterDelivery(bool sameEvent)
  {
    ILogger logger = null!;
    bool nested = false, inNested = false;
    string? identity = "unset";
    Probe inner = new(e =>
    {
      if (!nested)
      {
        nested = inNested = true;
        if (sameEvent)
        {
          logger.Log(e);
        }
        else
        {
          Log(logger, "nested");
        }
        inNested = false;
      }
    });
    Probe last = new(e =>
    {
      if (!inNested && e.RenderedMessage == "message")
      {
        identity = e.Identity;
      }
    });
    logger = CreateLogger(CreateBuffer(FixFlags.Partial), inner, last);

    RunAs("Alice", () => Log(logger));

    Assert.That(identity, Is.EqualTo("Alice"));
  }

  /// <summary>
  /// Thread B logs the event while thread A still delivers it and ends after A: the event stays locked.
  /// </summary>
  [Test]
  public void AnOverlappingDeliveryOnAnotherThreadLeavesTheEventLocked()
  {
    using ManualResetEventSlim inside = new();
    using ManualResetEventSlim ended = new();
    Thread? other = null;
    ILogger second = CreateLogger(new Probe(_ =>
    {
      inside.Set();
      ended.Wait(TimeSpan.FromSeconds(10));
    }));
    Probe probe = new(e =>
    {
      other = new(() => second.Log(e)) { IsBackground = true };
      other.Start();
      Assert.That(inside.Wait(TimeSpan.FromSeconds(10)), Is.True);
    });
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.Partial), probe);
    string? identity = "unset";

    RunAs("Alice", () =>
    {
      Log(logger);
      ended.Set();
      Assert.That(other!.Join(TimeSpan.FromSeconds(10)), Is.True);
      Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity("Intruder"), null);
      identity = probe.Events[0].Identity;
    });

    Assert.That(identity, Is.Null);
  }

  /// <summary>
  /// A full buffer flushes during the delivery of the newest event: only that one gets the identity.
  /// </summary>
  [Test]
  public void AFlushAtAFullBufferGivesOnlyTheCurrentEventItsIdentity()
  {
    List<string?> identities = [];
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.Partial, 2, new Probe(e => identities.Add(e.Identity))));

    RunAs("Alice", () =>
    {
      Log(logger, "1");
      Log(logger, "2");
      Log(logger, "3");
    });

    Assert.That(identities, Is.EqualTo(new string?[] { null, null, "Alice" }));
  }

  /// <summary>
  /// Another thread's fix that starts during the delivery and ends after it leaves the event locked.
  /// </summary>
  [Test]
  public void AnotherThreadFixingAcrossTheEndOfTheDeliveryLeavesTheEventLocked()
  {
    using ManualResetEventSlim inFix = new();
    using ManualResetEventSlim ended = new();
    Thread? other = null;
    Probe probe = new(e =>
    {
      other = new(() =>
      {
        Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity("Bob"), null);
        e.Fix = FixFlags.All;
      })
      { IsBackground = true };
      other.Start();
      Assert.That(inFix.Wait(TimeSpan.FromSeconds(10)), Is.True);
    });
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.None), probe);
    string? identity = "unset";

    RunAs("Alice", () =>
    {
      // The fix renders the message on the other thread and blocks there until the delivery ended.
      logger.Log(typeof(FixingTest), Level.Info, new BlockingMessage(inFix, ended), null);
      ended.Set();
      Assert.That(other!.Join(TimeSpan.FromSeconds(10)), Is.True);
      Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity("Intruder"), null);
      identity = probe.Events[0].Identity;
    });

    Assert.That(identity, Is.Null);
  }

  /// <summary>
  /// A fix after the delivery, on a flushing thread, captures the thread-independent fields only.
  /// </summary>
  [Test]
  public void AFixOnAnotherThreadAfterTheDeliveryCapturesOnlyThreadIndependentFields()
  {
    Probe probe = new();
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.None), probe);
    RunAs("Alice", () => Log(logger));
    LoggingEventData data = default;
    FixFlags fixFlags = FixFlags.None;

    RunAs("Bob", () =>
    {
      ThreadContext.Properties["who"] = "flusher";
      probe.Events[0].Fix = FixFlags.All;
      data = probe.Events[0].GetLoggingEventData(FixFlags.None);
      fixFlags = probe.Events[0].Fix;
      ThreadContext.Properties.Remove("who");
    });

    Assert.That(data.Message, Is.EqualTo("message"));
    Assert.That(data.ExceptionString, Does.Contain("boom"));
    Assert.That(data.Domain, Is.Not.Null.And.Not.Empty);
    Assert.That(data.ThreadName, Is.Null);
    Assert.That(data.Identity, Is.Null);
    Assert.That(data.LocationInfo, Is.Null);
    Assert.That(data.Properties, Is.Not.Null);
    Assert.That(data.Properties!.Contains("who"), Is.False);
    Assert.That(fixFlags, Is.EqualTo(FixFlags.Message | FixFlags.UserName | FixFlags.Domain | FixFlags.Exception | FixFlags.Properties));
  }

  /// <summary>
  /// A fix during the delivery captures everything, the logging thread's properties included.
  /// </summary>
  [Test]
  public void AFixDuringTheDeliveryCapturesEverything()
  {
    Probe probe = new(e => e.Fix = FixFlags.All);
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.None), probe);
    RunAs("Alice", () =>
    {
      ThreadContext.Properties["who"] = "logger";
      Log(logger);
      ThreadContext.Properties.Remove("who");
    });
    LoggingEventData data = default;

    RunAs("Bob", () => data = probe.Events[0].GetLoggingEventData(FixFlags.None));

    Assert.That(data.Identity, Is.EqualTo("Alice"));
    Assert.That(data.ThreadName, Is.EqualTo("Alice"));
    Assert.That(data.LocationInfo, Is.Not.Null);
    Assert.That(data.Properties?["who"], Is.EqualTo("logger"));
  }

  /// <summary>
  /// Read late without impersonation, the user name is the process user, the same on every thread.
  /// </summary>
  [Test]
  public void TheUserNameFixedOnAnotherThreadIsTheProcessUser()
  {
    Probe probe = new();
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.Partial), probe);
    RunAs("Alice", () => Log(logger));
    string? userName = null;

    RunAs("Bob", () =>
    {
      probe.Events[0].Fix = FixFlags.All;
      userName = probe.Events[0].UserName;
    });

    Assert.That(userName, Is.EqualTo(CreateEvent().UserName));
  }

  /// <summary>
  /// Three users log into a two-event buffer in front of an appender fixing everything: the flush
  /// on the third user's thread used to stamp its identity and call site on the older events.
  /// </summary>
  [Test]
  public void AFlushDoesNotStampTheFlushingThreadOnOlderEvents()
  {
    MemoryAppender memory = new();
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.Partial, 2, memory));

    foreach (string user in new[] { "user1", "user2", "user3" })
    {
      RunAs(user, () => Log(logger, user));
    }

    LoggingEvent[] events = memory.GetEvents();
    Assert.That(events.Select(e => e.Identity), Is.EqualTo(new string?[] { null, null, "user3" }));
    Assert.That(events.Select(e => e.LocationInformation is null), Is.EqualTo(new[] { true, true, false }));
  }

  /// <summary>
  /// A buffer without properties used to show the flushing thread's context to the target.
  /// </summary>
  [Test]
  public void ATargetBehindABufferWithoutPropertiesDoesNotSeeTheFlushingThreadsContext()
  {
    List<object?> seen = [];
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.None, 2, new Probe(e => seen.Add(e.LookupProperty("who")))));

    LogAsUsers(logger);

    Assert.That(seen, Is.EqualTo(new object?[] { null, null, "user3" }));
  }

  /// <summary>
  /// <see cref="FixFlags.Mdc"/> and <see cref="FixFlags.Ndc"/> fix the properties, so each event keeps its own value.
  /// </summary>
  [TestCase(FixFlags.Mdc)]
  [TestCase(FixFlags.Ndc)]
  public void MdcAndNdcFixTheProperties(FixFlags bufferFix)
  {
    List<object?> seen = [];
    ILogger logger = CreateLogger(CreateBuffer(bufferFix, 2, new Probe(e => seen.Add(e.LookupProperty("who")))));

    LogAsUsers(logger);

    Assert.That(seen, Is.EqualTo(new object?[] { "user1", "user2", "user3" }));
  }

  /// <summary>
  /// Off the delivering thread the event's own and the global properties stay visible.
  /// </summary>
  [Test]
  public void EventAndGlobalPropertiesStayVisibleOffTheDeliveringThread()
  {
    Probe probe = new();
    ILogger logger = CreateLogger(new Probe(e => e.Properties["own"] = "mine"), CreateBuffer(FixFlags.None), probe);
    GlobalContext.Properties["global"] = "everyone";
    try
    {
      RunAs("Alice", () => Log(logger));
      object? own = null, global = null;

      RunAs("Bob", () =>
      {
        own = probe.Events[0].LookupProperty("own");
        global = probe.Events[0].LookupProperty("global");
      });

      Assert.That(own, Is.EqualTo("mine"));
      Assert.That(global, Is.EqualTo("everyone"));
    }
    finally
    {
      GlobalContext.Properties.Remove("global");
    }
  }

  /// <summary>
  /// A later appender on the delivering thread still sees the thread and logical-thread properties.
  /// </summary>
  [Test]
  public void ALaterAppenderDuringTheDeliverySeesTheThreadProperties()
  {
    object? who = null, logicalWho = null;
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.None), new Probe(e =>
    {
      who = e.LookupProperty("who");
      logicalWho = e.LookupProperty("logical-who");
    }));

    RunAs("Alice", () =>
    {
      ThreadContext.Properties["who"] = "alice";
      LogicalThreadContext.Properties["logical-who"] = "alice";
      Log(logger);
    });

    Assert.That(who, Is.EqualTo("alice"));
    Assert.That(logicalWho, Is.EqualTo("alice"));
  }

  /// <summary>
  /// A key written through <see cref="LoggingEvent.GetProperties"/> off the delivering thread reads back.
  /// </summary>
  [Test]
  public void AKeyWrittenThroughGetPropertiesOffTheDeliveringThreadReadsBack()
  {
    StringAppender xml = new() { Layout = new XmlLayoutSchemaLog4J() };
    Probe probe = new();
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.None, 2, xml, probe));
    LogAsUsers(logger);
    object? value = null;

    RunAs("Bob", () =>
    {
      probe.Events[0].GetProperties()["written"] = "back";
      value = probe.Events[0].GetProperties()["written"];
    });

    Assert.That(value, Is.EqualTo("back"));
    Assert.That(xml.GetString().Split(["log4jmachinename"], StringSplitOptions.None), Has.Length.EqualTo(4));
  }

  /// <summary>
  /// Off the delivering thread the properties still hold the fixed identity and user name keys.
  /// </summary>
  [Test]
  public void TheIdentityAndUserNameKeysStayVisibleOffTheDeliveringThread()
  {
    Probe probe = new();
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.Identity | FixFlags.UserName | FixFlags.Message), probe);
    RunAs("Alice", () => Log(logger));
    PropertiesDictionary? properties = null;

    RunAs("Bob", () => properties = probe.Events[0].GetProperties());

    Assert.That(properties![LoggingEvent.IdentityProperty], Is.EqualTo("Alice"));
    Assert.That(properties.Contains(LoggingEvent.UserNameProperty), Is.True);
  }

  /// <summary>
  /// A later fix off the delivering thread keeps the event's own and the global properties, not its own context.
  /// </summary>
  [TestCase(FixFlags.Mdc)]
  [TestCase(FixFlags.All)]
  public void ALaterFixOffTheDeliveringThreadKeepsEventAndGlobalProperties(FixFlags flags)
  {
    Probe probe = new();
    ILogger logger = CreateLogger(new Probe(e => e.Properties["own"] = "mine"), CreateBuffer(FixFlags.None), probe);
    GlobalContext.Properties["global"] = "everyone";
    try
    {
      RunAs("Alice", () => Log(logger));
      LoggingEventData data = default;
      FixFlags fixFlags = FixFlags.None;

      RunAs("Bob", () =>
      {
        ThreadContext.Properties["who"] = "flusher";
        probe.Events[0].Fix = flags;
        data = probe.Events[0].GetLoggingEventData(FixFlags.None);
        fixFlags = probe.Events[0].Fix;
      });

      Assert.That(fixFlags & FixFlags.Properties, Is.EqualTo(FixFlags.Properties));
      Assert.That(data.Properties?["own"], Is.EqualTo("mine"));
      Assert.That(data.Properties?["global"], Is.EqualTo("everyone"));
      Assert.That(data.Properties?.Contains("who"), Is.False);
    }
    finally
    {
      GlobalContext.Properties.Remove("global");
    }
  }

  /// <summary>
  /// A property filter on the target used to match the flushing thread's value for every buffered event.
  /// </summary>
  [Test]
  public void APropertyFilterBehindABufferDoesNotMatchTheFlushingThreadsValue()
  {
    List<string?> accepted = [];
    Probe target = new(e => accepted.Add(e.RenderedMessage));
    target.AddFilter(new PropertyFilter { Key = "who", StringToMatch = "user3" });
    target.AddFilter(new DenyAllFilter());
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.None, 2, target));

    LogAsUsers(logger);

    Assert.That(accepted, Is.EqualTo(new[] { "user3" }));
  }

  /// <summary>
  /// Another thread's fix during the delivery must not take the properties away from the delivering thread.
  /// </summary>
  [Test]
  public void AnotherThreadsFixDuringTheDeliveryLeavesThePropertiesToTheDeliveringThread()
  {
    object? who = null;
    LoggingEventData data = default;
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.None), new Probe(e =>
    {
      Thread other = new(() =>
      {
        ThreadContext.Properties["who"] = "bob";
        e.Fix = FixFlags.All;
      })
      { IsBackground = true };
      other.Start();
      Assert.That(other.Join(TimeSpan.FromSeconds(10)), Is.True);
    }), new Probe(e =>
    {
      who = e.LookupProperty("who");
      e.Fix = FixFlags.All;
      data = e.GetLoggingEventData(FixFlags.None);
    }));

    RunAs("Alice", () =>
    {
      ThreadContext.Properties["who"] = "alice";
      Log(logger);
    });

    Assert.That(who, Is.EqualTo("alice"));
    Assert.That(data.Properties?["who"], Is.EqualTo("alice"));
  }

  /// <summary>
  /// The off-thread snapshot resolves an <see cref="IFixingRequired"/> value and takes no stack of the fixing thread.
  /// </summary>
  [Test]
  public void TheOffThreadSnapshotResolvesFixingRequiredValuesAndTakesNoThreadStack()
  {
    Probe probe = new();
    ILogger logger = CreateLogger(new Probe(e => e.Properties["own"] = new FixedOnRequest()), CreateBuffer(FixFlags.None), probe);
    RunAs("Alice", () => Log(logger));
    LoggingEventData data = default;

    RunAs("Bob", () =>
    {
      using (ThreadContext.Stacks["NDC"].Push("flusher"))
      {
        probe.Events[0].Fix = FixFlags.All;
      }
      data = probe.Events[0].GetLoggingEventData(FixFlags.None);
    });

    Assert.That(data.Properties?["own"], Is.EqualTo("fixed"));
    Assert.That(data.Properties?.Contains("NDC"), Is.False);
  }

  /// <summary>
  /// A key written through <see cref="LoggingEvent.GetProperties"/> before an off-thread fix lands in the snapshot.
  /// </summary>
  [Test]
  public void AKeyWrittenThroughGetPropertiesBeforeAnOffThreadFixIsKept()
  {
    Probe probe = new();
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.None), probe);
    RunAs("Alice", () => Log(logger));
    LoggingEventData data = default;

    RunAs("Bob", () =>
    {
      probe.Events[0].GetProperties()["written"] = "before";
      probe.Events[0].Fix = FixFlags.All;
      data = probe.Events[0].GetLoggingEventData(FixFlags.None);
    });

    Assert.That(data.Properties?["written"], Is.EqualTo("before"));
  }

  /// <summary>
  /// Once the logging call returned, an event fixed without properties no longer shows the logging thread's context.
  /// </summary>
  [Test]
  public void AfterTheLoggingCallTheLoggingThreadNoLongerSeesItsContextInAnEventFixedWithoutProperties()
  {
    MemoryAppender memory = new() { Fix = FixFlags.None };
    ILogger logger = CreateLogger(CreateBuffer(FixFlags.None), memory);
    object? who = "unset";

    RunAs("Alice", () =>
    {
      ThreadContext.Properties["who"] = "alice";
      Log(logger);
      who = memory.GetEvents()[0].LookupProperty("who");
    });

    Assert.That(who, Is.Null);
  }

  /// <summary>
  /// Logs one event each as user1, user2 and user3, each with its name in <c>who</c>.
  /// </summary>
  private static void LogAsUsers(ILogger logger)
  {
    foreach (string user in new[] { "user1", "user2", "user3" })
    {
      RunAs(user, () =>
      {
        ThreadContext.Properties["who"] = user;
        Log(logger, user);
      });
    }
  }

  /// <summary>
  /// Logs <paramref name="message"/> with an exception through <paramref name="logger"/>.
  /// </summary>
  private static void Log(ILogger logger, string message = "message")
    => logger.Log(typeof(FixingTest), Level.Info, message, new InvalidOperationException("boom"));

  /// <summary>
  /// Creates a repository of its own whose root holds <paramref name="appenders"/> in order.
  /// </summary>
  private static ILogger CreateLogger(params IAppender[] appenders)
  {
    Repository.Hierarchy.Hierarchy hierarchy = (Repository.Hierarchy.Hierarchy)LogManager.CreateRepository(Guid.NewGuid().ToString());
    foreach (IAppender appender in appenders)
    {
      hierarchy.Root.AddAppender(appender);
    }
    hierarchy.Configured = true;
    return hierarchy.GetLogger(nameof(FixingTest));
  }

  /// <summary>
  /// Creates a non-lossy buffer that fixes <paramref name="fix"/> and forwards to <paramref name="targets"/>.
  /// </summary>
  private static BufferingForwardingAppender CreateBuffer(FixFlags fix, int size = 10, params IAppender[] targets)
  {
    BufferingForwardingAppender buffer = new() { Fix = fix, BufferSize = size };
    foreach (IAppender target in targets)
    {
      buffer.AddAppender(target);
    }
    buffer.ActivateOptions();
    return buffer;
  }

  /// <summary>Resolves to "fixed" when the event is fixed.</summary>
  private sealed class FixedOnRequest : IFixingRequired
  {
    /// <inheritdoc/>
    public object? GetFixedObject() => "fixed";
  }

  /// <summary>A message whose rendering signals <paramref name="rendering"/> and waits for <paramref name="release"/>.</summary>
  private sealed class BlockingMessage(ManualResetEventSlim rendering, ManualResetEventSlim release)
  {
    /// <inheritdoc/>
    public override string ToString()
    {
      rendering.Set();
      release.Wait(TimeSpan.FromSeconds(10));
      return "blocking";
    }
  }

  /// <summary>Keeps every event it gets and runs <paramref name="onAppend"/> on it.</summary>
  private sealed class Probe(Action<LoggingEvent>? onAppend = null) : AppenderSkeleton
  {
    internal List<LoggingEvent> Events { get; } = [];

    /// <inheritdoc/>
    protected override void Append(LoggingEvent loggingEvent)
    {
      Events.Add(loggingEvent);
      onAppend?.Invoke(loggingEvent);
    }
  }

  /// <summary>
  /// Creates an event in the test repository.
  /// </summary>
  /// <returns>A new, unfixed event.</returns>
  private static LoggingEvent CreateEvent()
    => new(typeof(FixingTest),
      LogManager.GetRepository(TestRepository),
      typeof(FixingTest).FullName,
      Level.Warn,
      "Logging event works",
      null);

  /// <summary>
  /// Builds the event data the tests compare against.
  /// </summary>
  /// <returns>Event data with every field set to a known value.</returns>
  private static LoggingEventData BuildStandardEventData()
  {
    LoggingEventData loggingEventData = new()
    {
      LoggerName = typeof(FixingTest).FullName,
      Level = Level.Warn,
      Message = "Logging event works",
      Domain = "ReallySimpleApp",
      LocationInfo = new(nameof(FixingTest), "Main", "Class1.cs", "29"), //Completely arbitary
      ThreadName = Thread.CurrentThread.Name,
      TimeStampUtc = DateTime.UtcNow.Date,
      ExceptionString = "Exception occured here",
      UserName = "TestUser"
    };
    return loggingEventData;
  }

  /// <summary>
  /// Asserts that <paramref name="loggingEvent"/> carries the values of
  /// <paramref name="loggingEventData"/>.
  /// </summary>
  /// <param name="loggingEvent">The event to check.</param>
  /// <param name="loggingEventData">The expected values.</param>
  private static void AssertExpectedLoggingEvent(LoggingEvent loggingEvent, LoggingEventData loggingEventData)
  {
    Assert.That(loggingEventData.Domain, Is.EqualTo("ReallySimpleApp"), "Domain is incorrect");
    Assert.That(loggingEvent.GetExceptionString(), Is.EqualTo("System.Exception: This is the exception"), "Exception is incorrect");
    Assert.That(loggingEventData.Identity, Is.Null, "Identity is incorrect");
    Assert.That(loggingEventData.Level, Is.EqualTo(Level.Warn), "Level is incorrect");
    Assert.That(loggingEvent.LocationInformation, Is.Not.Null);
    Assert.That(loggingEvent.LocationInformation.MethodName, Is.EqualTo("get_LocationInformation"), "Location Info is incorrect");
    Assert.That(loggingEventData.LoggerName, Is.EqualTo("log4net.Tests.Core.FixingTest"), "LoggerName is incorrect");
    Assert.That(loggingEvent.Repository, Is.EqualTo(LogManager.GetRepository(TestRepository)), "Repository is incorrect");
    Assert.That(loggingEventData.ThreadName, Is.EqualTo(Thread.CurrentThread.Name), "ThreadName is incorrect");
    Assert.That(loggingEventData.UserName, Is.EqualTo("TestUser"), "UserName is incorrect");
    Assert.That(loggingEvent.RenderedMessage, Is.EqualTo("Logging event works"), "Message is incorrect");
  }
}