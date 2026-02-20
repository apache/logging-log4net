#region Copyright & License
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

namespace log4net.Core;

/// <summary>
/// An evaluator that triggers after specified number of seconds.
/// </summary>
/// <param name="interval">
/// The time threshold in seconds to trigger after.
/// Zero means it won't trigger at all.
/// </param>
/// <remarks>
/// <para>
/// This evaluator will trigger if the specified time period 
/// <see cref="Interval"/> has passed since last check.
/// </para>
/// </remarks>
/// <author>Robert Sevcik</author>
public class TimeEvaluator(int interval) : ITriggeringEventEvaluator
{
  private readonly object _syncRoot = new();

  /// <summary>
  /// The UTC time of last check. This gets updated when the object is created and when the evaluator triggers.
  /// </summary>
  private DateTime _lastTimeUtc = DateTime.UtcNow;

  /// <summary>
  /// The default time threshold for triggering in seconds. Zero means it won't trigger at all.
  /// </summary>
  const int DefaultInterval = 0;

  /// <summary>
  /// Create a new evaluator using the <see cref="DefaultInterval"/> time threshold in seconds.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Create a new evaluator using the <see cref="DefaultInterval"/> time threshold in seconds.
  /// </para>
  /// <para>
  /// This evaluator will trigger if the specified time period 
  /// <see cref="Interval"/> has passed since last check.
  /// </para>
  /// </remarks>
  public TimeEvaluator()
    : this(DefaultInterval)
  { }

  /// <summary>
  /// The time threshold in seconds to trigger after
  /// </summary>
  /// <value>
  /// The time threshold in seconds to trigger after.
  /// Zero means it won't trigger at all.
  /// </value>
  /// <remarks>
  /// <para>
  /// This evaluator will trigger if the specified time period 
  /// <see cref="Interval"/> has passed since last check.
  /// </para>
  /// </remarks>
  public int Interval { get; set; } = interval;

  /// <summary>
  /// Is this <paramref name="loggingEvent"/> the triggering event?
  /// </summary>
  /// <param name="loggingEvent">The event to check</param>
  /// <returns>This method returns <see langword="true"/>, if the specified time period 
  /// <see cref="Interval"/> has passed since last check.. 
  /// Otherwise it returns <see langword="false"/></returns>
  /// <remarks>
  /// <para>
  /// This evaluator will trigger if the specified time period 
  /// <see cref="Interval"/> has passed since last check.
  /// </para>
  /// </remarks>
  public bool IsTriggeringEvent(LoggingEvent loggingEvent)
  {
    // disable the evaluator if threshold is zero
    if (Interval == 0)
    {
      return false;
    }

    lock (_syncRoot) // avoid triggering multiple times
    {
      TimeSpan passed = DateTime.UtcNow.Subtract(_lastTimeUtc);

      if (passed.TotalSeconds > Interval)
      {
        _lastTimeUtc = DateTime.UtcNow;
        return true;
      }
      return false;
    }
  }
}