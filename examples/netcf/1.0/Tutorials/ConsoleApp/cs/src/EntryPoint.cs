#region Copyright
// 
// This framework is based on log4j see http://jakarta.apache.org/log4j
// Copyright (C) The Apache Software Foundation. All rights reserved.
//
// This software is published under the terms of the Apache Software
// License version 1.1, a copy of which has been included with this
// distribution in the LICENSE.txt file.
// 
#endregion

namespace ConsoleApp
{
	using System;

	using log4net;

	/// <summary>
	/// Example of how to simply configure and use log4net in a .NET Compact Framework
	/// application.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The .NET Compact Framework does not support retrieving assembly-level
	/// attributes, therefor log4net must be configured by code.
	/// </para>
	/// <para>
	/// The .NET Compact Framework does not support hooking up the <c>AppDomain.ProcessExit</c>
	/// and <c>AppDomain.DomainUnload</c> events, so log4net must be shutdown manually to 
	/// free all resources.
	/// </para>
	/// </remarks>
	public class EntryPoint
	{
		/// <summary>
		/// Application entry point.
		/// </summary>
		public static void Main() 
		{
			// Uncomment the next line to enable log4net internal debugging
			// log4net.helpers.LogLog.InternalDebugging = true;

			// This will instruct log4net to look for a configuration file
			// called ConsoleApp.exe.config in the application base
			// directory (i.e. the directory containing ConsoleApp.exe)
			log4net.Config.XmlConfigurator.Configure();

			// Create a logger
			ILog log = LogManager.GetLogger(typeof(EntryPoint));

			// Log an info level message
			if (log.IsInfoEnabled) log.Info("Application [ConsoleApp] Start");

			// Invoke static LogEvents method on LoggingExample class
			LoggingExample.LogEvents(); 

			Console.Write("Press Enter to exit...");
			Console.ReadLine();

			if (log.IsInfoEnabled) log.Info("Application [ConsoleApp] Stop");

			// It's not possible to use shutdown hooks in the .NET Compact Framework,
			// so you have manually shutdown log4net to free all resoures.
			LogManager.Shutdown();
		}
	}
}
