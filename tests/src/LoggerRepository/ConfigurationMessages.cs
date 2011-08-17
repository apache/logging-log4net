using System;
using System.Collections;
using System.Xml;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.LoggerRepository
{
    [TestFixture]
    public class ConfigurationMessages
    {
        [Test]
        public void ConfigurationMessagesTest()
        {
            LogLog.EmitInternalMessages = false;
            LogLog.InternalDebugging = true;

            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.LoadXml(@"
                <log4net>
                  <appender name=""LogLogAppender"" type=""log4net.Tests.LoggerRepository.LogLogAppender, log4net.Tests"">
                    <layout type=""log4net.Layout.SimpleLayout"" />
                  </appender>
                  <appender name=""MemoryAppender"" type=""log4net.Appender.MemoryAppender"">
                    <layout type=""log4net.Layout.SimpleLayout"" />
                  </appender>
                  <root>
                    <level value=""ALL"" />
                    <appender-ref ref=""LogLogAppender"" />
                    <appender-ref ref=""MemoryAppender"" />
                  </root>  
                </log4net>");

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            rep.ConfigurationChanged += new LoggerRepositoryConfigurationChangedEventHandler(rep_ConfigurationChanged);

            ICollection configurationMessages = XmlConfigurator.Configure(rep, log4netConfig["log4net"]);

            Assert.IsTrue(configurationMessages.Count > 0);
        }

        static void rep_ConfigurationChanged(object sender, EventArgs e)
        {
            ConfigurationChangedEventArgs configChanged = (ConfigurationChangedEventArgs)e;

            Assert.IsTrue(configChanged.ConfigurationMessages.Count > 0);
        }
    }

    public class LogLogAppender : AppenderSkeleton
    {
        private readonly static Type declaringType = typeof(LogLogAppender);

        public override void ActivateOptions()
        {
            LogLog.Debug(declaringType, "Debug - Activating options...");
            LogLog.Warn(declaringType, "Warn - Activating options...");
            LogLog.Error(declaringType, "Error - Activating options...");

            base.ActivateOptions();
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            LogLog.Debug(declaringType, "Debug - Appending...");
            LogLog.Warn(declaringType, "Warn - Appending...");
            LogLog.Error(declaringType, "Error - Appending...");
        }
    }
}