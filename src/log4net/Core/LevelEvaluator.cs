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

using log4net.Util;

namespace log4net.Core;

/// <summary>
/// An evaluator that triggers at a threshold level
/// </summary>
/// <param name="threshold">the threshold to trigger at</param>
/// <remarks>
/// <para>
/// This evaluator will trigger if the level of the event
/// passed to <see cref="IsTriggeringEvent(LoggingEvent)"/>
/// is equal to or greater than the <see cref="Threshold"/>
/// level.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
public class LevelEvaluator(Level threshold) : ITriggeringEventEvaluator
{
  /// <summary>
  /// Create a new evaluator using the <see cref="Level.Off"/> threshold.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This evaluator will trigger if the level of the event
  /// passed to <see cref="IsTriggeringEvent(LoggingEvent)"/>
  /// is equal to or greater than the <see cref="Threshold"/>
  /// level.
  /// </para>
  /// </remarks>
  public LevelEvaluator()
    : this(Level.Off)
  { }

  /// <summary>
  /// the threshold to trigger at
  /// </summary>
  /// <value>
  /// The <see cref="Level"/> that will cause this evaluator to trigger
  /// </value>
  /// <remarks>
  /// <para>
  /// This evaluator will trigger if the level of the event
  /// passed to <see cref="IsTriggeringEvent(LoggingEvent)"/>
  /// is equal to or greater than the <see cref="Threshold"/>
  /// level.
  /// </para>
  /// </remarks>
  public Level Threshold { get; set; } = threshold.EnsureNotNull();

  /// <summary>
  /// Is this <paramref name="loggingEvent"/> the triggering event?
  /// </summary>
  /// <param name="loggingEvent">The event to check</param>
  /// <returns>This method returns <c>true</c>, if the event level
  /// is equal or higher than the <see cref="Threshold"/>. 
  /// Otherwise it returns <c>false</c></returns>
  public bool IsTriggeringEvent(LoggingEvent loggingEvent)
    => loggingEvent.EnsureNotNull().Level is null || loggingEvent.Level >= Threshold;
}
