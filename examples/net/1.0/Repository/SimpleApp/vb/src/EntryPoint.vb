#Region "Copyright"
' 
' This framework is based on log4j see http://jakarta.apache.org/log4j
' Copyright (C) The Apache Software Foundation. All rights reserved.
'
' This software is published under the terms of the Apache Software
' License version 1.1, a copy of which has been included with this
' distribution in the LICENSE.txt file.
'
#End Region 

Imports System

' Configure logging for this assembly using the 'SimpleApp.exe.log4net' file
<Assembly: log4net.Config.XmlConfigurator(ConfigFileExtension:="log4net", Watch:=True)> 

' The following alias attribute can be used to capture the logging 
' repository for the 'SimpleModule' assembly. Without specifying this 
' attribute the logging configuration for the 'SimpleModule' assembly
' will be read from the 'SimpleModule.dll.log4net' file. When this
' attribute is specified the configuration will be shared with this
' assemby's configuration.
'<Assembly:log4net.Config.AliasRepository("SimpleModule")>

Namespace SimpleApp
	Public Class EntryPoint
		' Create a logger for use in this class
		Private Shared ReadOnly log As log4net.ILog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)

		' The main entry point for the application.
		
		<STAThread()> Public Shared Sub Main(Byval args() As String)
			If log.IsInfoEnabled Then log.Info(args)

			If args.Length <> 2 Then
				log.Error("Must supply 2 command line arguments")
			Else
				Dim left As Integer = Integer.Parse(args(0))
				Dim right As Integer = Integer.Parse(args(1))
				Dim result As Integer = 0

				If log.IsDebugEnabled Then log.Debug("Adding [" & left & "] to [" & right & "]")

				result = (new SimpleModule.Math()).Add(left, right)

				If log.IsDebugEnabled Then log.Debug("Result [" & result & "]")

				Console.Out.WriteLine(result)


				If  log.IsDebugEnabled Then log.Debug("Subtracting [" & right & "] from [" & left & "]")

				result = (new SharedModule.Math()).Subtract(left, right)

				If log.IsDebugEnabled Then log.Debug("Result [" & result & "]")

				Console.Out.WriteLine(result)
			End If
		End Sub
	End Class
End Namespace
