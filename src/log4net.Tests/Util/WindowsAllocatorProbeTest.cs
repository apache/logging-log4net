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
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Util;

/// <summary>
/// Temporary probe for audit finding da18b6fd-f045, to be removed before this branch is merged.
/// <c>FormatMessage</c> allocates the message with <c>LocalAlloc</c> while the <c>ref string</c>
/// marshalling frees it as COM task memory. This asks a Windows build agent whether the two
/// allocators share a heap, and whether the production path survives being driven hard.
/// </summary>
[TestFixture]
[Platform("Win")]
public sealed class WindowsAllocatorProbeTest
{
  private static class NativeMethods
  {
    /// <summary>The allocator <c>FormatMessage</c> uses for the buffer it hands back.</summary>
    [DllImport("Kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr LocalAlloc(uint flags, UIntPtr bytes);

    /// <summary>Releases what <see cref="LocalAlloc"/> returned.</summary>
    [DllImport("Kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr LocalFree(IntPtr memory);

    /// <summary>The heap COM task memory is released against.</summary>
    [DllImport("Kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr GetProcessHeap();

    /// <summary>Reports whether a block belongs to a heap, without freeing it.</summary>
    [DllImport("Kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool HeapValidate(IntPtr heap, uint flags, IntPtr memory);
  }

  /// <summary>
  /// If the block is in the process heap then the wrong free is benign on this Windows, and the
  /// finding is theoretical; if it is not, the marshaller frees against a foreign allocator.
  /// </summary>
  [Test]
  public void TheLocalAllocHeapIsTheHeapComTaskMemoryIsFreedAgainst()
  {
    IntPtr block = NativeMethods.LocalAlloc(0x0000, new UIntPtr(64));
    Assert.That(block, Is.Not.EqualTo(IntPtr.Zero), "LocalAlloc failed, the probe is broken");

    try
    {
      bool inProcessHeap = NativeMethods.HeapValidate(NativeMethods.GetProcessHeap(), 0, block);

      Assert.That(inProcessHeap, Is.True,
        $"audit da18b6fd-f045 confirmed: {RuntimeInformation.OSDescription}, "
        + $"{RuntimeInformation.ProcessArchitecture}, LocalAlloc block is outside the process heap");
    }
    finally
    {
      NativeMethods.LocalFree(block);
    }
  }

  /// <summary>
  /// The production path, driven hard enough that a heap the wrong free had corrupted would show.
  /// </summary>
  [Test]
  public void TheErrorMessagePathSurvivesRepeatedUse()
  {
    string? first = NativeError.GetErrorMessage(2);

    for (int i = 0; i < 500; i++)
    {
      Assert.That(NativeError.GetErrorMessage(2), Is.EqualTo(first));
    }

    Assert.That(first, Is.Not.Null.And.Not.Empty,
      $"no message for error 2 on {RuntimeInformation.OSDescription}");
  }
}
