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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Tests.Appender.Internal;

using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// A lone surrogate must not stop an appender, whatever writer the application gave it.
/// </summary>
[TestFixture]
[NonParallelizable]
public class LoneSurrogateTest
{
  /// <summary>Stands in for an appender outside this repository, on either overload.</summary>
  private sealed class ThirdPartyAppender(bool streaming) : AppenderSkeleton
  {
    internal StringBuilder Output { get; } = new();

    /// <inheritdoc/>
    protected override void Append(LoggingEvent loggingEvent)
    {
      if (streaming)
      {
        using StringWriter writer = new(Output);
        RenderLoggingEvent(writer, loggingEvent);
      }
      else
      {
        Output.Append(RenderLoggingEvent(loggingEvent));
      }
    }

    /// <inheritdoc/>
    protected override bool RequiresLayout => true;
  }

  /// <summary>Writes the two halves of a pair apart, as a layout is free to do.</summary>
  private sealed class SplittingLayout : LayoutSkeleton
  {
    /// <inheritdoc/>
    public override void ActivateOptions()
    { }

    /// <inheritdoc/>
    public override void Format(TextWriter writer, LoggingEvent loggingEvent)
    {
      writer.Write('\ud83d');
      writer.Write("\ude00\ud83d");
      writer.Write(new[] { '\ude00' }, 0, 1);
    }
  }

  private static LoggingEvent NewEvent(string message, Exception? exception = null)
    => new(typeof(LoneSurrogateTest), null, nameof(LoneSurrogateTest), Level.Info, message, exception);

  private static PatternLayout NewLayout(string pattern)
  {
    PatternLayout layout = new(pattern);
    layout.ActivateOptions();
    return layout;
  }

  private static string SinkOf(TextWriterAppender appender, MemoryStream sink, params string[] messages)
  {
    appender.ActivateOptions();
    foreach (string message in messages)
    {
      appender.DoAppend(NewEvent(message));
    }

    appender.Close();
    return Encoding.UTF8.GetString(sink.ToArray());
  }

  /// <summary>The report's fixture: the event and every later one used to be lost.</summary>
  /// <param name="immediateFlush">Whether the appender flushes after each event.</param>
  [Test]
  public void TextWriterAppenderKeepsWriting([Values] bool immediateFlush)
  {
    MemoryStream sink = new();
    TextWriterAppender appender = new()
    {
      Layout = NewLayout("[%message]"),
      Writer = new StreamWriter(sink),
      ImmediateFlush = immediateFlush
    };

    Assert.That(SinkOf(appender, sink, "before", "a\ud83db", "after"), Is.EqualTo(@"[before][a\ud83db][after]"));
  }

  /// <summary>A surrogate last in one event must not pair with the first of the next.</summary>
  [Test]
  public void EventsDoNotShareAPair()
  {
    MemoryStream sink = new();
    TextWriterAppender appender = new() { Layout = NewLayout("%message"), Writer = new StreamWriter(sink) };

    Assert.That(SinkOf(appender, sink, "a\ud83d", "\ude00b"), Is.EqualTo(@"a\ud83d" + @"\ude00b"));
  }

  /// <summary>Whole pairs reach the sink as they are, however the layout writes them.</summary>
  [Test]
  public void PairsWrittenApartStayPairs()
  {
    MemoryStream sink = new();
    TextWriterAppender appender = new() { Layout = new SplittingLayout(), Writer = new StreamWriter(sink) };

    Assert.That(SinkOf(appender, sink, "ignored"), Is.EqualTo("\U0001F600\U0001F600"));
  }

  /// <summary>Exception text goes through its own write, with a layout that leaves it to the appender.</summary>
  [Test]
  public void ExceptionTextIsEscaped()
  {
    MemoryStream sink = new();
    TextWriterAppender appender = new() { Layout = NewLayout("%message "), Writer = new StreamWriter(sink) };
    appender.ActivateOptions();

    appender.DoAppend(NewEvent("first", new InvalidOperationException("a\ud83db")));
    appender.DoAppend(NewEvent("second"));
    appender.Close();

    string written = Encoding.UTF8.GetString(sink.ToArray());
    Assert.That(written, Contains.Substring(@"a\ud83db").Using(StringComparison.Ordinal));
    Assert.That(written.EndsWith("second ", StringComparison.Ordinal), Is.True);
  }

  /// <summary>A maximum width cuts by UTF-16 unit, so well-formed content can be the trigger.</summary>
  [Test]
  public void WidthCuttingAPairDoesNotStopTheAppender()
  {
    MemoryStream sink = new();
    TextWriterAppender appender = new() { Layout = NewLayout("[%.5message]"), Writer = new StreamWriter(sink) };

    string written = SinkOf(appender, sink, "\U0001F600abcd", "next");

    Assert.That(written.EndsWith("[next]", StringComparison.Ordinal), Is.True);
    Assert.That(written.Any(char.IsSurrogate), Is.False);
  }

