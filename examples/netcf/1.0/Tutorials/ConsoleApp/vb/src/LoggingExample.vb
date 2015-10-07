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

Namespace ConsoleApp
	' Illustrates using log4net to conditionally log events, and 
	' using log4net to log exceptions, ...
	Public Class LoggingExample
		' Create a logger for use in this class
		Private Shared ReadOnly log As log4net.ILog = log4net.LogManager.GetLogger(GetType(LoggingExample))

		' Logs events.
		Public Shared Sub LogEvents()
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
				log4net.NDC.Push("NDC_Message")

				log.Warn("This should have an NDC message")

				' Set a Mapped Diagnostic Context value  
				log4net.MDC.Set("auth", "auth-none")
				log.Warn("This should have an MDC message for the key 'auth'")
			Finally
				' The NDC message is popped off the stack by using the Dispose method
   				If (Not disposableFrame is Nothing) Then disposableFrame.Dispose()
			End Try

			log.Warn("See the NDC has been popped of! The MDC 'auth' key is still with us.")
		End Sub

		' Helper methods to demonstrate location information and nested exceptions

		Private Shared Sub Bar()
			Goo()
		End Sub

		Private Shared Sub Foo()
			Throw New Exception("This is an Exception")
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
