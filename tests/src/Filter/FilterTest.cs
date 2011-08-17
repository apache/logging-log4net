#if NET_2_0
using System;
using System.Collections.Generic;
using System.Xml;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Filter;
using log4net.Repository;
using NUnit.Framework;

namespace log4net.Tests.Filter
{
    [TestFixture]
    public class FilterTest
    {
        [Test]
        public void FilterConfigurationTest()
        {
            XmlDocument log4netConfig = new XmlDocument();
            #region Load log4netConfig
            log4netConfig.LoadXml(@"
            <log4net>
            <appender name=""MemoryAppender"" type=""log4net.Appender.MemoryAppender, log4net"">
                <filter type=""log4net.Tests.Filter.MultiplePropertyFilter, log4net.Tests"">
                    <condition>
                        <key value=""ABC"" />
                        <stringToMatch value=""123"" />
                    </condition>
                    <condition>
                        <key value=""DEF"" />
                        <stringToMatch value=""456"" />
                    </condition>
                </filter>
            </appender>
            <root>
                <level value=""ALL"" />
                <appender-ref ref=""MemoryAppender"" />
            </root>
            </log4net>");
            #endregion

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            XmlConfigurator.Configure(rep, log4netConfig["log4net"]);

            IAppender[] appenders = LogManager.GetRepository(rep.Name).GetAppenders();
            Assert.IsTrue(appenders.Length == 1);

            IAppender appender = Array.Find(appenders, a => a.Name == "MemoryAppender");
            Assert.IsNotNull(appender);

            MultiplePropertyFilter multiplePropertyFilter = 
                ((AppenderSkeleton)appender).FilterHead as MultiplePropertyFilter;

            var conditions = multiplePropertyFilter.GetConditions();
            Assert.AreEqual(2, conditions.Length);
            Assert.AreEqual("ABC", conditions[0].Key);
            Assert.AreEqual("123", conditions[0].StringToMatch);
            Assert.AreEqual("DEF", conditions[1].Key);
            Assert.AreEqual("456", conditions[1].StringToMatch);
        }
    }

    public class MultiplePropertyFilter : FilterSkeleton
    {
        private readonly List<Condition> _conditions = new List<Condition>();

        public override FilterDecision Decide(LoggingEvent loggingEvent)
        {
            return FilterDecision.Accept;
        }

        public Condition[] GetConditions()
        {
            return _conditions.ToArray();
        }

        public void AddCondition(Condition condition)
        {
            _conditions.Add(condition);
        }
        
        public class Condition
        {
            public string Key { get; set; }
            public string StringToMatch { get; set; }
        }
    }
}
#endif
