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
using System.Diagnostics;
using System.Globalization;

using log4net.Config;
using log4net.Util;
using log4net.Layout;
using log4net.Core;
using log4net.Appender;
using log4net.Repository;

using log4net.Tests.Appender;

using NUnit.Framework;

namespace log4net.Tests.Core
{
	/// <summary>
	/// </remarks>
	[TestFixture] public class FixingTest
	{
		private LoggingEventData BuildStandardEventData()
		{
			LoggingEventData ed=new LoggingEventData();
			ed.LoggerName=typeof(FixingTest).FullName;
			ed.Level=Level.Warn;
			ed.Message="Logging event works";
			ed.Domain="ReallySimpleApp";
			ed.LocationInfo=new LocationInfo(typeof(FixingTest).Name,"Main","Class1.cs","29");	//Completely arbitary
			ed.ThreadName=System.Threading.Thread.CurrentThread.Name;
			ed.TimeStamp=new DateTime(2005,12,14,14,07,35,0);									//Completely arbitary
			ed.ExceptionString="Exception occured here";
			ed.UserName="TestUser";
			return ed;
		}

		static FixingTest()
		{
			log4net.LogManager.CreateRepository("Test Repository");
			System.Threading.Thread.CurrentThread.Name="Test thread";
		}

		[Test] public void TestUnfixedValues()
		{
			LoggingEventData ed=BuildStandardEventData();
			
			LoggingEvent evt=new LoggingEvent(
				ed.LocationInfo.GetType(),
				log4net.LogManager.GetRepository("Test Repository"),
				ed.LoggerName,
				ed.Level,
				ed.Message,
				new Exception("This is the exception")
				);


			Assert.AreEqual("domain-NUnitAddin.NUnit.dll",evt.Domain,"Domain is incorrect");
			Assert.AreEqual("System.Exception: This is the exception",evt.GetExceptionString(),"Exception is incorrect");
			Assert.AreEqual(FixFlags.None,evt.Fix,"Fixed Fields is incorrect");
			Assert.AreEqual("",evt.Identity,"Identity is incorrect");
			Assert.AreEqual(Level.Warn,evt.Level,"Level is incorrect");
			Assert.AreEqual("get_LocationInformation",evt.LocationInformation.MethodName,"Location Info is incorrect");
			Assert.AreEqual("log4net.Tests.Core.FixingTest",evt.LoggerName,"LoggerName is incorrect");
			Assert.AreEqual(log4net.LogManager.GetRepository("Test Repository"),evt.Repository,"Repository is incorrect");
			Assert.AreEqual("Test thread",evt.ThreadName,"ThreadName is incorrect");
			Assert.IsNotNull(evt.TimeStamp,"TimeStamp is incorrect");
			Assert.AreEqual(System.Security.Principal.WindowsIdentity.GetCurrent().Name ,evt.UserName,"UserName is incorrect");
			Assert.AreEqual("Logging event works",evt.RenderedMessage,"Message is incorrect");
		}

		[Test] public void TestAllFixedValues()
		{
			LoggingEventData ed=BuildStandardEventData();
			
			LoggingEvent evt=new LoggingEvent(
				ed.LocationInfo.GetType(),
				log4net.LogManager.GetRepository("Test Repository"),
				ed.LoggerName,
				ed.Level,
				ed.Message,
				new Exception("This is the exception")
				);
			evt.Fix=FixFlags.All;

			Assert.AreEqual("domain-NUnitAddin.NUnit.dll",evt.Domain,"Domain is incorrect");
			Assert.AreEqual("System.Exception: This is the exception",evt.GetExceptionString(),"Exception is incorrect");
			Assert.AreEqual(FixFlags.LocationInfo| FixFlags.UserName| FixFlags.Identity| FixFlags.Partial|FixFlags.Message | FixFlags.ThreadName | FixFlags.Exception | FixFlags.Domain | FixFlags.Properties,evt.Fix,"Fixed Fields is incorrect");
			Assert.AreEqual("",evt.Identity,"Identity is incorrect");
			Assert.AreEqual(Level.Warn,evt.Level,"Level is incorrect");
			Assert.AreEqual("get_LocationInformation",evt.LocationInformation.MethodName,"Location Info is incorrect");
			Assert.AreEqual("log4net.Tests.Core.FixingTest",evt.LoggerName,"LoggerName is incorrect");
			Assert.AreEqual(log4net.LogManager.GetRepository("Test Repository"),evt.Repository,"Repository is incorrect");
			Assert.AreEqual("Test thread",evt.ThreadName,"ThreadName is incorrect");
			Assert.IsNotNull(evt.TimeStamp,"TimeStamp is incorrect");
			Assert.AreEqual(System.Security.Principal.WindowsIdentity.GetCurrent().Name ,evt.UserName,"UserName is incorrect");
			Assert.AreEqual("Logging event works",evt.RenderedMessage,"Message is incorrect");
		}

		[Test] public void TestNoFixedValues()
		{
			LoggingEventData ed=BuildStandardEventData();
			
			LoggingEvent evt=new LoggingEvent(
				ed.LocationInfo.GetType(),
				log4net.LogManager.GetRepository("Test Repository"),
				ed.LoggerName,
				ed.Level,
				ed.Message,
				new Exception("This is the exception")
				);
			evt.Fix=FixFlags.None;

			Assert.IsNull(evt.Domain,"Domain is incorrect");
			Assert.IsNull(evt.GetExceptionString(),"Exception is incorrect");
			Assert.AreEqual(FixFlags.None,evt.Fix,"Fixed Fields is incorrect");
			Assert.IsNull(evt.Identity,"Identity is incorrect");
			Assert.AreEqual(Level.Warn,evt.Level,"Level is incorrect");
			Assert.IsNull(evt.LocationInformation,"Location Info is incorrect");
			Assert.AreEqual("log4net.Tests.Core.FixingTest",evt.LoggerName,"LoggerName is incorrect");
			Assert.AreEqual(log4net.LogManager.GetRepository("Test Repository"),evt.Repository,"Repository is incorrect");
			Assert.IsNull(evt.ThreadName,"ThreadName is incorrect");
			Assert.IsNotNull(evt.TimeStamp,"TimeStamp is incorrect");
			Assert.IsNull(evt.UserName,"UserName is incorrect");
			Assert.IsNull(evt.RenderedMessage,"Message is incorrect");
		}
	}
}
