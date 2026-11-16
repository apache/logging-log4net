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

using log4net.ObjectRenderer;
using log4net.Tests.Util;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.ObjectRenderer;

/// <summary>
/// Tests for <see cref="RendererMap"/>.
/// </summary>
[TestFixture]
public sealed class RendererMapTest
{
  /// <summary>
  /// An object whose rendering fails with an exception that cannot render itself still names that exception.
  /// </summary>
  [Test]
  public void StackTraceOfAnUnrenderableExceptionFallsBackToTheTypeNames()
  {
    string? text = null;
    LogLog.ExecuteWithoutEmittingInternalMessages(
      () => text = new RendererMap().FindAndRender(new ThrowsUnrenderableException()));

    Assert.That(text, Does.Contain(
      "<stackTrace>System.ArgumentOutOfRangeException: <Exception while rendering exception: System.ArgumentOutOfRangeException></stackTrace>"));
  }
}
