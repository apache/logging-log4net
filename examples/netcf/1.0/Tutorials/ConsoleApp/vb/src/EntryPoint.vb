#Region "Apache License"
'
' Licensed to the Apache Software Foundation (ASF) under one or more 
' contributor license agreements. See the NOTICE file distributed with
' this work for additional information regarding copyright ownership. 
' The ASF licenses this file to you under the Apache License, Version 2.0
' (the "License"); you may not use this file except in compliance with 
' the License. You may obtain a copy of the License at
'
' http://www.apache.org/licenses/LICENSE-2.0
'
' Unless required by applicable law or agreed to in writing, software
' distributed under the License is distributed on an "AS IS" BASIS,
' WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
' See the License for the specific language governing permissions and
' limitations under the License.
'
#End Region

Imports System

Imports log4net

Namespace ConsoleApp
	' Example of how to simply configure and use log4net in a .NET Compact Framework
	' application.
	'
	' The .NET Compact Framework does not support retrieving assembly-level
	' attributes, therefor log4net must be configured by code.
	'
	' The .NET Compact Framework does not support hooking up the <c>AppDomain.ProcessExit</c>
	' and <c>AppDomain.DomainUnload</c> events, so log4net must be shutdown manually to 
	' free all resources.
	Public Class EntryPoint
		' Application entry point.
		Public Shared Sub Main() 
			' Uncomment the next line to enable log4net internal debugging
			' log4net.helpers.LogLog.InternalDebugging = true;

			' This will instruct log4net to look for a configuration file
			' called ConsoleApp.exe.config in the application base
			' directory (i.e. the directory containing ConsoleApp.exe)
			log4net.Config.XmlConfigurator.Configure()

			' Create a logger
			Dim log As ILog = LogManager.GetLogger(GetType(EntryPoint))

			' Log an info level message
			If log.IsInfoEnabled Then log.Info("Application [ConsoleApp] Start")

			' Invoke shared LogEvents method on LoggingExample class
			LoggingExample.LogEvents()

			Console.Write("Press Enter to exit...")
			Console.ReadLine()

			If log.IsInfoEnabled Then log.Info("Application [ConsoleApp] Stop")

			' It's not possible to use shutdown hooks in the .NET Compact Framework,
			' so you have manually shutdown log4net to free all resoures.
			LogManager.Shutdown()
		End Sub
	End Class
End Namespace
