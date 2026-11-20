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
using System.Runtime.ExceptionServices;
using System.Security.Principal;
using System.Threading;

using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests;

/// <summary>
/// Runs test code on a thread of its own, as another user.
/// </summary>
internal static class TestThreads
{
  /// <summary>
  /// Runs <paramref name="action"/> on a new thread named after <paramref name="user"/> and logged in as them.
  /// </summary>
  /// <remarks>
  /// An exception in <paramref name="action"/> is rethrown on the calling thread, so it fails the
  /// test instead of the test host.
  /// </remarks>
  internal static void RunAs(string user, Action action)
  {
    ExceptionDispatchInfo? failure = null;
    Thread thread = new(() =>
    {
      Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(user), null);
      try
      {
        action();
      }
      catch (Exception e) when (!e.IsFatal())
      {
        failure = ExceptionDispatchInfo.Capture(e);
      }
    })
    { Name = user, IsBackground = true };
    thread.Start();
    Assert.That(thread.Join(TimeSpan.FromSeconds(10)), Is.True, $"{user} did not finish");
    failure?.Throw();
  }
}
