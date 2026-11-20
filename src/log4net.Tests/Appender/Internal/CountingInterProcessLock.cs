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

using System.IO;
using System.Text;

using log4net.Appender;

namespace log4net.Tests.Appender.Internal;

/// <summary>Counts the calls to <see cref="FileAppender.InterProcessLock.AcquireLock"/>, and the files opened and closed.</summary>
internal sealed class CountingInterProcessLock : FileAppender.InterProcessLock
{
  internal int Acquired { get; set; }

  internal int Opened { get; private set; }

  internal int Closed { get; private set; }

  /// <inheritdoc/>
  public override Stream? AcquireLock()
  {
    Acquired++;
    return base.AcquireLock();
  }

  /// <inheritdoc/>
  public override void OpenFile(string filename, bool append, Encoding encoding)
  {
    Opened++;
    base.OpenFile(filename, append, encoding);
  }

  /// <inheritdoc/>
  public override void CloseFile()
  {
    Closed++;
    base.CloseFile();
  }
}
