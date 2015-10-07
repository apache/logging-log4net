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
