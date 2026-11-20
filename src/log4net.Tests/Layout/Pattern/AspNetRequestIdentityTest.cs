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

// netstandard has no System.Web
#if NET462_OR_GREATER

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web;

using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Tests.Appender;
using log4net.Util;

using NUnit.Framework;

using static log4net.Tests.TestThreads;

namespace log4net.Tests.Layout.Pattern;

/// <summary>
/// The request converters show only the values of the request an event was logged in: behind a
/// buffer flushed by another request they write <c>NOT AVAILABLE</c> instead of that request's values.
/// </summary>
[TestFixture]
[NonParallelizable]
public sealed class AspNetRequestIdentityTest
{
  private const string CaptureKey = "log4net:AspNetRequest";

  /// <summary>
  /// Parses a request converter once, which registers the capture before any event is created.
  /// </summary>
  [OneTimeSetUp]
  public void RegisterTheCapture() => _ = new PatternLayout("%aspnet-context{who}");

  /// <summary>Leaves no capture behind for later tests.</summary>
  [OneTimeTearDown]
  public void RemoveTheCapture() => GlobalContext.Properties.Remove(CaptureKey);

  /// <summary>Detaches the hand-built request from the thread again.</summary>
  [TearDown]
  public void DetachTheRequest() => HttpContext.Current = null;

  /// <summary>
  /// A buffered event written in its own request shows that request's values.
  /// </summary>
  [Test]
  public void ABufferedEventInItsOwnRequestShowsItsValues()
  {
    Buffer buffer = new();
    HttpContext.Current = CreateRequest("A");
    buffer.Log();

    Assert.That(buffer.Flush(), Is.EqualTo("A|"));
  }

  /// <summary>
  /// Behind a buffer flushed by another request the event used to show that request's values.
  /// </summary>
  [Test]
  public void ABufferFlushedByAnotherRequestShowsNotAvailable()
  {
    Buffer buffer = new();
    HttpContext.Current = CreateRequest("A");
    buffer.Log();

    HttpContext.Current = CreateRequest("B");

    Assert.That(buffer.Flush(), Is.EqualTo(SystemInfo.NotAvailableText + "|"));
  }

  /// <summary>
  /// A buffer whose fix leaves out the properties cannot tell the requests apart: it shows the flushing one.
  /// </summary>
  [Test]
  public void ABufferWithoutFixedPropertiesShowsTheFlushingRequest()
  {
    Buffer buffer = new(fix: FixFlags.Message);
    HttpContext.Current = CreateRequest("A");
    buffer.Log();

    HttpContext.Current = CreateRequest("B");

    Assert.That(buffer.Flush(), Is.EqualTo("B|"));
  }

  /// <summary>
  /// An event logged outside any request has no request values, not those of the one flushing it.
  /// </summary>
  [Test]
  public void AnEventLoggedOutsideARequestShowsNotAvailableInOne()
  {
    Buffer buffer = new();
    buffer.Log();

    HttpContext.Current = CreateRequest("B");

    Assert.That(buffer.Flush(), Is.EqualTo(SystemInfo.NotAvailableText + "|"));
  }

  /// <summary>
  /// An event that was never fixed is written in the call that logged it, so it renders live as before.
  /// </summary>
  [Test]
  public void AnUnfixedEventRendersLiveInItsRequest()
  {
    PatternLayout layout = new("%aspnet-context{who}");
    HttpContext.Current = CreateRequest("A");

    Assert.That(layout.Format(CreateEvent()), Is.EqualTo("A"));
  }

  /// <summary>
  /// The cache is application wide, so <c>%aspnet-cache</c> keeps showing it in another request.
  /// </summary>
  [Test]
  public void TheCacheStillShowsItsItemInAnotherRequest()
  {
    HttpRuntime.Cache["log4net-test"] = "cached";
    try
    {
      Buffer buffer = new("%aspnet-cache{log4net-test}|");
      HttpContext.Current = CreateRequest("A");
      buffer.Log();

      HttpContext.Current = CreateRequest("B");

      Assert.That(buffer.Flush(), Is.EqualTo("cached|"));
    }
    finally
    {
      HttpRuntime.Cache.Remove("log4net-test");
    }
  }

  /// <summary>
  /// The same request on another thread is still the event's request.
  /// </summary>
  [Test]
  public void TheSameRequestOnAnotherThreadShowsItsValues()
  {
    Buffer buffer = new();
    HttpContext request = CreateRequest("A");
    HttpContext.Current = request;
    buffer.Log();
    string? written = null;

    RunAs("worker", () =>
    {
      HttpContext.Current = request;
      written = buffer.Flush();
    });

    Assert.That(written, Is.EqualTo("A|"));
  }

