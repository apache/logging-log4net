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
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

using log4net.Config;
using log4net.Util;
using log4net.Layout;
using log4net.Core;
using log4net.Appender;
using log4net.Repository;

using NUnit.Framework;

namespace log4net.Tests.Util
{
	/// <summary>
	/// Used for internal unit testing the <see cref="PropertiesDictionary"/> class.
	/// </summary>
	/// <remarks>
	/// Used for internal unit testing the <see cref="PropertiesDictionary"/> class.
	/// </remarks>
	[TestFixture] public class CyclicBufferTest
	{
		[Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TestConstructorSize0()
		{
			CyclicBuffer cb = new CyclicBuffer(0);
		}

		[Test] public void TestSize1()
		{
			CyclicBuffer cb = new CyclicBuffer(1);

			Assertion.AssertEquals("Empty Buffer should have length 0", 0, cb.Length);
			Assertion.AssertEquals("Buffer should have max size 1", 1, cb.MaxSize);

			LoggingEvent event1 = new LoggingEvent(null, null, null, null, null, null);
			LoggingEvent event2 = new LoggingEvent(null, null, null, null, null, null);

			LoggingEvent discardedEvent = cb.Append(event1);

			Assertion.AssertNull("No event should be discarded untill the buffer is full", discardedEvent);
			Assertion.AssertEquals("Buffer should have length 1", 1, cb.Length);
			Assertion.AssertEquals("Buffer should still have max size 1", 1, cb.MaxSize);


			discardedEvent = cb.Append(event2);

			Assertion.AssertSame("Expect event1 to now be discarded", event1, discardedEvent);
			Assertion.AssertEquals("Buffer should still have length 1", 1, cb.Length);
			Assertion.AssertEquals("Buffer should really still have max size 1", 1, cb.MaxSize);

			LoggingEvent[] discardedEvents = cb.PopAll();

			Assertion.AssertEquals("Poped events length should be 1", 1, discardedEvents.Length);
			Assertion.AssertSame("Expect event2 to now be popped", event2, discardedEvents[0]);
			Assertion.AssertEquals("Buffer should be back to length 0", 0, cb.Length);
			Assertion.AssertEquals("Buffer should really really still have max size 1", 1, cb.MaxSize);
		}

		[Test] public void TestSize2()
		{
			CyclicBuffer cb = new CyclicBuffer(2);

			Assertion.AssertEquals("Empty Buffer should have length 0", 0, cb.Length);
			Assertion.AssertEquals("Buffer should have max size 2", 2, cb.MaxSize);

			LoggingEvent event1 = new LoggingEvent(null, null, null, null, null, null);
			LoggingEvent event2 = new LoggingEvent(null, null, null, null, null, null);
			LoggingEvent event3 = new LoggingEvent(null, null, null, null, null, null);

			LoggingEvent discardedEvent = null;
			
			discardedEvent = cb.Append(event1);
			Assertion.AssertNull("No event should be discarded after append 1", discardedEvent);
			discardedEvent = cb.Append(event2);
			Assertion.AssertNull("No event should be discarded after append 2", discardedEvent);

			discardedEvent = cb.Append(event3);
			Assertion.AssertSame("Expect event1 to now be discarded", event1, discardedEvent);

			discardedEvent = cb.PopOldest();
			Assertion.AssertSame("Expect event2 to now be discarded", event2, discardedEvent);

			LoggingEvent[] discardedEvents = cb.PopAll();

			Assertion.AssertEquals("Poped events length should be 1", 1, discardedEvents.Length);
			Assertion.AssertSame("Expect event3 to now be popped", event3, discardedEvents[0]);
			Assertion.AssertEquals("Buffer should be back to length 0", 0, cb.Length);
			Assertion.AssertEquals("Buffer should really really still have max size 2", 2, cb.MaxSize);
		}
	}
}
