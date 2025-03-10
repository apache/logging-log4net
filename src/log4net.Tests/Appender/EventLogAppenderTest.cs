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

// netstandard doesn't support EventLog
#if NET462_OR_GREATER

using System.Diagnostics;

using log4net.Appender;
using log4net.Core;

using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Used for internal unit testing the <see cref="EventLogAppender"/> class.
/// </summary>
/// <remarks>
/// Used for internal unit testing the <see cref="EventLogAppender"/> class.
/// </remarks>
[TestFixture]
public sealed class EventLogAppenderTest
{
  /// <summary>
  /// Verifies that for each event log level, the correct system
  /// event log enumeration is returned
  /// </summary>
  [Test]
  public void TestGetEntryTypeForExistingApplicationName()
  {
    EventLogAppender eventAppender = new() { ApplicationName = "Winlogon" };
    eventAppender.ActivateOptions();

    Assert.That(eventAppender.GetEntryType(Level.All),
      Is.EqualTo(EventLogEntryType.Information));

    Assert.That(eventAppender.GetEntryType(Level.Debug),
      Is.EqualTo(EventLogEntryType.Information));

    Assert.That(eventAppender.GetEntryType(Level.Info),
      Is.EqualTo(EventLogEntryType.Information));

    Assert.That(eventAppender.GetEntryType(Level.Warn),
      Is.EqualTo(EventLogEntryType.Warning));

    Assert.That(eventAppender.GetEntryType(Level.Error),
      Is.EqualTo(EventLogEntryType.Error));

    Assert.That(eventAppender.GetEntryType(Level.Fatal),
      Is.EqualTo(EventLogEntryType.Error));

    Assert.That(eventAppender.GetEntryType(Level.Off),
      Is.EqualTo(EventLogEntryType.Error));
  }

  /// <summary>
  /// ActivateOption tries to create an event source if it doesn't exist but this is going to fail on more modern Windows versions unless the code is run with local administrator privileges.
  /// </summary>
  [Test]
  [Ignore("seems to require administrator privileges or a specific environment when run")]
  public void ActivateOptionsDisablesAppenderIfSourceDoesntExist()
  {
    EventLogAppender eventAppender = new();
    eventAppender.ActivateOptions();
    Assert.That(eventAppender.Threshold, Is.EqualTo(Level.Off));
  }
}

#endif // NET462_OR_GREATER