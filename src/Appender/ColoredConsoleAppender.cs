#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Globalization;
using System.Runtime.InteropServices;

using log4net.Layout;

namespace log4net.Appender
{
	/// <summary>
	/// Appends logging events to the console.
	/// </summary>
	/// <remarks>
	/// <para>
	/// ColoredConsoleAppender appends log events to the standard output stream
	/// or the error output stream using a layout specified by the 
	/// user. It also allows the color of a specific type of message to be set.
	/// </para>
	/// <para>
	/// By default, all output is written to the console's standard output stream.
	/// The <see cref="Target"/> property can be set to direct the output to the
	/// error stream.
	/// </para>
	/// <para>
	/// NOTE: This appender writes directly to the application's attached console
	/// not to the <c>System.Console.Out</c> or <c>System.Console.Error</c> <c>TextWriter</c>.
	/// The <c>System.Console.Out</c> and <c>System.Console.Error</c> streams can be
	/// programatically redirected (for example NUnit does this to capture program ouput).
	/// This appender will ignore these redirections because it needs to use Win32
	/// API calls to colorise the output. To respect these redirections the <see cref="ConsoleAppender"/>
	/// must be used.
	/// </para>
	/// <para>
	/// When configuring the colored console appender, mapping should be
	/// specified to map a logging level to a color. For example:
	/// </para>
	/// <code>
	/// &lt;mapping&gt;
	/// 	&lt;level value="ERROR" /&gt;
	/// 	&lt;foreColor value="White" /&gt;
	/// 	&lt;backColor value="Red, HighIntensity" /&gt;
	/// &lt;/mapping&gt;
	/// &lt;mapping&gt;
	/// 	&lt;level value="DEBUG" /&gt;
	/// 	&lt;backColor value="Green" /&gt;
	/// &lt;/mapping&gt;
	/// </code>
	/// <para>
	/// The Level is the standard log4net logging level and ForeColor and BackColor can be any
	/// combination of:
	/// <list type="bullet">
	/// <item><term>Blue</term><description>color is blue</description></item>
	/// <item><term>Green</term><description>color is red</description></item>
	/// <item><term>Red</term><description>color is green</description></item>
	/// <item><term>White</term><description>color is white</description></item>
	/// <item><term>Yellow</term><description>color is yellow</description></item>
	/// <item><term>Purple</term><description>color is purple</description></item>
	/// <item><term>Cyan</term><description>color is cyan</description></item>
	/// <item><term>HighIntensity</term><description>color is intensified</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	/// <author>Rick Hobbs</author>
	/// <author>Nicko Cadell</author>
	public class ColoredConsoleAppender : AppenderSkeleton
	{
		#region Colors Enum

		/// <summary>
		/// The enum of possible color values for use with the color mapping method
		/// </summary>
		[Flags]
		public enum Colors : int
		{
			/// <summary>
			/// color is blue
			/// </summary>
			Blue = 0x0001,

			/// <summary>
			/// color is green
			/// </summary>
			Green = 0x0002,

			/// <summary>
			/// color is red
			/// </summary>
			Red = 0x0004,

			/// <summary>
			/// color is white
			/// </summary>
			White = Blue | Green | Red,

			/// <summary>
			/// color is yellow
			/// </summary>
			Yellow = Red | Green,

			/// <summary>
			/// color is purple
			/// </summary>
			Purple = Red | Blue,

			/// <summary>
			/// color is cyan
			/// </summary>
			Cyan = Green | Blue,

			/// <summary>
			/// color is inensified
			/// </summary>
			HighIntensity = 0x0008,
		}


