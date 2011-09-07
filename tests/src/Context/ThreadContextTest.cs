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
using System.Threading;

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
	[TestFixture]
	public class ThreadContextTest
	{
        [TearDown]
        public void TearDown() {
            Utils.RemovePropertyFromAllContexts();
        }

        [Test]
		public void TestThreadPropertiesPattern()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%property{" +  Utils.PROPERTY_KEY + "}");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread properties value set");
			stringAppender.Reset();

			ThreadContext.Properties[Utils.PROPERTY_KEY] = "val1";

			log1.Info("TestMessage");
			Assert.AreEqual("val1", stringAppender.GetString(), "Test thread properties value set");
			stringAppender.Reset();

			ThreadContext.Properties.Remove(Utils.PROPERTY_KEY);

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread properties value removed");
			stringAppender.Reset();
		}

		[Test]
		public void TestThreadStackPattern()
		{
			StringAppender stringAppender = new StringAppender();
            stringAppender.Layout = new PatternLayout("%property{" + Utils.PROPERTY_KEY + "}");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadStackPattern");

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread stack value set");
			stringAppender.Reset();

			using(ThreadContext.Stacks[Utils.PROPERTY_KEY].Push("val1"))
			{
				log1.Info("TestMessage");
				Assert.AreEqual("val1", stringAppender.GetString(), "Test thread stack value set");
				stringAppender.Reset();
			}

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread stack value removed");
			stringAppender.Reset();
		}

		[Test]
		public void TestThreadStackPattern2()
		{
			StringAppender stringAppender = new StringAppender();
            stringAppender.Layout = new PatternLayout("%property{" + Utils.PROPERTY_KEY + "}");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadStackPattern");

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread stack value set");
			stringAppender.Reset();

			using(ThreadContext.Stacks[Utils.PROPERTY_KEY].Push("val1"))
			{
				log1.Info("TestMessage");
				Assert.AreEqual("val1", stringAppender.GetString(), "Test thread stack value set");
				stringAppender.Reset();

				using(ThreadContext.Stacks[Utils.PROPERTY_KEY].Push("val2"))
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

		[Test]
		public void TestThreadStackPatternNullVal()
		{
			StringAppender stringAppender = new StringAppender();
            stringAppender.Layout = new PatternLayout("%property{" + Utils.PROPERTY_KEY + "}");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadStackPattern");

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread stack value set");
			stringAppender.Reset();

			using(ThreadContext.Stacks[Utils.PROPERTY_KEY].Push(null))
			{
				log1.Info("TestMessage");
				Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread stack value set");
				stringAppender.Reset();
			}

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test thread stack value removed");
			stringAppender.Reset();
		}

		[Test]
		public void TestThreadStackPatternNullVal2()
		{
			StringAppender stringAppender = new StringAppender();
            stringAppender.Layout = new PatternLayout("%property{" + Utils.PROPERTY_KEY + "}");

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadStackPattern");

			log1.Info("TestMessage");
			Assert.AreEqual(SystemInfo.NullText, stringAppender.GetString(), "Test no thread stack value set");
			stringAppender.Reset();

			using(ThreadContext.Stacks[Utils.PROPERTY_KEY].Push("val1"))
			{
				log1.Info("TestMessage");
				Assert.AreEqual("val1", stringAppender.GetString(), "Test thread stack value set");
				stringAppender.Reset();

				using(ThreadContext.Stacks[Utils.PROPERTY_KEY].Push(null))
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

        private static string TestBackgroundThreadContextPropertyRepository;

		[Test]
		public void TestBackgroundThreadContextProperty()
		{
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = new PatternLayout("%property{DateTimeTodayToString}");

            ILoggerRepository rep = LogManager.CreateRepository(TestBackgroundThreadContextPropertyRepository = "TestBackgroundThreadContextPropertyRepository" + Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);

			Thread thread = new Thread(new ThreadStart(ExecuteBackgroundThread));
			thread.Start();

			Thread.CurrentThread.Join(2000);
		}

		private static void ExecuteBackgroundThread()
		{
            ILog log = LogManager.GetLogger(TestBackgroundThreadContextPropertyRepository, "ExecuteBackGroundThread");
			ThreadContext.Properties["DateTimeTodayToString"] = DateTime.Today.ToString();

			log.Info("TestMessage");

			Repository.Hierarchy.Hierarchy hierarchyLoggingRepository = (Repository.Hierarchy.Hierarchy)log.Logger.Repository;
			StringAppender stringAppender = (StringAppender)hierarchyLoggingRepository.Root.Appenders[0];

			Assert.AreEqual(DateTime.Today.ToString(), stringAppender.GetString());
		}
	}
}