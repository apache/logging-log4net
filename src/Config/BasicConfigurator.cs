#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System.Reflection;

using log4net.Appender;
using log4net.Layout;
using log4net.Util;
using log4net.Repository;
using log4net.Repository.Hierarchy;

namespace log4net.Config
{
	/// <summary>
	/// Use this class to quickly configure a <see cref="Hierarchy"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Allows very simple programmatic configuration of log4net.
	/// </para>
	/// <para>
	/// Only one appender can be configured using this configurator.
	/// The appender is set at the root of the hierarchy and all logging
	/// events will be delivered to that appender.
	/// </para>
	/// <para>
	/// Appenders can also implement the <see cref="log4net.Core.IOptionHandler"/> interface. Therefore
	/// they would require that the <see cref="log4net.Core.IOptionHandler.ActivateOptions()"/> method
	/// be called after the appenders properties have been configured.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public sealed class BasicConfigurator
	{
		#region Private Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="BasicConfigurator" /> class. 
		/// </summary>
		/// <remarks>
		/// Uses a private access modifier to prevent instantiation of this class.
		/// </remarks>
		private BasicConfigurator()
		{
		}

		#endregion Private Instance Constructors

		#region Public Static Methods

		/// <summary>
		/// Initializes the log4net system with a default configuration.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Initializes the log4net logging system using a <see cref="ConsoleAppender"/>
		/// that will write to <c>Console.Out</c>. The log messages are
		/// formatted using the <see cref="PatternLayout"/> layout object
		/// with the <see cref="PatternLayout.TtlnConversionPattern"/>
		/// layout style.
		/// </para>
		/// </remarks>
		static public void Configure() 
		{
			BasicConfigurator.Configure(LogManager.GetRepository(Assembly.GetCallingAssembly()));
		}

		/// <summary>
		/// Initializes the log4net system using the specified appender.
		/// </summary>
		/// <param name="appender">The appender to use to log all logging events.</param>
		static public void Configure(IAppender appender) 
		{
			BasicConfigurator.Configure(LogManager.GetRepository(Assembly.GetCallingAssembly()), appender);
		}

		/// <summary>
		/// Initializes the <see cref="ILoggerRepository"/> with a default configuration.
		/// </summary>
		/// <param name="repository">The repository to configure.</param>
		/// <remarks>
		/// <para>
		/// Initializes the specified repository using a <see cref="ConsoleAppender"/>
		/// that will write to <c>Console.Out</c>. The log messages are
		/// formatted using the <see cref="PatternLayout"/> layout object
		/// with the <see cref="PatternLayout.TtlnConversionPattern"/>
		/// layout style.
		/// </para>
		/// </remarks>
		static public void Configure(ILoggerRepository repository) 
		{
			BasicConfigurator.Configure(repository, new ConsoleAppender(new PatternLayout(PatternLayout.TtlnConversionPattern)));
		}

		/// <summary>
		/// Initializes the <see cref="ILoggerRepository"/> using the specified appender.
		/// </summary>
		/// <param name="repository">The repository to configure.</param>
		/// <param name="appender">The appender to use to log all logging events.</param>
		static public void Configure(ILoggerRepository repository, IAppender appender) 
		{
			if (repository is IBasicRepositoryConfigurator)
			{
				((IBasicRepositoryConfigurator)repository).Configure(appender);
			}
			else
			{
				LogLog.Warn("Repository [" + repository + "] does not support the BasicConfigurator");
			}
		}

		#endregion Public Static Methods
	}
}
