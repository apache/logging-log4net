using System;
using System.Diagnostics;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using log4net.Repository;
using NUnit.Framework;

namespace log4net.Tests.Appender
{
    [TestFixture]
    public class TraceAppenderTest
    {
        [Test]
        public void DefaultCategoryTest()
        {
            CategoryTraceListener categoryTraceListener = new CategoryTraceListener();
            Trace.Listeners.Clear();
            Trace.Listeners.Add(categoryTraceListener);

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

            TraceAppender traceAppender = new TraceAppender();
            traceAppender.Layout = new SimpleLayout();
            traceAppender.ActivateOptions();

            BasicConfigurator.Configure(rep, traceAppender);

            ILog log = LogManager.GetLogger(rep.Name, GetType());
            log.Debug("Message");

            Assert.AreEqual(
                GetType().ToString(),
                categoryTraceListener.Category);
        }

        [Test]
        public void MethodNameCategoryTest()
        {
            CategoryTraceListener categoryTraceListener = new CategoryTraceListener();
            Trace.Listeners.Clear();
            Trace.Listeners.Add(categoryTraceListener);

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());

            TraceAppender traceAppender = new TraceAppender();
            PatternLayout methodLayout = new PatternLayout("%method");
            methodLayout.ActivateOptions();
            traceAppender.Category = methodLayout;
            traceAppender.Layout = new SimpleLayout();
            traceAppender.ActivateOptions();

            BasicConfigurator.Configure(rep, traceAppender);

            ILog log = LogManager.GetLogger(rep.Name, GetType());
            log.Debug("Message");

            Assert.AreEqual(
                System.Reflection.MethodInfo.GetCurrentMethod().Name,
                categoryTraceListener.Category);
        }
    }

    public class CategoryTraceListener : TraceListener
    {
        private string lastCategory;

        public override void Write(string message)
        {
            // empty
        }

        public override void WriteLine(string message)
        {
            Write(message);
        }

        public override void Write(string message, string category)
        {
            lastCategory = category;
            base.Write(message, category);
        }

        public string Category
        {
            get { return lastCategory; }
        }
    }
}
