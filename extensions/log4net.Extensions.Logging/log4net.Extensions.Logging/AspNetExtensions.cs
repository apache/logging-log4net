using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using log4net.Config;
using log4net.Core;

namespace log4net.Extensions.Logging
{
    /// <summary>
    /// Helpers for ASP.NET Core
    /// </summary>
    public static class AspNetExtensions
    {

        /// <summary>
        /// Enable log4net as logging provider in ASP.NET Core.
        /// </summary>
        /// <param name="factory"></param>
        /// <returns>Instance of <c>ILoggerFactory</c></returns>
        public static ILoggerFactory AddLog4Net(this ILoggerFactory factory, Type type)
        {
            using (var provider = new Log4NetLoggerProvider(type))
            {
                factory.AddProvider(provider);
            }

            return factory;
        }

        /// <summary>
        /// Apply log4net configuration from XML config.
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configFileRelativePath">relative path to log4net configuration file.</param>
        /// <returns>Instance of <c>ILoggerFactory</c></returns>
        public static void ConfigureLog4Net(this IHostingEnvironment env, string configFileRelativePath, Type type)
        {
            GlobalContext.Properties["appRoot"] = env.ContentRootPath;

            var fileName = Path.Combine(env.ContentRootPath, configFileRelativePath);
            var repository = LoggerManager.RepositorySelector.GetRepository(type.GetTypeInfo().Assembly);

            XmlConfigurator.Configure(repository, new FileInfo(fileName));
        }
    }
}
