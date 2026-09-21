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

using System.ComponentModel;

using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Util;

/// <summary>
/// Tests for <see cref="NativeError"/>, which formats a Win32 error code through
/// <c>FormatMessage</c> and releases its buffer itself.
/// </summary>
[TestFixture]
[Platform("Win")]
public sealed class NativeErrorTest
{
  /// <summary>ERROR_FILE_NOT_FOUND, a code every Windows has a message for.</summary>
  private const int FileNotFound = 2;

  /// <summary>
  /// The framework formats the same code through the same API, so it is the text oracle. Only the
  /// trailing period differs: .NET Framework strips it, log4net and .NET do not.
  /// </summary>
  [Test]
  public void TheMessageIsTheOneWindowsReports()
    => Assert.That(NativeError.GetErrorMessage(FileNotFound)?.TrimEnd('.'),
      Is.EqualTo(new Win32Exception(FileNotFound).Message.TrimEnd('.')));

  /// <summary>The message arrives without the newlines <c>FormatMessage</c> appends.</summary>
  [Test]
  public void TheMessageCarriesNoTrailingNewLine()
    => Assert.That(NativeError.GetErrorMessage(FileNotFound), Does.Not.Match("[\r\n]$"));

  /// <summary>Zero is "no error", which has no message rather than an empty one.</summary>
  [Test]
  public void ThereIsNoMessageForErrorZero()
    => Assert.That(NativeError.GetErrorMessage(0), Is.Null);

  /// <summary>
  /// Driven hard enough that a heap the wrong release had corrupted would show up here.
  /// </summary>
  [Test]
  public void RepeatedUseKeepsReturningTheSameMessage()
  {
    string? first = NativeError.GetErrorMessage(FileNotFound);

    for (int i = 0; i < 500; i++)
    {
      Assert.That(NativeError.GetErrorMessage(FileNotFound), Is.EqualTo(first));
    }

    Assert.That(first, Is.Not.Null.And.Not.Empty);
  }

  /// <summary>The number and the message both reach the formatted text.</summary>
  [Test]
  public void ToStringCarriesTheNumberAndTheMessage()
    => Assert.That(NativeError.GetError(FileNotFound).ToString(),
      Is.EqualTo($"0x00000002: {NativeError.GetErrorMessage(FileNotFound)}"));
}
