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
using System.Configuration;
using System.Diagnostics;

namespace log4net.Util
{
	/// <summary>
	/// Outputs log statements from within the log4net assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Log4net components cannot make log4net logging calls. However, it is
	/// sometimes useful for the user to learn about what log4net is
	/// doing.
	/// </para>
	/// <para>
	/// All log4net internal debug calls go to the standard output stream
	/// whereas internal error messages are sent to the standard error output 
	/// stream.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public sealed class LogLog
	{
		#region Private Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="LogLog" /> class. 
		/// </summary>
		/// <remarks>
		/// Uses a private access modifier to prevent instantiation of this class.
		/// </remarks>
		private LogLog()
		{
		}

		#endregion Private Instance Constructors

		#region Static Constructor

		/// <summary>
		/// Static constructor that initializes logging by reading 
		/// settings from the application configuration file.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The <c>log4net.Internal.Debug</c> application setting
		/// controls internal debugging. This setting should be set
		/// to <c>true</c> to enable debugging.
		/// </para>
		/// <para>
		/// The <c>log4net.Internal.Quiet</c> application setting
		/// suppresses all internal logging including error messages. 
		/// This setting should be set to <c>true</c> to enable message
		/// suppression.
		/// </para>
		/// </remarks>
		static LogLog()
		{
#if !NETCF
			try
			{
				InternalDebugging = OptionConverter.ToBoolean(ConfigurationSettings.AppSettings["log4net.Internal.Debug"], false);
				QuietMode = OptionConverter.ToBoolean(ConfigurationSettings.AppSettings["log4net.Internal.Quiet"], false);
			}
			catch(Exception ex)
			{
				// If an exception is thrown here then it looks like the config file does not
				// parse correctly.
				//
				// We will leave debug OFF and print an Error message
				Error("LogLog: Exception while reading ConfigurationSettings. Check your .config file is well formed XML.", ex);
			}
#endif
		}

		#endregion Static Constructor

		#region Public Static Properties

		/// <summary>
		/// Gets or sets a value indicating whether log4net internal logging
		/// is enabled or disabled.
		/// </summary>
		/// <value>
		/// <c>true</c> if log4net internal logging is enabled, otherwise 
		/// <c>false</c>.
		/// </value>
		/// <remarks>
		/// <para>
		/// When set to <c>true</c>, internal debug level logging will be 
		/// displayed.
		/// </para>
		/// <para>
		/// This value can be set by setting the application setting 
		/// <c>log4net.Internal.Debug</c> in the application configuration
		/// file.
		/// </para>
		/// <para>
		/// The default value is <c>false</c>, i.e. debugging is
		/// disabled.
		/// </para>
		/// </remarks>
		/// <example>
		/// <para>
		/// The following example enables internal debugging using the 
		/// application configuration file :
		/// </para>
		/// <code>
		/// &lt;configuration&gt;
		///		&lt;appSettings&gt;
		///			&lt;add key="log4net.Internal.Debug" value="true" /&gt;
		///		&lt;/appSettings&gt;
		/// &lt;/configuration&gt;
		/// </code>
		/// </example>
		public static bool InternalDebugging
		{
			get { return s_debugEnabled; }
			set { s_debugEnabled = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether log4net should generate no output
		/// from internal logging, not even for errors. 
		/// </summary>
		/// <value>
		/// <c>true</c> if log4net should generate no output at all from internal 
		/// logging, otherwise <c>false</c>.
		/// </value>
		/// <remarks>
		/// <para>
		/// When set to <c>true</c> will cause internal logging at all levels to be 
		/// suppressed. This means that no warning or error reports will be logged. 
		/// This option overrides the <see cref="InternalDebugging"/> setting and 
		/// disables all debug also.
		/// </para>
		/// <para>This value can be set by setting the application setting
		/// <c>log4net.Internal.Quiet</c> in the application configuration file.
		/// </para>
		/// <para>
		/// The default value is <c>false</c>, i.e. internal logging is not
		/// disabled.
		/// </para>
		/// </remarks>
		/// <example>
		/// The following example disables internal logging using the 
		/// application configuration file :
		/// <code>
		/// &lt;configuration&gt;
		///		&lt;appSettings&gt;
		///			&lt;add key="log4net.Internal.Quiet" value="true" /&gt;
		///		&lt;/appSettings&gt;
		/// &lt;/configuration&gt;
		/// </code>
		/// </example>
		public static bool QuietMode
		{
			get { return s_quietMode; }
			set { s_quietMode = value; }
		}

		#endregion Public Static Properties

		#region Public Static Methods

		/// <summary>
		/// Writes output to the standard output stream.  
		/// </summary>
		/// <param name="msg">The message to log.</param>
		/// <remarks>
		/// Uses Console.Out for console output,
		/// and Trace for OutputDebugString output.
		/// </remarks>
		private static void EmitOutLine(string msg)
		{
#if NETCF
			Console.WriteLine(msg);
			//System.Diagnostics.Debug.WriteLine(msg);
#else
			Console.Out.WriteLine(msg);
			Trace.WriteLine(msg);
#endif
		}

		/// <summary>
		/// Writes output to the standard error stream.  
		/// </summary>
		/// <param name="msg">The message to log.</param>
		/// <remarks>
		/// Use Console.Error for console output,
		/// and Trace for OutputDebugString output.
		/// </remarks>
		private static void EmitErrorLine(string msg)
		{
#if NETCF
			Console.WriteLine(msg);
			//System.Diagnostics.Debug.WriteLine(msg);
#else
			Console.Error.WriteLine(msg);
			Trace.WriteLine(msg);
#endif
		}

		/// <summary>
		/// Writes log4net internal debug messages to the 
		/// standard output stream.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		/// <remarks>
		/// <para>
		///	All internal debug messages are prepended with 
		///	the string "log4net: ".
		/// </para>
		/// </remarks>
		public static void Debug(string msg) 
		{
			if (s_debugEnabled && !s_quietMode) 
			{
				EmitOutLine(PREFIX + msg);
			}
		}

		/// <summary>
		/// Writes log4net internal debug messages to the 
		/// standard output stream.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		/// <param name="t">An exception to log.</param>
		/// <remarks>
		/// <para>
		///	All internal debug messages are prepended with 
		///	the string "log4net: ".
		/// </para>
		/// </remarks>
		public static void Debug(string msg, Exception t) 
		{
			if (s_debugEnabled && !s_quietMode) 
			{
				EmitOutLine(PREFIX + msg);
				if (t != null)
				{
					EmitOutLine(t.ToString());
				}
			}
		}
  
		/// <summary>
		/// Writes log4net internal error messages to the 
		/// standard error stream.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		/// <remarks>
		/// <para>
		///	All internal error messages are prepended with 
		///	the string "log4net:ERROR ".
		/// </para>
		/// </remarks>
		public static void Error(string msg) 
		{
			if (!s_quietMode)
			{
				EmitErrorLine(ERR_PREFIX + msg);
			}
		}  

		/// <summary>
		/// Writes log4net internal error messages to the 
		/// standard error stream.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		/// <param name="t">An exception to log.</param>
		/// <remarks>
		/// <para>
		///	All internal debug messages are prepended with 
		///	the string "log4net:ERROR ".
		/// </para>
		/// </remarks>
		public static void Error(string msg, Exception t) 
		{
			if (!s_quietMode)
			{
				EmitErrorLine(ERR_PREFIX + msg);
				if (t != null) 
				{
					EmitErrorLine(t.ToString());
				}
			}
		}  

		/// <summary>
		/// Writes log4net internal warning messages to the 
		/// standard error stream.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		/// <remarks>
		/// <para>
		///	All internal warning messages are prepended with 
		///	the string "log4net:WARN ".
		/// </para>
		/// </remarks>
		public static void Warn(string msg) 
		{
			if (!s_quietMode)
			{
				EmitErrorLine(WARN_PREFIX + msg);
			}
		}  

		/// <summary>
		/// Writes log4net internal warning messages to the 
		/// standard error stream.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		/// <param name="t">An exception to log.</param>
		/// <remarks>
		/// <para>
		///	All internal warning messages are prepended with 
		///	the string "log4net:WARN ".
		/// </para>
		/// </remarks>
		public static void Warn(string msg, Exception t) 
		{
			if (!s_quietMode)
			{
				EmitErrorLine(WARN_PREFIX + msg);
				if (t != null) 
				{
					EmitErrorLine(t.ToString());
				}
			}
		} 

		#endregion Public Static Methods

		#region Private Static Fields

		/// <summary>
		///  Default debug level
		/// </summary>
		private static bool s_debugEnabled = false;

		/// <summary>
		/// In quietMode not even errors generate any output.
		/// </summary>
		private static bool s_quietMode = false;

		private const string PREFIX			= "log4net: ";
		private const string ERR_PREFIX		= "log4net:ERROR ";
		private const string WARN_PREFIX	= "log4net:WARN ";

		#endregion Private Static Fields
	}
}
