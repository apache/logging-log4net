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
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace log4net.Util;

/// <summary>
/// Hands work to a single background thread so that an appender does not perform
/// slow I/O while it holds the appender lock.
/// </summary>
/// <typeparam name="T">The type of the queued work items.</typeparam>
/// <remarks>
/// <para>
/// An appender that sends over the network blocks the thread that made the logging call,
/// and every other thread logging to the same appender behind it, for as long as the sink
/// takes to answer. Queueing the work instead bounds that wait to the enqueue timeout.
/// </para>
/// <para>
/// The queue has a fixed capacity. Work is delivered in the order it was queued, by one
/// thread, so a send implementation does not need to be thread safe. Items that could not
/// be queued or sent are counted in <see cref="DroppedItemCount"/>.
/// </para>
/// <para>
/// Public only because <c>log4net.Ext.Mail</c> is deliberately not strong named and so cannot be
/// a friend assembly. It is infrastructure, not part of the surface an application configures.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class BackgroundSender<T> : IDisposable
{
  private readonly BlockingCollection<Item> _queue;
  private readonly Action<T, CancellationToken> _send;
  private readonly Action<string, Exception?> _reportError;
  private readonly string _name;
  private readonly Thread _pump;
  private readonly CancellationTokenSource _shutdown = new();
  private int _droppedItemCount;
  private int _dropReported;
  private int _faultReported;
  private volatile bool _isFaulted;

  /// <summary>
  /// Creates a queue and starts its background thread.
  /// </summary>
  /// <param name="name">Name of the owning appender, used in error messages.</param>
  /// <param name="capacity">The maximum number of items the queue holds. Must be positive.</param>
  /// <param name="send">
  /// Delivers one item. Called on the background thread only. May throw: the exception is
  /// reported and the item dropped. The token is cancelled once <see cref="Close"/> has run
  /// out of time, so an implementation that can abort its I/O should pass it on.
  /// </param>
  /// <param name="reportError">Reports a message and its optional exception, typically to an error handler.</param>
  public BackgroundSender(string name, int capacity, Action<T, CancellationToken> send,
    Action<string, Exception?> reportError)
  {
    _name = name.EnsureNotNull();
    _send = send.EnsureNotNull();
    _reportError = reportError.EnsureNotNull();
    if (capacity <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "The capacity must be positive.");
    }

    _queue = new(capacity);
    _pump = new(Pump)
    {
      IsBackground = true,
      Name = $"log4net {name} sender"
    };
    _pump.Start();
  }

  /// <summary>
  /// The number of items that were not delivered, because the queue was full, closed or faulted,
  /// or because sending them threw.
  /// </summary>
  public int DroppedItemCount => Volatile.Read(ref _droppedItemCount);

  /// <summary>
  /// Whether the background thread has stopped for good. Nothing more will be delivered.
  /// </summary>
  public bool IsFaulted => _isFaulted;

  /// <summary>
  /// Queues one item for delivery.
  /// </summary>
  /// <param name="item">The item to deliver.</param>
  /// <param name="millisecondsTimeout">
  /// How long to wait for room in a full queue. Zero returns immediately, which loses the item
  /// rather than delaying the logging call.
  /// </param>
  /// <returns><see langword="true"/> if the item was queued, <see langword="false"/> if it was dropped.</returns>
  public bool TryEnqueue(T item, int millisecondsTimeout)
  {
    if (!_isFaulted)
    {
      try
      {
        if (_queue.TryAdd(new Item(item), millisecondsTimeout))
        {
          return true;
        }
      }
      catch (Exception e) when (!e.IsFatal())
      {
        // Closed, or CompleteAdding ran, while this call was in flight.
      }
    }

    ReportQueueFull();
    return false;
  }

  /// <summary>
  /// Waits until everything queued before this call has been sent.
  /// </summary>
  /// <param name="millisecondsTimeout">The maximum time to wait.</param>
  /// <returns>
  /// <see langword="true"/> if the queue drained in time, <see langword="false"/> on timeout
  /// or if the background thread is no longer running.
  /// </returns>
  /// <remarks>
  /// <para>
  /// A marker is placed at the end of the queue and awaited, so a caller is not held up by
  /// items queued after it asked.
  /// </para>
  /// </remarks>
  public bool Flush(int millisecondsTimeout)
  {
    if (_isFaulted)
    {
      return false;
    }

    TaskCompletionSource<bool> marker = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int startTicks = Environment.TickCount;
    try
    {
      if (!_queue.TryAdd(new Item(marker), millisecondsTimeout))
      {
        return false;
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // Closed while this call was in flight.
      return false;
    }

    return marker.Task.Wait(Remaining(startTicks, millisecondsTimeout));
  }

  /// <summary>
  /// Stops the queue, sending what is still in it until the time runs out.
  /// </summary>
  /// <param name="millisecondsTimeout">The maximum time to spend draining.</param>
  /// <remarks>
  /// <para>
  /// Never throws. Once the time is up the send in flight is cancelled and the remaining items
  /// are counted as dropped, so closing an appender cannot hang on an unresponsive sink.
  /// </para>
  /// </remarks>
  public void Close(int millisecondsTimeout)
  {
    try
    {
      _queue.CompleteAdding();
      if (!_pump.Join(Math.Max(millisecondsTimeout, 0)))
      {
        // Out of time: stop the send in flight and let the pump drop the rest.
        _shutdown.Cancel();
        _pump.Join(CancelGraceMillis);
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      Report($"[{_name}] Failed to shut the background sender down.", e);
    }

    int dropped = DroppedItemCount;
    if (dropped > 0)
    {
      Report($"[{_name}] {dropped} logging event(s) were not sent.", null);
    }
  }

  /// <inheritdoc/>
  public void Dispose()
  {
    Close(0);

    // Disposing these while the pump still runs would throw on the pump thread, which is
    // an unhandled exception. If it did not stop in time, leave them to the finalizers.
    if (!_pump.IsAlive)
    {
      _shutdown.Dispose();
      _queue.Dispose();
    }
  }

  /// <summary>
  /// Reports without ever throwing. An <see cref="Action{T1, T2}"/> supplied by a caller may
  /// throw, and on the pump thread that would be an unhandled exception.
  /// </summary>
  private void Report(string message, Exception? exception)
  {
    try
    {
      _reportError(message, exception);
    }
    catch (Exception e) when (!e.IsFatal())
    {
      LogLog.Error(_declaringType, $"[{_name}] The error handler threw.", e);
    }
  }

  private void Pump()
  {
    try
    {
      foreach (Item item in _queue.GetConsumingEnumerable())
      {
        if (item.Marker is TaskCompletionSource<bool> marker)
        {
          marker.TrySetResult(true);
          continue;
        }

        if (_shutdown.IsCancellationRequested)
        {
          // Closing and out of time. Drain without sending, so that Close returns.
          CountLost();
          continue;
        }

        try
        {
          _send(item.Payload!, _shutdown.Token);
        }
        catch (Exception e) when (!e.IsFatal())
        {
          CountLost();
          Report($"[{_name}] Failed to send a logging event.", e);
        }
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // Nothing will be sent from here on, so say so once and let TryEnqueue fail fast
      // instead of filling a queue that nobody empties.
      _isFaulted = true;
      if (Interlocked.Exchange(ref _faultReported, 1) == 0)
      {
        Report($"[{_name}] The background sender stopped. No further events will be sent.", e);
      }
    }
    finally
    {
      _isFaulted = true;
      ReleaseWaiters();
    }
  }

  /// <summary>
  /// Releases anyone waiting in <see cref="Flush"/> once the pump is gone, rather than
  /// letting them wait out their timeout for a marker that will never be reached.
  /// </summary>
  private void ReleaseWaiters()
  {
    try
    {
      while (_queue.TryTake(out Item item))
      {
        if (item.Marker is TaskCompletionSource<bool> marker)
        {
          marker.TrySetResult(true);
        }
        else
        {
          CountLost();
        }
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // The queue may already be disposed. Nothing left to release.
      LogLog.Debug(_declaringType, $"[{_name}] Could not drain the queue on shutdown.", e);
    }
  }

  private void CountLost() => Interlocked.Increment(ref _droppedItemCount);

  /// <summary>
  /// A full or closed queue, unlike a failed send, which reports for itself.
  /// </summary>
  private void ReportQueueFull()
  {
    CountLost();
    if (Interlocked.Exchange(ref _dropReported, 1) == 0)
    {
      Report($"[{_name}] A logging event was dropped: the queue is full or closed. "
        + "Further losses are counted and reported when the sender closes.", null);
    }
  }

  private static int Remaining(int startTicks, int millisecondsTimeout)
  {
    if (millisecondsTimeout == Timeout.Infinite)
    {
      return Timeout.Infinite;
    }

    int elapsed = unchecked(Environment.TickCount - startTicks);
    return Math.Max(millisecondsTimeout - elapsed, 0);
  }

  private const int CancelGraceMillis = 1_000;

  private static readonly Type _declaringType = typeof(BackgroundSender<T>);

  /// <summary>
  /// Either a payload or a flush marker: markers travel in the queue so that they observe
  /// the order the items were queued in.
  /// </summary>
  private readonly record struct Item(T? Payload, TaskCompletionSource<bool>? Marker)
  {
    internal Item(T payload) : this(payload, null)
    { }

    internal Item(TaskCompletionSource<bool> marker) : this(default, marker)
    { }
  }
}
