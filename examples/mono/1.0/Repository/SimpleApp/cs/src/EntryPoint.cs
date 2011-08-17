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
