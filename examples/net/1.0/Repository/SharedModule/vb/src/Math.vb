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

Namespace SharedModule
	Public Class Math
		' Create a logger for use in this class
		Private Shared ReadOnly log As log4net.ILog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)

		Public Sub New()
			If log.IsDebugEnabled Then log.Debug("Constructor")
		End Sub

		Public Function Subtract(Byval left As Integer, Byval right As Integer) As Integer 
			Dim result As Integer = left - right
			If log.IsInfoEnabled Then log.Info("" & left & " - " & right & " = " & result)
			Return result
		End Function
	End Class
End Namespace
