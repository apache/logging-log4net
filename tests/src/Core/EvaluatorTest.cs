/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using log4net.Appender;
using log4net.Core;
using log4net.Tests.Appender;
using NUnit.Framework;

namespace log4net.Tests.Core
{
	[TestFixture]
	public class EvaluatorTest
	{
		private BufferingForwardingAppender m_bufferingForwardingAppender;
		private CountingAppender m_countingAppender;
		private Repository.Hierarchy.Hierarchy m_hierarchy;

		[SetUp]
		public void SetupRepository()
		{
			m_hierarchy = new Repository.Hierarchy.Hierarchy();

			m_countingAppender = new CountingAppender();
			m_countingAppender.ActivateOptions();

			m_bufferingForwardingAppender = new BufferingForwardingAppender();
			m_bufferingForwardingAppender.AddAppender(m_countingAppender);

			m_bufferingForwardingAppender.BufferSize = 5;
			m_bufferingForwardingAppender.ClearFilters();
			m_bufferingForwardingAppender.Fix = FixFlags.Partial;
			m_bufferingForwardingAppender.Lossy = false;
			m_bufferingForwardingAppender.LossyEvaluator = null;
			m_bufferingForwardingAppender.Threshold = Level.All;
		}

		[Test]
		public void TestLevelEvaluator()
		{
			m_bufferingForwardingAppender.Evaluator = new LevelEvaluator(Level.Info);
			m_bufferingForwardingAppender.ActivateOptions();
			log4net.Config.BasicConfigurator.Configure(m_hierarchy, m_bufferingForwardingAppender);

			ILogger logger = m_hierarchy.GetLogger("TestLevelEvaluator");

			logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
			logger.Log(typeof(EvaluatorTest), Level.Debug, "Debug message logged", null);
			Assert.AreEqual(0, m_countingAppender.Counter, "Test 2 events buffered");

			logger.Log(typeof(EvaluatorTest), Level.Info, "Info message logged", null);
			Assert.AreEqual(3, m_countingAppender.Counter, "Test 3 events flushed on Info message.");
		}

		[Test]
		public void TestExceptionEvaluator()
		{
			m_bufferingForwardingAppender.Evaluator = new ExceptionEvaluator(typeof(ApplicationException), true);
			m_bufferingForwardingAppender.ActivateOptions();
			log4net.Config.BasicConfigurator.Configure(m_hierarchy, m_bufferingForwardingAppender);

			ILogger logger = m_hierarchy.GetLogger("TestExceptionEvaluator");

			logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
			logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
			Assert.AreEqual(0, m_countingAppender.Counter, "Test 2 events buffered");

			logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", new ApplicationException());
			Assert.AreEqual(3, m_countingAppender.Counter, "Test 3 events flushed on ApplicationException message.");
		}

		[Test]
		public void TestExceptionEvaluatorTriggerOnSubClass()
		{
			m_bufferingForwardingAppender.Evaluator = new ExceptionEvaluator(typeof(Exception), true);
			m_bufferingForwardingAppender.ActivateOptions();
			log4net.Config.BasicConfigurator.Configure(m_hierarchy, m_bufferingForwardingAppender);

			ILogger logger = m_hierarchy.GetLogger("TestExceptionEvaluatorTriggerOnSubClass");

			logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
			logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
			Assert.AreEqual(0, m_countingAppender.Counter, "Test 2 events buffered");

			logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", new ApplicationException());
			Assert.AreEqual(3, m_countingAppender.Counter, "Test 3 events flushed on ApplicationException message.");
		}

		[Test]
		public void TestExceptionEvaluatorNoTriggerOnSubClass()
		{
			m_bufferingForwardingAppender.Evaluator = new ExceptionEvaluator(typeof(Exception), false);
			m_bufferingForwardingAppender.ActivateOptions();
			log4net.Config.BasicConfigurator.Configure(m_hierarchy, m_bufferingForwardingAppender);

			ILogger logger = m_hierarchy.GetLogger("TestExceptionEvaluatorNoTriggerOnSubClass");

			logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
			logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
			Assert.AreEqual(0, m_countingAppender.Counter, "Test 2 events buffered");

			logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", new ApplicationException());
			Assert.AreEqual(0, m_countingAppender.Counter, "Test 3 events buffered");
		}

		[Test]
		public void TestInvalidExceptionEvaluator()
		{
			// warning: String is not a subclass of Exception
			m_bufferingForwardingAppender.Evaluator = new ExceptionEvaluator(typeof(String), false);
			m_bufferingForwardingAppender.ActivateOptions();
			log4net.Config.BasicConfigurator.Configure(m_hierarchy, m_bufferingForwardingAppender);

			ILogger logger = m_hierarchy.GetLogger("TestExceptionEvaluatorNoTriggerOnSubClass");

			logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
			logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", null);
			Assert.AreEqual(0, m_countingAppender.Counter, "Test 2 events buffered");

			logger.Log(typeof(EvaluatorTest), Level.Warn, "Warn message logged", new ApplicationException());
			Assert.AreEqual(0, m_countingAppender.Counter, "Test 3 events buffered");
		}
	}
}
