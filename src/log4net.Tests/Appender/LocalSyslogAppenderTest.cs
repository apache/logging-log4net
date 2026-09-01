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

using log4net.Appender;
using log4net.Layout;

using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Tests for <see cref="LocalSyslogAppender"/>
/// </summary>
/// <remarks>
/// <para>
/// The appender itself writes through <c>syslog(3)</c>, whose output cannot be read back from a
/// test, so these tests cover the message preparation that happens before the native call.
/// </para>
/// </remarks>
[TestFixture]
public class LocalSyslogAppenderTest
{
  /// <summary>
  /// The message is marshaled to libc as a null-terminated string, so a NUL character in logged
  /// content would end the record there and drop everything the layout rendered after it.
  /// </summary>
  [Test]
  public void NulCharactersAreEscaped()
    => Assert.That(EscapeNulCharacters("priority=high\0user=alice"), Is.EqualTo("priority=high\\0user=alice"));

  /// <summary>
  /// Several NUL characters must all be escaped, not just the first one.
  /// </summary>
  [Test]
  public void EveryNulCharacterIsEscaped()
    => Assert.That(EscapeNulCharacters("a\0b\0c"), Is.EqualTo("a\\0b\\0c"));

  /// <summary>
  /// This escape is only about NUL. Newlines are <see cref="LocalSyslogAppender.NewLineHandling"/>.
  /// </summary>
  [Test]
  public void MessagesWithoutNulCharactersAreUnchanged()
  {
    const string message = "System.InvalidOperationException: boom\r\n   at Program.Main()\tfield=1";

    Assert.That(EscapeNulCharacters(message), Is.EqualTo(message));
  }

  /// <summary>
  /// An empty message takes the fast path, which must not turn it into anything else.
  /// </summary>
  [Test]
  public void EmptyMessageIsUnchanged()
    => Assert.That(EscapeNulCharacters(string.Empty), Is.Empty);

  /// <summary>
  /// <c>openlog</c> registers the identity for the process rather than for an appender, so the
  /// handle to it has to be shared instead of being kept per instance.
  /// </summary>
  [Test]
  public void TheIdentityHandleBelongsToTheProcess()
  {
    FieldInfo field = typeof(LocalSyslogAppender)
      .GetField("_handleToIdentity", BindingFlags.Static | BindingFlags.NonPublic)
      ?? throw new InvalidOperationException("LocalSyslogAppender._handleToIdentity is missing");

    Assert.That(field.IsStatic, Is.True);
  }

  /// <summary>
  /// Activating twice has to replace the registered identity rather than allocate another one and
  /// forget the first, which leaked a buffer per call.
  /// </summary>
  [Test]
  [Platform("Linux")]
  [NonParallelizable]
  public void ActivatingTwiceReplacesTheIdentity()
  {
    LocalSyslogAppender appender = new() { Identity = "log4net-test-first", Layout = new PatternLayout("%message") };
    appender.ActivateOptions();
    IntPtr first = CurrentIdentityHandle();

    appender.Identity = "log4net-test-second";
    appender.ActivateOptions();
    IntPtr second = CurrentIdentityHandle();

    Assert.That(first, Is.Not.EqualTo(IntPtr.Zero));
    Assert.That(second, Is.Not.EqualTo(IntPtr.Zero));
    Assert.That(second, Is.Not.EqualTo(first));
  }

  private static IntPtr CurrentIdentityHandle()
    => (IntPtr)typeof(LocalSyslogAppender)
      .GetField("_handleToIdentity", BindingFlags.Static | BindingFlags.NonPublic)!
      .GetValue(null)!;

  /// <summary>
  /// A newline ends the record for a daemon that writes the message through to a line oriented
  /// log, so content could otherwise forge a second entry. glibc does not escape it.
  /// </summary>
  [Test]
  public void NewLinesAreEscaped()
    => Assert.That(EscapeNewLines("value\r\nJan  1 00:00:00 host sshd[1]: forged"),
      Is.EqualTo("value\\r\\nJan  1 00:00:00 host sshd[1]: forged"));

  /// <summary>Both characters count, on their own as well as paired.</summary>
  [TestCase("a\rb", "a\\rb")]
  [TestCase("a\nb", "a\\nb")]
  [TestCase("a\n\nb", "a\\n\\nb")]
  public void EveryNewLineIsEscaped(string message, string expected)
    => Assert.That(EscapeNewLines(message), Is.EqualTo(expected));

  /// <summary>A message without newlines takes the fast path and comes through untouched.</summary>
  [Test]
  public void MessagesWithoutNewLinesAreUnchanged()
    => Assert.That(EscapeNewLines("field=1\tfield=2"), Is.EqualTo("field=1\tfield=2"));

  /// <summary>Escaping is the default, because a daemon that splits the record is the common case.</summary>
  [Test]
  public void NewLineHandlingDefaultsToEscape()
    => Assert.That(new LocalSyslogAppender().NewLineHandling,
      Is.EqualTo(SyslogNewLineHandling.Escape));

  /// <summary>One record per line, and a blank line is no record at all.</summary>
  [Test]
  public void SplittingDropsTheEmptyLines()
    => Assert.That(SplitLines("first\r\nsecond\n\nthird\r"), Is.EqualTo(new[] { "first", "second", "third" }));

  private static string EscapeNewLines(string message)
    => (string)typeof(LocalSyslogAppender)
      .GetMethod("EscapeNewLines", BindingFlags.Static | BindingFlags.NonPublic)!
      .Invoke(null, [message])!;

  private static string[] SplitLines(string message)
    => (string[])typeof(LocalSyslogAppender)
      .GetMethod("SplitLines", BindingFlags.Static | BindingFlags.NonPublic)!
      .Invoke(null, [message])!;

  private static string EscapeNulCharacters(string message)
    => (string)typeof(LocalSyslogAppender).Assembly
      .GetType("log4net.Appender.Internal.NativeStringEscape")!
      .GetMethod("EscapeNulCharacters", BindingFlags.Static | BindingFlags.NonPublic)!
      .Invoke(null, [message])!;
}
