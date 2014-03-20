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
using System.Runtime.InteropServices;
using System.Text;
using log4net.Core;
using Microsoft.Win32.SafeHandles;

namespace log4net.Appender
{
    /// <summary>
    /// Appends log events to Sysinternals Process Monitor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The application configuration file can be used to control what listeners 
    /// are actually used.
    /// </para>
    /// <para>
    /// Events are written using a write only file handle that procmon listens for. Process Monitor will then display these messages amongst the IO data.
    /// </para>
    /// </remarks>
    /// <author>Justin Dearing</author>
    /// <seealso cref="http://www.wintellect.com/blogs/jrobbins/see-the-i-o-you-caused-by-getting-your-diagnostic-tracing-into-process-monitor"/>
    /// <seealso cref="http://www.wintellect.com/blogs/jrobbins/procmondebugoutput-now-on-github"/>
    /// <seealso cref="https://github.com/Wintellect/ProcMonDebugOutput"/>
    public class ProcMonAppender : AppenderSkeleton
    {
        // Constants to represent C preprocessor macros for PInvokes
        const uint GENERIC_WRITE = 0x40000000;
        const uint OPEN_EXISTING = 3;
        const uint FILE_WRITE_ACCESS = 0x0002;
        const uint FILE_SHARE_WRITE = 0x00000002;
        const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        const uint METHOD_BUFFERED = 0;

        // Procmon Constants 
        const uint FILE_DEVICE_PROCMON_LOG = 0x00009535;
        const string PROCMON_DEBUGGER_HANDLER = "\\\\.\\Global\\ProcmonDebugLogger";

        /// <summary>
        /// The handle to the procmon log device.
        /// </summary>
        private static SafeFileHandle hProcMon;

        /// <summary>
        /// Get the IO Control code for the ProcMon log.
        /// </summary>
        private static uint IOCTL_EXTERNAL_LOG_DEBUGOUT { get { return CTL_CODE(); } }

        /// <seealso href="http://msdn.microsoft.com/en-us/library/windows/hardware/ff543023(v=vs.85).aspx"/>
        private static uint CTL_CODE(
            uint DeviceType = FILE_DEVICE_PROCMON_LOG,
            uint Function = 0x81,
            uint Method = METHOD_BUFFERED,
            uint Access = FILE_WRITE_ACCESS)
        {
            return ((DeviceType << 16) | (Access << 14) | (Function << 2) | Method);
        }

        /// <remarks>This is only used for opening the procmon log handle, hence the default parameters.</remarks>
        /// <seealso href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa363858(v=vs.85).aspx"/>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName = PROCMON_DEBUGGER_HANDLER,
            uint dwDesiredAccess = GENERIC_WRITE,
            uint dwShareMode = FILE_SHARE_WRITE,
            IntPtr lpSecurityAttributes = default(IntPtr),
            uint dwCreationDisposition = OPEN_EXISTING,
            uint dwFlagsAndAttributes = FILE_ATTRIBUTE_NORMAL,
            IntPtr hTemplateFile = default(IntPtr));

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
            SafeFileHandle hDevice, uint dwIoControlCode,
            StringBuilder lpInBuffer, uint nInBufferSize,
            IntPtr lpOutBuffer, uint nOutBufferSize,
            out uint lpBytesReturned, IntPtr lpOverlapped);


        static ProcMonAppender()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                if (!hProcMon.IsInvalid) hProcMon.Close();
            };
        }

        /// <summary>
        /// Does the actual tracing to Process Monitor.
        /// </summary>
        /// <param name="message">
        /// The message to display.
        /// </param>
        /// <param name="args">
        /// The formatting arguments for the message
        /// </param>
        /// <returns>
        /// True if the trace succeeded, false otherwise.
        /// </returns>
        private static bool ProcMonDebugOutput(string message, params object[] args)
        {
            bool returnValue = false;
            try
            {
                StringBuilder renderedMessage = new StringBuilder(); 
                renderedMessage.AppendFormat(message, args);
                uint outLen;
                if (hProcMon == null || hProcMon.IsInvalid)
                {
                    hProcMon = CreateFile();
                }
                DeviceIoControl(
                    hProcMon, IOCTL_EXTERNAL_LOG_DEBUGOUT,
                    renderedMessage, (uint)(message.Length * Marshal.SizeOf(typeof(char))),
                    IntPtr.Zero, 0, out outLen, IntPtr.Zero);
            }
            catch (EntryPointNotFoundException notFoundException)
            {
                // This means the appropriate ProcMonDebugOutput[Win32|x64].DLL
                // file could not be found. I'll eat this exception so it does
                // not take down the application.
                Debug.WriteLine(notFoundException.Message);
            }

            return returnValue;
        }

        /// <summary>
        /// This appender requires a <see cref="AppenderSkeleton.Layout"/> to be set.
        /// </summary>
        /// <value><c>true</c></value>
        override protected bool RequiresLayout
        {
            get { return true; }
        }

        /// <summary>
        /// Writes the logging event to Sysinternals Process Monitor.
        /// </summary>
        /// <param name="loggingEvent">The event to log.</param>
        /// <remarks>
        /// <para>
        /// Writes the logging event to Sysinternals Process Monitor.
        /// </para>
        /// </remarks>
        protected override void Append(LoggingEvent loggingEvent)
        {
            throw new System.NotImplementedException();
        }
    }
}