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
using System.Globalization;
using log4net.Tests.Util;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests;

/// <summary>
/// Used for internal unit testing the <see cref="NDC"/> class.
/// </summary>
[TestFixture]
public class NdcTest
{
  /// <summary>
  /// Test regression (https://github.com/apache/logging-log4net/issues/245)
  /// </summary>
  [Test]
  public void InheritTest()
  {
    NDC.Push("first");
    NDC.Push("last");
    System.Collections.Stack context = NDC.CloneStack();
    NDC.Clear();
    NDC.Inherit(context);
    Assert.That(NDC.Pop(), Is.EqualTo("last"));
    Assert.That(NDC.Pop(), Is.EqualTo("first"));
  }

  /// <summary>
  /// Each test starts with an empty stack on the test thread.
  /// </summary>
  [SetUp]
  public void ClearStack() => NDC.Clear();

  /// <summary>
  /// An argument that fails to format gives the error text, as <see cref="ILog.InfoFormat(string, object?)"/> does.
  /// </summary>
  [Test]
  public void PushFormatWithAnArgumentThatThrowsPushesTheErrorText()
  {
    // The format error is reported through the internal log.
    LogLog.ExecuteWithoutEmittingInternalMessages(
      () => Assert.That(() => NDC.PushFormat("{0}", new ThrowsUnrenderableException()), Throws.Nothing));
    Assert.That(NDC.Pop(), Does.StartWith("<log4net.Error>"));
  }

  /// <summary>
  /// A date the culture's calendar cannot represent fails to format.
  /// </summary>
  [Test]
  [SetCulture("ar-SA")]
  public void PushFormatWithADateOutsideTheCalendarPushesTheErrorText()
  {
    if (CultureInfo.CurrentCulture.Calendar is not UmAlQuraCalendar)
    {
      Assert.Ignore("ar-SA does not default to the Um Al Qura calendar here");
    }
    LogLog.ExecuteWithoutEmittingInternalMessages(
      () => Assert.That(() => NDC.PushFormat("{0}", new DateTime(2077, 11, 17)), Throws.Nothing));
    Assert.That(NDC.Pop(), Does.StartWith("<log4net.Error>"));
  }

  /// <summary>
  /// A malformed format string gives the error text.
  /// </summary>
  [Test]
  public void PushFormatWithAMalformedFormatPushesTheErrorText()
  {
    LogLog.ExecuteWithoutEmittingInternalMessages(
      () => Assert.That(() => NDC.PushFormat("{", 1), Throws.Nothing));
    Assert.That(NDC.Pop(), Does.StartWith("<log4net.Error>"));
  }

  /// <summary>
  /// Null arguments push the format string unchanged.
  /// </summary>
  [Test]
  public void PushFormatWithNullArgumentsPushesTheFormat()
  {
    NDC.PushFormat("a {0}", null!);
    Assert.That(NDC.Pop(), Is.EqualTo("a {0}"));
  }

  /// <summary>
  /// A valid format is formatted in the thread's culture, as before.
  /// </summary>
  [Test]
  [SetCulture("de-DE")]
  public void PushFormatUsesTheThreadCulture()
  {
    NDC.PushFormat("a {0}", 1234.5);
    Assert.That(NDC.Pop(), Is.EqualTo("a 1234,5"));
  }

  /// <summary>
  /// A null format is misuse and still throws.
  /// </summary>
  [Test]
  public void PushFormatWithANullFormatThrows()
    => Assert.That(() => NDC.PushFormat(null!, 1), Throws.ArgumentNullException);
}