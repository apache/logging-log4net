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
/// Tests for the internal <c>SurrogateEscapingWriter</c>, through what a layout writes to it.
/// </summary>
[TestFixture]
public class SurrogateEscapingWriterTest
{
  private const char High = '\ud83d';
  private const char Low = '\ude00';
  private const string Pair = "\U0001F600";

  /// <summary>How a test hands a surrogate to the writer.</summary>
  public enum Overload
  {
    /// <summary><see cref="TextWriter.Write(char)"/></summary>
    Char,

    /// <summary><see cref="TextWriter.Write(string)"/></summary>
    String,

    /// <summary><see cref="TextWriter.Write(char[], int, int)"/></summary>
    Array
  }

  /// <summary>Counts writes, because a counting writer sizes each write on its own.</summary>
  private sealed class RecordingWriter : StringWriter
  {
    internal int Writes { get; private set; }

    internal bool Closed { get; private set; }

    /// <inheritdoc/>
    public override void Write(char value)
    {
      Writes++;
      base.Write(value);
    }

    /// <inheritdoc/>
    public override void Write(string? value)
    {
      Writes++;
      base.Write(value);
    }

    /// <inheritdoc/>
    public override void Write(char[] buffer, int index, int count)
    {
      Writes++;
      base.Write(buffer, index, count);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
      Closed = true;
      base.Dispose(disposing);
    }
  }

  /// <summary>A layout that runs the test's writes against the writer it is handed.</summary>
  private sealed class ActionLayout(Action<TextWriter> write) : LayoutSkeleton
  {
    /// <inheritdoc/>
    public override void ActivateOptions()
    { }

    /// <inheritdoc/>
    public override void Format(TextWriter writer, LoggingEvent loggingEvent) => write(writer);
  }

  /// <summary>Renders one event to a target.</summary>
  private sealed class RenderingAppender(TextWriter target) : AppenderSkeleton
  {
    /// <inheritdoc/>
    protected override void Append(LoggingEvent loggingEvent) => RenderLoggingEvent(target, loggingEvent);
  }

  private static void Render(TextWriter target, Action<TextWriter> write)
  {
    RenderingAppender appender = new(target) { Layout = new ActionLayout(write) };
    appender.ActivateOptions();
    appender.DoAppend(new LoggingEvent(typeof(SurrogateEscapingWriterTest), null, "test", Level.Info, "ignored", null));
  }

  private static void Write(TextWriter writer, Overload overload, char value)
  {
    switch (overload)
    {
      case Overload.Char:
        writer.Write(value);
        break;
      case Overload.String:
        writer.Write(value.ToString());
        break;
      default:
        writer.Write(new[] { 'x', value, 'x' }, 1, 1);
        break;
    }
  }

  /// <summary>Clean text must not be chunked, a stream counts each write.</summary>
  [Test]
  public void CleanStringIsOneWrite()
  {
    using RecordingWriter target = new();

    Render(target, writer => writer.Write("abc" + Pair + "def"));

    Assert.That(target.Writes, Is.EqualTo(1));
    Assert.That(target.ToString(), Is.EqualTo("abc" + Pair + "def"));
  }

  /// <summary>Clean text must not be chunked, a stream counts each write.</summary>
  [Test]
  public void CleanArrayIsOneWrite()
  {
    using RecordingWriter target = new();

    Render(target, writer => writer.Write("xabcx".ToCharArray(), 1, 3));

    Assert.That(target.Writes, Is.EqualTo(1));
    Assert.That(target.ToString(), Is.EqualTo("abc"));
  }

  /// <summary>A pair split across two writes stays a pair, and arrives as one write.</summary>
  /// <param name="first">How the high surrogate is written.</param>
  /// <param name="second">How the low surrogate is written.</param>
  [Test]
  public void PairAcrossTwoWritesStaysAPair(
    [Values] Overload first,
    [Values] Overload second)
  {
    using RecordingWriter target = new();

    Render(target, writer =>
    {
      Write(writer, first, High);
      Write(writer, second, Low);
    });

    Assert.That(target.Writes, Is.EqualTo(1));
    Assert.That(target.ToString(), Is.EqualTo(Pair));
  }

