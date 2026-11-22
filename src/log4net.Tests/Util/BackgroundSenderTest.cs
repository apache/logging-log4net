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
using System.Diagnostics;
using System.Linq;
using System.Threading;

using log4net.Tests.Util.Internal;
using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Util;

/// <summary>
/// Tests for <see cref="BackgroundSender{T}"/>.
/// </summary>
[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class BackgroundSenderTest
{
  /// <summary>How long a test waits for the background thread before calling it a failure.</summary>
  private const int WaitMillis = 30_000;

  private readonly RecordingSink _sink = new();

  private BackgroundSender<int> CreateSender(int capacity, Action<int, CancellationToken> send)
    => new("test", capacity, send, _sink.Report);

  /// <summary>
  /// A queue without room for at least one item cannot work.
  /// </summary>
  [TestCase(0)]
  [TestCase(-1)]
  public void ConstructorRejectsCapacityBelowOne(int capacity)
    => Assert.That(() => CreateSender(capacity, (_, _) => { }),
      Throws.TypeOf<ArgumentOutOfRangeException>());

  /// <summary>
  /// One thread delivers, so order is the order the items were queued in.
  /// </summary>
  [Test]
  public void ItemsAreSentInTheOrderTheyWereQueued()
  {
    const int itemCount = 100;
    using BackgroundSender<int> sender = CreateSender(itemCount, (item, _) => _sink.Send(item));

    for (int i = 0; i < itemCount; i++)
    {
      Assert.That(sender.TryEnqueue(i, WaitMillis), Is.True);
    }

    Assert.That(sender.Flush(WaitMillis), Is.True);
    Assert.That(_sink.SentItems, Is.EqualTo(Enumerable.Range(0, itemCount).ToArray()));
    Assert.That(sender.DroppedItemCount, Is.EqualTo(0));
  }

  /// <summary>
  /// A full queue must not hold the logging call up when the caller allows no wait.
  /// </summary>
  [Test]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2016:Forward the CancellationToken parameter to methods that take one",
    Justification = "Stands for a sink that does not cooperate with cancellation.")]
  public void AFullQueueDropsInsteadOfBlockingTheCaller()
  {
    using ManualResetEventSlim sendEntered = new(false);
    using ManualResetEventSlim release = new(false);
    using BackgroundSender<int> sender = CreateSender(2, (item, _) =>
    {
      sendEntered.Set();
      release.Wait(WaitMillis);
      _sink.Send(item);
    });

    // Park the only sending thread, so that nothing leaves the queue from here on.
    Assert.That(sender.TryEnqueue(1, WaitMillis), Is.True);
    Assert.That(sendEntered.Wait(WaitMillis), Is.True);

    // Fill the two slots, then prove the next one is dropped rather than waited for.
    Assert.That(sender.TryEnqueue(2, WaitMillis), Is.True);
    Assert.That(sender.TryEnqueue(3, WaitMillis), Is.True);

    Stopwatch stopwatch = Stopwatch.StartNew();
    Assert.That(sender.TryEnqueue(4, 0), Is.False);
    stopwatch.Stop();

    Assert.That(sender.DroppedItemCount, Is.EqualTo(1));
    Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(WaitMillis));

    release.Set();
    Assert.That(sender.Flush(WaitMillis), Is.True);
    Assert.That(_sink.SentItems, Is.EqualTo(new[] { 1, 2, 3 }));
  }

  /// <summary>
  /// Closing sends what is still queued, rather than discarding it like the popped batch did.
  /// </summary>
  [Test]
  public void CloseSendsWhatIsStillQueued()
  {
    const int itemCount = 10;
    BackgroundSender<int> sender = CreateSender(itemCount, (item, _) => _sink.Send(item));
    try
    {
      for (int i = 0; i < itemCount; i++)
      {
        Assert.That(sender.TryEnqueue(i, WaitMillis), Is.True);
      }

      sender.Close(WaitMillis);
      Assert.That(_sink.SentItems, Has.Length.EqualTo(itemCount));
      Assert.That(sender.DroppedItemCount, Is.EqualTo(0));
    }
    finally
    {
      sender.Dispose();
    }
  }

  /// <summary>
  /// An unresponsive sink must not make closing the appender hang. What is left is dropped,
  /// counted and reported.
  /// </summary>
  [Test]
  public void CloseGivesUpOnAnUnresponsiveSink()
  {
    const int closeTimeoutMillis = 200;
    using ManualResetEventSlim sendEntered = new(false);
    BackgroundSender<int> sender = CreateSender(5, (item, token) =>
    {
      sendEntered.Set();
      // Answers only when Close runs out of patience and cancels.
      token.WaitHandle.WaitOne(WaitMillis);
      _sink.Send(item);
    });
    try
    {
      Assert.That(sender.TryEnqueue(1, WaitMillis), Is.True);
      Assert.That(sendEntered.Wait(WaitMillis), Is.True);
      Assert.That(sender.TryEnqueue(2, WaitMillis), Is.True);
      Assert.That(sender.TryEnqueue(3, WaitMillis), Is.True);

      Stopwatch stopwatch = Stopwatch.StartNew();
      sender.Close(closeTimeoutMillis);
      stopwatch.Stop();

      Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(WaitMillis));
      Assert.That(sender.DroppedItemCount, Is.EqualTo(2));
      Assert.That(_sink.Reports, Is.Not.Empty);
    }
    finally
    {
      sender.Dispose();
    }
  }

  /// <summary>
  /// Once the sender has stopped, queueing fails instead of filling a queue nobody empties.
  /// </summary>
  [Test]
  public void NothingIsAcceptedAfterClose()
  {
    BackgroundSender<int> sender = CreateSender(5, (item, _) => _sink.Send(item));
    try
    {
      sender.Close(WaitMillis);

      Assert.That(sender.IsFaulted, Is.True);
      Assert.That(sender.TryEnqueue(1, WaitMillis), Is.False);
      Assert.That(sender.Flush(WaitMillis), Is.False);
      Assert.That(_sink.SentItems, Is.Empty);
    }
    finally
    {
      sender.Dispose();
    }
  }

  /// <summary>
  /// A send that throws costs its own item and nothing else.
  /// </summary>
  [Test]
  public void AFailedSendDoesNotStopTheOnesAfterIt()
  {
    using BackgroundSender<int> sender = CreateSender(10, (item, _) =>
    {
      if (item == 1)
      {
        throw new InvalidOperationException("simulated send failure");
      }

      _sink.Send(item);
    });

    for (int i = 0; i < 4; i++)
    {
      Assert.That(sender.TryEnqueue(i, WaitMillis), Is.True);
    }

    Assert.That(sender.Flush(WaitMillis), Is.True);
    Assert.That(_sink.SentItems, Is.EqualTo(new[] { 0, 2, 3 }));
    Assert.That(sender.DroppedItemCount, Is.EqualTo(1));

    // A failed send is not queue pressure and must not be reported as such.
    Assert.That(_sink.Reports, Has.Some.Contains("Failed to send"));
    Assert.That(_sink.Reports, Has.None.Contains("queue is full"));
  }

  /// <summary>
  /// An error handler that throws must not take the sending thread, and with it the process, down.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void AnErrorHandlerThatThrowsDoesNotStopTheSender()
  {
    List<LogLog> internalMessages = [];

    // The sender is disposed inside the wrapped action, because closing it reports the drop
    // total through the same throwing handler.
    LogLog.ExecuteWithoutEmittingInternalMessages(() =>
    {
      using LogLog.LogReceivedAdapter adapter = new(internalMessages);
      using BackgroundSender<int> sender = new("test", 10, (item, _) =>
        {
          if (item == 1)
          {
            throw new InvalidOperationException("simulated send failure");
          }

          _sink.Send(item);
        },
        (_, _) => throw new InvalidOperationException("simulated error handler failure"));

      for (int i = 0; i < 4; i++)
      {
        Assert.That(sender.TryEnqueue(i, WaitMillis), Is.True);
      }

      Assert.That(sender.Flush(WaitMillis), Is.True);
      Assert.That(sender.IsFaulted, Is.False);
    });

    Assert.That(_sink.SentItems, Is.EqualTo(new[] { 0, 2, 3 }));
    Assert.That(internalMessages, Is.Not.Empty);
  }

  /// <summary>
  /// <see cref="BackgroundSender{T}.Dispose"/> closes again, which must not repeat the summary.
  /// </summary>
  [Test]
  public void CloseThenDisposeReportsTheSummaryOnce()
  {
    BackgroundSender<int> sender = CreateSender(5, (_, _) => throw new InvalidOperationException("simulated send failure"));
    Assert.That(sender.TryEnqueue(1, WaitMillis), Is.True);

    sender.Close(WaitMillis);
    sender.Dispose();

    Assert.That(_sink.CloseSummaries, Is.EqualTo(new[] { "[test] 1 message(s) in total were not sent." }));
  }

  /// <summary>
  /// Disposing without closing first reports the summary too.
  /// </summary>
  [Test]
  public void DisposeAloneReportsTheSummaryOnce()
  {
    BackgroundSender<int> sender = CreateSender(5, (_, _) => throw new InvalidOperationException("simulated send failure"));
    Assert.That(sender.TryEnqueue(1, WaitMillis), Is.True);
    Assert.That(sender.Flush(WaitMillis), Is.True);

    sender.Dispose();

    Assert.That(_sink.CloseSummaries, Is.EqualTo(new[] { "[test] 1 message(s) in total were not sent." }));
  }

  /// <summary>
  /// A close with nothing dropped reports nothing, and a drop after it is still reported.
  /// </summary>
  [Test]
  public void ACloseWithoutLossesLeavesTheSummaryForLater()
  {
    BackgroundSender<int> sender = CreateSender(5, (item, _) => _sink.Send(item));

    sender.Close(WaitMillis);
    Assert.That(sender.TryEnqueue(1, WaitMillis), Is.False);
    sender.Dispose();

    Assert.That(_sink.CloseSummaries, Is.EqualTo(new[] { "[test] 1 message(s) in total were not sent." }));
  }

  /// <summary>
  /// A count that grew after the first summary is reported again.
  /// </summary>
  [Test]
  public void ACountThatGrewIsReportedAgain()
  {
    BackgroundSender<int> sender = CreateSender(5, (_, _) => throw new InvalidOperationException("simulated send failure"));
    Assert.That(sender.TryEnqueue(1, WaitMillis), Is.True);

    sender.Close(WaitMillis);
    Assert.That(sender.TryEnqueue(2, WaitMillis), Is.False);
    sender.Dispose();

    Assert.That(_sink.CloseSummaries, Is.EqualTo(new[]
    {
      "[test] 1 message(s) in total were not sent.",
      "[test] 2 message(s) in total were not sent."
    }));
  }

  /// <summary>
  /// After one timed out wait a full queue drops at once, until a send succeeds.
  /// </summary>
  [Test]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2016:Forward the CancellationToken parameter to methods that take one",
    Justification = "Stands for a sink that does not cooperate with cancellation.")]
  public void AFullQueueWaitsOnceUntilASendSucceeds()
  {
    using ManualResetEventSlim sendEntered = new(false);
    using ManualResetEventSlim release = new(false);
    using BackgroundSender<int> sender = CreateSender(1, (item, _) =>
    {
      sendEntered.Set();
      release.Wait(WaitMillis);
      _sink.Send(item);
    });
    Assert.That(sender.TryEnqueue(1, WaitMillis), Is.True);
    Assert.That(sendEntered.Wait(WaitMillis), Is.True);
    Assert.That(sender.TryEnqueue(2, WaitMillis), Is.True);

    Assert.That(sender.TryEnqueue(3, 1), Is.False);
    bool isCongestedAfterTheFirstTimeout = sender.GetFieldValue<bool>("_isCongested");
    Stopwatch stopwatch = Stopwatch.StartNew();
    for (int i = 4; i < 14; i++)
    {
      Assert.That(sender.TryEnqueue(i, 1_000), Is.False);
    }

    stopwatch.Stop();
    bool isCongestedAfterTenMore = sender.GetFieldValue<bool>("_isCongested");
    release.Set();
    Assert.That(sender.Flush(WaitMillis), Is.True);

    Assert.That(isCongestedAfterTheFirstTimeout, Is.True);
    Assert.That(isCongestedAfterTenMore, Is.True);
    // Ten waits of a second each without the flag.
    Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5_000));
    Assert.That(sender.DroppedItemCount, Is.EqualTo(11));
    Assert.That(sender.GetFieldValue<bool>("_isCongested"), Is.False);
    Assert.That(_sink.SentItems, Is.EqualTo(new[] { 1, 2 }));
  }

  /// <summary>
  /// A send that throws is no sign the sink drains again, so a full queue still drops at once.
  /// </summary>
  [Test]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2016:Forward the CancellationToken parameter to methods that take one",
    Justification = "Stands for a sink that does not cooperate with cancellation.")]
  public void AFailedSendKeepsTheQueueCongested()
  {
    using ManualResetEventSlim sendEntered = new(false);
    using ManualResetEventSlim release = new(false);
    using BackgroundSender<int> sender = CreateSender(1, (_, _) =>
    {
      sendEntered.Set();
      release.Wait(WaitMillis);
      throw new InvalidOperationException("simulated send failure");
    });
    Assert.That(sender.TryEnqueue(1, WaitMillis), Is.True);
    Assert.That(sendEntered.Wait(WaitMillis), Is.True);
    Assert.That(sender.TryEnqueue(2, WaitMillis), Is.True);
    Assert.That(sender.TryEnqueue(3, 1), Is.False);

    release.Set();
    Assert.That(sender.Flush(WaitMillis), Is.True);

    Assert.That(sender.GetFieldValue<bool>("_isCongested"), Is.True);
    Assert.That(sender.DroppedItemCount, Is.EqualTo(3));
  }
}
