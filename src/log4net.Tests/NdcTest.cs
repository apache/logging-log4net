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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Layout.Pattern;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests;

/// <summary>
/// Used for internal unit testing the <see cref="NDC"/> class.
/// </summary>
[TestFixture]
public class NdcTest
{
  /// <summary>
  /// Test regression (https://github.com/apache/logging-log4net/issues/245)
  /// </summary>
  [Test]
  public void InheritTest()
  {
    NDC.Push("first");
    NDC.Push("last");
    System.Collections.Stack context = NDC.CloneStack();
    NDC.Clear();
    NDC.Inherit(context);
    Assert.That(NDC.Pop(), Is.EqualTo("last"));
    Assert.That(NDC.Pop(), Is.EqualTo("first"));
  }
}