		#endregion

		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ColoredConsoleAppender" /> class.
		/// </summary>
		/// <remarks>
		/// The instance of the <see cref="ColoredConsoleAppender" /> class is set up to write 
		/// to the standard output stream.
		/// </remarks>
		public ColoredConsoleAppender() 
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ColoredConsoleAppender" /> class
		/// with the specified layout.
		/// </summary>
		/// <param name="layout">the layout to use for this appender</param>
		/// <remarks>
		/// The instance of the <see cref="ColoredConsoleAppender" /> class is set up to write 
		/// to the standard output stream.
		/// </remarks>
		public ColoredConsoleAppender(ILayout layout) : this(layout, false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ColoredConsoleAppender" /> class
		/// with the specified layout.
		/// </summary>
		/// <param name="layout">the layout to use for this appender</param>
		/// <param name="writeToErrorStream">flag set to <c>true</c> to write to the console error stream</param>
		/// <remarks>
		/// When <paramref name="writeToErrorStream" /> is set to <c>true</c>, output is written to
		/// the standard error output stream.  Otherwise, output is written to the standard
		/// output stream.
		/// </remarks>
		public ColoredConsoleAppender(ILayout layout, bool writeToErrorStream) 
		{
			Layout = layout;
			m_writeToErrorStream = writeToErrorStream;
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Target is the value of the console output stream.
		/// This is either <c>"Console.Out"</c> or <c>"Console.Error"</c>.
		/// </summary>
		/// <value>
		/// Target is the value of the console output stream.
		/// This is either <c>"Console.Out"</c> or <c>"Console.Error"</c>.
		/// </value>
		virtual public string Target
		{
			get { return m_writeToErrorStream ? ConsoleError : ConsoleOut; }
			set
			{
				string v = value.Trim();
				
				if (string.Compare(ConsoleError, v, true, CultureInfo.InvariantCulture) == 0) 
				{
					m_writeToErrorStream = true;
				} 
				else 
				{
					m_writeToErrorStream = false;
				}
			}
		}

		/// <summary>
		/// Add a mapping of level to color - done by the config file
		/// </summary>
		/// <param name="mapping">The mapping to add</param>
		public void AddMapping(ColoredConsoleAppenderLevelColorMapping mapping)
		{
			ushort usMapping = (ushort)((int)mapping.ForeColor + (((int)mapping.BackColor) << 4) );
			m_Level2ColorMap[mapping.Level] = usMapping;
		}

		/// <summary>
		/// A class to act as a mapping between the level that a logging call is made at and
		/// the color it should be displayed as.
		/// </summary>
		public class ColoredConsoleAppenderLevelColorMapping
		{
			private log4net.Core.Level m_level;
			private Colors m_foreColor;
			private Colors m_backColor;

			/// <summary>
			/// The level to map to a color
			/// </summary>
			public log4net.Core.Level Level
			{
				get { return m_level; }
				set { m_level = value; }
			}

			/// <summary>
			/// The mapped foreground color for the specified level
			/// </summary>
			public Colors ForeColor
			{
				get { return m_foreColor; }
				set { m_foreColor = value; }
			}

			/// <summary>
			/// The mapped background color for the specified level
			/// </summary>
			public Colors BackColor
			{
				get { return m_backColor; }
				set { m_backColor = value; }
			}
		}

		#endregion Public Instance Properties

		#region Override implementation of AppenderSkeleton

		/// <summary>
		/// This method is called by the <see cref="AppenderSkeleton.DoAppend"/> method.
		/// </summary>
		/// <param name="loggingEvent">The event to log.</param>
		/// <remarks>
		/// <para>
		/// Writes the event to the console.
		/// </para>
		/// <para>
		/// The format of the output will depend on the appender's layout.
		/// </para>
		/// </remarks>
		override protected void Append(log4net.Core.LoggingEvent loggingEvent) 
		{
			IntPtr iConsoleHandle = IntPtr.Zero;
			if (m_writeToErrorStream)
			{
				// Write to the error stream
				iConsoleHandle = GetStdHandle(STD_ERROR_HANDLE);
			}
			else
			{
				// Write to the output stream
				iConsoleHandle = GetStdHandle(STD_OUTPUT_HANDLE);
			}

			// set the output parameters
			// Default to white on black
			ushort uiColorInfo = (UInt16)(Colors.Red | Colors.Blue | Colors.Green);

			// see if there is a lookup.
			Object colLookup = m_Level2ColorMap[loggingEvent.Level];
			if(colLookup != null)
			{
				uiColorInfo = (ushort)colLookup;
			}

			// get the current console colour.
			CONSOLE_SCREEN_BUFFER_INFO bufferInfo;
			GetConsoleScreenBufferInfo(iConsoleHandle, out bufferInfo);

			// set the console.
			SetConsoleTextAttribute(iConsoleHandle, uiColorInfo);

			string strLoggingMessage = RenderLoggingEvent(loggingEvent);

			// write the output.
			UInt32 uiWritten = 0;
			WriteConsoleW(	iConsoleHandle,
							strLoggingMessage,
							(UInt32)strLoggingMessage.Length,
							out (UInt32)uiWritten,
							IntPtr.Zero);

			// reset the console back to its previous colour scheme.
			SetConsoleTextAttribute(iConsoleHandle, bufferInfo.wAttributes);
		}

		/// <summary>
		/// This appender requires a <see cref="Layout"/> to be set.
		/// </summary>
		/// <value><c>true</c></value>
		override protected bool RequiresLayout
		{
			get { return true; }
		}

		#endregion Override implementation of AppenderSkeleton

		#region Public Static Fields

		/// <summary>
		/// The <see cref="ColoredConsoleAppender.Target"/> to use when writing to the Console 
		/// standard output stream.
		/// </summary>
		public const string ConsoleOut = "Console.Out";

		/// <summary>
		/// The <see cref="ColoredConsoleAppender.Target"/> to use when writing to the Console 
		/// standard error output stream.
		/// </summary>
		public const string ConsoleError = "Console.Error";

		#endregion Public Static Fields

		#region Private Instances Fields

		/// <summary>
		/// Flag to write output to the error stream rather than the standard output stream
		/// </summary>
		private bool m_writeToErrorStream = false;

		/// <summary>
		/// Mapping from level object to colour value
		/// </summary>
		private System.Collections.Hashtable m_Level2ColorMap = new System.Collections.Hashtable();

		#endregion Private Instances Fields

		#region Win32 Methods

		[DllImport("Kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
		private static extern bool SetConsoleTextAttribute(
			IntPtr hConsoleHandle,
			ushort uiAttributes);

		[DllImport("Kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
		private static extern bool GetConsoleScreenBufferInfo(
			IntPtr hConsoleHandle,
			out  CONSOLE_SCREEN_BUFFER_INFO bufferInfo);

		[DllImport("Kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
		private static extern bool WriteConsoleW(
			IntPtr hConsoleHandle,
			[MarshalAs(UnmanagedType.LPWStr)] string strBuffer,
			UInt32 uiBufferLen,
			out UInt32 uiWritten,
			IntPtr pReserved);

		//private static readonly UInt32 STD_INPUT_HANDLE = unchecked((UInt32)(-10));
		private static readonly UInt32 STD_OUTPUT_HANDLE = unchecked((UInt32)(-11));
		private static readonly UInt32 STD_ERROR_HANDLE = unchecked((UInt32)(-12));

		[DllImport("Kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
		private static extern IntPtr GetStdHandle(
			UInt32 uiType);

		[StructLayout(LayoutKind.Sequential)]
		private struct COORD 
		{
			public UInt16 x; 
			public UInt16 y; 
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct SMALL_RECT 
		{
			public UInt16 Left; 
			public UInt16 Top; 
			public UInt16 Right; 
			public UInt16 Bottom; 
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct CONSOLE_SCREEN_BUFFER_INFO 
		{ 
			public COORD      dwSize; 
			public COORD      dwCursorPosition; 
			public ushort     wAttributes; 
			public SMALL_RECT srWindow; 
			public COORD      dwMaximumWindowSize; 
		}

		#endregion
	}
}
