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
using System.Text;

using log4net.Appender;

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

  private static Encoding WithEscapingFallback(Encoding encoding)
    => _contentEscape.Invoke<Encoding>(nameof(WithEscapingFallback), [encoding]);

  private static string EscapeUnencodableText(string message, Encoding encoding)
    => _contentEscape.Invoke<string>(nameof(EscapeUnencodableText), [message, encoding]);

  private static string EscapeUnpairedSurrogates(string message)
    => _contentEscape.Invoke<string>(nameof(EscapeUnpairedSurrogates), [message]);
}
