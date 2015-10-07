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
Imports System.Collections
Imports System.ComponentModel
Imports System.Web
Imports System.Web.SessionState

' Load the configuration from the 'WebApp.dll.log4net' file
<Assembly:log4net.Config.XmlConfigurator(ConfigFileExtension:="log4net", Watch:=true)>

Namespace WebApp 
	Public Class Global 
		Inherits System.Web.HttpApplication

		Public Sub New()
			InitializeComponent()
		End Sub
		
		' Required method for Designer support - do not modify
		' the contents of this method with the code editor.
		Private Sub InitializeComponent()
		End Sub
	End Class
End Namespace

