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

using log4net.Appender;
using System;
using System.Runtime.InteropServices;

namespace log4net.Util
{
  /// <summary>
  /// Native Methods
  /// </summary>
  /// <author>Jan Friedrich</author>
  internal static class NativeMethods
  {
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern bool CloseHandle(IntPtr handle);

    [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern bool DuplicateToken(IntPtr existingTokenHandle, int securityImpersonationLevel, ref IntPtr duplicateTokenHandle);

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern int GetConsoleOutputCP();

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern bool SetConsoleTextAttribute(
      IntPtr consoleHandle,
      ushort attributes);

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern bool GetConsoleScreenBufferInfo(
      IntPtr consoleHandle,
      out ConsoleScreenBufferInfo bufferInfo);

    internal const uint StdOutputHandle = unchecked((uint)-11);
    internal const uint StdErrorHandle = unchecked((uint)-12);

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr GetStdHandle(uint type);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Coord
    {
      public ushort x;
      public ushort y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SmallRect
    {
      public ushort Left;
      public ushort Top;
      public ushort Right;
      public ushort Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ConsoleScreenBufferInfo
    {
      public Coord dwSize;
      public Coord dwCursorPosition;
      public ushort wAttributes;
      public SmallRect srWindow;
      public Coord dwMaximumWindowSize;
    }

    /// <summary>
    /// Formats a message string.
    /// </summary>
    /// <param name="dwFlags">Formatting options, and how to interpret the <paramref name="lpSource" /> parameter.</param>
    /// <param name="lpSource">Location of the message definition.</param>
    /// <param name="dwMessageId">Message identifier for the requested message.</param>
    /// <param name="dwLanguageId">Language identifier for the requested message.</param>
    /// <param name="lpBuffer">If <paramref name="dwFlags" /> includes FORMAT_MESSAGE_ALLOCATE_BUFFER, the function allocates a buffer using the <c>LocalAlloc</c> function, and places the pointer to the buffer at the address specified in <paramref name="lpBuffer" />.</param>
    /// <param name="nSize">If the FORMAT_MESSAGE_ALLOCATE_BUFFER flag is not set, this parameter specifies the maximum number of TCHARs that can be stored in the output buffer. If FORMAT_MESSAGE_ALLOCATE_BUFFER is set, this parameter specifies the minimum number of TCHARs to allocate for an output buffer.</param>
    /// <param name="arguments">Pointer to an array of values that are used as insert values in the formatted message.</param>
    /// <remarks>
    /// <para>
    /// The function requires a message definition as input. The message definition can come from a 
    /// buffer passed into the function. It can come from a message table resource in an 
    /// already-loaded module. Or the caller can ask the function to search the system's message 
    /// table resource(s) for the message definition. The function finds the message definition 
    /// in a message table resource based on a message identifier and a language identifier. 
    /// The function copies the formatted message text to an output buffer, processing any embedded 
    /// insert sequences if requested.
    /// </para>
    /// <para>
    /// To prevent the usage of unsafe code, this stub does not support inserting values in the formatted message.
    /// </para>
    /// </remarks>
    /// <returns>
    /// <para>
    /// If the function succeeds, the return value is the number of TCHARs stored in the output 
    /// buffer, excluding the terminating null character.
    /// </para>
    /// <para>
    /// If the function fails, the return value is zero. To get extended error information, 
    /// call <see cref="Marshal.GetLastWin32Error()" />.
    /// </para>
    /// </returns>
    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern int FormatMessage(
      int dwFlags,
      ref IntPtr lpSource,
      int dwMessageId,
      int dwLanguageId,
      ref string lpBuffer,
      int nSize,
      IntPtr arguments);

    /// <summary>
    /// Stub for OutputDebugString native method
    /// </summary>
    /// <param name="message">the string to output</param>
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]    
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern void OutputDebugString(string message);


    /// <summary>
    /// Open connection to system logger.
    /// </summary>
    [DllImport("libc")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5392:Use DefaultDllImportSearchPaths attribute for P/Invokes", Justification = "Only Linux")]
    internal static extern void openlog(IntPtr ident, int option, LocalSyslogAppender.SyslogFacility facility);

    /// <summary>
    /// Generate a log message.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The libc syslog method takes a format string and a variable argument list similar
    /// to the classic printf function. As this type of vararg list is not supported
    /// by C# we need to specify the arguments explicitly. Here we have specified the
    /// format string with a single message argument. The caller must set the format 
    /// string to <c>"%s"</c>.
    /// </para>
    /// </remarks>
    [DllImport("libc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5392:Use DefaultDllImportSearchPaths attribute for P/Invokes", Justification = "Only Linux")]
    internal static extern void syslog(int priority, string format, string message);

    /// <summary>
    /// Close descriptor used to write to system logger.
    /// </summary>
    [DllImport("libc")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5392:Use DefaultDllImportSearchPaths attribute for P/Invokes", Justification = "Only Linux")]
    internal static extern void closelog();
  }
}