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
using log4net.Tests.Appender.Internal;
using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Tests for <see cref="UdpAppender"/>
/// </summary>
[TestFixture]
public sealed class UdpAppenderTest
{
  private const string Marker = "...[truncated]";

  /// <summary>Calls the internal encoder the way <c>Append</c> does.</summary>
  private static byte[] GetDatagramBytes(UdpAppender appender, string message)
    => appender.Invoke<byte[]>(nameof(GetDatagramBytes), [message]);

  /// <summary>The maximum UDP payload of an IPv4 packet, measured as the first size the socket rejects minus one.</summary>
  [Test]
  public void MaxDatagramSizeDefaultsTo65507()
    => Assert.That(new UdpAppender().MaxDatagramSize, Is.EqualTo(65507));

  /// <summary>Below 512 nothing useful fits, above 65507 an IPv4 socket rejects the datagram.</summary>
  [TestCase(511)]
  [TestCase(65508)]
  public void MaxDatagramSizeRejectsValuesOutsideTheRange(int value)
    => Assert.That(() => new UdpAppender().MaxDatagramSize = value,
      Throws.TypeOf<ArgumentOutOfRangeException>());

  /// <summary>An event that fits is sent exactly as the layout rendered it.</summary>
  [Test]
  public void AMessageThatFitsIsEncodedUnchanged()
  {
    UdpAppender appender = new() { Encoding = Encoding.UTF8 };

    Assert.That(GetDatagramBytes(appender, "hello"), Is.EqualTo(Encoding.UTF8.GetBytes("hello")));
  }

  /// <summary>An event of exactly the limit still fits, so nothing is cut and nothing reported.</summary>
  [Test]
  public void AMessageOfExactlyTheLimitIsEncodedUnchanged()
  {
    UdpAppender appender = new() { Encoding = Encoding.ASCII, MaxDatagramSize = 512 };
    RecordingErrorHandler errorHandler = new();
    appender.ErrorHandler = errorHandler;

    byte[] datagram = GetDatagramBytes(appender, new string('a', 512));

    Assert.That(datagram, Is.EqualTo(Encoding.ASCII.GetBytes(new string('a', 512))));
    Assert.That(errorHandler.Messages, Is.Empty);
  }

  /// <summary>
  /// An oversize datagram used to be rejected by the socket, which cost the whole event.
  /// </summary>
  [Test]
  public void AnOversizeMessageIsTruncatedInsteadOfLost()
  {
    UdpAppender appender = new() { Encoding = Encoding.ASCII, MaxDatagramSize = 512 };
    RecordingErrorHandler errorHandler = new();
    appender.ErrorHandler = errorHandler;

    byte[] datagram = GetDatagramBytes(appender, new string('a', 1000));

    Assert.That(datagram, Has.Length.EqualTo(512));
    Assert.That(Encoding.ASCII.GetString(datagram), Does.EndWith(Marker));
    Assert.That(errorHandler.Messages, Has.Count.EqualTo(1));
  }

  /// <summary>A cut inside a character would encode as the replacement character.</summary>
  [Test]
  public void TruncationDoesNotSplitAMultiByteCharacter()
  {
    UdpAppender appender = new() { Encoding = Encoding.UTF8, MaxDatagramSize = 513 };
    appender.ErrorHandler = new RecordingErrorHandler();

    string decoded = Encoding.UTF8.GetString(GetDatagramBytes(appender, new string('\u00e4', 1000)));

    // 513 bytes hold the 14 byte marker and 249 of the two byte characters.
    Assert.That(decoded, Is.EqualTo(new string('\u00e4', 249) + Marker));
  }

  /// <summary>The same for a cut between the two halves of a surrogate pair.</summary>
  [Test]
  public void TruncationDoesNotSplitASurrogatePair()
  {
    UdpAppender appender = new() { Encoding = Encoding.UTF8, MaxDatagramSize = 515 };
    appender.ErrorHandler = new RecordingErrorHandler();

    string decoded = Encoding.UTF8.GetString(GetDatagramBytes(appender, string.Concat(Enumerable.Repeat("\U0001f600", 1000))));

    Assert.That(decoded, Does.EndWith(Marker));
    Assert.That(decoded, !Contains.Substring("\ufffd").Using(StringComparison.Ordinal));
  }
}
