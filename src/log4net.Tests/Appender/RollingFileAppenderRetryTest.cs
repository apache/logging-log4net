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
using System.IO;
using System.Linq;
using System.Text;

using log4net.Appender;
using log4net.Layout;
using log4net.Tests.Appender.Internal;

using NUnit.Framework;

using PeanutButter.Utils;

using static log4net.Tests.Appender.Internal.TestFileAppenders;

namespace log4net.Tests.Appender;

/// <summary>
/// A <see cref="RollingFileAppender"/> retrying a failed open adds the roll suffix once.
/// </summary>
[TestFixture]
public sealed class RollingFileAppenderRetryTest
{
  /// <summary>
  /// The retry reopened the suffixed name of the failed open and suffixed it again: one more empty
  /// file per dropped event. It appends to what the lock holder wrote meanwhile.
  /// </summary>
  [TestCase(RollingFileAppender.RollingMode.Date)]
  [TestCase(RollingFileAppender.RollingMode.Size)]
  [TestCase(RollingFileAppender.RollingMode.Composite)]
  [NonParallelizable]
  public void ARetriedOpenAddsTheSuffixOnce(RollingFileAppender.RollingMode rollingStyle)
  {
    using AutoTempFolder folder = new();
    string file = Path.Combine(folder.Path, "rolling.log");
    RollingFileAppender appender = new()
    {
      File = file,
      StaticLogFileName = false,
      RollingStyle = rollingStyle,
      CountDirection = 1,
      Layout = new PatternLayout("%message%newline"),
      LockingModel = new FileAppender.InterProcessLock { LockTimeoutMillis = 200 },
      ErrorHandler = new RecordingErrorHandler()
    };

    using (new HeldLock(file))
    {
      appender.ActivateOptions();
      appender.DoAppend(CreateEvent("lost"));
      using FileStream holder = new(Directory.GetFiles(folder.Path).Single(), FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
      byte[] theirs = Encoding.ASCII.GetBytes("theirs" + Environment.NewLine);
      holder.Write(theirs, 0, theirs.Length);
    }
    appender.DoAppend(CreateEvent("written"));
    appender.Close();

    string[] files = Directory.GetFiles(folder.Path);
    Assert.That(files, Has.Length.EqualTo(1), string.Join(", ", files.Select(Path.GetFileName)));
    Assert.That(File.ReadAllText(files[0]),
      Is.EqualTo("theirs" + Environment.NewLine + "written" + Environment.NewLine));
  }
}