  /// <summary>A high surrogate at the end of the event has no low half to wait for.</summary>
  /// <param name="overload">How the surrogate is written.</param>
  [Test]
  public void HighSurrogateAtTheEndIsEscaped([Values] Overload overload)
  {
    using RecordingWriter target = new();

    Render(target, writer =>
    {
      Write(writer, overload, High);
      Assert.That(target.ToString(), Is.Empty);
    });

    Assert.That(target.ToString(), Is.EqualTo(@"\ud83d"));
  }

  /// <summary>A low surrogate on its own never pairs with anything.</summary>
  /// <param name="overload">How the surrogate is written.</param>
  [Test]
  public void LowSurrogateIsEscaped([Values] Overload overload)
  {
    using RecordingWriter target = new();

    Render(target, writer => Write(writer, overload, Low));

    Assert.That(target.ToString(), Is.EqualTo(@"\ude00"));
  }

  /// <summary>What follows a held surrogate decides its fate, and is kept itself.</summary>
  [Test]
  public void HeldSurrogateIsEscapedByTheNextCharacter()
  {
    using RecordingWriter target = new();

    Render(target, writer =>
    {
      writer.Write(High);
      writer.Write('a');
      writer.Write(High);
      writer.Write("bc");
      writer.Write(High);
      writer.Write("de".ToCharArray(), 0, 2);
    });

    Assert.That(target.ToString(), Is.EqualTo(@"\ud83da\ud83dbc\ud83dde"));
  }

  /// <summary>Of two high surrogates in a row only the second can still get its low half.</summary>
  [Test]
  public void SecondHighSurrogateReplacesTheHeldOne()
  {
    using RecordingWriter target = new();

    Render(target, writer =>
    {
      writer.Write(High);
      writer.Write(new string(new[] { High, High, Low, 'x', High }));
      writer.Write(Low);
    });

    Assert.That(target.ToString(), Is.EqualTo(@"\ud83d\ud83d" + Pair + "x" + Pair));
  }

  /// <summary>A surrogate inside a string or array is escaped where it stands.</summary>
  [Test]
  public void SurrogateInsideTextIsEscaped()
  {
    using RecordingWriter target = new();

    Render(target, writer =>
    {
      writer.Write(new string(new[] { 'a', High, 'b' }));
      writer.Write(new[] { 'c', Low, 'd' }, 0, 3);
    });

    Assert.That(target.ToString(), Is.EqualTo(@"a\ud83dbc\ude00d"));
  }

  /// <summary>A flush may fall between the two halves of a pair.</summary>
  [Test]
  public void FlushKeepsAHeldSurrogate()
  {
    using RecordingWriter target = new();

    Render(target, writer =>
    {
      writer.Write(High);
      writer.Flush();
      writer.Write(Low);
    });

    Assert.That(target.ToString(), Is.EqualTo(Pair));
  }

  /// <summary>Layouts wrap and close their writer, the target belongs to the appender.</summary>
  [Test]
  public void DisposeEscapesAHeldSurrogateAndLeavesTheTargetOpen()
  {
    using RecordingWriter target = new();

    Render(target, writer =>
    {
      writer.Write(High);
      writer.Dispose();
    });

    Assert.That(target.Closed, Is.False);
    Assert.That(target.ToString(), Is.EqualTo(@"\ud83d"));
  }

  /// <summary>Text after the wrapper must look as if it was written to the target.</summary>
  [Test]
  public void NewLineFollowsTheTarget()
  {
    using RecordingWriter target = new() { NewLine = "|" };

    Render(target, writer =>
    {
      writer.WriteLine("a");
      writer.WriteLine();
      Assert.That(writer.Encoding, Is.EqualTo(target.Encoding));
      Assert.That(writer.FormatProvider, Is.EqualTo(target.FormatProvider));
    });

    Assert.That(target.ToString(), Is.EqualTo("a||"));
  }
}
