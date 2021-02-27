using log4net.Appender;
using log4net.Core;

namespace log4net.Tests.Appender
{
    public class CountingContext
    {
        public int Count { get; set; }
    }

    public class CountingAppenderWithDependency : AppenderSkeleton
    {
        public CountingAppenderWithDependency(CountingContext context)
        {
            Context = context;
        }

        protected CountingContext Context { get; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            Context.Count++;
        }
    }
}
