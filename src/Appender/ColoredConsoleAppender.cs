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

// MONO 1.0 Beta mcs does not like #if !A && !B && !C syntax

// .NET Compact Framework 1.0 has no support for Win32 Console API's
#if !NETCF 
// .Mono 1.0 has no support for Win32 Console API's
#if !MONO 
// SSCLI 1.0 has no support for Win32 Console API's
#if !SSCLI
// We don't want framework or platform specific code in the Core version of
// log4net
#if !CORE

using System;
using System.Globalization;
using System.Runtime.InteropServices;

using log4net.Layout;
using log4net.Util;

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
	/// programmatically redirected (for example NUnit does this to capture program output).
	/// This appender will ignore these redirections because it needs to use Win32
	/// API calls to colorize the output. To respect these redirections the <see cref="ConsoleAppender"/>
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
	/// combination of the following values:
	/// <list type="bullet">
	/// <item><term>Blue</term><description></description></item>
	/// <item><term>Green</term><description></description></item>
	/// <item><term>Red</term><description></description></item>
	/// <item><term>White</term><description></description></item>
	/// <item><term>Yellow</term><description></description></item>
	/// <item><term>Purple</term><description></description></item>
	/// <item><term>Cyan</term><description></description></item>
	/// <item><term>HighIntensity</term><description></description></item>
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
		/// <remarks>
		/// <para>
		/// The following flags can be combined together to
		/// form the colors.
		/// </para>
		/// </remarks>
		/// <seealso cref="ColoredConsoleAppender" />
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
			/// color is intensified
			/// </summary>
			HighIntensity = 0x0008,
		}

		#endregion // Colors Enum

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
		[Obsolete("Instead use the default constructor and set the Layout property")]
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
		[Obsolete("Instead use the default constructor and set the Layout & Target properties")]
		public ColoredConsoleAppender(ILayout layout, bool writeToErrorStream) 
		{
			Layout = layout;
			m_writeToErrorStream = writeToErrorStream;
		}

		#endregion // Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Target is the value of the console output stream.
		/// This is either <c>"Console.Out"</c> or <c>"Console.Error"</c>.
		/// </summary>
		/// <value>
		/// Target is the value of the console output stream.
		/// This is either <c>"Console.Out"</c> or <c>"Console.Error"</c>.
		/// </value>
		/// <remarks>
		/// <para>
		/// Target is the value of the console output stream.
		/// This is either <c>"Console.Out"</c> or <c>"Console.Error"</c>.
		/// </para>
		/// </remarks>
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
		/// <remarks>
		/// <para>
		/// Add a <see cref="LevelColors"/> mapping to this appender.
		/// Each mapping defines the foreground and background colours
		/// for a level.
		/// </para>
		/// </remarks>
		public void AddMapping(LevelColors mapping)
		{
			m_levelMapping.Add(mapping);
		}

		#endregion // Public Instance Properties

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
			IntPtr consoleHandle = IntPtr.Zero;
			if (m_writeToErrorStream)
			{
				// Write to the error stream
				consoleHandle = GetStdHandle(STD_ERROR_HANDLE);
			}
			else
			{
				// Write to the output stream
				consoleHandle = GetStdHandle(STD_OUTPUT_HANDLE);
			}

			// set the output parameters
			// Default to white on black
			ushort colorInfo = (ushort)Colors.White;

			// see if there is a specified lookup.
			LevelColors levelColors = m_levelMapping.Lookup(loggingEvent.Level) as LevelColors;
			if (levelColors != null)
			{
				colorInfo = levelColors.CombinedColor;
			}

			// get the current console color.
			CONSOLE_SCREEN_BUFFER_INFO bufferInfo;
			GetConsoleScreenBufferInfo(consoleHandle, out bufferInfo);

			// set the console.
			SetConsoleTextAttribute(consoleHandle, colorInfo);

			string strLoggingMessage = RenderLoggingEvent(loggingEvent);

			// write the output.
			UInt32 ignoreWrittenCount = 0;
			WriteConsoleW(	consoleHandle,
							strLoggingMessage,
							(UInt32)strLoggingMessage.Length,
							out (UInt32)ignoreWrittenCount,
							IntPtr.Zero);

			// reset the console back to its previous color scheme.
			SetConsoleTextAttribute(consoleHandle, bufferInfo.wAttributes);
		}

		/// <summary>
		/// This appender requires a <see cref="Layout"/> to be set.
		/// </summary>
		/// <value><c>true</c></value>
		/// <remarks>
		/// <para>
		/// This appender requires a <see cref="Layout"/> to be set.
		/// </para>
		/// </remarks>
		override protected bool RequiresLayout
		{
			get { return true; }
		}

		/// <summary>
		/// Initialize the options for this appender
		/// </summary>
		/// <remarks>
		/// <para>
		/// Initialize the level to color mappings set on this appender.
		/// </para>
		/// </remarks>
		public override void ActivateOptions()
		{
			base.ActivateOptions();
			m_levelMapping.ActivateOptions();
		}

		#endregion // Override implementation of AppenderSkeleton

		#region Public Static Fields

		/// <summary>
		/// The <see cref="ColoredConsoleAppender.Target"/> to use when writing to the Console 
		/// standard output stream.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The <see cref="ColoredConsoleAppender.Target"/> to use when writing to the Console 
		/// standard output stream.
		/// </para>
		/// </remarks>
		public const string ConsoleOut = "Console.Out";

		/// <summary>
		/// The <see cref="ColoredConsoleAppender.Target"/> to use when writing to the Console 
		/// standard error output stream.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The <see cref="ColoredConsoleAppender.Target"/> to use when writing to the Console 
		/// standard error output stream.
		/// </para>
		/// </remarks>
		public const string ConsoleError = "Console.Error";

		#endregion // Public Static Fields

		#region Private Instances Fields

		/// <summary>
		/// Flag to write output to the error stream rather than the standard output stream
		/// </summary>
		private bool m_writeToErrorStream = false;

		/// <summary>
		/// Mapping from level object to color value
		/// </summary>
		private LevelMapping m_levelMapping = new LevelMapping();

		#endregion // Private Instances Fields

		#region Win32 Methods

		[DllImport("Kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
		private static extern bool SetConsoleTextAttribute(
			IntPtr consoleHandle,
			ushort attributes);

		[DllImport("Kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
		private static extern bool GetConsoleScreenBufferInfo(
			IntPtr consoleHandle,
			out CONSOLE_SCREEN_BUFFER_INFO bufferInfo);

		[DllImport("Kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
		private static extern bool WriteConsoleW(
			IntPtr hConsoleHandle,
			[MarshalAs(UnmanagedType.LPWStr)] string strBuffer,
			UInt32 bufferLen,
			out UInt32 written,
			IntPtr reserved);

		//private static readonly UInt32 STD_INPUT_HANDLE = unchecked((UInt32)(-10));
		private static readonly UInt32 STD_OUTPUT_HANDLE = unchecked((UInt32)(-11));
		private static readonly UInt32 STD_ERROR_HANDLE = unchecked((UInt32)(-12));

		[DllImport("Kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
		private static extern IntPtr GetStdHandle(
			UInt32 type);

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

		#endregion // Win32 Methods

		#region LevelColors LevelMapping Entry

		/// <summary>
		/// A class to act as a mapping between the level that a logging call is made at and
		/// the color it should be displayed as.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Defines the mapping between a level and the color it should be displayed in.
		/// </para>
		/// </remarks>
		public class LevelColors : LevelMappingEntry
		{
			private Colors m_foreColor;
			private Colors m_backColor;
			private ushort m_combinedColor = 0;

			/// <summary>
			/// The mapped foreground color for the specified level
			/// </summary>
			/// <remarks>
			/// <para>
			/// Required property.
			/// The mapped foreground color for the specified level.
			/// </para>
			/// </remarks>
			public Colors ForeColor
			{
				get { return m_foreColor; }
				set { m_foreColor = value; }
			}

			/// <summary>
			/// The mapped background color for the specified level
			/// </summary>
			/// <remarks>
			/// <para>
			/// Required property.
			/// The mapped background color for the specified level.
			/// </para>
			/// </remarks>
			public Colors BackColor
			{
				get { return m_backColor; }
				set { m_backColor = value; }
			}

			/// <summary>
			/// Initialize the options for the object
			/// </summary>
			/// <remarks>
			/// <para>
			/// Combine the <see cref="ForeColor"/> and <see cref="BackColor"/> together.
			/// </para>
			/// </remarks>
			public override void ActivateOptions()
			{
				base.ActivateOptions();
				m_combinedColor = (ushort)( (int)m_foreColor + (((int)m_backColor) << 4) );
			}

			/// <summary>
			/// The combined <see cref="ForeColor"/> and <see cref="BackColor"/> suitable for 
			/// setting the console color.
			/// </summary>
			internal ushort CombinedColor
			{
				get { return m_combinedColor; }
			}
		}

		#endregion // LevelColors LevelMapping Entry
	}
}

#endif // !CORE
#endif // !SSCLI
#endif // !MONO
#endif // !NETCF
