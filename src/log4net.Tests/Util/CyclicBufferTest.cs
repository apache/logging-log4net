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

namespace log4net.Tests.Util;

/// <summary>
/// Used for internal unit testing the <see cref="CyclicBuffer"/> class.
/// </summary>
[TestFixture]
public class CyclicBufferTest
{
  [Test]
  public void TestConstructorSize0()
    => Assert.That(() => new CyclicBuffer(0), Throws.TypeOf<ArgumentOutOfRangeException>());

  [Test]
  public void TestSize1()
  {
    CyclicBuffer cb = new(1);

    Assert.That(cb.Length, Is.EqualTo(0), "Empty Buffer should have length 0");
    Assert.That(cb.MaxSize, Is.EqualTo(1), "Buffer should have max size 1");

    LoggingEvent event1 = new(null, null, null, null, null, null);
    LoggingEvent event2 = new(null, null, null, null, null, null);

    LoggingEvent? discardedEvent = cb.Append(event1);

    Assert.That(discardedEvent, Is.Null, "No event should be discarded until the buffer is full");
    Assert.That(cb.Length, Is.EqualTo(1), "Buffer should have length 1");
    Assert.That(cb.MaxSize, Is.EqualTo(1), "Buffer should still have max size 1");

    discardedEvent = cb.Append(event2);

    Assert.That(discardedEvent, Is.SameAs(event1), "Expect event1 to now be discarded");
    Assert.That(cb.Length, Is.EqualTo(1), "Buffer should still have length 1");
    Assert.That(cb.MaxSize, Is.EqualTo(1), "Buffer should really still have max size 1");

    LoggingEvent[] discardedEvents = cb.PopAll();

    Assert.That(discardedEvents, Has.Length.EqualTo(1), "Popped events length should be 1");
    Assert.That(discardedEvents[0], Is.SameAs(event2), "Expect event2 to now be popped");
    Assert.That(cb.Length, Is.EqualTo(0), "Buffer should be back to length 0");
    Assert.That(cb.MaxSize, Is.EqualTo(1), "Buffer should really really still have max size 1");
  }

  [Test]
  public void TestSize2()
  {
    CyclicBuffer cb = new(2);

    Assert.That(cb.Length, Is.EqualTo(0), "Empty Buffer should have length 0");
    Assert.That(cb.MaxSize, Is.EqualTo(2), "Buffer should have max size 2");

    LoggingEvent event1 = new(null, null, null, null, null, null);
    LoggingEvent event2 = new(null, null, null, null, null, null);
    LoggingEvent event3 = new(null, null, null, null, null, null);

    LoggingEvent? discardedEvent;

    discardedEvent = cb.Append(event1);
    Assert.That(discardedEvent, Is.Null, "No event should be discarded after append 1");
    discardedEvent = cb.Append(event2);
    Assert.That(discardedEvent, Is.Null, "No event should be discarded after append 2");

    discardedEvent = cb.Append(event3);
    Assert.That(discardedEvent, Is.SameAs(event1), "Expect event1 to now be discarded");

    discardedEvent = cb.PopOldest();
    Assert.That(discardedEvent, Is.SameAs(event2), "Expect event2 to now be discarded");

    LoggingEvent[] discardedEvents = cb.PopAll();

    Assert.That(discardedEvents, Has.Length.EqualTo(1), "Popped events length should be 1");
    Assert.That(discardedEvents[0], Is.SameAs(event3), "Expect event3 to now be popped");
    Assert.That(cb.Length, Is.EqualTo(0), "Buffer should be back to length 0");
    Assert.That(cb.MaxSize, Is.EqualTo(2), "Buffer should really really still have max size 2");
  }
}