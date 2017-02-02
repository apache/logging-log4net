using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace log4net.Tests.Appender
{
    /// <summary>
    /// Provides data for the <see cref="EventRaisingAppender.LoggingEventAppended"/> event.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class LoggingEventEventArgs : EventArgs
    {
        public log4net.Core.LoggingEvent LoggingEvent { get; private set; }

        public LoggingEventEventArgs(log4net.Core.LoggingEvent loggingEvent)
        {
            if (loggingEvent == null) throw new ArgumentNullException("loggingEvent");
            LoggingEvent = loggingEvent;
        }
    }

    /// <summary>
    /// A log4net appender that raises an event each time a logging event is appended
    /// </summary>
    /// <remarks>
    /// This class is intended to provide a way for test code to inspect logging
    /// events as they are generated.
    /// </remarks>
    public class EventRaisingAppender : log4net.Appender.IAppender
    {
        public event EventHandler<LoggingEventEventArgs> LoggingEventAppended;

        protected void OnLoggingEventAppended(LoggingEventEventArgs e)
        {
            var loggingEventAppended = LoggingEventAppended;
            if (loggingEventAppended != null)
            {
                loggingEventAppended(this, e);
            }
        }

        public void Close()
        {
        }

        public void DoAppend(log4net.Core.LoggingEvent loggingEvent)
        {
            OnLoggingEventAppended(new LoggingEventEventArgs(loggingEvent));
        }

        public string Name
        {
            get; set;
        }
    }
}
