﻿#region Apache License
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

using log4net.Repository;
using NUnit.Framework;

namespace log4net.Tests;

[TestFixture]
public class Signing
{
  [Test]
  public void AssemblyShouldBeSigned()
  {
    System.Reflection.Assembly assembly = typeof(LoggerRepositorySkeleton).Assembly;
    byte[]? result = assembly.GetName().GetPublicKey();
    Assert.That(result, Is.Not.Null);
    Assert.That(result, Is.Not.Empty);
  }
}