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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;

using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Util;

/// <summary>
/// Tests for <see cref="CompositeProperties"/>.
/// </summary>
[TestFixture]
public sealed class CompositePropertiesTest
{
  private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

  /// <summary>
  /// <see cref="CompositeProperties.Flatten"/> used to publish its dictionary before filling it, so a
  /// second caller during the build got a half-filled one.
  /// </summary>
  [Test]
  public void ASecondFlattenDuringTheBuildGetsTheSameCompleteDictionary()
  {
    using ManualResetEventSlim halfway = new();
    using ManualResetEventSlim release = new();
    CompositeProperties composite = typeof(CompositeProperties).Construct<CompositeProperties>([]);
    composite.Add(new GatedProperties(halfway, release) { ["a"] = 1, ["b"] = 2, ["c"] = 3 });
    PropertiesDictionary? first = null;
    ExceptionDispatchInfo? failure = null;
    Thread builder = new(() =>
    {
      try
      {
        first = composite.Flatten();
      }
      catch (Exception e) when (!e.IsFatal())
      {
        failure = ExceptionDispatchInfo.Capture(e);
      }
    })
    { IsBackground = true };
    builder.Start();
    Assert.That(halfway.Wait(_timeout), Is.True, "the first build did not start");

    PropertiesDictionary second = composite.Flatten();
    int secondCount = second.Count;
    release.Set();
    Assert.That(builder.Join(_timeout), Is.True, "the first build did not finish");
    failure?.Throw();

    Assert.That(secondCount, Is.EqualTo(3));
    Assert.That(first, Is.SameAs(second));
  }

  /// <summary>
  /// Its first enumeration yields one entry, signals <paramref name="halfway"/> and waits for <paramref name="release"/>.
  /// </summary>
  private sealed class GatedProperties(ManualResetEventSlim halfway, ManualResetEventSlim release)
    : ReadOnlyPropertiesDictionary, IEnumerable<KeyValuePair<string, object?>>
  {
    private int _enumerations;

    /// <inheritdoc/>
    public override object? this[string key]
    {
      set => InnerHashtable[key] = value;
    }

    /// <inheritdoc/>
    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
      KeyValuePair<string, object?>[] entries = [.. InnerHashtable];
      bool gated = Interlocked.Increment(ref _enumerations) == 1;
      for (int i = 0; i < entries.Length; i++)
      {
        yield return entries[i];
        if (gated && i == 0)
        {
          halfway.Set();
          release.Wait(_timeout);
        }
      }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<string, object?>>)this).GetEnumerator();
  }
}
