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
using System.Reflection;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Util;

[TestFixture]
public class TransformTest
{
  /// <summary>
  /// Verifies that a valid BMP character in the allowed XML 1.0 range (U+203B ※)
  /// is passed through unchanged.
  /// </summary>
  [Test]
  public void MaskXmlInvalidCharactersAllowsJapaneseCharacters()
  {
    const string kome = "\u203B";
    Assert.That(Transform.MaskXmlInvalidCharacters(kome, "?"), Is.EqualTo(kome));
  }

  /// <summary>
  /// Verifies that the NUL character (U+0000), which is illegal in XML 1.0,
  /// is replaced with the supplied mask string.
  /// </summary>
  [Test]
  public void MaskXmlInvalidCharactersMasks0Char()
  {
    const string c = "\0";
    Assert.That(Transform.MaskXmlInvalidCharacters(c, "?"), Is.EqualTo("?"));
  }

  /// <summary>
  /// Verifies that a well-formed UTF-16 surrogate pair representing a supplementary
  /// character (U+1F511 🔑) is treated as a valid XML character and preserved intact.
  /// </summary>
  [Test]
  public void MaskXmlInvalidCharactersPreservesValidSurrogatePair()
  {
    // U+1F511 KEY (🔑) — encoded as surrogate pair \uD83D\uDD11
    const string key = "\uD83D\uDD11";
    Assert.That(Transform.MaskXmlInvalidCharacters(key, "?"), Is.EqualTo(key));
  }

  /// <summary>
  /// Verifies that a supplementary character embedded within plain ASCII text
  /// is preserved without corrupting the surrounding characters.
  /// </summary>
  [Test]
  public void MaskXmlInvalidCharactersPreservesSupplementaryCharacterInContext()
  {
    // "admin🔑" — only the emoji is a surrogate pair; all other chars are BMP
    const string input = "admin\uD83D\uDD11";
    Assert.That(Transform.MaskXmlInvalidCharacters(input, "?"), Is.EqualTo(input));
  }

  /// <summary>
  /// Verifies that a lone high surrogate (U+D800–U+DBFF) not followed by a
  /// low surrogate is malformed UTF-16 and is replaced with the mask string.
  /// </summary>
  [Test]
  public void MaskXmlInvalidCharactersMasksLoneHighSurrogate()
  {
    // A lone high surrogate with no following low surrogate is illegal
    const string loneSurrogate = "\uD83D";
    Assert.That(Transform.MaskXmlInvalidCharacters(loneSurrogate, "?"), Is.EqualTo("?"));
  }

  /// <summary>
  /// Verifies that a lone low surrogate (U+DC00–U+DFFF) not preceded by a
  /// high surrogate is malformed UTF-16 and is replaced with the mask string.
  /// </summary>
  [Test]
  public void MaskXmlInvalidCharactersMasksLoneLowSurrogate()
  {
    const string loneSurrogate = "\uDD11";
    Assert.That(Transform.MaskXmlInvalidCharacters(loneSurrogate, "?"), Is.EqualTo("?"));
  }

  /// <summary>
  /// Verifies current behaviour: a null input throws <see cref="ArgumentNullException"/>
  /// (delegated to the regex engine). Add a null-guard to <see cref="Transform.MaskXmlInvalidCharacters"/>
  /// if a graceful fallback is preferred.
  /// </summary>
  [Test]
  public void MaskXmlInvalidCharactersNullInputThrowsArgumentNullException()
    => Assert.That(() => Transform.MaskXmlInvalidCharacters(null!, "?"),
        Throws.ArgumentNullException);

  #region CountSubstrings tests (private method accessed via reflection)

  /// <summary>
  /// Resolves the private static <see cref="CountSubstrings"/> method once for all tests
  /// </summary>
  private static Func<string, string, int> CountSubstrings { get; } = (Func<string, string, int>)Delegate.CreateDelegate(
    typeof(Func<string, string, int>), typeof(Transform).GetMethod(nameof(CountSubstrings), BindingFlags.NonPublic | BindingFlags.Static)
    ?? throw new MissingMethodException(nameof(Transform), nameof(CountSubstrings)));

  /// <summary>
  /// Verifies that an empty text returns zero regardless of the substring.
  /// </summary>
  [Test]
  public void CountSubstringsEmptyTextReturnsZero()
    => Assert.That(CountSubstrings("", "a"), Is.Zero);

  /// <summary>
  /// Verifies that an empty substring returns zero regardless of the text.
  /// </summary>
  [Test]
  public void CountSubstringsEmptySubstringReturnsZero()
    => Assert.That(CountSubstrings("abc", ""), Is.Zero);

  /// <summary>
  /// Verifies that a substring not present in the text returns zero.
  /// </summary>
  [Test]
  public void CountSubstringsNoMatchReturnsZero()
    => Assert.That(CountSubstrings("hello", "x"), Is.Zero);

  /// <summary>
  /// Verifies that a single-character substring occurring once is counted correctly.
  /// </summary>
  [Test]
  public void CountSubstringsSingleCharSingleMatchReturnsOne()
    => Assert.That(CountSubstrings("hello", "h"), Is.EqualTo(1));

  /// <summary>
  /// Verifies that a single-character substring occurring multiple times is counted correctly.
  /// </summary>
  [Test]
  public void CountSubstringsSingleCharMultipleMatchesReturnsCorrectCount()
    => Assert.That(CountSubstrings("banana", "a"), Is.EqualTo(3));

  /// <summary>
  /// Verifies counting of XML special characters as used by <c>WriteEscapedXmlString</c>.
  /// </summary>
  [TestCase("<hello><world>", "<", 2)]
  [TestCase("<hello><world>", ">", 2)]
  [TestCase("a&amp;&amp;b", "&", 2)]
  public void CountSubstringsXmlSpecialCharsReturnsCorrectCount(string text, string substring, int expected)
    => Assert.That(CountSubstrings(text, substring), Is.EqualTo(expected));

  /// <summary>
  /// Verifies that the CDATA end token <c>]]&gt;</c> is counted correctly as a multi-character substring.
  /// </summary>
  [Test]
  public void CountSubstringsCdataEndReturnsCorrectCount()
    => Assert.That(CountSubstrings("foo]]>bar]]>baz", "]]>"), Is.EqualTo(2));

  /// <summary>
  /// Verifies that non-overlapping occurrences are counted and overlapping ones are not,
  /// consistent with the method's documented assumption.
  /// </summary>
  [Test]
  public void CountSubstringsNonOverlappingReturnsCorrectCount()
    => Assert.That(CountSubstrings("aaaa", "aa"), Is.EqualTo(2));

  /// <summary>
  /// Verifies that a substring equal to the entire text counts as one match.
  /// </summary>
  [Test]
  public void CountSubstringsSubstringEqualsTextReturnsOne()
    => Assert.That(CountSubstrings("]]>", "]]>"), Is.EqualTo(1));
  #endregion
}