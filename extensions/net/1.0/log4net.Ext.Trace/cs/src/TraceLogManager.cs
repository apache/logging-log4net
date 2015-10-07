#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
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

using System;
using System.Reflection;
using System.Collections;

using log4net;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;

/*
 * Custom Logging Classes to support additional logging levels.
 */
namespace log4net.Ext.Trace
{
	public class TraceLogManager
	{
		#region Static Member Variables

		/// <summary>
		/// The wrapper map to use to hold the <see cref="TraceLogImpl"/> objects
		/// </summary>
		private static readonly WrapperMap s_wrapperMap = new WrapperMap(new WrapperCreationHandler(WrapperCreationHandler));

		#endregion

		#region Constructor

		/// <summary>
		/// Private constructor to prevent object creation
		/// </summary>
		private TraceLogManager() { }

		#endregion

		#region Type Specific Manager Methods

		/// <summary>
		/// Returns the named logger if it exists
		/// </summary>
		/// <remarks>
		/// <para>If the named logger exists (in the default hierarchy) then it
		/// returns a reference to the logger, otherwise it returns
		/// <c>null</c>.</para>
		/// </remarks>
		/// <param name="name">The fully qualified logger name to look for</param>
		/// <returns>The logger found, or null</returns>
		public static ITraceLog Exists(string name) 
		{
			return Exists(Assembly.GetCallingAssembly(), name);
		}

		/// <summary>
		/// Returns the named logger if it exists
		/// </summary>
		/// <remarks>
		/// <para>If the named logger exists (in the specified domain) then it
		/// returns a reference to the logger, otherwise it returns
		/// <c>null</c>.</para>
		/// </remarks>
		/// <param name="domain">the domain to lookup in</param>
		/// <param name="name">The fully qualified logger name to look for</param>
		/// <returns>The logger found, or null</returns>
		public static ITraceLog Exists(string domain, string name) 
		{
			return WrapLogger(LoggerManager.Exists(domain, name));
		}

		/// <summary>
		/// Returns the named logger if it exists
		/// </summary>
		/// <remarks>
		/// <para>If the named logger exists (in the specified assembly's domain) then it
		/// returns a reference to the logger, otherwise it returns
		/// <c>null</c>.</para>
		/// </remarks>
		/// <param name="assembly">the assembly to use to lookup the domain</param>
		/// <param name="name">The fully qualified logger name to look for</param>
		/// <returns>The logger found, or null</returns>
		public static ITraceLog Exists(Assembly assembly, string name) 
		{
			return WrapLogger(LoggerManager.Exists(assembly, name));
		}

		/// <summary>
		/// Returns all the currently defined loggers in the default domain.
		/// </summary>
		/// <remarks>
		/// <para>The root logger is <b>not</b> included in the returned array.</para>
		/// </remarks>
		/// <returns>All the defined loggers</returns>
		public static ITraceLog[] GetCurrentLoggers()
		{
			return GetCurrentLoggers(Assembly.GetCallingAssembly());
		}

		/// <summary>
		/// Returns all the currently defined loggers in the specified domain.
		/// </summary>
		/// <param name="domain">the domain to lookup in</param>
		/// <remarks>
		/// The root logger is <b>not</b> included in the returned array.
		/// </remarks>
		/// <returns>All the defined loggers</returns>
		public static ITraceLog[] GetCurrentLoggers(string domain)
		{
			return WrapLoggers(LoggerManager.GetCurrentLoggers(domain));
		}

		/// <summary>
		/// Returns all the currently defined loggers in the specified assembly's domain.
		/// </summary>
		/// <param name="assembly">the assembly to use to lookup the domain</param>
		/// <remarks>
		/// The root logger is <b>not</b> included in the returned array.
		/// </remarks>
		/// <returns>All the defined loggers</returns>
		public static ITraceLog[] GetCurrentLoggers(Assembly assembly)
		{
			return WrapLoggers(LoggerManager.GetCurrentLoggers(assembly));
		}

		/// <summary>
		/// Retrieve or create a named logger.
		/// </summary>
		/// <remarks>
		/// <para>Retrieve a logger named as the <paramref name="name"/>
		/// parameter. If the named logger already exists, then the
		/// existing instance will be returned. Otherwise, a new instance is
		/// created.</para>
		/// 
		/// <para>By default, loggers do not have a set level but inherit
		/// it from the hierarchy. This is one of the central features of
		/// log4net.</para>
		/// </remarks>
		/// <param name="name">The name of the logger to retrieve.</param>
		/// <returns>the logger with the name specified</returns>
		public static ITraceLog GetLogger(string name)
		{
			return GetLogger(Assembly.GetCallingAssembly(), name);
		}

