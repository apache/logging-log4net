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
using System.Linq;
using System.Threading;

using log4net.Core;

using NUnit.Framework;

namespace log4net.Tests.Core;

[TestFixture]
public class FixingTest
{
  const string TestRepository = "Test Repository";

  [OneTimeSetUp]
  public void CreateRepository()
  {
    bool exists = false;
    Repository.ILoggerRepository[] repositories = LogManager.GetAllRepositories();
    if (repositories is not null)
    {
      foreach (Repository.ILoggerRepository r in repositories)
      {
        if (r.Name == TestRepository)
        {
          exists = true;
          break;
        }
      }
    }
    if (!exists)
    {
      LogManager.CreateRepository(TestRepository);
    }

    // write-once
    if (Thread.CurrentThread.Name is null)
    {
      Thread.CurrentThread.Name = "Log4Net Test thread";
    }
  }

  [Test]
  public void All_ShouldContainAllFlags()
  {
    // Arrange
    // Act
    var allFlags = Enum.GetValues(typeof(FixFlags)).Cast<FixFlags>()
      .Except(new[] { FixFlags.None })
      .ToArray();
    // Assert
    foreach (var flag in allFlags)
    {
      Assert.AreEqual(flag, FixFlags.All & flag, $"FixFlags.All does not contain {flag}");
    }
  }

  [Test]
  public void TestUnfixedValues()
  {
    LoggingEventData loggingEventData = BuildStandardEventData();

    // LoggingEvents occur at distinct points in time
    LoggingEvent loggingEvent = new(
      loggingEventData.LocationInfo?.GetType(),
      LogManager.GetRepository(TestRepository),
      loggingEventData.LoggerName,
      loggingEventData.Level,
      loggingEventData.Message,
      new Exception("This is the exception"));

    AssertExpectedLoggingEvent(loggingEvent, loggingEventData);

    Assert.AreEqual(FixFlags.None, loggingEvent.Fix, "Fixed Fields is incorrect");
  }

  [Test]
  public void TestAllFixedValues()
  {
    LoggingEventData loggingEventData = BuildStandardEventData();

    // LoggingEvents occur at distinct points in time
    LoggingEvent loggingEvent = new(
      loggingEventData.LocationInfo?.GetType(),
      LogManager.GetRepository(TestRepository),
      loggingEventData.LoggerName,
      loggingEventData.Level,
      loggingEventData.Message,
      new Exception("This is the exception"));

    AssertExpectedLoggingEvent(loggingEvent, loggingEventData);

    loggingEvent.Fix = FixFlags.All;

    Assert.AreEqual(FixFlags.LocationInfo | FixFlags.UserName | FixFlags.Identity | FixFlags.Partial | FixFlags.Message | FixFlags.ThreadName | FixFlags.Exception | FixFlags.Domain | FixFlags.Properties, loggingEvent.Fix, "Fixed Fields is incorrect");
  }

  [Test]
  public void TestNoFixedValues()
  {
    LoggingEventData loggingEventData = BuildStandardEventData();

    // LoggingEvents occur at distinct points in time
    LoggingEvent loggingEvent = new(
      loggingEventData.LocationInfo?.GetType(),
      LogManager.GetRepository(TestRepository),
      loggingEventData.LoggerName,
      loggingEventData.Level,
      loggingEventData.Message,
      new Exception("This is the exception"));

    AssertExpectedLoggingEvent(loggingEvent, loggingEventData);

    loggingEvent.Fix = FixFlags.None;

    Assert.AreEqual(FixFlags.None, loggingEvent.Fix, "Fixed Fields is incorrect");
  }

  private static LoggingEventData BuildStandardEventData()
  {
    LoggingEventData loggingEventData = new()
    {
      LoggerName = typeof(FixingTest).FullName,
      Level = Level.Warn,
      Message = "Logging event works",
      Domain = "ReallySimpleApp",
      LocationInfo = new LocationInfo(typeof(FixingTest).Name, "Main", "Class1.cs", "29"), //Completely arbitary
      ThreadName = Thread.CurrentThread.Name,
      TimeStampUtc = DateTime.UtcNow.Date,
      ExceptionString = "Exception occured here",
      UserName = "TestUser"
    };
    return loggingEventData;
  }

  private static void AssertExpectedLoggingEvent(LoggingEvent loggingEvent, LoggingEventData loggingEventData)
  {
    Assert.AreEqual("ReallySimpleApp", loggingEventData.Domain, "Domain is incorrect");
    Assert.AreEqual("System.Exception: This is the exception", loggingEvent.GetExceptionString(), "Exception is incorrect");
    Assert.IsNull(loggingEventData.Identity, "Identity is incorrect");
    Assert.AreEqual(Level.Warn, loggingEventData.Level, "Level is incorrect");
    Assert.IsNotNull(loggingEvent.LocationInformation);
    Assert.AreEqual("get_LocationInformation", loggingEvent.LocationInformation!.MethodName, "Location Info is incorrect");
    Assert.AreEqual("log4net.Tests.Core.FixingTest", loggingEventData.LoggerName, "LoggerName is incorrect");
    Assert.AreEqual(LogManager.GetRepository(TestRepository), loggingEvent.Repository, "Repository is incorrect");
    Assert.AreEqual(Thread.CurrentThread.Name, loggingEventData.ThreadName, "ThreadName is incorrect");
    // This test is redundant as loggingEventData.TimeStamp is a value type and cannot be null
    //Assert.IsNotNull(loggingEventData.TimeStampUtc, "TimeStamp is incorrect");
    Assert.AreEqual("TestUser", loggingEventData.UserName, "UserName is incorrect");
    Assert.AreEqual("Logging event works", loggingEvent.RenderedMessage, "Message is incorrect");
  }
}