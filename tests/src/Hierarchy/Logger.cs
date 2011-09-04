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

using System.Collections;

using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Tests.Appender;

using NUnit.Framework;

namespace log4net.Tests.Hierarchy
{
	/// <summary>
	/// Used for internal unit testing the <see cref="Logger"/> class.
	/// </summary>
	/// <remarks>
	/// Internal unit test. Uses the NUnit test harness.
	/// </remarks>
	[TestFixture]
	public class LoggerTest
	{
		private Logger log;

		// A short message.
		private static string MSG = "M";

		/// <summary>
		/// Any initialization that happens before each test can
		/// go here
		/// </summary>
		[SetUp]
		public void SetUp()
		{
		}

		/// <summary>
		/// Any steps that happen after each test go here
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			// Regular users should not use the clear method lightly!
			LogManager.GetRepository().ResetConfiguration();
			LogManager.GetRepository().Shutdown();
			((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Clear();
		}

		/// <summary>
		/// Add an appender and see if it can be retrieved.
		/// </summary>
		[Test]
		public void TestAppender1()
		{
			log = (Logger)LogManager.GetLogger("test").Logger;
			CountingAppender a1 = new CountingAppender();
			a1.Name = "testAppender1";
			log.AddAppender(a1);

			IEnumerator enumAppenders = ((IEnumerable)log.Appenders).GetEnumerator();
			Assert.IsTrue(enumAppenders.MoveNext());
			CountingAppender aHat = (CountingAppender)enumAppenders.Current;
			Assert.AreEqual(a1, aHat);
		}

		/// <summary>
		/// Add an appender X, Y, remove X and check if Y is the only
		/// remaining appender.
		/// </summary>
		[Test]
		public void TestAppender2()
		{
			CountingAppender a1 = new CountingAppender();
			a1.Name = "testAppender2.1";
			CountingAppender a2 = new CountingAppender();
			a2.Name = "testAppender2.2";

			log = (Logger)LogManager.GetLogger("test").Logger;
			log.AddAppender(a1);
			log.AddAppender(a2);

			CountingAppender aHat = (CountingAppender)log.GetAppender(a1.Name);
			Assert.AreEqual(a1, aHat);

			aHat = (CountingAppender)log.GetAppender(a2.Name);
			Assert.AreEqual(a2, aHat);

			log.RemoveAppender("testAppender2.1");

			IEnumerator enumAppenders = ((IEnumerable)log.Appenders).GetEnumerator();
			Assert.IsTrue(enumAppenders.MoveNext());
			aHat = (CountingAppender)enumAppenders.Current;
			Assert.AreEqual(a2, aHat);
			Assert.IsTrue(!enumAppenders.MoveNext());

			aHat = (CountingAppender)log.GetAppender(a2.Name);
			Assert.AreEqual(a2, aHat);
		}

		/// <summary>
		/// Test if logger a.b inherits its appender from a.
		/// </summary>
		[Test]
		public void TestAdditivity1()
		{
			Logger a = (Logger)LogManager.GetLogger("a").Logger;
			Logger ab = (Logger)LogManager.GetLogger("a.b").Logger;
			CountingAppender ca = new CountingAppender();

			a.AddAppender(ca);
			a.Repository.Configured = true;

			Assert.AreEqual(ca.Counter, 0);
			ab.Log(Level.Debug, MSG, null);
			Assert.AreEqual(ca.Counter, 1);
			ab.Log(Level.Info, MSG, null);
			Assert.AreEqual(ca.Counter, 2);
			ab.Log(Level.Warn, MSG, null);
			Assert.AreEqual(ca.Counter, 3);
			ab.Log(Level.Error, MSG, null);
			Assert.AreEqual(ca.Counter, 4);
		}

		/// <summary>
		/// Test multiple additivity.
		/// </summary>
		[Test]
		public void TestAdditivity2()
		{
			Logger a = (Logger)LogManager.GetLogger("a").Logger;
			Logger ab = (Logger)LogManager.GetLogger("a.b").Logger;
			Logger abc = (Logger)LogManager.GetLogger("a.b.c").Logger;
			Logger x = (Logger)LogManager.GetLogger("x").Logger;

			CountingAppender ca1 = new CountingAppender();
			CountingAppender ca2 = new CountingAppender();

			a.AddAppender(ca1);
			abc.AddAppender(ca2);
			a.Repository.Configured = true;

			Assert.AreEqual(ca1.Counter, 0);
			Assert.AreEqual(ca2.Counter, 0);

			ab.Log(Level.Debug, MSG, null);
			Assert.AreEqual(ca1.Counter, 1);
			Assert.AreEqual(ca2.Counter, 0);

			abc.Log(Level.Debug, MSG, null);
			Assert.AreEqual(ca1.Counter, 2);
			Assert.AreEqual(ca2.Counter, 1);

			x.Log(Level.Debug, MSG, null);
			Assert.AreEqual(ca1.Counter, 2);
			Assert.AreEqual(ca2.Counter, 1);
		}

		/// <summary>
		/// Test additivity flag.
		/// </summary>
		[Test]
		public void TestAdditivity3()
		{
			Logger root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;
			Logger a = (Logger)LogManager.GetLogger("a").Logger;
			Logger ab = (Logger)LogManager.GetLogger("a.b").Logger;
			Logger abc = (Logger)LogManager.GetLogger("a.b.c").Logger;

			CountingAppender caRoot = new CountingAppender();
			CountingAppender caA = new CountingAppender();
			CountingAppender caABC = new CountingAppender();

			root.AddAppender(caRoot);
			a.AddAppender(caA);
			abc.AddAppender(caABC);
			a.Repository.Configured = true;

			Assert.AreEqual(caRoot.Counter, 0);
			Assert.AreEqual(caA.Counter, 0);
			Assert.AreEqual(caABC.Counter, 0);

			ab.Additivity = false;

			a.Log(Level.Debug, MSG, null);
			Assert.AreEqual(caRoot.Counter, 1);
			Assert.AreEqual(caA.Counter, 1);
			Assert.AreEqual(caABC.Counter, 0);

			ab.Log(Level.Debug, MSG, null);
			Assert.AreEqual(caRoot.Counter, 1);
			Assert.AreEqual(caA.Counter, 1);
			Assert.AreEqual(caABC.Counter, 0);

			abc.Log(Level.Debug, MSG, null);
			Assert.AreEqual(caRoot.Counter, 1);
			Assert.AreEqual(caA.Counter, 1);
			Assert.AreEqual(caABC.Counter, 1);
		}

		/// <summary>
		/// Test the ability to disable a level of message
		/// </summary>
		[Test]
		public void TestDisable1()
		{
			CountingAppender caRoot = new CountingAppender();
			Logger root = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root;
			root.AddAppender(caRoot);

			Repository.Hierarchy.Hierarchy h = ((Repository.Hierarchy.Hierarchy)LogManager.GetRepository());
			h.Threshold = Level.Info;
			h.Configured = true;

			Assert.AreEqual(caRoot.Counter, 0);

			root.Log(Level.Debug, MSG, null);
			Assert.AreEqual(caRoot.Counter, 0);
			root.Log(Level.Info, MSG, null);
			Assert.AreEqual(caRoot.Counter, 1);
			root.Log(Level.Warn, MSG, null);
			Assert.AreEqual(caRoot.Counter, 2);
			root.Log(Level.Warn, MSG, null);
			Assert.AreEqual(caRoot.Counter, 3);

			h.Threshold = Level.Warn;
			root.Log(Level.Debug, MSG, null);
			Assert.AreEqual(caRoot.Counter, 3);
			root.Log(Level.Info, MSG, null);
			Assert.AreEqual(caRoot.Counter, 3);
			root.Log(Level.Warn, MSG, null);
			Assert.AreEqual(caRoot.Counter, 4);
			root.Log(Level.Error, MSG, null);
			Assert.AreEqual(caRoot.Counter, 5);
			root.Log(Level.Error, MSG, null);
			Assert.AreEqual(caRoot.Counter, 6);

			h.Threshold = Level.Off;
			root.Log(Level.Debug, MSG, null);
			Assert.AreEqual(caRoot.Counter, 6);
			root.Log(Level.Info, MSG, null);
			Assert.AreEqual(caRoot.Counter, 6);
			root.Log(Level.Warn, MSG, null);
			Assert.AreEqual(caRoot.Counter, 6);
			root.Log(Level.Error, MSG, null);
			Assert.AreEqual(caRoot.Counter, 6);
			root.Log(Level.Fatal, MSG, null);
			Assert.AreEqual(caRoot.Counter, 6);
			root.Log(Level.Fatal, MSG, null);
			Assert.AreEqual(caRoot.Counter, 6);
		}

		/// <summary>
		/// Tests the Exists method of the Logger class
		/// </summary>
		[Test]
		public void TestExists()
		{
			object a = LogManager.GetLogger("a");
			object a_b = LogManager.GetLogger("a.b");
			object a_b_c = LogManager.GetLogger("a.b.c");

			object t;
			t = LogManager.Exists("xx");
			Assert.IsNull(t);
			t = LogManager.Exists("a");
			Assert.AreSame(a, t);
			t = LogManager.Exists("a.b");
			Assert.AreSame(a_b, t);
			t = LogManager.Exists("a.b.c");
			Assert.AreSame(a_b_c, t);
		}

		/// <summary>
		/// Tests the chained level for a hierarchy
		/// </summary>
		[Test]
		public void TestHierarchy1()
		{
			Repository.Hierarchy.Hierarchy h = new Repository.Hierarchy.Hierarchy();
			h.Root.Level = Level.Error;

			Logger a0 = (Logger)h.GetLogger("a");
			Assert.AreEqual("a", a0.Name);
			Assert.IsNull(a0.Level);
			Assert.AreSame(Level.Error, a0.EffectiveLevel);

			Logger a1 = (Logger)h.GetLogger("a");
			Assert.AreSame(a0, a1);
		}
	}
}