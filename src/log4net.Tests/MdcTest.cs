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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace log4net.Tests;

/// <summary>
/// Tests for the <see cref="MDC"/> class.
/// </summary>
[TestFixture]
public class MdcTest
{
  /// <summary>
  /// <see cref="MDC"/> is hidden from IntelliSense to steer new code to <c>ThreadContext.Properties</c>,
  /// without the warnings an <see cref="ObsoleteAttribute"/> would give existing callers.
  /// </summary>
  [Test]
  public void MdcIsHiddenFromTheEditorButNotObsolete()
  {
    MemberInfo[] members = [typeof(MDC), .. typeof(MDC).DeclaredPublicMembers()];

    Assert.That(members.Where(member => member.GetCustomAttribute<EditorBrowsableAttribute>()?.State
      != EditorBrowsableState.Never).Select(member => member.Name), Is.Empty);
    Assert.That(members.Where(member => member.IsDefined(typeof(ObsoleteAttribute))).Select(member => member.Name), Is.Empty);
  }
}
