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

using log4net.Core;

using NUnit.Framework;

namespace log4net.Tests.Core;

/// <summary>
/// Used for internal unit testing the <see cref="LevelMap"/> class.
/// </summary>
[TestFixture]
public sealed class LevelMapTest
{
  /// <summary>
  /// Tests the creation of a <see cref="LevelMap"/> and calling its <see cref="LevelMap.Clear"/> method
  /// </summary>
  [Test]
  public void LevelMapCreateClear()
  {
    LevelMap map = new();
    LevelCollection allLevels = map.AllLevels;
    Assert.That(allLevels, Is.Empty);
    Assert.That(map["nonexistent"], Is.Null);

    map.Add("level1234", 1234, "displayName");
    allLevels = map.AllLevels;
    Assert.That(allLevels, Has.Count.EqualTo(1));
    Assert.That(allLevels[0].Name, Is.EqualTo("level1234"));
    Assert.That(allLevels[0].DisplayName, Is.EqualTo("displayName"));
    Assert.That(allLevels[0].Value, Is.EqualTo(1234));
    Level? level1234 = map["level1234"];
    Assert.That(level1234, Is.Not.Null);
    Assert.That(allLevels[0], Is.SameAs(level1234));

    Level lookupLevel = map.LookupWithDefault(level1234!);
    Assert.That(lookupLevel, Is.SameAs(level1234));

    Level otherLevel = new(5678, "level5678", "display");
    lookupLevel = map.LookupWithDefault(otherLevel);
    Assert.That(lookupLevel, Is.SameAs(otherLevel));
    Assert.That(map["LEVEL5678"], Is.SameAs(otherLevel));

    map.Clear();
    allLevels = map.AllLevels;
    Assert.That(allLevels, Is.Empty);
    Assert.That(map["level1234"], Is.Null);
    Assert.That(map["LEVEL5678"], Is.Null);
  }
}