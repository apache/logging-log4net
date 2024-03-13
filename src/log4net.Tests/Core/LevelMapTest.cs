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
using log4net.Tests.Layout;

using NUnit.Framework;

#nullable enable

namespace log4net.Tests.Core
{
  /// <summary>
  /// Used for internal unit testing the <see cref="PatternLayoutTest"/> class.
  /// </summary>
  /// <remarks>
  /// Used for internal unit testing the <see cref="PatternLayoutTest"/> class.
  /// </remarks>
  [TestFixture]
  public class LevelMapTest
  {
    [Test]
    public void LevelMapCreateClear()
    {
      var map = new LevelMap();
      LevelCollection allLevels = map.AllLevels;
      Assert.AreEqual(0, allLevels.Count);
      Assert.IsNull(map["nonexistent"]);

      map.Add("level1234", 1234, "displayName");
      allLevels = map.AllLevels;
      Assert.AreEqual(1, allLevels.Count);
      Assert.AreEqual("level1234", allLevels[0].Name);
      Assert.AreEqual("displayName", allLevels[0].DisplayName);
      Assert.AreEqual(1234, allLevels[0].Value);
      Level? level1234 = map["level1234"];
      Assert.IsNotNull(level1234);
      Assert.AreSame(level1234, allLevels[0]);

      Level lookupLevel = map.LookupWithDefault(level1234!);
      Assert.AreSame(level1234, lookupLevel);

      var otherLevel = new Level(5678, "level5678", "display");
      lookupLevel = map.LookupWithDefault(otherLevel);
      Assert.AreSame(otherLevel, lookupLevel);
      Assert.AreSame(otherLevel, map["LEVEL5678"]);

      map.Clear();
      allLevels = map.AllLevels;
      Assert.AreEqual(0, allLevels.Count);
      Assert.IsNull(map["level1234"]);
      Assert.IsNull(map["LEVEL5678"]);
    }
  }
}