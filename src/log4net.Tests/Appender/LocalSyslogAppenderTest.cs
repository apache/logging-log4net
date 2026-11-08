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

using System.Linq;

using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Tests.Appender.Internal;

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
    FieldInfo field = typeof(LocalSyslogAppender).NonPublicField("_handleToIdentity");

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
    => typeof(LocalSyslogAppender).GetFieldValue<IntPtr>("_handleToIdentity");

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

  /// <summary>
  /// The record is the printf format string libc parses, so a percent sign in content would make
  /// the callee read an argument that was never passed.
  /// </summary>
  [TestCase("%s", "%%s")]
  [TestCase("100%", "100%%")]
  [TestCase("%d%n", "%%d%%n")]
  [TestCase("rate=%s of %d", "rate=%%s of %%d")]
  public void EveryPercentSignIsEscaped(string message, string expected)
    => Assert.That(EscapePercent(message), Is.EqualTo(expected));

  /// <summary>A message without a percent sign takes the fast path and comes through untouched.</summary>
  [Test]
  public void MessagesWithoutPercentSignsAreUnchanged()
    => Assert.That(EscapePercent("field=1 field=2"), Is.EqualTo("field=1 field=2"));

  /// <summary>
  /// Only the two fixed parameters of the variadic libc function may be declared. A variadic
  /// argument travels differently from a fixed one on Apple arm64, where the callee then reads the
  /// wrong slot.
  /// </summary>
  [Test]
  public void TheSyslogDeclarationPassesNoVariadicArgument()
  {
    MethodInfo syslog = typeof(LocalSyslogAppender).Assembly
      .NonPublicType("log4net.Util.NativeMethods")
      .NonPublicMethod("syslog");

    Assert.That(syslog.GetParameters().Select(parameter => parameter.ParameterType),
      Is.EqualTo(new[] { typeof(int), typeof(string) }));
  }

  /// <summary>
  /// Content that looks like a printf conversion reaches libc as the format string, so the
  /// escaping has to hold on the real call.
  /// </summary>
  [Test]
  [Platform("Linux")]
  [NonParallelizable]
  public void ContentThatLooksLikeAFormatStringIsLogged()
  {
    LocalSyslogAppender appender = new()
    {
      Identity = "log4net-test-format",
      Layout = new PatternLayout("%message")
    };
    appender.ActivateOptions();

    Assert.DoesNotThrow(() => appender.DoAppend(new LoggingEvent(new LoggingEventData
    {
      Level = Level.Info,
      Message = "%s %n %x %99999$s",
      LoggerName = "FormatString"
    })));
  }

  /// <summary>The default leaves room for glibc's prefix under the 212960 byte datagram.</summary>
  [Test]
  public void MaxRecordSizeDefaultsTo204800()
    => Assert.That(new LocalSyslogAppender().MaxRecordSize, Is.EqualTo(204800));

  /// <summary>Below 512 a record could not hold much more than the marker.</summary>
  [TestCase(0)]
  [TestCase(511)]
  public void MaxRecordSizeRejectsValuesBelow512(int value)
    => Assert.That(() => new LocalSyslogAppender().MaxRecordSize = value,
      Throws.TypeOf<ArgumentOutOfRangeException>());

  /// <summary>A record of exactly the limit is sent as it is, and nothing is reported.</summary>
  [Test]
  public void ARecordOfExactlyTheLimitIsUnchanged()
  {
    LocalSyslogAppender appender = new() { MaxRecordSize = 512, NewLineHandling = SyslogNewLineHandling.Keep };
    RecordingErrorHandler errorHandler = new();
    appender.ErrorHandler = errorHandler;

    Assert.That(GetRecords(appender, new string('a', 512)), Is.EqualTo(new[] { new string('a', 512) }));
    Assert.That(errorHandler.Messages, Is.Empty);
  }

  /// <summary>glibc drops a datagram over its send buffer silently: the record is cut and reported instead.</summary>
  [Test]
  public void AnOversizeRecordIsCutAndReported()
  {
    LocalSyslogAppender appender = new() { MaxRecordSize = 512, NewLineHandling = SyslogNewLineHandling.Keep };
    RecordingErrorHandler errorHandler = new();
    appender.ErrorHandler = errorHandler;

    string[] records = GetRecords(appender, new string('a', 1000));

    Assert.That(records, Is.EqualTo(new[] { new string('a', 498) + "...[truncated]" }));
    Assert.That(errorHandler.Messages, Has.Count.EqualTo(1));
  }

  /// <summary>The limit counts UTF-8 bytes, and a cut keeps characters whole.</summary>
  [Test]
  public void TheLimitCountsUtf8Bytes()
  {
    LocalSyslogAppender appender = new() { MaxRecordSize = 512, NewLineHandling = SyslogNewLineHandling.Keep };
    appender.ErrorHandler = new RecordingErrorHandler();

    Assert.That(GetRecords(appender, new string('\u3042', 500)),
      Is.EqualTo(new[] { new string('\u3042', 166) + "...[truncated]" }));
  }

  /// <summary>
  /// The cut comes before the percent signs are doubled: glibc folds them back, and a cut after
  /// doubling could leave a lone percent sign.
  /// </summary>
  [Test]
  public void PercentSignsAreCountedOnce()
  {
    LocalSyslogAppender appender = new() { MaxRecordSize = 512, NewLineHandling = SyslogNewLineHandling.Keep };
    appender.ErrorHandler = new RecordingErrorHandler();

    Assert.That(GetRecords(appender, new string('%', 1000)), Is.EqualTo(new[] { new string('%', 498) + "...[truncated]" }));
  }

  /// <summary>Escaped newlines are longer than the newlines, so the escape comes before the cut.</summary>
  [Test]
  public void EscapedNewLinesCountAgainstTheLimit()
  {
    LocalSyslogAppender appender = new() { MaxRecordSize = 512, NewLineHandling = SyslogNewLineHandling.Escape };
    appender.ErrorHandler = new RecordingErrorHandler();

    string expected = string.Concat(Enumerable.Repeat("\\n", 249)) + "...[truncated]";
    Assert.That(GetRecords(appender, new string('\n', 300)), Is.EqualTo(new[] { expected }));
  }

  /// <summary>In split mode the limit applies to each line on its own.</summary>
  [Test]
  public void SplitModeCutsEachLine()
  {
    LocalSyslogAppender appender = new() { MaxRecordSize = 512, NewLineHandling = SyslogNewLineHandling.Split };
    RecordingErrorHandler errorHandler = new();
    appender.ErrorHandler = errorHandler;

    string[] records = GetRecords(appender, "short\n" + new string('a', 1000));

    Assert.That(records, Is.EqualTo(new[] { "short", new string('a', 498) + "...[truncated]" }));
    Assert.That(errorHandler.Messages, Has.Count.EqualTo(1));
  }

  private static string[] GetRecords(LocalSyslogAppender appender, string message)
    => appender.Invoke<string[]>(nameof(GetRecords), [message]);

  private static string EscapePercent(string message)
    => typeof(LocalSyslogAppender).Invoke<string>(nameof(EscapePercent), [message]);

  private static string EscapeNewLines(string message)
    => typeof(LocalSyslogAppender).Invoke<string>(nameof(EscapeNewLines), [message]);

  private static string[] SplitLines(string message)
    => typeof(LocalSyslogAppender).Invoke<string[]>(nameof(SplitLines), [message]);

  private static string EscapeNulCharacters(string message)
    => typeof(LocalSyslogAppender).Assembly
      .NonPublicType("log4net.Appender.Internal.ContentEscape")
      .Invoke<string>(nameof(EscapeNulCharacters), [message]);
}
