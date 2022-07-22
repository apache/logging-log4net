using log4net.Appender;
using log4net.Core;
using log4net.Layout;

namespace log4net_611_lib;

public class DerivedAppender: RollingFileAppender
{
    protected override void Append(LoggingEvent loggingEvent)
    {
        loggingEvent.Properties["appender-class-name"] = nameof(DerivedAppender);
        base.Append(loggingEvent);
    }
}