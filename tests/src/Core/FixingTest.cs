#region Copyright & License
//
// Copyright 2001-2005 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
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
using System.Threading;
using log4net.Core;

using NUnit.Framework;

namespace log4net.Tests.Core
{
	/// <summary>
	/// </<summary>
	[TestFixture] public class FixingTest
	{
		static FixingTest()
		{
			LogManager.CreateRepository("Test Repository");

			// write-once
			if (Thread.CurrentThread.Name == null)
			{
				Thread.CurrentThread.Name = "Log4Net Test thread";
			}
		}

		private LoggingEventData buildStandardEventData()
		{
			LoggingEventData loggingEventData = new LoggingEventData();
			loggingEventData.LoggerName = typeof(FixingTest).FullName;
			loggingEventData.Level = Level.Warn;
			loggingEventData.Message = "Logging event works";
			loggingEventData.Domain = "ReallySimpleApp";
			loggingEventData.LocationInfo = new LocationInfo(typeof(FixingTest).Name,"Main","Class1.cs","29"); //Completely arbitary
			loggingEventData.ThreadName = Thread.CurrentThread.Name;
			loggingEventData.TimeStamp = DateTime.Today;
			loggingEventData.ExceptionString = "Exception occured here";
			loggingEventData.UserName = "TestUser";
			return loggingEventData;
		}

		[Test] public void TestUnfixedValues()
		{
			LoggingEventData loggingEventData = buildStandardEventData();
			
			LoggingEvent loggingEvent = new LoggingEvent(
				loggingEventData.LocationInfo.GetType(),
				LogManager.GetRepository("Test Repository"),
				loggingEventData.LoggerName,
				loggingEventData.Level,
				loggingEventData.Message,
				new Exception("This is the exception"));

			assertStandardEventData(loggingEvent);

			Assert.AreEqual(FixFlags.None,loggingEvent.Fix,"Fixed Fields is incorrect");
		}

		[Test] public void TestAllFixedValues()
		{
			LoggingEventData loggingEventData = buildStandardEventData();
			
			LoggingEvent loggingEvent = new LoggingEvent(
				loggingEventData.LocationInfo.GetType(),
				LogManager.GetRepository("Test Repository"),
				loggingEventData.LoggerName,
				loggingEventData.Level,
				loggingEventData.Message,
				new Exception("This is the exception"));

			assertStandardEventData(loggingEvent);

			loggingEvent.Fix = FixFlags.All;

			Assert.AreEqual(FixFlags.LocationInfo| FixFlags.UserName| FixFlags.Identity| FixFlags.Partial|FixFlags.Message | FixFlags.ThreadName | FixFlags.Exception | FixFlags.Domain | FixFlags.Properties,loggingEvent.Fix,"Fixed Fields is incorrect");
		}

		[Test] public void TestNoFixedValues()
		{
			LoggingEventData loggingEventData = buildStandardEventData();
			
			LoggingEvent loggingEvent = new LoggingEvent(
				loggingEventData.LocationInfo.GetType(),
				LogManager.GetRepository("Test Repository"),
				loggingEventData.LoggerName,
				loggingEventData.Level,
				loggingEventData.Message,
				new Exception("This is the exception"));

			assertStandardEventData(loggingEvent);

			loggingEvent.Fix = FixFlags.None;

			Assert.AreEqual(FixFlags.None,loggingEvent.Fix,"Fixed Fields is incorrect");
		}

		private void assertStandardEventData(LoggingEvent loggingEvent)
		{
			Assert.AreEqual("domain-log4net.Tests.dll",loggingEvent.Domain,"Domain is incorrect");
			Assert.AreEqual("System.Exception: This is the exception",loggingEvent.GetExceptionString(),"Exception is incorrect");
			Assert.AreEqual("",loggingEvent.Identity,"Identity is incorrect");
			Assert.AreEqual(Level.Warn,loggingEvent.Level,"Level is incorrect");
			Assert.AreEqual("get_LocationInformation",loggingEvent.LocationInformation.MethodName,"Location Info is incorrect");
			Assert.AreEqual("log4net.Tests.Core.FixingTest",loggingEvent.LoggerName,"LoggerName is incorrect");
			Assert.AreEqual(LogManager.GetRepository("Test Repository"),loggingEvent.Repository,"Repository is incorrect");
			Assert.AreEqual(Thread.CurrentThread.Name,loggingEvent.ThreadName,"ThreadName is incorrect");
			Assert.IsNotNull(loggingEvent.TimeStamp,"TimeStamp is incorrect");
			Assert.AreEqual(System.Security.Principal.WindowsIdentity.GetCurrent().Name ,loggingEvent.UserName,"UserName is incorrect");
			Assert.AreEqual("Logging event works",loggingEvent.RenderedMessage,"Message is incorrect");
		}
	}
}
