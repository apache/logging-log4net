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
using System;

namespace log4net.Core;

/// <summary>
/// An evaluator that triggers on an Exception type
/// </summary>
/// <remarks>
/// <para>
/// This evaluator will trigger if the type of the Exception
/// passed to <see cref="IsTriggeringEvent(LoggingEvent)"/>
/// is equal to a Type in <see cref="ExceptionType"/>.    /// 
/// </para>
/// </remarks>
/// <author>Drew Schaeffer</author>
public class ExceptionEvaluator : ITriggeringEventEvaluator
{
  /// <summary>
  /// Default ctor to allow dynamic creation through a configurator.
  /// </summary>
  public ExceptionEvaluator()
  {
    // empty
  }

  /// <summary>
  /// Constructs an evaluator and initializes to trigger on <paramref name="exType"/>
  /// </summary>
  /// <param name="exType">the type that triggers this evaluator.</param>
  /// <param name="triggerOnSubClass">If true, this evaluator will trigger on subclasses of <see cref="ExceptionType"/>.</param>
  public ExceptionEvaluator(Type exType, bool triggerOnSubClass)
  {
    ExceptionType = exType.EnsureNotNull();
    TriggerOnSubclass = triggerOnSubClass;
  }

  /// <summary>
  /// The type that triggers this evaluator.
  /// </summary>
  public Type? ExceptionType { get; set; }

  /// <summary>
  /// If true, this evaluator will trigger on subclasses of <see cref="ExceptionType"/>.
  /// </summary>
  public bool TriggerOnSubclass { get; set; }

  /// <summary>
  /// Is this <paramref name="loggingEvent"/> the triggering event?
  /// </summary>
  /// <param name="loggingEvent">The event to check</param>
  /// <returns>This method returns <c>true</c>, if the logging event Exception 
  /// Type is <see cref="ExceptionType"/>. 
  /// Otherwise it returns <c>false</c></returns>
  /// <remarks>
  /// <para>
  /// This evaluator will trigger if the Exception Type of the event
  /// passed to <see cref="IsTriggeringEvent(LoggingEvent)"/>
  /// is <see cref="ExceptionType"/>.
  /// </para>
  /// </remarks>
  public bool IsTriggeringEvent(LoggingEvent loggingEvent)
  {
    loggingEvent.EnsureNotNull();
    if (TriggerOnSubclass && loggingEvent.ExceptionObject is not null)
    {
      // check if loggingEvent.ExceptionObject is of type ExceptionType or subclass of ExceptionType
      Type exceptionObjectType = loggingEvent.ExceptionObject.GetType();
      return ExceptionType is null || exceptionObjectType == ExceptionType || ExceptionType.IsAssignableFrom(exceptionObjectType);
    }
    if (!TriggerOnSubclass && loggingEvent.ExceptionObject is not null)
    {
      // check if loggingEvent.ExceptionObject is of type ExceptionType
      return loggingEvent.ExceptionObject.GetType() == ExceptionType;
    }
    // loggingEvent.ExceptionObject is null
    return false;
  }
}
