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

using log4net.Appender;
using log4net.Config;
using log4net.Core;

using NUnit.Framework;

namespace log4net.Tests.Appender
{
	/// <summary>
	/// Used for internal unit testing the <see cref="BufferingAppenderSkeleton"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="BufferingAppenderSkeleton"/> class.
	/// </remarks>
	[TestFixture]
	public class BufferingAppenderTest
	{
		private BufferingForwardingAppender m_bufferingForwardingAppender;
		private CountingAppender m_countingAppender;
		private Repository.Hierarchy.Hierarchy m_hierarchy;


		private void SetupRepository()
		{
			m_hierarchy = new Repository.Hierarchy.Hierarchy();

			m_countingAppender = new CountingAppender();
			m_countingAppender.ActivateOptions();

			m_bufferingForwardingAppender = new BufferingForwardingAppender();
			m_bufferingForwardingAppender.AddAppender(m_countingAppender);

			m_bufferingForwardingAppender.BufferSize = 0;
			m_bufferingForwardingAppender.ClearFilters();
			m_bufferingForwardingAppender.Evaluator = null;
			m_bufferingForwardingAppender.Fix = FixFlags.Partial;
			m_bufferingForwardingAppender.Lossy = false;
			m_bufferingForwardingAppender.LossyEvaluator = null;
			m_bufferingForwardingAppender.Threshold = Level.All;

			m_bufferingForwardingAppender.ActivateOptions();

			BasicConfigurator.Configure(m_hierarchy, m_bufferingForwardingAppender);
		}

		/// <summary>
		/// </summary>
		[Test]
		public void TestSetupAppender()
		{
			SetupRepository();

			Assert.AreEqual(0, m_countingAppender.Counter, "Test empty appender");

			ILogger logger = m_hierarchy.GetLogger("test");
			logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message logged", null);

			Assert.AreEqual(1, m_countingAppender.Counter, "Test 1 event logged");
		}

		/// <summary>
		/// </summary>
		[Test]
		public void TestBufferSize5()
		{
			SetupRepository();

			m_bufferingForwardingAppender.BufferSize = 5;
			m_bufferingForwardingAppender.ActivateOptions();

			Assert.AreEqual(m_countingAppender.Counter, 0);

			ILogger logger = m_hierarchy.GetLogger("test");

			logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 1", null);
			Assert.AreEqual(0, m_countingAppender.Counter, "Test 1 event in buffer");
			logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 2", null);
			Assert.AreEqual(0, m_countingAppender.Counter, "Test 2 event in buffer");
			logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 3", null);
			Assert.AreEqual(0, m_countingAppender.Counter, "Test 3 event in buffer");
			logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 4", null);
			Assert.AreEqual(0, m_countingAppender.Counter, "Test 4 event in buffer");
			logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 5", null);
			Assert.AreEqual(0, m_countingAppender.Counter, "Test 5 event in buffer");
			logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 6", null);
			Assert.AreEqual(6, m_countingAppender.Counter, "Test 0 event in buffer. 6 event sent");
			logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 7", null);
			Assert.AreEqual(6, m_countingAppender.Counter, "Test 1 event in buffer. 6 event sent");
			logger.Log(typeof(BufferingAppenderTest), Level.Warn, "Message 8", null);
			Assert.AreEqual(6, m_countingAppender.Counter, "Test 2 event in buffer. 6 event sent");
		}
	}
}