  /// <summary>The cut drops a pair it would split, and the shorter text is padded as usual.</summary>
  /// <param name="pattern">The conversion pattern.</param>
  /// <param name="message">The logged message.</param>
  /// <param name="expected">The rendered text.</param>
  [TestCase("%.0message", "\U0001F600", "", TestName = "WidthZero")]
  [TestCase("%.4message", "\U0001F600abcd", "abcd", TestName = "CutAfterAPair")]
  [TestCase("%.5message", "\U0001F600abcd", "abcd", TestName = "CutInsideAPair")]
  [TestCase("%.6message", "\U0001F600abcd", "\U0001F600abcd", TestName = "NoCut")]
  [TestCase("%6.5message", "\U0001F600abcd", "  abcd", TestName = "PaddedAfterTheCut")]
  [TestCase("%.5message", "x\U0001F600abc", "\U0001F600abc", TestName = "CutBeforeAPair")]
  public void WidthKeepsPairsWhole(string pattern, string message, string expected)
  {
    RecordingErrorHandler errorHandler = new();
    ThirdPartyAppender appender = new(streaming: true) { Layout = NewLayout(pattern), ErrorHandler = errorHandler };
    appender.ActivateOptions();

    appender.DoAppend(NewEvent(message));

    Assert.That(errorHandler.Messages, Is.Empty);
    Assert.That(appender.Output.ToString(), Is.EqualTo(expected));
  }

  /// <summary>A low surrogate that was alone already has no pair to protect, so it stays and gets escaped.</summary>
  [Test]
  public void WidthKeepsALoneLowSurrogate()
  {
    ThirdPartyAppender appender = new(streaming: true) { Layout = NewLayout("%.4message") };
    appender.ActivateOptions();

    appender.DoAppend(NewEvent("x\ude00abc"));

    Assert.That(appender.Output.ToString(), Is.EqualTo(@"\ude00abc"));
  }

  /// <summary>Console.SetOut with a stream writer used to stop all three, and the application's own writes.</summary>
  /// <param name="appenderType">The console appender.</param>
  [TestCase(typeof(ConsoleAppender))]
  [TestCase(typeof(AnsiColorTerminalAppender))]
  [TestCase(typeof(ManagedColoredConsoleAppender))]
  public void ConsoleAppendersKeepWriting(Type appenderType)
  {
    MemoryStream sink = new();
    using StreamWriter writer = new(sink) { AutoFlush = true };
    TextWriter previous = Console.Out;
    try
    {
      Console.SetOut(writer);
      AppenderSkeleton appender = (AppenderSkeleton)Activator.CreateInstance(appenderType)!;
      appender.Layout = NewLayout("[%message]");
      appender.ActivateOptions();

      appender.DoAppend(NewEvent("a\ud83db"));
      appender.DoAppend(NewEvent("after"));
    }
    finally
    {
      Console.SetOut(previous);
    }

    // The ANSI appender closes each event with a reset sequence.
    string written = Regex.Replace(Encoding.UTF8.GetString(sink.ToArray()), @"\x1b\[0m", string.Empty);
    Assert.That(written, Is.EqualTo(@"[a\ud83db][after]"));
  }

  /// <summary>A listener over a stream writes message and category to the same writer.</summary>
  [Test]
  public void TraceAppenderKeepsWriting()
  {
    MemoryStream sink = new();
    TraceListener[] previous = [.. Trace.Listeners.Cast<TraceListener>()];
    Trace.Listeners.Clear();
    TextWriterTraceListener listener = new(sink);
    Trace.Listeners.Add(listener);
    try
    {
      TraceAppender appender = new() { Layout = NewLayout("[%message]"), Category = NewLayout("%message") };
      appender.ActivateOptions();

      appender.DoAppend(NewEvent("a\ud83db"));
      appender.DoAppend(NewEvent("after"));
      listener.Flush();
    }
    finally
    {
      Trace.Listeners.Clear();
      Trace.Listeners.AddRange(previous);
    }

    Assert.That(Encoding.UTF8.GetString(sink.ToArray()), Is.EqualTo(@"a\ud83db: [a\ud83db]after: [after]"));
  }

  /// <summary>Escaping sits in the base class, so an appender outside this repository gets it unasked.</summary>
  /// <param name="streaming">Which overload of <c>RenderLoggingEvent</c> the appender calls.</param>
  [Test]
  public void ThirdPartyAppendersGetTheEscape([Values] bool streaming)
  {
    ThirdPartyAppender appender = new(streaming) { Layout = NewLayout("[%message]") };
    appender.ActivateOptions();

    appender.DoAppend(NewEvent("a\ud83db"));

    Assert.That(appender.Output.ToString(), Is.EqualTo(@"[a\ud83db]"));
  }

  /// <summary>
  /// Pin, passes without the central escape too: the bytes of a file stay what the encoder wrote.
  /// </summary>
  [Test]
  public void FileAppenderOutputStaysTheSame()
  {
    string path = Path.GetTempFileName();
    try
    {
      FileAppender appender = new()
      {
        File = path,
        Encoding = new UTF8Encoding(false),
        Layout = NewLayout("%message")
      };
      appender.ActivateOptions();
      appender.DoAppend(NewEvent("a\ud83db"));
      appender.Close();

      Assert.That(File.ReadAllBytes(path), Is.EqualTo(new byte[] { 0x61, 0x5C, 0x75, 0x64, 0x38, 0x33, 0x64, 0x62 }));
    }
    finally
    {
      File.Delete(path);
    }
  }
}
