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

// Configure this assembly using the 'EventIDLogApp.exe.config' config file
[assembly: log4net.Config.XmlConfigurator(Watch=true)]

namespace EventIDLogApp
{
	using System;

	using log4net.Ext.EventID;

	class EventIDLogApp
	{
		// Create a logger for use in this class
		private static readonly IEventIDLog log = EventIDLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			log.Info(1, "Application ["+System.Reflection.Assembly.GetEntryAssembly().GetName().Name+"] Start");

			log.Warn(40, "This is a warn message ");

			log.Info(2, "Application ["+System.Reflection.Assembly.GetEntryAssembly().GetName().Name+"] Stop");
		}
	}
}
