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

using System.IO;

using log4net.Appender;
using log4net.Core;
using log4net.Layout;

namespace log4net.Tests.Appender.Internal;

/// <summary>Builds file appenders and events for the file appender tests.</summary>
internal static class TestFileAppenders
{
  internal static FileAppender CreateAppender(string file, FileAppender.LockingModelBase lockingModel) => new()
  {
    File = file,
    Layout = new PatternLayout("%message%newline"),
    LockingModel = lockingModel,
    ErrorHandler = new RecordingErrorHandler()
  };

  /// <summary>Reads a file the appender still holds open, which Windows refuses without shared write access.</summary>
  internal static string ReadWhileOpen(string file)
  {
    using FileStream stream = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    using StreamReader reader = new(stream);
    return reader.ReadToEnd();
  }

  internal static LoggingEvent CreateEvent(string message)
    => new(typeof(TestFileAppenders), null, "test", Level.Info, message, null);
}
