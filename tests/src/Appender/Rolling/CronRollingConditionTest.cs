using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using log4net.Appender.Rolling;

namespace log4net.Tests.Appender.Rolling
{
    [TestFixture]
    public class CronRollingConditionTest
    {
        [Test]
        public void IsMetTest()
        {
            Tuple<CronRollingCondition, Tuple<DateTime, bool>[]>[] tests = new Tuple<CronRollingCondition, Tuple<DateTime, bool>[]>[]{
                Tuple.Create(new CronRollingCondition("*", "*", "*", "*", "5"), new Tuple<DateTime, bool>[]{
                    Tuple.Create(new DateTime(2009, 10, 10, 12, 5, 0), true),
                    Tuple.Create(new DateTime(2009, 10, 10, 12, 5, 1), false),
                    Tuple.Create(new DateTime(2009, 10, 10, 12, 6, 0), false),
                    Tuple.Create(new DateTime(2009, 10, 10, 12, 10, 0), false),
                    Tuple.Create(new DateTime(2009, 10, 10, 13, 5, 1), true),
                    Tuple.Create(new DateTime(2009, 10, 10, 14, 5, 1), true),
                }),
                Tuple.Create(new CronRollingCondition("*", "*", "*", "*", "*/3"), new Tuple<DateTime, bool>[]{
                    Tuple.Create(new DateTime(2009, 10, 10, 12, 0, 0), true),
                    Tuple.Create(new DateTime(2009, 10, 10, 12, 0, 1), false),
                    Tuple.Create(new DateTime(2009, 10, 10, 12, 0, 59), false),
                    Tuple.Create(new DateTime(2009, 10, 10, 12, 3, 0), true),
                    Tuple.Create(new DateTime(2009, 10, 10, 12, 3, 59), false),
                    Tuple.Create(new DateTime(2009, 10, 10, 12, 4, 1), false),
                    Tuple.Create(new DateTime(2009, 10, 10, 12, 6, 33), true),
                    Tuple.Create(new DateTime(2009, 10, 10, 12, 10, 0), false),
                }),
            };
            foreach (Tuple<CronRollingCondition, Tuple<DateTime, bool>[]> test in tests)
            {
                foreach (Tuple<DateTime, bool> testCheck in test.Item2)
                {
                    Assert.AreEqual(test.Item1.IsMet(testCheck.Item1), testCheck.Item2, string.Format("failed for {0} with condition {1}", testCheck.Item1, test.Item1));
                }
            }
        }
    }
}
