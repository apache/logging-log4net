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
