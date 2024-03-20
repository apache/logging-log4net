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
using System.Diagnostics;
using System.Reflection;
using log4net.Util;

namespace log4net.Core
{
  /// <summary>
  /// Provides stack frame information without actually referencing a System.Diagnostics.StackFrame
  /// as that would require that the containing assembly is loaded.
  /// </summary>
  [Serializable]
  public class StackFrameItem
  {
    /// <summary>
    /// Creates a stack frame item from a stack frame.
    /// </summary>
    /// <param name="frame"></param>
    public StackFrameItem(StackFrame frame)
    {
      // set default values
      LineNumber = NA;
      FileName = NA;
      Method = new MethodItem();
      ClassName = NA;

      try
      {
        // get frame values
        LineNumber = frame.GetFileLineNumber().ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        FileName = frame.GetFileName();

        // get method values
        MethodBase? method = frame.GetMethod();
        if (method is not null)
        {
          if (method.DeclaringType is not null)
          {
            ClassName = method.DeclaringType.FullName;
          }

          Method = new MethodItem(method);
        }
      }
      catch (Exception ex)
      {
        LogLog.Error(declaringType, "An exception ocurred while retreiving stack frame information.", ex);
      }

      // set full info
      FullInfo = $"{ClassName}.{Method.Name}({FileName}:{LineNumber})";
    }

    /// <summary>
    /// Gets the fully qualified class name of the caller making the logging 
    /// request.
    /// </summary>
    public string ClassName { get; }

    /// <summary>
    /// Gets the file name of the caller.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the line number of the caller.
    /// </summary>
    public string LineNumber { get; }

    /// <summary>
    /// Gets the method name of the caller.
    /// </summary>
    public MethodItem Method { get; }

    /// <summary>
    /// Gets all available caller information in the format
    /// <c>fully.qualified.classname.of.caller.methodName(Filename:line)</c>
    /// </summary>
    public string FullInfo { get; }

    /// <summary>
    /// The fully qualified type of the StackFrameItem class.
    /// </summary>
    /// <remarks>
    /// Used by the internal logger to record the Type of the
    /// log message.
    /// </remarks>
    private static readonly Type declaringType = typeof(StackFrameItem);

    /// <summary>
    /// When location information is not available the constant
    /// <c>NA</c> is returned. Current value of this string
    /// constant is <b>?</b>.
    /// </summary>
    private const string NA = "?";
  }
}