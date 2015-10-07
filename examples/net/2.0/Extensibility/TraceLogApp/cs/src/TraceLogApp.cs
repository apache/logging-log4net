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

// Configure this assembly using the 'TraceLogApp.exe.log4net' config file
[assembly: log4net.Config.XmlConfigurator(ConfigFileExtension="log4net", Watch=true)]

namespace TraceLogApp
{
	using System;

	using log4net.Ext.Trace;

	class TraceLogApp
	{
		// Create a logger for use in this class
		private static readonly ITraceLog log = TraceLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			for (int i=0; i<10; i++)
			{
				log.Trace("This is a trace message "+i);
				System.Threading.Thread.Sleep(new TimeSpan(0, 0, 2));
			}
		}
	}
}
