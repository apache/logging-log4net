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

// Configure log4net using the .config file
[assembly: log4net.Config.XmlConfigurator()]

namespace RemotingServer
{
	using System;
	using System.Runtime.Remoting;

	/// <summary>
	/// Example of how to simply configure and use log4net
	/// </summary>
	public class RemotingServer
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Application entry point
		/// </summary>
		/// <param name="args">command line arguments</param>
		static void Main(string[] args)
		{
			// Log an info level message
			if (log.IsInfoEnabled) log.Info("Application [RemotingServer] Start");

			// Configure remoting. This loads the TCP channel as specified in the .config file.
			RemotingConfiguration.Configure(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

			// Publish the remote logging server. This is done using the log4net plugin.
			log4net.LogManager.GetLoggerRepository().PluginMap.Add(new log4net.Plugin.RemoteLoggingServerPlugin("LoggingSink"));

			// Wait for the user to exit
			Console.WriteLine("Press 0 and ENTER to Exit");
			String keyState = "";
			while (String.Compare(keyState,"0", true) != 0)
			{
				keyState = Console.ReadLine();
			}

			// Log an info level message
			if (log.IsInfoEnabled) log.Info("Application [RemotingServer] End");
		}
	}
}