		/// <summary>
		/// Retrieve or create a named logger.
		/// </summary>
		/// <remarks>
		/// <para>Retrieve a logger named as the <paramref name="name"/>
		/// parameter. If the named logger already exists, then the
		/// existing instance will be returned. Otherwise, a new instance is
		/// created.</para>
		/// 
		/// <para>By default, loggers do not have a set level but inherit
		/// it from the hierarchy. This is one of the central features of
		/// log4net.</para>
		/// </remarks>
		/// <param name="domain">the domain to lookup in</param>
		/// <param name="name">The name of the logger to retrieve.</param>
		/// <returns>the logger with the name specified</returns>
		public static ITraceLog GetLogger(string domain, string name)
		{
			return WrapLogger(LoggerManager.GetLogger(domain, name));
		}

		/// <summary>
		/// Retrieve or create a named logger.
		/// </summary>
		/// <remarks>
		/// <para>Retrieve a logger named as the <paramref name="name"/>
		/// parameter. If the named logger already exists, then the
		/// existing instance will be returned. Otherwise, a new instance is
		/// created.</para>
		/// 
		/// <para>By default, loggers do not have a set level but inherit
		/// it from the hierarchy. This is one of the central features of
		/// log4net.</para>
		/// </remarks>
		/// <param name="assembly">the assembly to use to lookup the domain</param>
		/// <param name="name">The name of the logger to retrieve.</param>
		/// <returns>the logger with the name specified</returns>
		public static ITraceLog GetLogger(Assembly assembly, string name)
		{
			return WrapLogger(LoggerManager.GetLogger(assembly, name));
		}	

		/// <summary>
		/// Shorthand for <see cref="M:LogManager.GetLogger(string)"/>.
		/// </summary>
		/// <remarks>
		/// Get the logger for the fully qualified name of the type specified.
		/// </remarks>
		/// <param name="type">The full name of <paramref name="type"/> will 
		/// be used as the name of the logger to retrieve.</param>
		/// <returns>the logger with the name specified</returns>
		public static ITraceLog GetLogger(Type type) 
		{
			return GetLogger(Assembly.GetCallingAssembly(), type.FullName);
		}

		/// <summary>
		/// Shorthand for <see cref="M:LogManager.GetLogger(string)"/>.
		/// </summary>
		/// <remarks>
		/// Get the logger for the fully qualified name of the type specified.
		/// </remarks>
		/// <param name="domain">the domain to lookup in</param>
		/// <param name="type">The full name of <paramref name="type"/> will 
		/// be used as the name of the logger to retrieve.</param>
		/// <returns>the logger with the name specified</returns>
		public static ITraceLog GetLogger(string domain, Type type) 
		{
			return WrapLogger(LoggerManager.GetLogger(domain, type));
		}

		/// <summary>
		/// Shorthand for <see cref="M:LogManager.GetLogger(string)"/>.
		/// </summary>
		/// <remarks>
		/// Get the logger for the fully qualified name of the type specified.
		/// </remarks>
		/// <param name="assembly">the assembly to use to lookup the domain</param>
		/// <param name="type">The full name of <paramref name="type"/> will 
		/// be used as the name of the logger to retrieve.</param>
		/// <returns>the logger with the name specified</returns>
		public static ITraceLog GetLogger(Assembly assembly, Type type) 
		{
			return WrapLogger(LoggerManager.GetLogger(assembly, type));
		}

		#endregion

		#region Extension Handlers

		/// <summary>
		/// Lookup the wrapper object for the logger specified
		/// </summary>
		/// <param name="logger">the logger to get the wrapper for</param>
		/// <returns>the wrapper for the logger specified</returns>
		private static ITraceLog WrapLogger(ILogger logger)
		{
			return (ITraceLog)s_wrapperMap.GetWrapper(logger);
		}

		/// <summary>
		/// Lookup the wrapper objects for the loggers specified
		/// </summary>
		/// <param name="loggers">the loggers to get the wrappers for</param>
		/// <returns>Lookup the wrapper objects for the loggers specified</returns>
		private static ITraceLog[] WrapLoggers(ILogger[] loggers)
		{
			ITraceLog[] results = new ITraceLog[loggers.Length];
			for(int i=0; i<loggers.Length; i++)
			{
				results[i] = WrapLogger(loggers[i]);
			}
			return results;
		}

		/// <summary>
		/// Method to create the <see cref="ILoggerWrapper"/> objects used by
		/// this manager.
		/// </summary>
		/// <param name="logger">The logger to wrap</param>
		/// <returns>The wrapper for the logger specified</returns>
		private static ILoggerWrapper WrapperCreationHandler(ILogger logger)
		{
			return new TraceLogImpl(logger);
		}

		#endregion
	}
}
