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
using System.Linq;
using System.Text;

using log4net.Appender;
using log4net.Core;
using log4net.Layout;

using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Tests for the internal <c>ContentEscape</c> helper.
/// </summary>
[TestFixture]
public class ContentEscapeTest
{
  private static readonly Type _contentEscape =
    typeof(TelnetAppender).Assembly.NonPublicType("log4net.Appender.Internal.ContentEscape");

  /// <summary>
  /// An unpaired surrogate cannot be encoded, and an encoder that throws costs the event. The
  /// input is built here rather than in the attribute: an attribute argument lives in metadata as
  /// UTF-8, so the compiler would replace the surrogate with U+FFFD before the test ran.
  /// </summary>
  [TestCase(0xd800)]
  [TestCase(0xdbff)]
  [TestCase(0xdc00)]
  [TestCase(0xdfff)]
  public void UnpairedSurrogatesAreEscaped(int surrogate)
  {
    string input = "before" + (char)surrogate + "after";

    Assert.That(EscapeUnpairedSurrogates(input), Is.EqualTo($@"before\u{surrogate:x4}after"));
  }

  /// <summary>Every one of them, not just the first.</summary>
  [Test]
  public void EveryUnpairedSurrogateIsEscaped()
    => Assert.That(EscapeUnpairedSurrogates("a" + (char)0xd800 + "b" + (char)0xdc00 + "c"),
      Is.EqualTo(@"a\ud800b\udc00c"));

  /// <summary>A valid pair is one character and must survive untouched.</summary>
  [Test]
  public void ValidSurrogatePairsAreLeftAlone()
    => Assert.That(EscapeUnpairedSurrogates("emoji \U0001F600 here"), Is.EqualTo("emoji \U0001F600 here"));

  /// <summary>The common case takes a fast path that must not alter anything.</summary>
  [TestCase("")]
  [TestCase("plain ascii")]
  [TestCase("Schönwetter 你好")]
  public void MessagesWithoutSurrogatesAreUnchanged(string message)
    => Assert.That(EscapeUnpairedSurrogates(message), Is.EqualTo(message));

  /// <summary>What the encoding carries is untouched, what it cannot is a visible escape.</summary>
  [TestCase("a\uff1cb", @"a\uff1cb")]
  [TestCase("Sch\u00f6n", "Sch\u00f6n")]
  [TestCase("\U0001f600", @"\ud83d\ude00")]
  public void TextIsEscapedForTheEncoding(string text, string expected)
    => Assert.That(EscapeUnencodableText(text, Encoding.GetEncoding("iso-8859-1")), Is.EqualTo(expected));

  /// <summary>Only the fallback differs: same code page, same preamble, the original untouched.</summary>
  [Test]
  public void TheCopyDiffersOnlyInItsFallback()
  {
    UTF8Encoding original = new(encoderShouldEmitUTF8Identifier: true);
    EncoderFallback fallback = original.EncoderFallback;

    Encoding copy = WithEscapingFallback(original);

    Assert.That(copy.CodePage, Is.EqualTo(original.CodePage));
    Assert.That(copy.GetPreamble(), Is.EqualTo(original.GetPreamble()));
    Assert.That(copy.EncoderFallback, Is.Not.SameAs(fallback));
    Assert.That(original.EncoderFallback, Is.SameAs(fallback));
  }

  /// <summary>On .NET the sealed UTF-8 singleton has a <c>Clone</c> of its own.</summary>
  [Test]
  public void TheSealedUtf8KeepsItsPreamble()
    => Assert.That(WithEscapingFallback(Encoding.UTF8).GetPreamble(), Is.EqualTo(Encoding.UTF8.GetPreamble()));

  /// <summary>A stateful encoding keeps its text through the round trip the core mail appender takes.</summary>
  [Test]
  public void AStatefulEncodingKeepsItsText()
    => Assert.That(EscapeUnencodableText("\u3042 \uff1c\U0001f600", TestEncodings.Get("iso-2022-jp")),
      Is.EqualTo("\u3042 \uff1c\\ud83d\\ude00"));

