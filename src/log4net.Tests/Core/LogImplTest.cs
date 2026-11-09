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

using System.Linq;
using System.Reflection;
using log4net.Core;

using NUnit.Framework;

namespace log4net.Tests.Core;

/// <summary>
/// Tests for <see cref="LogImpl"/>.
/// </summary>
[TestFixture]
public sealed class LogImplTest
{
  /// <summary>
  /// An inlined or tail-calling logging method leaves no <see cref="LogImpl"/> frame, so location information is lost.
  /// </summary>
  [Test]
  public void LoggingMethodsKeepTheirFrame()
  {
    string[] levels = ["Debug", "Info", "Warn", "Error", "Fatal"];
    MethodInfo[] methods = [.. typeof(LogImpl)
      .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
      .Where(method => levels.Any(method.Name.StartsWith))];

    Assert.That(methods, Has.Length.EqualTo(35));
    Assert.That(methods.Where(method => !method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining)
      || !method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoOptimization)).Select(method => method.ToString()),
      Is.Empty);
  }
}
