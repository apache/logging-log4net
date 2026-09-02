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

using System.Reflection;

using log4net.Appender;

using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Tests for the internal <c>ContentEscape</c> helper.
/// </summary>
[TestFixture]
public class ContentEscapeTest
{
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

  private static string EscapeUnpairedSurrogates(string message)
    => (string)typeof(TelnetAppender).Assembly
      .GetType("log4net.Appender.Internal.ContentEscape")!
      .GetMethod("EscapeUnpairedSurrogates", BindingFlags.Static | BindingFlags.NonPublic)!
      .Invoke(null, [message])!;
}