  /// <summary>An extra layout such as a category gets the same escape as the main layout.</summary>
  [Test]
  public void AnExtraLayoutEscapesUnpairedSurrogates()
  {
    PatternLayout layout = new("[%message]");
    layout.ActivateOptions();
    LoggingEvent loggingEvent = new(typeof(ContentEscapeTest), null, nameof(ContentEscapeTest), Level.Info,
      "a\ud83db \U0001F600", null);

    Assert.That(FormatWithEscapedSurrogates(layout, loggingEvent), Is.EqualTo("[a\\ud83db \U0001F600]"));
  }

  /// <summary>Text that fits comes back unchanged, with its encoded length.</summary>
  [Test]
  public void TextThatFitsIsNotCut()
  {
    string result = TruncateToBytes("hello", Encoding.ASCII, 5, out int byteCount);

    Assert.That(result, Is.EqualTo("hello"));
    Assert.That(byteCount, Is.EqualTo(5));
  }

  /// <summary>The marker counts against the budget.</summary>
  [Test]
  public void TheMarkerIsPartOfTheBudget()
  {
    string result = TruncateToBytes(new string('a', 1000), Encoding.ASCII, 512, out int byteCount);

    Assert.That(result, Is.EqualTo(new string('a', 498) + "...[truncated]"));
    Assert.That(byteCount, Is.EqualTo(512));
  }

  /// <summary>A cut inside a three byte character would decode as the replacement character.</summary>
  [Test]
  public void ACutKeepsMultiByteCharactersWhole()
  {
    string result = TruncateToBytes(new string('\u3042', 1000), Encoding.UTF8, 516, out int byteCount);

    // 516 bytes hold the 14 byte marker and 167 of the three byte characters, one byte stays unused.
    Assert.That(result, Is.EqualTo(new string('\u3042', 167) + "...[truncated]"));
    Assert.That(byteCount, Is.EqualTo(515));
  }

  /// <summary>The same for the two halves of a surrogate pair.</summary>
  [Test]
  public void ACutKeepsSurrogatePairsWhole()
  {
    string result = TruncateToBytes(string.Concat(Enumerable.Repeat("\U0001F600", 1000)), Encoding.UTF8, 516,
      out int byteCount);

    Assert.That(result, Is.EqualTo(string.Concat(Enumerable.Repeat("\U0001F600", 125)) + "...[truncated]"));
    Assert.That(byteCount, Is.EqualTo(514));
  }

  /// <summary>A pair across the boundary of the chunks the cut works in stays whole.</summary>
  [Test]
  public void APairAcrossAChunkBoundaryStaysWhole()
  {
    string text = "a" + string.Concat(Enumerable.Repeat("\U0001F600", 1000));

    string result = TruncateToBytes(text, Encoding.UTF8, 3015, out int byteCount);

    Assert.That(result, Is.EqualTo("a" + string.Concat(Enumerable.Repeat("\U0001F600", 750)) + "...[truncated]"));
    Assert.That(byteCount, Is.EqualTo(3015));
  }

  /// <summary>A budget larger than the encoder's buffer is filled across several rounds.</summary>
  [Test]
  public void ABudgetAboveTheBufferIsFilled()
  {
    string result = TruncateToBytes(new string('a', 10000), Encoding.ASCII, 5000, out int byteCount);

    Assert.That(result, Is.EqualTo(new string('a', 4986) + "...[truncated]"));
    Assert.That(byteCount, Is.EqualTo(5000));
  }

