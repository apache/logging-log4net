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
using System.Linq;

namespace log4net.Tests.Util.Internal;

/// <summary>
/// Stands in for the send and report callbacks of a <see cref="log4net.Util.BackgroundSender{T}"/> and records what each receives.
/// </summary>
internal sealed class RecordingSink
{
  private readonly List<int> _sentItems = [];
  private readonly List<string> _reports = [];

  internal int[] SentItems
  {
    get
    {
      lock (_sentItems)
      {
        return [.. _sentItems];
      }
    }
  }

  internal List<string> Reports
  {
    get
    {
      lock (_reports)
      {
        return [.. _reports];
      }
    }
  }

  internal List<string> CloseSummaries
    => [.. Reports.Where(message => message.EndsWith("were not sent.", StringComparison.Ordinal))];

  internal void Send(int item)
  {
    lock (_sentItems)
    {
      _sentItems.Add(item);
    }
  }

  internal void Report(string message, Exception? exception)
  {
    lock (_reports)
    {
      _reports.Add(message);
    }
  }
}
