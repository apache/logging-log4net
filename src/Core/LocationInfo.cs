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
using System.Diagnostics;

using log4net.Util;

namespace log4net.Core
{
	/// <summary>
	/// The internal representation of caller location information.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class uses the <c>System.Diagnostics.StackTrace</c> class to generate
	/// a call stack. The caller's information is then extracted from this stack.
	/// </para>
	/// <para>
	/// The <c>System.Diagnostics.StackTrace</c> class is not supported on the 
	/// .NET Compact Framework 1.0 therefore caller location information is not
	/// available on that framework.
	/// </para>
	/// <para>
	/// The <c>System.Diagnostics.StackTrace</c> class has this to say about Release builds:
	/// </para>
	/// <para>
	/// "StackTrace information will be most informative with Debug build configurations. 
	/// By default, Debug builds include debug symbols, while Release builds do not. The 
	/// debug symbols contain most of the file, method name, line number, and column 
	/// information used in constructing StackFrame and StackTrace objects. StackTrace 
	/// might not report as many method calls as expected, due to code transformations 
	/// that occur during optimization."
	/// </para>
	/// <para>
	/// This means that in a Release build the caller information may be incomplete or may 
	/// not exist at all! Therefore caller location information cannot be relied upon in a Release build.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
#if !NETCF
	[Serializable]
#endif
	public class LocationInfo
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="LocationInfo" />
		/// class based on the current thread.
		/// </summary>
		/// <param name="fullNameOfCallingClass">The fully name of the calling class (not assembly qualified).</param>
		public LocationInfo(string fullNameOfCallingClass) 
		{
			// Initialise all fields
			m_className = NA;
			m_fileName = NA;
			m_lineNumber = NA;
			m_methodName = NA;
			m_fullInfo = NA;

#if !NETCF
			if (fullNameOfCallingClass != null && fullNameOfCallingClass.Length > 0)
			{
				try
				{
					StackTrace st = new StackTrace(true);
					int frameIndex = 0;

					// skip frames not from fqnOfCallingClass
					while (frameIndex < st.FrameCount)
					{
						StackFrame frame = st.GetFrame(frameIndex);
						if (frame.GetMethod().DeclaringType.FullName == fullNameOfCallingClass)
						{
							break;
						}
						frameIndex++;
					}

					// skip frames from fqnOfCallingClass
					while (frameIndex < st.FrameCount)
					{
						StackFrame frame = st.GetFrame(frameIndex);
						if (frame.GetMethod().DeclaringType.FullName != fullNameOfCallingClass)
						{
							break;
						}
						frameIndex++;
					}

					if (frameIndex < st.FrameCount)
					{
						// now frameIndex is the first 'user' caller frame
						StackFrame locationFrame = st.GetFrame(frameIndex);

						m_className = locationFrame.GetMethod().DeclaringType.FullName;
						m_fileName = locationFrame.GetFileName();
						m_lineNumber = locationFrame.GetFileLineNumber().ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
						m_methodName =  locationFrame.GetMethod().Name;
						m_fullInfo =  m_className + '.' + m_methodName + '(' + 
							m_fileName + ':' + m_lineNumber + ')';
					}
				}
				catch(System.Security.SecurityException)
				{
					// This security exception will occur if the caller does not have 
					// some undefined set of SecurityPermission flags.
					LogLog.Debug("LocationInfo: Security exception while trying to get caller stack frame. Error Ignored. Location Information Not Available.");
				}
			}
#endif
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LocationInfo" />
		/// class with the specified data.
		/// </summary>
		/// <param name="className">The fully qualified class name.</param>
		/// <param name="methodName">The method name.</param>
		/// <param name="fileName">The file name.</param>
		/// <param name="lineNumber">The line number of the method within the file.</param>
		public LocationInfo(string className, string methodName, string fileName, string lineNumber)
		{
			m_className = className;
			m_fileName = fileName;
			m_lineNumber = lineNumber;
			m_methodName = methodName;
			m_fullInfo = m_className + '.' + m_methodName + '(' + m_fileName + 
				':' + m_lineNumber + ')';
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets the fully qualified class name of the caller making the logging 
		/// request.
		/// </summary>
		/// <value>
		/// The fully qualified class name of the caller making the logging 
		/// request.
		/// </value>
		public string ClassName
		{
			get { return m_className; }
		}

		/// <summary>
		/// Gets the file name of the caller.
		/// </summary>
		/// <value>The file name of the caller.</value>
		public string FileName
		{
			get { return m_fileName; }
		}

		/// <summary>
		/// Gets the line number of the caller.
		/// </summary>
		/// <value>
		/// The line number of the caller.
		/// </value>
		public string LineNumber
		{
			get { return m_lineNumber; }
		}

		/// <summary>
		/// Gets the method name of the caller.
		/// </summary>
		/// <value>
		/// The method name of the caller.
		/// </value>
		public string MethodName
		{
			get { return m_methodName; }
		}

		/// <summary>
		/// Gets all available caller information, in the format
		/// <c>fully.qualified.classname.of.caller.methodName(Filename:line)</c>
		/// </summary>
		/// <value>
		/// All available caller information, in the format
		/// <c>fully.qualified.classname.of.caller.methodName(Filename:line)</c>
		/// </value>
		public string FullInfo
		{
			get { return m_fullInfo; }
		}

		#endregion Public Instance Properties

		#region Private Instance Fields

		private string m_className;
		private string m_fileName;
		private string m_lineNumber;
		private string m_methodName;
		private string m_fullInfo;

		#endregion Private Instance Fields

		#region Private Static Fields

		/// <summary>
		/// When location information is not available the constant
		/// <c>NA</c> is returned. Current value of this string
		/// constant is <b>?</b>.
		/// </summary>
		private const string NA = "?";

		#endregion Private Static Fields
	}
}
