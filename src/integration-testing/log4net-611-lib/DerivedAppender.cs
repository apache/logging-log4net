using System;
using System.Diagnostics;
using log4net.Appender;
using log4net.Core;

namespace log4net_611_lib;

public class DerivedAppender : RollingFileAppender
{
  protected override void Append(LoggingEvent loggingEvent)
  {
    if (loggingEvent is null)
    {
      throw new ArgumentNullException(nameof(loggingEvent));
    }
    loggingEvent.Properties["appender-class-name"] = nameof(DerivedAppender);
    base.Append(loggingEvent);
  }
}