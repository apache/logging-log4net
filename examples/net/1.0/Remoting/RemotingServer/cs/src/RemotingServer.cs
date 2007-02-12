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
			log4net.LogManager.GetRepository().PluginMap.Add(new log4net.Plugin.RemoteLoggingServerPlugin("LoggingSink"));

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
