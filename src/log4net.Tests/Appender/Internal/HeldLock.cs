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
using System.Threading;
using System.Threading.Tasks;

using log4net.Appender;

namespace log4net.Tests.Appender.Internal;

/// <summary>
/// Holds the inter process lock of a file on another thread, as another process would, until disposed;
/// with the suffix <c>_rolling</c> the lock of its roll.
/// </summary>
internal sealed class HeldLock : IDisposable
{
  private readonly ManualResetEventSlim _held = new();
  private readonly ManualResetEventSlim _release = new();
  private readonly Task _holder;

  internal HeldLock(string file, string suffix = "")
  {
    // A mutex has to be taken and released on one thread, so the holder does both.
    _holder = Task.Run(() =>
    {
      using Mutex mutex = new(false, typeof(FileAppender).Invoke<string>("MutexName", [file, suffix]));
      mutex.WaitOne();
      _held.Set();
      _release.Wait();
      mutex.ReleaseMutex();
    });
    if (!_held.Wait(TimeSpan.FromSeconds(10)))
    {
      // Lets the holder end in case it takes the mutex after all.
      _release.Set();
      throw new TimeoutException("the holder never took the mutex");
    }
  }

  /// <inheritdoc/>
  public void Dispose()
  {
    _release.Set();
    _holder.Wait(TimeSpan.FromSeconds(10));
    _held.Dispose();
    _release.Dispose();
  }
}
