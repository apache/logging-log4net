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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace log4net.Util;

/// <summary>
/// Class for assertions
/// </summary>
internal static class Log4NetAssert
{
  private static ArgumentNullException ArgumentNull(string name, string? errorMessage)
    => string.IsNullOrEmpty(errorMessage) ? new(name) : new(name, errorMessage);

  /// <summary>
  /// Ensures that <paramref name="value"/> is not <see langword="null"/> and returns the validated value
  /// </summary>
  /// <typeparam name="T">Type of <paramref name="value"/></typeparam>
  /// <param name="value">Value to validate</param>
  /// <param name="name">Name of the value</param>
  /// <param name="errorMessage">Error message (optional)</param>
  /// <returns>Value (when not null)</returns>
  /// <exception cref="ArgumentNullException" />
  public static T EnsureNotNull<T>([NotNull][ValidatedNotNull] this T? value,
    [CallerArgumentExpression("value")] string name = "",
    string? errorMessage = null)
    where T : class
  {
    if (value is null)
    {
      throw ArgumentNull(name, errorMessage);
    }

    return value;
  }

  /// <summary>
  /// Ensures that <paramref name="value"/> is not null and an instance of <typeparamref name="T"/>
  /// and returns the validated value
  /// </summary>
  /// <typeparam name="T">Type to check for</typeparam>
  /// <param name="value">Value to validate</param>
  /// <param name="name">Name of the value</param>
  /// <param name="errorMessage">Error message (optional)</param>
  /// <returns>Value (when not null and of the required type)</returns>
  /// <exception cref="ArgumentNullException" />
  /// <exception cref="InvalidCastException" />
  public static T EnsureIs<T>(
    [NotNull][ValidatedNotNull] this object? value,
    [CallerArgumentExpression("value")] string name = "",
    string? errorMessage = null)
  {
    if (value is T result)
    {
      return result;
    }
    if (value is null)
    {
      throw ArgumentNull(name, errorMessage);
    }
    throw new InvalidCastException(@$"Can't convert objects objects of type ""{value.GetType()}"" to type ""{typeof(T)}"".");
  }

  /// <summary>
  /// Determines whether this is a fatal exception that should not be handled
  /// </summary>
  /// <param name="exception">Exception</param>
  /// <returns><see langword="true"/>, if it is a fatal exception, otherwise <see langword="false"/></returns>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public static bool IsFatal(this Exception exception)
    => exception is OutOfMemoryException or StackOverflowException;
}