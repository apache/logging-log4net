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
using log4net.Util.PatternStringConverters;
using NUnit.Framework;

namespace log4net.Tests.Util;

/// <summary>
/// Used for internal unit testing the <see cref="RandomStringPatternConverter"/> class.
/// </summary>
[TestFixture]
public class RandomStringPatternConverterTest
{
  [Test]
  public void TestConvert()
  {
    RandomStringPatternConverter converter = new();

    // Check default string length
    using StringWriter sw = new();
    converter.Convert(sw, null);

    Assert.That(sw.ToString(), Has.Length.EqualTo(4), "Default string length should be 4");

    // Set string length to 7
    converter.Option = "7";
    converter.ActivateOptions();

    using StringWriter sw2 = new();
    converter.Convert(sw2, null);

    string string1 = sw2.ToString();
    Assert.That(string1, Has.Length.EqualTo(7), "string length should be 7");

    // Check for duplicate result
    using StringWriter sw3 = new();
    converter.Convert(sw3, null);

    string string2 = sw3.ToString();
    Assert.That(string1, Is.Not.EqualTo(string2), "strings should be different");
  }
}