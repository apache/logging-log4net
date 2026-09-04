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
using log4net.Tests.Appender.Internal;
using log4net.Layout;

using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Tests for <see cref="AnsiColorTerminalAppender"/>, which places the terminal reset codes
/// before any trailing line break so the colour ends with the text.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AnsiColorTerminalAppenderTest
{
  /// <summary>Matches the appender's private PostEventCodes.</summary>
  private const string Reset = "\x1b[0m";

  /// <summary>The reset codes belong before the line break, whichever one it is.</summary>
  // Explicit names: two arguments where one holds an escape character make the whole fixture
  // invisible to "dotnet test --filter", reproduced with [TestCase("one", "\x1b[0m")].
  [TestCase("", Reset, TestName = "AnEmptyRender")]
  [TestCase("x", "x" + Reset, TestName = "ASingleCharacter")]
  [TestCase("\n", Reset + "\n", TestName = "NothingButALineFeed")]
  [TestCase("text", "text" + Reset, TestName = "NoTrailingLineBreak")]
  [TestCase("\r", Reset + "\r", TestName = "NothingButACarriageReturn")]
  [TestCase("text\n", "text" + Reset + "\n", TestName = "TrailingLineFeed")]
  [TestCase("text\r", "text" + Reset + "\r", TestName = "TrailingCarriageReturn")]
  [TestCase("text\r\n", "text" + Reset + "\r\n", TestName = "TrailingCarriageReturnLineFeed")]
  [TestCase("text\n\r", "text" + Reset + "\n\r", TestName = "TrailingLineFeedCarriageReturn")]
  [TestCase("text\n\n", "text\n" + Reset + "\n", TestName = "TrailingDoubledLineFeedCountsAsOne")]
  public void TheResetCodesGoBeforeATrailingLineBreak(string message, string expected)
  {
    RecordingErrorHandler errorHandler = new();
    // Level.Info has no colour mapping configured, so nothing is prepended and the rendered
    // message is exactly what was logged, down to the empty string.
    AnsiColorTerminalAppender appender = new()
    {
      Layout = new PatternLayout("%message"),
      ErrorHandler = errorHandler
    };
    appender.ActivateOptions();

    TextWriter previous = Console.Out;
    using StringWriter captured = new();
    try
    {
      Console.SetOut(captured);
      // DoAppend is overloaded on LoggingEvent and LoggingEvent[], so this new cannot be short.
      appender.DoAppend(new LoggingEvent(new()
      {
        Level = Level.Info,
        Message = message,
        LoggerName = nameof(AnsiColorTerminalAppenderTest)
      }));
    }
    finally
    {
      Console.SetOut(previous);
    }

    Assert.That(errorHandler.Messages, Is.Empty, "the event must not be dropped");
    Assert.That(captured.ToString(), Is.EqualTo(expected));
  }

}
