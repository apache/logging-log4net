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

