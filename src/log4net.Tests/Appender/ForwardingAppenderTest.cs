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
using log4net.Appender;
using log4net.Core;
using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Tests for <see cref="ForwardingAppender"/>.
/// </summary>
[TestFixture]
public sealed class ForwardingAppenderTest
{
  /// <summary>
  /// A batch is forwarded as one array to each attached appender in turn.
  /// </summary>
  [Test]
  public void BatchIsForwardedToEachAppenderInTurn()
  {
    List<string> log = [];
    ForwardingAppender forwarder = new();
    forwarder.AddAppender(new NamedRecordingAppender("A", log));
    forwarder.AddAppender(new NamedRecordingAppender("B", log));

    forwarder.DoAppend(
    [
      new(typeof(ForwardingAppenderTest), null, "test", Level.Info, "1", null),
      new(typeof(ForwardingAppenderTest), null, "test", Level.Info, "2", null)
    ]);

    Assert.That(log, Is.EqualTo(new[] { "A1", "A2", "B1", "B2" }));
  }

  /// <summary>Records its name and each message into a shared list.</summary>
  private sealed class NamedRecordingAppender(string name, List<string> log) : AppenderSkeleton
  {
    /// <inheritdoc/>
    protected override void Append(LoggingEvent loggingEvent) => log.Add(name + loggingEvent.RenderedMessage);
  }
}