  /// <summary>
  /// Latin-1 escapes a pair as two <c>\uXXXX</c>, 12 bytes. When only part of that fits, the pair goes whole.
  /// </summary>
  [Test]
  public void AnEscapedPairThatOnlyPartlyFitsGoesWhole()
  {
    Encoding latin1 = WithEscapingFallback(Encoding.GetEncoding("iso-8859-1"));
    string text = string.Concat(Enumerable.Repeat("\U0001F600", 10));

    string result = TruncateToBytes(text, latin1, 32, out int byteCount);

    Assert.That(result, Is.EqualTo("\U0001F600...[truncated]"));
    Assert.That(byteCount, Is.EqualTo(26));
  }

  /// <summary>A pair whose escape is only partly written at a chunk boundary is dropped whole.</summary>
  [Test]
  public void APartlyEscapedPairAtAChunkBoundaryIsDroppedWhole()
  {
    Encoding latin1 = WithEscapingFallback(Encoding.GetEncoding("iso-8859-1"));
    string text = new string('a', 1023) + string.Concat(Enumerable.Repeat("\U0001F600", 10));

    // 1023 bytes of text, 8 to spare for a pair whose escape takes 12.
    string result = TruncateToBytes(text, latin1, 1045, out int byteCount);

    Assert.That(result, Is.EqualTo(new string('a', 1023) + "...[truncated]"));
    Assert.That(byteCount, Is.EqualTo(1037));
  }

  /// <summary>
  /// A high surrogate held back at a chunk boundary is not kept, even when its replacement character
  /// would fit where the pair does not.
  /// </summary>
  [Test]
  public void AHighSurrogateHeldBackAtAChunkBoundaryIsNotKept()
  {
    string text = new string('a', 1023) + string.Concat(Enumerable.Repeat("\U0001F600", 10));

    // 1023 bytes of text, 3 to spare: U+FFFD for a lone half would fit, the pair's 4 bytes do not.
    string result = TruncateToBytes(text, Encoding.UTF8, 1040, out int byteCount);

    Assert.That(result, Is.EqualTo(new string('a', 1023) + "...[truncated]"));
    Assert.That(byteCount, Is.EqualTo(1037));
  }

  /// <summary>A budget below the marker never throws: the text goes, the marker stays.</summary>
  [Test]
  public void ABudgetBelowTheMarkerGivesTheMarkerAlone()
  {
    string result = TruncateToBytes(new string('a', 100), Encoding.ASCII, 10, out int byteCount);

    Assert.That(result, Is.EqualTo("...[truncated]"));
    Assert.That(byteCount, Is.EqualTo(14));
  }

  /// <summary>Room for the marker but not for the first character is the same case.</summary>
  [Test]
  public void ABudgetTooSmallForTheFirstCharacterGivesTheMarkerAlone()
  {
    string result = TruncateToBytes(new string('\u3042', 10), Encoding.UTF8, 16, out int byteCount);

    Assert.That(result, Is.EqualTo("...[truncated]"));
    Assert.That(byteCount, Is.EqualTo(14));
  }

  private static Encoding WithEscapingFallback(Encoding encoding)
    => _contentEscape.Invoke<Encoding>(nameof(WithEscapingFallback), [encoding]);

  private static string EscapeUnencodableText(string message, Encoding encoding)
    => _contentEscape.Invoke<string>(nameof(EscapeUnencodableText), [message, encoding]);

  private static string EscapeUnpairedSurrogates(string message)
    => _contentEscape.Invoke<string>(nameof(EscapeUnpairedSurrogates), [message]);

  private static string TruncateToBytes(string text, Encoding encoding, int maxBytes, out int byteCount)
  {
    object?[] arguments = [text, encoding, maxBytes, 0];
    string result = _contentEscape.Invoke<string>(nameof(TruncateToBytes), arguments);
    byteCount = (int)arguments[3]!;
    return result;
  }

  private static string FormatWithEscapedSurrogates(LayoutSkeleton layout, LoggingEvent loggingEvent)
    => _contentEscape.Invoke<string>(nameof(FormatWithEscapedSurrogates), [layout, loggingEvent]);
}
