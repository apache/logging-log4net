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
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;

namespace log4net.Tests.Util;

/// <summary>
/// Temporary probe for audit finding da18b6fd-f046, to be removed before this branch is merged.
/// It asks a build agent whether a fixed-signature P/Invoke to a variadic libc function reads its
/// argument from the place the runtime wrote it, which is what <c>NativeMethods.syslog</c> assumes.
/// Linux is the control, its two calling conventions coincide. Windows is out, it has no `libc`,
/// and there is no varargs arm, CoreCLR answers a `__arglist` P/Invoke with "Vararg calling
/// convention not supported".
/// </summary>
[TestFixture]
[Platform("MacOsX,Linux")]
public sealed class VariadicAbiProbeTest
{
  private static class NativeMethods
  {
    /// <summary>Declared the way <c>NativeMethods.syslog</c> is: the variadic argument is fixed.</summary>
    [DllImport("libc", EntryPoint = "snprintf", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5392:Use DefaultDllImportSearchPaths attribute for P/Invokes", Justification = "Temporary macOS probe")]
    internal static extern int SnprintfWithAFixedArgument(byte[] buffer, IntPtr size, string format, int value);
  }

  /// <summary>
  /// An integer, not a string, so a wrong argument slot prints a wrong number rather than
  /// dereferencing a pointer the runtime never wrote.
  /// </summary>
  [Test]
  public void AFixedSignatureCallPassesTheArgumentWhereTheCalleeReadsIt()
  {
    byte[] buffer = new byte[64];
    int written = NativeMethods.SnprintfWithAFixedArgument(buffer, new IntPtr(buffer.Length), "%d", 12345);

    string context = $"{RuntimeInformation.OSDescription}, {RuntimeInformation.ProcessArchitecture}, "
      + $"wrote {written} bytes [{Read(buffer)}]";

    Assert.That(written, Is.GreaterThan(0), $"the probe itself is broken: {context}");
    Assert.That(Read(buffer), Is.EqualTo("12345"), $"audit da18b6fd-f046 confirmed: {context}");
  }

  /// <summary>Reads the NUL terminated text snprintf wrote.</summary>
  private static string Read(byte[] buffer)
  {
    int length = Array.IndexOf(buffer, (byte)0);
    return Encoding.ASCII.GetString(buffer, 0, length < 0 ? buffer.Length : length);
  }
}
