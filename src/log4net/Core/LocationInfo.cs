#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security;
using log4net.Util;

namespace log4net.Core;

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
[Log4NetSerializable]
public class LocationInfo : ILog4NetSerializable
{
  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="callerStackBoundaryDeclaringType">The declaring type of the method that is
  /// the stack boundary into the logging system for this call.</param>
  /// <remarks>
  /// <para>
  /// Initializes a new instance of the <see cref="LocationInfo" />
  /// class based on the current thread.
  /// </para>
  /// </remarks>
  public LocationInfo(Type? callerStackBoundaryDeclaringType)
  {
    // Initialize all fields
    ClassName = NotAvailable;
    FileName = NotAvailable;
    LineNumber = NotAvailable;
    MethodName = NotAvailable;
    FullInfo = NotAvailable;

    if (callerStackBoundaryDeclaringType is not null)
    {
      try
      {
        StackTrace st = new(true);
        int frameIndex = 0;

        // skip frames not from fqnOfCallingClass
        while (frameIndex < st.FrameCount)
        {
          if (st.GetFrame(frameIndex) is StackFrame frame
              && frame.GetMethod()?.DeclaringType == callerStackBoundaryDeclaringType)
          {
            break;
          }
          frameIndex++;
        }

        // skip frames from fqnOfCallingClass
        while (frameIndex < st.FrameCount)
        {
          if (st.GetFrame(frameIndex) is StackFrame frame
              && frame.GetMethod()?.DeclaringType != callerStackBoundaryDeclaringType)
          {
            break;
          }
          frameIndex++;
        }

        if (frameIndex < st.FrameCount)
        {
          // take into account the frames we skip above
          int adjustedFrameCount = st.FrameCount - frameIndex;
          var stackFramesList = new List<StackFrameItem>(adjustedFrameCount);
          StackFrames = new StackFrameItem[adjustedFrameCount];
          for (int i = frameIndex; i < st.FrameCount; i++)
          {
            if (st.GetFrame(i) is StackFrame frame)
            {
              stackFramesList.Add(new StackFrameItem(frame));
            }
          }

          stackFramesList.CopyTo(StackFrames, 0);

          // now frameIndex is the first 'user' caller frame
          if (st.GetFrame(frameIndex) is StackFrame locationFrame)
          {
            if (locationFrame.GetMethod() is System.Reflection.MethodBase method)
            {
              MethodName = method.Name;
              if (method.DeclaringType is not null)
              {
                ClassName = method.DeclaringType.FullName;
              }
            }
            FileName = locationFrame.GetFileName();
            LineNumber = locationFrame.GetFileLineNumber().ToString(System.Globalization.NumberFormatInfo.InvariantInfo);

            // Combine all location info
            FullInfo = $"{ClassName}.{MethodName}({FileName}:{LineNumber})";
          }
        }
      }
      catch (System.Security.SecurityException)
      {
        // This security exception will occur if the caller does not have 
        // some undefined set of SecurityPermission flags.
        LogLog.Debug(_declaringType, "Security exception while trying to get caller stack frame. Error Ignored. Location Information Not Available.");
      }
    }
  }

  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="className">The fully qualified class name.</param>
  /// <param name="methodName">The method name.</param>
  /// <param name="fileName">The file name.</param>
  /// <param name="lineNumber">The line number of the method within the file.</param>
  /// <remarks>
  /// <para>
  /// Initializes a new instance of the <see cref="LocationInfo" />
  /// class with the specified data.
  /// </para>
  /// </remarks>
  public LocationInfo(string className, string methodName, string fileName, string lineNumber)
  {
    ClassName = className;
    FileName = fileName;
    LineNumber = lineNumber;
    MethodName = methodName;
    FullInfo = $"{ClassName}.{MethodName}({FileName}:{LineNumber})";
  }

  /// <summary>
  /// Serialization constructor
  /// </summary>
  /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data.</param>
  /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
  /// <remarks>
  /// <para>
  /// Initializes a new instance of the <see cref="Level" /> class 
  /// with serialized data.
  /// </para>
  /// </remarks>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter")]
  protected LocationInfo(SerializationInfo info, StreamingContext context)
  {
    // Use member names from log4net 2.x implicit serialzation for cross-version compat.
    ClassName = info.EnsureNotNull().GetString("m_className");
    FileName = info.GetString("m_fileName");
    LineNumber = info.GetString("m_lineNumber") ?? string.Empty;
    MethodName = info.GetString("m_methodName") ?? string.Empty;
    FullInfo = info.GetString("m_fullInfo") ?? string.Empty;
    StackFrames = info.GetValue("m_stackFrames", typeof(StackFrameItem[])) as StackFrameItem[];
  }

  /// <summary>
  /// Gets the fully qualified class name of the caller making the logging 
  /// request.
  /// </summary>
  public string? ClassName { get; }

  /// <summary>
  /// Gets the file name of the caller.
  /// </summary>
  public string? FileName { get; }

  /// <summary>
  /// Gets the line number of the caller.
  /// </summary>
  public string LineNumber { get; }

  /// <summary>
  /// Gets the method name of the caller.
  /// </summary>
  public string MethodName { get; }

  /// <summary>
  /// Gets all available caller information
  /// </summary>
  /// <value>
  /// All available caller information, in the format
  /// <c>fully.qualified.classname.of.caller.methodName(Filename:line)</c>
  /// </value>
  public string FullInfo { get; }

  /// <summary>
  /// Gets the stack frames from the stack trace of the caller making the log request
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
  public StackFrameItem[]? StackFrames { get; }

  /// <summary>
  /// Serializes this object into the <see cref="SerializationInfo" /> provided.
  /// </summary>
  /// <param name="info">The <see cref="SerializationInfo" /> to populate with data.</param>
  /// <param name="context">The destination for this serialization.</param>
  [SecurityCritical]
  [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand,
    SerializationFormatter = true)]
  public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
  {
    // Use member names from log4net 2.x implicit serialization for cross-version compat.
    info.EnsureNotNull().AddValue("m_className", ClassName);
    info.AddValue("m_fileName", FileName);
    info.AddValue("m_lineNumber", LineNumber);
    info.AddValue("m_methodName", MethodName);
    info.AddValue("m_fullInfo", FullInfo);
    info.AddValue("m_stackFrames", StackFrames);
  }

  /// <summary>
  /// The fully qualified type of the LocationInfo class.
  /// </summary>
  /// <remarks>
  /// Used by the internal logger to record the Type of the
  /// log message.
  /// </remarks>
  private static readonly Type _declaringType = typeof(LocationInfo);

  /// <summary>
  /// When location information is not available the constant
  /// <c>NA</c> is returned. Current value of this string
  /// constant is <b>?</b>.
  /// </summary>
  private const string NotAvailable = "?";
}
