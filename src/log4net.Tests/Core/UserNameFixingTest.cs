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
using System.Security.Principal;

using log4net.Core;

using NUnit.Framework;

namespace log4net.Tests.Core;

/// <summary>
/// Tests for <see cref="LoggingEvent.UserName"/>, whose name is resolved once for the process
/// identity and once per impersonated user rather than once per logging event.
/// </summary>
[TestFixture]
[Platform("Win")]
[NonParallelizable]
#if NET8_0_OR_GREATER
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
public class UserNameFixingTest
{
  /// <summary>
  /// The assumption the impersonation tests below rest on: running under a token - even the
  /// process's own - is observable as impersonation.
  /// </summary>
  [Test]
  public void RunImpersonatedIsObservableAsImpersonation()
  {
    using WindowsIdentity identity = WindowsIdentity.GetCurrent();

    bool impersonating = WindowsIdentity.RunImpersonated(identity.AccessToken, () =>
    {
      using WindowsIdentity? current = WindowsIdentity.GetCurrent(ifImpersonating: true);
      return current is not null;
    });

    Assert.That(impersonating, Is.True);
  }

  [Test]
  public void UserNameMatchesTheCurrentWindowsIdentity()
  {
    using WindowsIdentity identity = WindowsIdentity.GetCurrent();

    Assert.That(CreateEvent().UserName, Is.EqualTo(identity.Name));
  }

  [Test]
  public void UserNameIsStableAcrossEvents()
  {
    string first = CreateEvent().UserName;

    Assert.That(CreateEvent().UserName, Is.EqualTo(first));
  }

  [Test]
  public void UserNameIsResolvedWhileImpersonating()
  {
    using WindowsIdentity identity = WindowsIdentity.GetCurrent();
    string expected = identity.Name;

    string actual = WindowsIdentity.RunImpersonated(
      identity.AccessToken,
      () => CreateEvent().UserName);

    Assert.That(actual, Is.EqualTo(expected));
  }

  /// <summary>
  /// The process identity name may only be resolved on a thread that is not impersonating.
  /// Seeding it from an impersonating thread would report that user for every later event in
  /// the process, including events raised on threads that impersonate nobody.
  /// </summary>
  [Test]
  public void ImpersonationDoesNotSeedTheProcessUserName()
  {
    FieldInfo field = typeof(LoggingEvent).GetField(
        "_processUserName",
        BindingFlags.Static | BindingFlags.NonPublic)
      ?? throw new InvalidOperationException("LoggingEvent._processUserName is missing");
    object? saved = field.GetValue(null);
    try
    {
      field.SetValue(null, null);
      using WindowsIdentity identity = WindowsIdentity.GetCurrent();

      WindowsIdentity.RunImpersonated(identity.AccessToken, () => CreateEvent().UserName);

      Assert.That(field.GetValue(null), Is.Null);
    }
    finally
    {
      field.SetValue(null, saved);
    }
  }

  private static LoggingEvent CreateEvent()
    => new(typeof(UserNameFixingTest), null, "UserNameFixingTest", Level.Info, "message", null);
}
