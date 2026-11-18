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
using log4net.Appender;
using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Tests for the internal <c>MailBodies</c> helper.
/// </summary>
[TestFixture]
public sealed class MailBodiesTest
{
  private const string Euro = "€";

  private const string Emoji = "\U0001F600";

  private static readonly Type _mailBodies =
    typeof(SmtpPickupDirAppender).Assembly.NonPublicType("log4net.Appender.Internal.MailBodies");

  /// <summary>
  /// RFC 5322 allows 998 octets per line, the line break excluded.
  /// </summary>
  [TestCase(998, true)]
  [TestCase(999, false)]
  public void AnAsciiLineFitsUpTo998Octets(int length, bool fits)
    => Assert.That(FitsEightBit(new string('a', length)), Is.EqualTo(fits));

  /// <summary>
  /// The line break does not count against the limit, a CR before the LF included.
  /// </summary>
  [TestCase(998, "\n", true, TestName = "LfAfter998")]
  [TestCase(999, "\n", false, TestName = "LfAfter999")]
  [TestCase(998, "\r\n", true, TestName = "CrLfAfter998")]
  [TestCase(999, "\r\n", false, TestName = "CrLfAfter999")]
  public void TheLineBreakDoesNotCount(int length, string lineBreak, bool fits)
    => Assert.That(FitsEightBit(new string('a', length) + lineBreak + "next" + lineBreak), Is.EqualTo(fits));

  /// <summary>
  /// A Cyrillic letter is two octets: 500 of them are 1000 octets.
  /// </summary>
  [TestCase(499, true)]
  [TestCase(500, false)]
  public void CyrillicLettersCountTwoOctets(int count, bool fits)
    => Assert.That(FitsEightBit(Repeat("я", count)), Is.EqualTo(fits));

  /// <summary>
  /// The limit is in UTF-8 octets, not characters: 333 euro signs are 999 octets.
  /// </summary>
  [TestCase(332, true)]
  [TestCase(333, false)]
  public void EuroSignsCountThreeOctets(int count, bool fits)
    => Assert.That(FitsEightBit(Repeat(Euro, count)), Is.EqualTo(fits));

  /// <summary>
  /// A character outside the BMP is four octets: 249 of them plus 2 are 998 octets.
  /// </summary>
  [TestCase(249, true)]
  [TestCase(250, false)]
  public void EmojiCountFourOctets(int count, bool fits)
    => Assert.That(FitsEightBit(Repeat(Emoji, count) + "aa"), Is.EqualTo(fits));

  /// <summary>
  /// An unpaired surrogate is written as U+FFFD, three octets: 332 euro signs, it and one more are 1000.
  /// </summary>
  [Test]
  public void AnUnpairedSurrogateCountsThreeOctets()
    => Assert.That(FitsEightBit(Repeat(Euro, 332) + "\uD83D" + "a"), Is.False);

  /// <summary>
  /// 8bit forbids a NUL (RFC 2045 2.8).
  /// </summary>
  [Test]
  public void ANulDoesNotFit()
    => Assert.That(FitsEightBit("a\0b"), Is.False);

  /// <summary>
  /// Short lines fit however long the body is.
  /// </summary>
  [TestCase("\n", TestName = "ShortLfLinesFit")]
  [TestCase("\r\n", TestName = "ShortCrLfLinesFit")]
  public void ShortLinesFit(string lineBreak)
    => Assert.That(FitsEightBit(string.Join(lineBreak, Enumerable.Repeat(new string('a', 500), 3)) + lineBreak), Is.True);

  private static string Repeat(string text, int count) => string.Concat(Enumerable.Repeat(text, count));

  private static bool FitsEightBit(string body) => _mailBodies.Invoke<bool>(nameof(FitsEightBit), [body]);
}
