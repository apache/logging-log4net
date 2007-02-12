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

namespace RemotingClient
{
	using System;
	using System.Runtime.Remoting;

	/// <summary>
	/// Example of how to simply configure and use log4net
	/// </summary>
	public class RemotingClient
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
			if (log.IsInfoEnabled) log.Info("Application [RemotingClient] Start");

			log.Fatal("First Fatal message");

			for(int i=0; i<8; i++)
			{
				log.Debug("Hello");
			}

			// Log a message with an exception and nested exception
			log.Error("An exception has occured", new Exception("Some exception", new Exception("Some nested exception")));

			for(int i=0; i<8; i++)
			{
				log.Debug("There");
			}

			// Stress test can be called here
			//StessTest();

			// Log an info level message
			if (log.IsInfoEnabled) log.Info("Application [RemotingClient] End");
		}

		// Example stress test.
		static void StessTest()
		{
			int milliStart = Environment.TickCount;
			for (int i=0; i<10000; i++)
			{
				log.Error("["+i+"] This is an error message");
			}
			int milliEnd = Environment.TickCount;

			Console.WriteLine("Test Run Time: "+TimeSpan.FromMilliseconds(milliEnd - milliStart).ToString());
		}
	}
}
