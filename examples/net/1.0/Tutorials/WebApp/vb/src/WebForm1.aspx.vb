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
Imports System.Data
Imports System.Drawing
Imports System.Web
Imports System.Web.SessionState
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Web.UI.HtmlControls

Imports log4net

Namespace WebApp
	Public Class WebForm1 
		Inherits System.Web.UI.Page

		Private Shared ReadOnly log As ILog = LogManager.GetLogger(GetType(WebForm1))

		Protected Label1 As System.Web.UI.WebControls.Label
		Protected Label2 As System.Web.UI.WebControls.Label 
		Protected Label3 As System.Web.UI.WebControls.Label 
		Protected Label4 As System.Web.UI.WebControls.Label 

		Protected txtAdd1 As System.Web.UI.WebControls.TextBox
		Protected txtAdd2 As System.Web.UI.WebControls.TextBox
		Protected txtAdd3 As System.Web.UI.WebControls.TextBox
		Protected WithEvents btnCalcAdd As System.Web.UI.WebControls.Button

		Protected txtSub1 As System.Web.UI.WebControls.TextBox
		Protected txtSub2 As System.Web.UI.WebControls.TextBox
		Protected txtSub3 As System.Web.UI.WebControls.TextBox
		Protected WithEvents btnCalcSub As System.Web.UI.WebControls.Button

		Protected m_MathAdd As SimpleModule.Math = new SimpleModule.Math()
		Protected m_MathSub As SharedModule.Math = new SharedModule.Math()

		Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
			' Put user code to initialize the page here
		End Sub

		#Region " Web Form Designer Generated Code "

		Protected Overrides Sub OnInit(e As EventArgs)
			Dim appDom As AppDomain = AppDomain.CurrentDomain
			Dim context As HttpContext= HttpContext.Current
			'
			' CODEGEN: This call is required by the ASP.NET Web Form Designer.
			'
			InitializeComponent()
			MyBase.OnInit(e)

			txtAdd1.Text = "0"
			txtAdd2.Text = "0"
			txtAdd3.Text = "0"

			txtSub1.Text = "0"
			txtSub2.Text = "0"
			txtSub3.Text = "0"
		End Sub
		
		' Required method for Designer support - do not modify
		' the contents of this method with the code editor.
		Private Sub InitializeComponent()
		End Sub

		#End Region

		Private Sub btnCalcAdd_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCalcAdd.Click
			If log.IsDebugEnabled Then log.Debug("txtAdd1=[" & txtAdd1.Text & "] txtAdd2=[" & txtAdd2.Text & "]")

			Dim result As Integer = m_MathAdd.Add(Integer.Parse(txtAdd1.Text), Integer.Parse(txtAdd2.Text))

			If log.IsInfoEnabled Then log.Info("result=[" & result & "]")
			
			txtAdd3.Text = result.ToString()
		End Sub

		Private Sub btnCalcSub_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCalcSub.Click
			If log.IsDebugEnabled Then log.Debug("txtSub1=[" & txtSub1.Text & "] txtSub2=[" & txtSub2.Text & "]")

			Dim result As Integer = m_MathSub.Subtract(Integer.Parse(txtSub1.Text), Integer.Parse(txtSub2.Text))

			If log.IsInfoEnabled Then log.Info("result=[" & result & "]")
			
			txtSub3.Text = result.ToString()
		End Sub
	End Class
End Namespace
