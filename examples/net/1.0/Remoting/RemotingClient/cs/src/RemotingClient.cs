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
