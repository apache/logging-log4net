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

// Configure logging for this assembly using the 'SimpleApp.exe.log4net' file
[assembly: log4net.Config.XmlConfigurator(ConfigFileExtension="log4net", Watch=true)]

// The following alias attribute can be used to capture the logging 
// repository for the 'SimpleModule' assembly. Without specifying this 
// attribute the logging configuration for the 'SimpleModule' assembly
// will be read from the 'SimpleModule.dll.log4net' file. When this
// attribute is specified the configuration will be shared with this
// assemby's configuration.
//[assembly: log4net.Config.AliasRepository("SimpleModule")]

namespace SimpleApp
{
	using System;

	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class EntryPoint
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			if (log.IsInfoEnabled) log.Info(args);

			if (args.Length != 2)
			{
				log.Error("Must supply 2 command line arguments");
			}
			else
			{
				int left = int.Parse(args[0]);
				int right = int.Parse(args[1]);
				int result = 0;

				if (log.IsDebugEnabled) log.Debug("Adding ["+left+"] to ["+right+"]");

				result = (new SimpleModule.Math()).Add(left, right);

				if (log.IsDebugEnabled) log.Debug("Result ["+result+"]");

				Console.Out.WriteLine(result);


				if (log.IsDebugEnabled) log.Debug("Subtracting ["+right+"] from ["+left+"]");

				result = (new SharedModule.Math()).Subtract(left, right);

				if (log.IsDebugEnabled) log.Debug("Result ["+result+"]");

				Console.Out.WriteLine(result);
			}
		}
	}
}
