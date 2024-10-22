/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Util;

/// <summary>
/// Tests for <see cref="EncodingWithoutPreamble"/>
/// </summary>
[TestFixture]
public sealed class EncodingWithoutPreambleTest
{
  /// <summary>
  /// Tests the wrapping functionality
  /// </summary>
  [Test]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Assertion", "NUnit2010:Use EqualConstraint for better assertion messages in case of failure",
    Justification = "Is.EqualTo checks the type first, we just want to call Equals()")]
  public void WrappedTest()
  {
    Encoding wrapped = Encoding.UTF8;
    Type encodingType = typeof(LogLog).Assembly.GetType("log4net.Util.EncodingWithoutPreamble", true)!;
    Encoding target = (Encoding)encodingType
      .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
      .First()
      .Invoke([wrapped]);
    Assert.That(target.Equals(wrapped));
    const string text = "Hallöchen!";
    byte[] bytes = wrapped.GetBytes(text);
    Assert.That(target.GetBytes(text), Is.EqualTo(bytes));
    Assert.That(target.GetString(bytes), Is.EqualTo(wrapped.GetString(bytes)));
    Assert.That(wrapped.GetPreamble(), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }).AsCollection);
    Assert.That(target.GetPreamble(), Is.EqualTo(Array.Empty<byte>()).AsCollection);
  }
}