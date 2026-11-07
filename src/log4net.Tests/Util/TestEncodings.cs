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

using System.Text;

namespace log4net.Tests;

/// <summary>Finds an encoding the way the configuration would, for code pages .NET does not ship.</summary>
internal static class TestEncodings
{
  /// <summary>Asks the runtime first. The provider is not registered: that would change the default for the whole run.</summary>
  /// <param name="name">The encoding name.</param>
  /// <returns>The encoding.</returns>
  internal static Encoding Get(string name)
  {
#if NET462_OR_GREATER
    return Encoding.GetEncoding(name);
#else
    try
    {
      return Encoding.GetEncoding(name);
    }
    catch (System.ArgumentException)
    {
      return CodePagesEncodingProvider.Instance.GetEncoding(name)!;
    }
#endif
  }
}
