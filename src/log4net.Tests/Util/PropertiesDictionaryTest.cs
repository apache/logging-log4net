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

#if NET462_OR_GREATER

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Util;

/// <summary>
/// Used for internal unit testing the <see cref="PropertiesDictionary"/> class.
/// </summary>
/// <remarks>
/// Used for internal unit testing the <see cref="PropertiesDictionary"/> class.
/// </remarks>
[TestFixture]
public class PropertiesDictionaryTest
{
  [Test]
  public void TestSerialization()
  {
    PropertiesDictionary pd = new();

    for (int i = 0; i < 10; i++)
    {
      pd[i.ToString()] = i;
    }

    Assert.That(pd, Has.Count.EqualTo(10), "Dictionary should have 10 items");

    Assert.That(pd["notThere"], Is.Null, "Getter should act as IDictionary not IDictionary<TKey, TValue>");

    // Serialize the properties into a memory stream
    BinaryFormatter formatter = new();
    MemoryStream memory = new();
    formatter.Serialize(memory, pd);

    // Deserialize the stream into a new properties dictionary
    memory.Position = 0;
    PropertiesDictionary pd2 = (PropertiesDictionary)formatter.Deserialize(memory);

    Assert.That(pd2, Has.Count.EqualTo(10), "Deserialized Dictionary should have 10 items");

    foreach (string key in pd.GetKeys())
    {
      Assert.That(pd2[key], Is.EqualTo(pd[key]), $"Check Value Persisted for key [{key}]");
    }
  }
}

#endif
