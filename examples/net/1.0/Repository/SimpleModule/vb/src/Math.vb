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

' We want this assembly to have a seperate logging repository to the 
' rest of the application. We will configure this repository seperatly.
<Assembly: log4net.Config.Repository("SimpleModule")> 

' Configure logging for this assembly using the 'SimpleModule.dll.log4net' file
<Assembly: log4net.Config.XmlConfigurator(ConfigFileExtension:="log4net", Watch:=True)> 

Namespace SimpleModule
	Public Class Math
		' Create a logger for use in this class
		Private Shared ReadOnly log As log4net.ILog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)

		Public Sub New()
			If log.IsDebugEnabled Then log.Debug("Constructor")
		End Sub

		Public Function Add(Byval left As Integer, Byval right As Integer) As Integer 
			Dim result As Integer = left + right
			If log.IsInfoEnabled Then log.Info("" & left & " + " & right & " = " & result)
			Return result
		End Function
	End Class
End Namespace
