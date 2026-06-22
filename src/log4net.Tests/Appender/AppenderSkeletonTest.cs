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

using log4net.Filter;
using log4net.Appender;
using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Unit tests for <see cref="AppenderSkeleton"/> filter chain management.
/// </summary>
[TestFixture]
public sealed class AppenderSkeletonTest
{
  private CountingAppender _appender = null!;

  [SetUp]
  public void SetUp() => _appender = new CountingAppender();

  [TearDown]
  public void TearDown() => _appender.Close();

  /// <summary>
  /// Verifies that <see cref="AppenderSkeleton.AddFilter"/> sets <see cref="AppenderSkeleton.FilterHead"/> when the chain is empty.
  /// </summary>
  [Test]
  public void AddFilter_FirstFilter_SetsFilterHead()
  {
    DenyAllFilter filter = new();
    _appender.AddFilter(filter);
    Assert.That(_appender.FilterHead, Is.SameAs(filter));
  }

  /// <summary>
  /// Verifies that a second <see cref="AppenderSkeleton.AddFilter"/> call appends the filter via <see cref="Filter.IFilter.Next"/>.
  /// </summary>
  [Test]
  public void AddFilter_SecondFilter_LinksToChain()
  {
    DenyAllFilter first = new();
    DenyAllFilter second = new();
    _appender.AddFilter(first);
    _appender.AddFilter(second);
    Assert.That(_appender.FilterHead, Is.SameAs(first));
    Assert.That(_appender.FilterHead!.Next, Is.SameAs(second));
  }

  /// <summary>
  /// Verifies that <see cref="AppenderSkeleton.ClearFilters"/> resets <see cref="AppenderSkeleton.FilterHead"/> to null.
  /// </summary>
  [Test]
  public void ClearFilters_ResetsFilterHead()
  {
    _appender.AddFilter(new DenyAllFilter());
    _appender.ClearFilters();
    Assert.That(_appender.FilterHead, Is.Null);
  }

  /// <summary>
  /// Verifies that filters can be added again after <see cref="AppenderSkeleton.ClearFilters"/>.
  /// </summary>
  [Test]
  public void ClearFilters_AllowsAddingFiltersAgain()
  {
    _appender.AddFilter(new DenyAllFilter());
    _appender.ClearFilters();
    DenyAllFilter filter = new();
    _appender.AddFilter(filter);
    Assert.That(_appender.FilterHead, Is.SameAs(filter));
  }
}
