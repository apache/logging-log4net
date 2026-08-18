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
using System.IO;

using log4net.Appender;
using log4net.Core;
using log4net.Layout;

using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Tests for <see cref="TextWriterAppender"/>
/// </summary>
[TestFixture]
public class TextWriterAppenderTest
{
  /// <summary>
  /// A writer that accepts everything written to it but cannot be flushed, standing in for a full
  /// disk or a broken stream.
  /// </summary>
  private sealed class UnflushableWriter : StringWriter
  {
    /// <inheritdoc/>
    public override void Flush() => throw new IOException("Simulated failure to flush");
  }

  /// <summary>
  /// Swallows what the appender reports, so that the tests observe the return value rather than
  /// internal logging.
  /// </summary>
  private sealed class SilentErrorHandler : IErrorHandler
  {
    /// <inheritdoc/>
    public void Error(string message, Exception? e, ErrorCode errorCode)
    { }

    /// <inheritdoc/>
    public void Error(string message, Exception e)
    { }

    /// <inheritdoc/>
    public void Error(string message)
    { }
  }

  /// <summary>
  /// Flush reported success whatever happened. Its contract is to say whether the events were
  /// flushed, and a caller such as a shutdown hook relies on that.
  /// </summary>
  [Test]
  public void FlushReportsFailure()
    => Assert.That(CreateAppender(new UnflushableWriter()).Flush(1000), Is.False);

  /// <summary>
  /// A writer that flushes cleanly still has to report success.
  /// </summary>
  [Test]
  public void FlushReportsSuccess()
    => Assert.That(CreateAppender(new StringWriter()).Flush(1000), Is.True);

  /// <summary>
  /// A failing flush must not escape to the caller. QuietTextWriter routes failing writes to the
  /// ErrorHandler but does not override Flush, so the appender has to catch it.
  /// </summary>
  [Test]
  public void FlushDoesNotThrow()
    => Assert.That(() => CreateAppender(new UnflushableWriter()).Flush(1000), Throws.Nothing);

  /// <summary>
  /// With ImmediateFlush there is nothing buffered, so the writer is never touched.
  /// </summary>
  [Test]
  public void FlushIsANoOpWhenImmediateFlushIsSet()
  {
    TextWriterAppender appender = CreateAppender(new UnflushableWriter());
    appender.ImmediateFlush = true;

    Assert.That(appender.Flush(1000), Is.True);
  }

  private static TextWriterAppender CreateAppender(TextWriter writer)
  {
    PatternLayout layout = new("%message%newline");
    layout.ActivateOptions();

    TextWriterAppender appender = new()
    {
      Layout = layout,
      ImmediateFlush = false,
      ErrorHandler = new SilentErrorHandler(),
      Writer = writer
    };
    appender.ActivateOptions();
    return appender;
  }
}
