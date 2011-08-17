using System;
using System.Xml;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using log4net.Tests.Appender;
using NUnit.Framework;

namespace log4net.Tests.Hierarchy
{
    [TestFixture]
    public class Hierarchy
    {
        [Test]
        public void SetRepositoryPropertiesInConfigFile()
        {
            // LOG4NET-53: Allow repository properties to be set in the config file
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.LoadXml(@"
                <log4net>
                  <property>
                    <key value=""two-plus-two"" />
                    <value value=""4"" />
                  </property>
                  <appender name=""StringAppender"" type=""log4net.Tests.Appender.StringAppender, log4net.Tests"">
                    <layout type=""log4net.Layout.SimpleLayout"" />
                  </appender>
                  <root>
                    <level value=""ALL"" />                  
                    <appender-ref ref=""StringAppender"" />
                  </root>  
                </log4net>");

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            XmlConfigurator.Configure(rep, log4netConfig["log4net"]);

            Assert.AreEqual("4", rep.Properties["two-plus-two"]);
            Assert.IsNull(rep.Properties["one-plus-one"]);
        }

        [Test]
        public void AddingMultipleAppenders()
        {
            CountingAppender alpha = new CountingAppender();
            CountingAppender beta = new CountingAppender();

            Repository.Hierarchy.Hierarchy hierarchy = 
                (Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
            hierarchy.Root.AddAppender(alpha);
            hierarchy.Root.AddAppender(beta);
            hierarchy.Configured = true;

            ILog log = LogManager.GetLogger(GetType());
            log.Debug("Hello World");

            Assert.AreEqual(1, alpha.Counter);
            Assert.AreEqual(1, beta.Counter);
        }

        [Test]
        public void AddingMultipleAppenders2()
        {
            CountingAppender alpha = new CountingAppender();
            CountingAppender beta = new CountingAppender();
            
            BasicConfigurator.Configure(alpha, beta);

            ILog log = LogManager.GetLogger(GetType());
            log.Debug("Hello World");

            Assert.AreEqual(1, alpha.Counter);
            Assert.AreEqual(1, beta.Counter);
        }
    }
}
