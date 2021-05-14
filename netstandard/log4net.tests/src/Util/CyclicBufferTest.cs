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

using log4net.Core;
using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Util
{
	/// <summary>
	/// Used for internal unit testing the <see cref="PropertiesDictionary"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="PropertiesDictionary"/> class.
	/// </remarks>
	[TestFixture]
	public class CyclicBufferTest
	{
		[Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TestConstructorSize0()
		{
			CyclicBuffer cb = new CyclicBuffer(0);
			Assert.IsNotNull(cb);
		}

		[Test]
		public void TestSize1()
		{
			CyclicBuffer cb = new CyclicBuffer(1);

			Assert.AreEqual(0, cb.Length, "Empty Buffer should have length 0");
			Assert.AreEqual(1, cb.MaxSize, "Buffer should have max size 1");

			LoggingEvent event1 = new LoggingEvent(null, null, null, null, null, null);
			LoggingEvent event2 = new LoggingEvent(null, null, null, null, null, null);

			LoggingEvent discardedEvent = cb.Append(event1);

			Assert.IsNull(discardedEvent, "No event should be discarded untill the buffer is full");
			Assert.AreEqual(1, cb.Length, "Buffer should have length 1");
			Assert.AreEqual(1, cb.MaxSize, "Buffer should still have max size 1");


			discardedEvent = cb.Append(event2);

			Assert.AreSame(event1, discardedEvent, "Expect event1 to now be discarded");
			Assert.AreEqual(1, cb.Length, "Buffer should still have length 1");
			Assert.AreEqual(1, cb.MaxSize, "Buffer should really still have max size 1");

			LoggingEvent[] discardedEvents = cb.PopAll();

			Assert.AreEqual(1, discardedEvents.Length, "Poped events length should be 1");
			Assert.AreSame(event2, discardedEvents[0], "Expect event2 to now be popped");
			Assert.AreEqual(0, cb.Length, "Buffer should be back to length 0");
			Assert.AreEqual(1, cb.MaxSize, "Buffer should really really still have max size 1");
		}

		[Test]
		public void TestSize2()
		{
			CyclicBuffer cb = new CyclicBuffer(2);

			Assert.AreEqual(0, cb.Length, "Empty Buffer should have length 0");
			Assert.AreEqual(2, cb.MaxSize, "Buffer should have max size 2");

			LoggingEvent event1 = new LoggingEvent(null, null, null, null, null, null);
			LoggingEvent event2 = new LoggingEvent(null, null, null, null, null, null);
			LoggingEvent event3 = new LoggingEvent(null, null, null, null, null, null);

			LoggingEvent discardedEvent;

			discardedEvent = cb.Append(event1);
			Assert.IsNull(discardedEvent, "No event should be discarded after append 1");
			discardedEvent = cb.Append(event2);
			Assert.IsNull(discardedEvent, "No event should be discarded after append 2");

			discardedEvent = cb.Append(event3);
			Assert.AreSame(event1, discardedEvent, "Expect event1 to now be discarded");

			discardedEvent = cb.PopOldest();
			Assert.AreSame(event2, discardedEvent, "Expect event2 to now be discarded");

			LoggingEvent[] discardedEvents = cb.PopAll();

			Assert.AreEqual(1, discardedEvents.Length, "Poped events length should be 1");
			Assert.AreSame(event3, discardedEvents[0], "Expect event3 to now be popped");
			Assert.AreEqual(0, cb.Length, "Buffer should be back to length 0");
			Assert.AreEqual(2, cb.MaxSize, "Buffer should really really still have max size 2");
		}
	}
}