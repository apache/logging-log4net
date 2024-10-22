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
using System.Runtime.CompilerServices;
using log4net.Core;

using NUnit.Framework;

namespace log4net.Tests.Core;

/// <summary>
/// Used for internal unit testing the <see cref="Level"/> class.
/// </summary>
[TestFixture]
public sealed class LevelTest
{
  /// <summary>
  /// Tests the comparison between two <see cref="Level"/>s
  /// </summary>
  [Test]
  public void LevelCompare()
  {
    Level? nullLevel = null;
    int? nullInt = null;
    Compare(nullInt, nullInt, nullLevel, nullLevel);
    Compare(nullInt, Level.Verbose.Value, nullLevel, Level.Verbose);
    Compare(Level.Verbose.Value, nullInt, Level.Verbose, nullLevel);
    Compare(Level.Verbose.Value, Level.Verbose.Value, Level.Verbose, Level.Verbose);
    Compare(Level.Debug.Value, Level.Verbose.Value, Level.Debug, Level.Verbose);
  }

  private static void Compare(int? leftInt, int? rightInt, Level? left, Level? right,
    [CallerArgumentExpression(nameof(left))] string leftName = "",
    [CallerArgumentExpression(nameof(right))] string rightName = "")
  {
    Assert.That(left < right, Is.EqualTo(leftInt < rightInt), $"{leftName} < {rightName}");
    Assert.That(left > right, Is.EqualTo(leftInt > rightInt), $"{leftName} > {rightName}");
    Assert.That(left <= right, Is.EqualTo(leftInt <= rightInt), $"{leftName} <= {rightName}");
    Assert.That(left >= right, Is.EqualTo(leftInt >= rightInt), $"{leftName} >= {rightName}");
    Assert.That(left == right, Is.EqualTo(leftInt == rightInt), $"{leftName} == {rightName}");
    Assert.That(left != right, Is.EqualTo(leftInt != rightInt), $"{leftName} != {rightName}");
    Assert.That(left?.Equals(right), Is.EqualTo(leftInt?.Equals(rightInt)), $"{leftName}?.Equals({rightName})");
    if (leftInt is not null)
    {
      if (rightInt is not null)
      {
        Assert.That(left?.CompareTo(right!), Is.EqualTo(leftInt?.CompareTo(rightInt)), $"{leftName}?.CompareTo({rightName})");
      }
      else
      {
        Assert.Throws<ArgumentNullException>(() => left!.CompareTo(right!));
      }
    }
  }
}