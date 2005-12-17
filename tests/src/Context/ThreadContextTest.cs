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

using log4net.Config;
using log4net.Layout;
using log4net.Repository;

using log4net.Tests.Appender;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Context
{
	/// <summary>
	/// Used for internal unit testing the <see cref="ThreadContext"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="ThreadContext"/> class.
	/// </remarks>
	[TestFixture] public class ThreadContextTest
	{
		[Test] public void TestThreadPropertiesPattern()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%property{prop1}");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread properties value set");
			stringAppender.Reset();

			ThreadContext.Properties["prop1"] = "val1";

			log1.Info("TestMessage");
			Assert.AreEqual("val1", stringAppender.GetString(), "Test thread properties value set");
			stringAppender.Reset();

			ThreadContext.Properties.Remove("prop1");

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread properties value removed");
			stringAppender.Reset();
		}

		[Test] public void TestThreadStackPattern()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%property{prop1}");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadStackPattern");

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread stack value set");
			stringAppender.Reset();

			using(ThreadContext.Stacks["prop1"].Push("val1"))
			{
				log1.Info("TestMessage");
				Assert.AreEqual("val1", stringAppender.GetString(), "Test thread stack value set");
				stringAppender.Reset();
			}

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread stack value removed");
			stringAppender.Reset();
		}

		[Test] public void TestThreadStackPattern2()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%property{prop1}");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadStackPattern");

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread stack value set");
			stringAppender.Reset();

			using(ThreadContext.Stacks["prop1"].Push("val1"))
			{
				log1.Info("TestMessage");
				Assert.AreEqual("val1", stringAppender.GetString(), "Test thread stack value set");
				stringAppender.Reset();

				using(ThreadContext.Stacks["prop1"].Push("val2"))
				{
					log1.Info("TestMessage");
					Assert.AreEqual("val1 val2", stringAppender.GetString(), "Test thread stack value pushed 2nd val");
					stringAppender.Reset();
				}
			}

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread stack value removed");
			stringAppender.Reset();
		}

		[Test] public void TestThreadStackPatternNullVal()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%property{prop1}");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadStackPattern");

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread stack value set");
			stringAppender.Reset();

			using(ThreadContext.Stacks["prop1"].Push(null))
			{
				log1.Info("TestMessage");
				Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread stack value set");
				stringAppender.Reset();
			}

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread stack value removed");
			stringAppender.Reset();
		}

		[Test] public void TestThreadStackPatternNullVal2()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%property{prop1}");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadStackPattern");

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread stack value set");
			stringAppender.Reset();

			using(ThreadContext.Stacks["prop1"].Push("val1"))
			{
				log1.Info("TestMessage");
				Assert.AreEqual("val1", stringAppender.GetString(), "Test thread stack value set");
				stringAppender.Reset();

				using(ThreadContext.Stacks["prop1"].Push(null))
				{
					log1.Info("TestMessage");
					Assert.AreEqual("val1 ", stringAppender.GetString(), "Test thread stack value pushed null");
					stringAppender.Reset();
				}
			}

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread stack value removed");
			stringAppender.Reset();
		}
	}
}