  /// <summary>
  /// A buffered event must not keep its ended request alive.
  /// </summary>
  [Test]
  public void ABufferedEventDoesNotKeepItsEndedRequestAlive()
  {
    Buffer buffer = new();
    WeakReference request = LogInARequestThatEnds(buffer);

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    Assert.That(request.IsAlive, Is.False);
    GC.KeepAlive(buffer);
  }

  /// <summary>
  /// A deserialized event has lost its token, so it shows <c>NOT AVAILABLE</c> even in its request.
  /// </summary>
  [Test]
  public void ADeserializedEventHasNoTokenAndShowsNotAvailable()
  {
    HttpContext.Current = CreateRequest("A");
    LoggingEvent loggingEvent = CreateFixedEvent();

    LoggingEvent copy = RoundTrip(loggingEvent);

    Assert.That(copy.LookupProperty(CaptureKey), Is.Null);
    Assert.That(Render(copy), Is.EqualTo(SystemInfo.NotAvailableText));
  }

  /// <summary>
  /// A copy built from a fixed event's data keeps its token and so its request.
  /// </summary>
  [Test]
  public void ACopyOfAFixedEventKeepsItsRequest()
  {
    HttpContext.Current = CreateRequest("A");
    LoggingEvent copy = new(CreateFixedEvent().GetLoggingEventData());

    Assert.That(Render(copy), Is.EqualTo("A"));
    HttpContext.Current = CreateRequest("B");
    Assert.That(Render(copy), Is.EqualTo(SystemInfo.NotAvailableText));
  }

  /// <summary>
  /// An event built from <see cref="LoggingEventData"/> counts as fixed without a token: it shows
  /// <c>NOT AVAILABLE</c> even in the request it was built in.
  /// </summary>
  [Test]
  public void AnEventBuiltFromDataShowsNotAvailableInItsOwnRequest()
  {
    HttpContext.Current = CreateRequest("A");
    LoggingEvent loggingEvent = new(new LoggingEventData { LoggerName = "test", Level = Level.Info, Message = "built" });

    Assert.That(Render(loggingEvent), Is.EqualTo(SystemInfo.NotAvailableText));
  }

  /// <summary>
  /// The capture gives one token per request, none outside a request, and prints nothing.
  /// </summary>
  [Test]
  public void TheCaptureGivesOneTokenPerRequest()
  {
    _ = new PatternLayout("%aspnet-context{who}");
    IFixingRequired capture = (IFixingRequired)GlobalContext.Properties[CaptureKey]!;
    HttpContext requestA = CreateRequest("A");

    HttpContext.Current = requestA;
    object? first = capture.GetFixedObject();
    object? second = capture.GetFixedObject();
    HttpContext.Current = CreateRequest("B");
    object? other = capture.GetFixedObject();
    HttpContext.Current = null;
    object? outside = capture.GetFixedObject();

    Assert.That(first, Is.Not.Null.And.SameAs(second));
    Assert.That(other, Is.Not.Null.And.Not.SameAs(first));
    Assert.That(outside, Is.Null);
    Assert.That(capture.ToString(), Is.Empty);
    Assert.That(first!.ToString(), Is.Empty);
  }

  /// <summary>
  /// System.Web is not loaded by a process whose patterns use no <c>%aspnet-*</c> converter, and is
  /// by one that does. Each runs in a fresh application domain, the event through a buffer.
  /// </summary>
  [TestCase("%message", false)]
  [TestCase("%aspnet-context{who}", true)]
  public void SystemWebIsLoadedOnlyForAnAspNetPattern(string pattern, bool loaded)
    => Assert.That(InFreshDomain<DomainStateProbe, bool>(probe => probe.LogsAndReportsSystemWeb(pattern)), Is.EqualTo(loaded));

  /// <summary>
  /// The three request converters register the capture when they are created; the cache converter does not.
  /// </summary>
  [TestCase("%aspnet-cache", false)]
  [TestCase("%aspnet-context", true)]
  [TestCase("%aspnet-request", true)]
  [TestCase("%aspnet-session", true)]
  public void OnlyTheRequestConvertersRegisterTheCapture(string pattern, bool registered)
    => Assert.That(InFreshDomain<DomainStateProbe, bool>(probe => probe.ParsesAndReportsTheCapture(pattern)), Is.EqualTo(registered));

  /// <summary>
  /// An event fixed before any request converter was created carries no token: <c>NOT AVAILABLE</c>.
  /// </summary>
  [Test]
  public void AnEventFixedBeforeRegistrationShowsNotAvailable()
    => Assert.That(InFreshDomain<DomainRequestProbe, string>(probe => probe.FixesBeforeRegistrationAndRenders()),
      Is.EqualTo(SystemInfo.NotAvailableText));

  private static HttpContext CreateRequest(string who)
  {
    HttpContext httpContext = new(new HttpRequest(string.Empty, "http://localhost/", string.Empty),
      new HttpResponse(TextWriter.Null));
    httpContext.Items["who"] = who;
    return httpContext;
  }

