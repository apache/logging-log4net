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

using System.Collections.Generic;

using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Util;

/// <summary>
/// Used for internal unit testing the <see cref="OnlyOnceErrorHandler"/> class.
/// </summary>
[TestFixture]
public class OnlyOnceErrorHandlerTest
{
  /// <summary>
  /// The first error must reach <see cref="LogLog"/> even when
  /// <see cref="LogLog.InternalDebugging"/> is off, which is the default. An appender that
  /// stops delivering events would otherwise leave no trace at all.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void FirstErrorIsEmittedWithoutInternalDebugging()
  {
    bool internalDebugging = LogLog.InternalDebugging;
    LogLog.InternalDebugging = false;
    try
    {
      List<LogLog> messages = [];
      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        using LogLog.LogReceivedAdapter _ = new(messages);
        new OnlyOnceErrorHandler("TestAppender").Error("Something went wrong");
      });

      Assert.That(messages, Has.Count.EqualTo(1));
      Assert.That(messages[0].Message, Does.Contain("Something went wrong"));
    }
    finally
    {
      LogLog.InternalDebugging = internalDebugging;
    }
  }

  /// <summary>
  /// Only the first error is reported: the handler disables itself afterwards so that a
  /// repeatedly failing appender cannot flood the internal log.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void OnlyTheFirstErrorIsEmitted()
  {
    bool internalDebugging = LogLog.InternalDebugging;
    LogLog.InternalDebugging = false;
    try
    {
      List<LogLog> messages = [];
      OnlyOnceErrorHandler handler = new("TestAppender");
      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        using LogLog.LogReceivedAdapter _ = new(messages);
        handler.Error("First failure");
        handler.Error("Second failure");
        handler.Error("Third failure");
      });

      Assert.That(messages, Has.Count.EqualTo(1));
      Assert.That(messages[0].Message, Does.Contain("First failure"));
      Assert.That(handler.IsEnabled, Is.False);
    }
    finally
    {
      LogLog.InternalDebugging = internalDebugging;
    }
  }

  /// <summary>
  /// <see cref="LogLog.QuietMode"/> (the <c>log4net.Internal.Quiet</c> setting) remains the
  /// documented way to silence internal messages, including appender errors.
  /// </summary>
  [Test]
  [NonParallelizable]
  public void QuietModeSuppressesTheError()
  {
    bool quietMode = LogLog.QuietMode;
    LogLog.QuietMode = true;
    try
    {
      List<LogLog> messages = [];
      LogLog.ExecuteWithoutEmittingInternalMessages(() =>
      {
        using LogLog.LogReceivedAdapter _ = new(messages);
        new OnlyOnceErrorHandler("TestAppender").Error("Something went wrong");
      });

      Assert.That(messages, Is.Empty);
    }
    finally
    {
      LogLog.QuietMode = quietMode;
    }
  }
}
