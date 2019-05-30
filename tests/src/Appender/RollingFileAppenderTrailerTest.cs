using System.Text;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Appender
{
	[TestFixture]
	public class RollingFileAppenderTrailerTest : RollingFileAppenderTest
	{
		[SetUp]
		public void Setup()
		{
		}

		[TearDown]
		public void TearDown()
		{
		}

		[Test]
		public void TestPeriodValue()
		{
			var appender = new RollingFileAppenderTrailing();
			appender.TrailPeriod = "00:05:00";

			var logger = CreateLogger("test.log", 100, new OnlyOnceErrorHandler(), 100,
				RollingFileAppender.RollingLockStrategyKind.None, new FileAppender.MinimalLock());

			ILog log = new LogImpl(logger);

			log.Error("Hello world 1");
			log.Error("Hello world 2");
		}

		private ILogger CreateLogger(string filename, long maxFileSize, IErrorHandler handler, int maxSizeRollBackups,
			RollingFileAppender.RollingLockStrategyKind rollingLockStrategy, FileAppender.LockingModelBase lockModel)
		{
			Repository.Hierarchy.Hierarchy h = (Repository.Hierarchy.Hierarchy)LogManager.CreateRepository("TestRepository");

			RollingFileAppenderTrailing appender = new RollingFileAppenderTrailing();;
			appender.TrailPeriod = "00:00:30";
			appender.File = filename;
			appender.AppendToFile = false;
			appender.CountDirection = 0;
			appender.RollingStyle = RollingFileAppender.RollingMode.Size;
			appender.MaxFileSize = maxFileSize;
			appender.Encoding = Encoding.ASCII;
			appender.ErrorHandler = handler;
			appender.MaxSizeRollBackups = maxSizeRollBackups;
			appender.RollingLockStrategy = rollingLockStrategy;
			if (lockModel != null)
			{
				appender.LockingModel = lockModel;
			}

			PatternLayout layout = new PatternLayout();
			layout.ConversionPattern = "%m%n";
			layout.ActivateOptions();

			appender.Layout = layout;
			appender.ActivateOptions();

			h.Root.AddAppender(appender);
			h.Configured = true;

			ILogger log = h.GetLogger("Logger");

			return log;
		}
	}
}