  private static LoggingEvent CreateEvent()
    => new(typeof(AspNetRequestIdentityTest), null, "test", Level.Info, "message", null);

  private static LoggingEvent CreateFixedEvent()
  {
    LoggingEvent loggingEvent = CreateEvent();
    loggingEvent.Fix = FixFlags.All;
    return loggingEvent;
  }

  private static string Render(LoggingEvent loggingEvent)
    => new PatternLayout("%aspnet-context{who}").Format(loggingEvent);

  [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2300:Do not use insecure deserializer BinaryFormatter")]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2301:Do not use insecure deserializer BinaryFormatter")]
  private static LoggingEvent RoundTrip(LoggingEvent loggingEvent)
  {
    using MemoryStream stream = new();
    BinaryFormatter formatter = new();
    formatter.Serialize(stream, loggingEvent);
    stream.Position = 0;
    return (LoggingEvent)formatter.Deserialize(stream);
  }

  /// <summary>Logs in a request and ends it, so that only the buffered event could keep it alive.</summary>
  [MethodImpl(MethodImplOptions.NoInlining)]
  private static WeakReference LogInARequestThatEnds(Buffer buffer)
  {
    HttpContext request = CreateRequest("A");
    HttpContext.Current = request;
    buffer.Log();
    HttpContext.Current = null;
    return new(request);
  }

  private static TResult InFreshDomain<TProbe, TResult>(Func<TProbe, TResult> call)
    where TProbe : MarshalByRefObject
  {
    AppDomain domain = AppDomain.CreateDomain("log4net-aspnet-probe", null,
      new AppDomainSetup { ApplicationBase = AppDomain.CurrentDomain.BaseDirectory });
    try
    {
      TProbe probe = (TProbe)domain.CreateInstanceAndUnwrap(typeof(TProbe).Assembly.FullName!, typeof(TProbe).FullName!);
      return call(probe);
    }
    finally
    {
      AppDomain.Unload(domain);
    }
  }

  /// <summary>
  /// A buffer forwarding to a string appender; by default it fixes everything and writes <c>%aspnet-context{who}</c>.
  /// </summary>
  private sealed class Buffer
  {
    private readonly BufferingForwardingAppender _buffer = new() { BufferSize = 10 };
    private readonly StringAppender _target;

    internal Buffer(string pattern = "%aspnet-context{who}|", FixFlags fix = FixFlags.All)
    {
      _buffer.Fix = fix;
      _target = new() { Layout = new PatternLayout(pattern) };
      _buffer.AddAppender(_target);
      _buffer.ActivateOptions();
    }

    internal void Log() => _buffer.DoAppend(CreateEvent());

    internal string Flush()
    {
      _buffer.Flush();
      return _target.GetString();
    }
  }
}

/// <summary>
/// Runs in a fresh application domain and touches no System.Web type itself.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Called on the remoting proxy")]
file sealed class DomainStateProbe : MarshalByRefObject
{
  /// <summary>Logs one event through a buffer with <paramref name="pattern"/> and reports whether System.Web got loaded.</summary>
  public bool LogsAndReportsSystemWeb(string pattern)
  {
    StringAppender target = new() { Layout = new PatternLayout(pattern) };
    BufferingForwardingAppender buffer = new() { BufferSize = 10, Fix = FixFlags.All };
    buffer.AddAppender(target);
    buffer.ActivateOptions();
    buffer.DoAppend(new LoggingEvent(typeof(DomainStateProbe), null, "probe", Level.Info, "message", null));
    buffer.Flush();
    return AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "System.Web");
  }

  /// <summary>Parses <paramref name="pattern"/> and reports whether the request capture is registered.</summary>
  public bool ParsesAndReportsTheCapture(string pattern)
  {
    _ = new PatternLayout(pattern);
    return GlobalContext.Properties["log4net:AspNetRequest"] is not null;
  }
}

/// <summary>
/// Runs in a fresh application domain with a hand-built request.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Called on the remoting proxy")]
file sealed class DomainRequestProbe : MarshalByRefObject
{
  /// <summary>Fixes an event in a request before any request converter exists, then renders it there.</summary>
  public string FixesBeforeRegistrationAndRenders()
  {
    HttpContext.Current = new HttpContext(new HttpRequest(string.Empty, "http://localhost/", string.Empty),
      new HttpResponse(TextWriter.Null));
    HttpContext.Current.Items["who"] = "A";
    LoggingEvent loggingEvent = new(typeof(DomainRequestProbe), null, "probe", Level.Info, "message", null);
    loggingEvent.Fix = FixFlags.All;
    string written = new PatternLayout("%aspnet-context{who}").Format(loggingEvent);
    HttpContext.Current = null;
    return written;
  }
}

#endif
