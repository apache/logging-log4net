using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using Xunit;

namespace Log4NetConcurrencyRepro
{
    public class LoggingConcurrencyTests : IDisposable
    {
        private readonly TestMemoryAppender _memoryAppender;
        private readonly ILoggerRepository _repository;

        public LoggingConcurrencyTests()
        {
            _repository = LogManager.CreateRepository(Guid.NewGuid().ToString());

            _memoryAppender = new TestMemoryAppender();
            _memoryAppender.Layout = new PatternLayout("%message");
            _memoryAppender.ActivateOptions();

            var hierarchy = (Hierarchy)_repository;
            hierarchy.Root.AddAppender(_memoryAppender);
            hierarchy.Root.Level = Level.All;
            hierarchy.Configured = true;
        }

        [Fact]
        public void Logging_DuringHighConcurrency_ShouldNotLoseMessages()
        {
            const int messageCount = 400;
            const int loggerCount = 20;

            // alt b: var loggers = Enumerable.Range(0, loggerCount).Select(i => LogManager.GetLogger(_repository.Name, "ConcurrencyTest-" + i)).ToArray();

            Parallel.For(0, messageCount, i =>
            {
                // alt b: var logger = loggers[i % loggerCount];
                var logger = LogManager.GetLogger(_repository.Name, "ConcurrencyTest-" + (i % loggerCount));
                logger.Info("High concurrency message " + i);
            });

            Assert.Equal(messageCount, _memoryAppender.GetAppendCount());

            var events = _memoryAppender.GetEvents();
            Assert.Equal(messageCount, events.Length);

            for (var i = 0; i < messageCount; i++)
            {
                var expectedMessage = "High concurrency message " + i;
                Assert.Contains(events, e => e.RenderedMessage == expectedMessage);
            }
        }

        public void Dispose()
        {
            _memoryAppender.Clear();
            _repository.Shutdown();
        }

        private sealed class TestMemoryAppender : AppenderSkeleton
        {
            private readonly List<LoggingEvent> _events = new List<LoggingEvent>();
            private readonly object _lock = new object();
            private int _appendCount;

            protected override void Append(LoggingEvent loggingEvent)
            {
                Interlocked.Increment(ref _appendCount);

                if (loggingEvent == null)
                    return;

                loggingEvent.Fix = FixFlags.All;

                lock (_lock)
                {
                    _events.Add(loggingEvent);
                }
            }

            public int GetAppendCount()
            {
                return Volatile.Read(ref _appendCount);
            }

            public LoggingEvent[] GetEvents()
            {
                lock (_lock)
                {
                    return _events.ToArray();
                }
            }

            public void Clear()
            {
                lock (_lock)
                {
                    _events.Clear();
                    _appendCount = 0;
                }
            }
        }
    }
}