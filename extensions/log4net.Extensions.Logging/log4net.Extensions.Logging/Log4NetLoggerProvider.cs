using System;
using Microsoft.Extensions.Logging;

namespace log4net.Extensions.Logging
{
    public class Log4NetLoggerProvider : IDisposable,Microsoft.Extensions.Logging.ILoggerProvider
    {
    	private readonly Type _type;

        public Log4NetLoggerProvider(Type type)
        {
        	_type = type;
        }

        /// <summary>
        /// Create a logger with the name <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the logger to be created.</param>
        /// <returns>New Logger</returns>
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name)
        {
            return new Log4NetLogger(_type);
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        public void Dispose()
        { }
    }
}
