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

' Configure log4net using the .config file
<Assembly: log4net.Config.XmlConfigurator(Watch:=True)> 
' This will cause log4net to look for a configuration file
' called ConsoleApp.exe.config in the application base
' directory (i.e. the directory containing ConsoleApp.exe)

Namespace ConsoleApp
	' Example of how to simply configure and use log4net
	Public Class LoggingExample
		' Create a logger for use in this class
		Private Shared ReadOnly log As log4net.ILog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)
		' NOTE that using System.Reflection.MethodBase.GetCurrentMethod().DeclaringType
		' is equivalent to typeof(LoggingExample) but is more portable
		' i.e. you can copy the code directly into another class without
		' needing to edit the code.

		' Application entry point
		Public Shared Sub Main(Byval args As String())
			' Log an info level message
			If log.IsInfoEnabled Then log.Info("Application [ConsoleApp] Start")

			' Log a debug message. Test if debug is enabled before
			' attempting to log the message. This is not required but
			' can make running without logging faster.
			If log.IsDebugEnabled Then log.Debug("This is a debug message")

			Try
				Bar()
			Catch ex As Exception
				' Log an error with an exception
				log.Error("Exception thrown from method Bar", ex)
			End Try

			log.Error("Hey this is an error!")

			Dim disposableFrame As IDisposable

			Try 
				' Push a message on to the Nested Diagnostic Context stack
				disposableFrame = log4net.NDC.Push("NDC_Message")

				log.Warn("This should have an NDC message")

				' Set a Mapped Diagnostic Context value  
				log4net.MDC.Set("auth", "auth-none")
				log.Warn("This should have an MDC message for the key 'auth'")
			Finally
				' The NDC message is popped off the stack by using the Dispose method
   				If (Not disposableFrame is Nothing) Then disposableFrame.Dispose()
			End Try

			log.Warn("See the NDC has been popped of! The MDC 'auth' key is still with us.")

			' Log an info level message
			If log.IsInfoEnabled Then log.Info("Application [ConsoleApp] End")

			Console.Write("Press Enter to exit...")
			Console.ReadLine()
		End Sub

		' Helper methods to demonstrate location information and nested exceptions

		Private Shared Sub Bar()
			Goo()
		End Sub

		Private Shared Sub Foo()
			Throw new Exception("This is an Exception")
		End Sub

		Private Shared Sub Goo()
			Try
				Foo()
			Catch ex As Exception
				Throw New ArithmeticException("Failed in Goo. Calling Foo. Inner Exception provided", ex)
			End Try
		End Sub
	End Class
End Namespace
