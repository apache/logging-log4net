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
using System.Diagnostics;

using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Used for internal unit testing the <see cref="OutputDebugStringAppender"/> class.
/// </summary>
/// <remarks>
/// Used for internal unit testing the <see cref="OutputDebugStringAppender"/> class.
/// </remarks>
[TestFixture]
[Platform(Include = "Win")]
public sealed class OutputDebugStringAppenderTest
{
  /// <summary>
  /// Verifies that the OutputDebugString api is called by the appender without issues
  /// </summary>
  [Test]
  public void AppendShouldNotCauseAnyErrors()
  {
    ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

    OutputDebugStringAppender outputDebugStringAppender = new()
    {
      Layout = new SimpleLayout(),
      ErrorHandler = new FailOnError()
    };
    outputDebugStringAppender.ActivateOptions();

    BasicConfigurator.Configure(rep, outputDebugStringAppender);

    ILog log = LogManager.GetLogger(rep.Name, GetType());
    log.Debug("Message - Сообщение - הודעה");

    // need a way to check that the api is actually called and the string is properly marshalled.
  }
}

class FailOnError : IErrorHandler
{
  public void Error(string message, Exception? e, ErrorCode errorCode) => Assert.Fail($"Unexpected error: {message} exception: {e}, errorCode: {errorCode}");
  public void Error(string message, Exception e) => Assert.Fail($"Unexpected error: {message} exception: {e}");
  public void Error(string message) =>  Assert.Fail($"Unexpected error: {message}");
}
