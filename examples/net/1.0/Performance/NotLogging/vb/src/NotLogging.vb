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

Imports Microsoft.VisualBasic

Imports log4net
Imports log4net.Appender
Imports log4net.Layout
Imports log4net.Repository
Imports log4net.Repository.Hierarchy

Namespace NotLogging
	Public Class NotLogging 
		#Region "Init Code"

		Private Shared WARM_UP_CYCLES As Integer = 10000

		Shared ReadOnly SHORT_LOG As ILog = LogManager.GetLogger("A0123456789")
		Shared ReadOnly MEDIUM_LOG As ILog = LogManager.GetLogger("A0123456789.B0123456789")
		Shared ReadOnly LONG_LOG As ILog = LogManager.GetLogger("A0123456789.B0123456789.C0123456789")

		Shared ReadOnly INEXISTENT_SHORT_LOG As ILog = LogManager.GetLogger("I0123456789")
		Shared ReadOnly INEXISTENT_MEDIUM_LOG As ILog = LogManager.GetLogger("I0123456789.B0123456789")
		Shared ReadOnly INEXISTENT_LONG_LOG As ILog = LogManager.GetLogger("I0123456789.B0123456789.C0123456789")

		Shared ReadOnly LOG_ARRAY As ILog() = New ILog() { _
								    SHORT_LOG, _
								    MEDIUM_LOG, _
								    LONG_LOG, _
								    INEXISTENT_SHORT_LOG, _
								    INEXISTENT_MEDIUM_LOG, _
								    INEXISTENT_LONG_LOG}

		Shared ReadOnly TIMED_TESTS As TimedTest() = New TimedTest() { _
										New SimpleMessage_Bare(), _
										New SimpleMessage_Array(), _
										New SimpleMessage_MethodGuard_Bare(), _
										New SimpleMessage_LocalGuard_Bare(), _
			  						    	New ComplexMessage_Bare(), _
										New ComplexMessage_Array(), _
										New ComplexMessage_MethodGuard_Bare(), _
										New ComplexMessage_MethodGuard_Array(), _
										New ComplexMessage_MemberGuard_Bare(), _
										New ComplexMessage_LocalGuard_Bare()}

		Private Shared Sub Usage() 
			System.Console.WriteLine( _
				"Usage: NotLogging <true|false> <runLength>" & Environment.NewLine & _
				vbTab & " true indicates shipped code" & Environment.NewLine & _
				vbTab & " false indicates code in development" & Environment.NewLine & _
				vbTab & " runLength is an int representing the run length of loops"  & Environment.NewLine & _
				vbTab & " We suggest that runLength be at least 1000000 (1 million).")
			Environment.Exit(1)
		End Sub


		' Program wide initialization method
		Private Shared Function ProgramInit(Byval args As String()) As Integer 
			Dim runLength As Integer = 0

			Try 
				runLength = Integer.Parse(args(1))
			Catch e As Exception
				System.Console.Error.WriteLine(e)
				Usage()
			End Try
    
			Dim layout As SimpleLayout = new SimpleLayout()
			layout.ActivateOptions
			Dim appender As ConsoleAppender = new ConsoleAppender()
			appender.Layout = layout
			appender.ActivateOptions
	    
			If "false" = args(0) Then
				' nothing to do
			Else If "true" = args(0) Then
				System.Console.WriteLine("Flagging as shipped code.")
				CType(LogManager.GetRepository(), log4net.Repository.Hierarchy.Hierarchy).Threshold = log4net.Core.Level.Warn
			Else
				Usage()
			End If

			CType(SHORT_LOG.Logger, Logger).Level = log4net.Core.Level.Info
			CType(LogManager.GetRepository(), log4net.Repository.Hierarchy.Hierarchy).Root.Level = log4net.Core.Level.Info
			CType(LogManager.GetRepository(), log4net.Repository.Hierarchy.Hierarchy).Root.AddAppender(appender)

			Return runLength
		End Function
	  
		#End Region

		' The main entry point for the application.
		<STAThread()> Public Shared Sub Main(Byval argv As String()) 
			Dim loggerInstance As ILog
			Dim timedTestInstance As TimedTest

			If System.Diagnostics.Debugger.IsAttached Then
				WARM_UP_CYCLES = 0
				argv = New String() {"false", "2"}
			End If
			If argv.Length <> 2 Then
				Usage()
			End If

			Dim runLength As Integer = ProgramInit(argv)

			System.Console.WriteLine()
			System.Console.Write("Warming Up...")

			If WARM_UP_CYCLES > 0 Then
				For Each loggerInstance In LOG_ARRAY
					For Each timedTestInstance In TIMED_TESTS
						timedTestInstance.Run(loggerInstance, WARM_UP_CYCLES)
					Next
				Next
			End If
			System.Console.WriteLine("Done")
			System.Console.WriteLine()

			' Calculate maximum description length
			Dim maxDescLen As Integer = 0
			For Each timedTestInstance In TIMED_TESTS
				maxDescLen = Math.Max(maxDescLen, timedTestInstance.Description.Length)
			Next

			Dim formatString As String = "{0,-" & (maxDescLen + 1) & "} {1,9:G} ticks. Log: {2}"
			Dim delta As Double

			Dim averageData As ArrayList = new ArrayList()

			For Each timedTestInstance In TIMED_TESTS
				Dim total As Double = 0
				For Each loggerInstance In LOG_ARRAY
					delta = timedTestInstance.Run(loggerInstance, runLength)
					System.Console.WriteLine(string.Format(formatString, timedTestInstance.Description, delta, CType(loggerInstance.Logger, Logger).Name))

					total = total + delta
				Next
				System.Console.WriteLine()

				averageData.Add(New Object() {timedTestInstance, total / Ctype(LOG_ARRAY.Length, Double)})
			Next
			System.Console.WriteLine()
			System.Console.WriteLine("Averages:")
			System.Console.WriteLine()

			Dim pair As Object()
			For Each pair In averageData
				Dim avgFormatString As String = "{0,-" & (maxDescLen + 1) & "} {1,9:G} ticks."
				System.Console.WriteLine(string.Format(avgFormatString, CType(pair(0), TimedTest).Description, CType(pair(1), Double)))
			Next
		End Sub
	End Class

	MustInherit Class TimedTest
		Public MustOverride Function Run(log As ILog, runLength As Long) As Double
		Public MustOverride ReadOnly Property Description() As String 
	End Class

	#Region "Tests calling Debug(string)"

	Class SimpleMessage_Bare 
		Inherits TimedTest

		Public Overrides Function Run(log As ILog, runLength As Long) As Double
			Dim before As DateTime = DateTime.Now
			Dim i as Integer
			For i = 0 To runLength - 1 Step 1 
				log.Debug("msg")
			Next  i
			Dim after As DateTime = DateTime.Now
			Dim diff As TimeSpan = after.Subtract(before)
			Return CType(diff.Ticks, Double) / CType(runLength, Double)
		End Function

		Public Overrides ReadOnly Property Description() As String
			Get 
				Return "log.Debug(""msg"")"
			End Get
		End Property
	End Class
	Class ComplexMessage_MethodGuard_Bare 
		Inherits TimedTest

		Public Overrides Function Run(log As ILog, runLength As Long) As Double
			Dim before As DateTime = DateTime.Now
			Dim i as Integer
			For i = 0 To runLength - 1 Step 1 
				If log.IsDebugEnabled Then
					log.Debug("msg" & i & "msg")
				End If
			Next i
			Dim after As DateTime = DateTime.Now
			Dim diff As TimeSpan = after.Subtract(before)
			Return CType(diff.Ticks, Double) / CType(runLength, Double)
		End Function

		Public Overrides ReadOnly Property Description() As String
			Get 
				Return "If log.IsDebugEnabled Then log.Debug(""msg"" & i & ""msg"")"
			End Get
		End Property
	End Class
	class ComplexMessage_Bare 
		Inherits TimedTest

		Public Overrides Function Run(log As ILog, runLength As Long) As Double
			Dim before As DateTime = DateTime.Now
			Dim i as Integer
			For i = 0 To runLength - 1 Step 1 
				log.Debug("msg" & i & "msg")
			Next i
			Dim after As DateTime = DateTime.Now
			Dim diff As TimeSpan = after.Subtract(before)
			Return CType(diff.Ticks, Double) / CType(runLength, Double)
		End Function

		Public Overrides ReadOnly Property Description() As String
			Get 
				Return "log.Debug(""msg"" & i & ""msg"")"
			End Get
		End Property
	End Class

	#End Region

	#Region "Tests calling Debug(new object[] { ... })"

	Class SimpleMessage_Array
		Inherits TimedTest

		Public Overrides Function Run(log As ILog, runLength As Long) As Double
			Dim before As DateTime = DateTime.Now
			Dim i as Integer
			For i = 0 To runLength - 1 Step 1 
				log.Debug(New Object() {"msg"})
			Next i
			Dim after As DateTime = DateTime.Now
			Dim diff As TimeSpan = after.Subtract(before)
			Return CType(diff.Ticks, Double) / CType(runLength, Double)
		End Function

		Public Overrides ReadOnly Property Description() As String
			Get 
				Return "log.Debug(New Object() {""msg""})"
			End Get
		End Property
	End Class
	Class ComplexMessage_MethodGuard_Array
		Inherits TimedTest

		Public Overrides Function Run(log As ILog, runLength As Long) As Double
			Dim before As DateTime = DateTime.Now
			Dim i as Integer
			For i = 0 To runLength - 1 Step 1 
				If log.IsDebugEnabled Then
					log.Debug(New Object() {"msg", i, "msg"})
				End If
			Next i
			Dim after As DateTime = DateTime.Now
			Dim diff As TimeSpan = after.Subtract(before)
			Return CType(diff.Ticks, Double) / CType(runLength, Double)
		End Function

		Public Overrides ReadOnly Property Description() As String
			Get 
				Return "If log.IsDebugEnabled Then log.Debug(New Object() {""msg"", i, ""msg""})"
			End Get
		End Property
	End Class
	Class ComplexMessage_Array
		Inherits TimedTest

		Public Overrides Function Run(log As ILog, runLength As Long) As Double
			Dim before As DateTime = DateTime.Now
			Dim i as Integer
			For i = 0 To runLength - 1 Step 1 
				log.Debug(New Object() {"msg", i, "msg"})
			Next i
			Dim after As DateTime = DateTime.Now
			Dim diff As TimeSpan = after.Subtract(before)
			Return CType(diff.Ticks, Double) / CType(runLength, Double)
		End Function

		Public Overrides ReadOnly Property Description() As String
			Get 
				Return "log.Debug(New Object() {""msg"", i, ""msg""})"
			End Get
		End Property
	End Class

	#End Region

	#Region "Tests calling Debug(string) (using class members)"

	Class ComplexMessage_MemberGuard_Bare
		Inherits TimedTest

		Public Overrides Function Run(log As ILog, runLength As Long) As Double
			Return (New Impl(log)).Run(runLength)
		End Function

		Public Overrides ReadOnly Property Description() As String
			Get
				Return "If m_isEnabled Then m_log.Debug(""msg"" & i & ""msg"")"
			End Get
		End Property

		Class Impl
			Private Readonly m_log As ILog 
			Private Readonly m_isEnabled As Boolean

			Public Sub New(log As ILog)
				m_log = log
				m_isEnabled = m_log.IsDebugEnabled
			End Sub

			Public Function Run(runLength As Long) As Double
				Dim before As DateTime = DateTime.Now
				Dim i as Integer
				For i = 0 To runLength - 1 Step 1 
					If m_isEnabled Then
						m_log.Debug("msg" & i & "msg")
					End If
				Next i
				Dim after As DateTime = DateTime.Now
				Dim diff As TimeSpan = after.Subtract(before)
				Return CType(diff.Ticks, Double) / CType(runLength, Double)
			End Function
		End Class
	End Class
	Class SimpleMessage_LocalGuard_Bare
		Inherits TimedTest

		Public Overrides Function Run(log As ILog, runLength As Long) As Double
			Dim isEnabled As Boolean = log.IsDebugEnabled

			Dim before As DateTime = DateTime.Now
			Dim i as Integer
			For i = 0 To runLength - 1 Step 1 
				If isEnabled Then log.Debug("msg")
			Next i
			Dim after As DateTime = DateTime.Now
			Dim diff As TimeSpan = after.Subtract(before)
			Return CType(diff.Ticks, Double) / CType(runLength, Double)
		End Function

		Public Overrides ReadOnly Property Description() As String
			Get
				Return "If isEnabled Then log.Debug(""msg"")"
			End Get
		End Property
	End Class
	Class SimpleMessage_MethodGuard_Bare
		Inherits TimedTest

		Public Overrides Function Run(log As ILog, runLength As Long) As Double
			Dim before As DateTime = DateTime.Now
			Dim i as Integer
			For i = 0 To runLength - 1 Step 1 
				If log.IsDebugEnabled Then log.Debug("msg")
			Next i
			Dim after As DateTime = DateTime.Now
			Dim diff As TimeSpan = after.Subtract(before)
			Return CType(diff.Ticks, Double) / CType(runLength, Double)
		End Function

		Public Overrides ReadOnly Property Description() As String
			Get
				Return "If log.IsDebugEnabled Then log.Debug(""msg"")"
			End Get
		End Property
	End Class
	Class ComplexMessage_LocalGuard_Bare
		Inherits TimedTest

		Public Overrides Function Run(log As ILog, runLength As Long) As Double
			Dim isEnabled As Boolean = log.IsDebugEnabled

			Dim before As DateTime = DateTime.Now
			Dim i as Integer
			For i = 0 To runLength - 1 Step 1 
				If isEnabled Then log.Debug("msg" & i & "msg")
			Next i
			Dim after As DateTime = DateTime.Now
			Dim diff As TimeSpan = after.Subtract(before)
			Return CType(diff.Ticks, Double) / CType(runLength, Double)
		End Function

		Public Overrides ReadOnly Property Description() As String
			Get
				Return "If isEnabled Then log.Debug(""msg"" & i & ""msg"")"
			End Get
		End Property
	End Class

	#End Region 
End Namespace
