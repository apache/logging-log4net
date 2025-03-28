/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using log4net.ObjectRenderer;
using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.ObjectRenderer;

[TestFixture]
public class DefaultRendererTest
{
  readonly RendererMap _rendererMap = new();
  readonly DefaultRenderer _renderer = new();

  [Test]
  public void DefaultRendererForDifferentObjectTypes()
  {
    VerifyRenderObject(null, SystemInfo.NullText);

    VerifyRenderObject(new object[] { 1, 2, 3 }, "Object[] {1, 2, 3}");
    VerifyRenderObject(
      new object[3][] { [1, 2, 3], [4, 5, 6], [7, 8, 9] },
      "Object[][] {Object[] {1, 2, 3}, Object[] {4, 5, 6}, Object[] {7, 8, 9}}");

    int[] intArray = [1, 2, 3];
    VerifyRenderObject(intArray, "Int32[] {1, 2, 3}");
    VerifyRenderObject(intArray.GetEnumerator(), "{1, 2, 3}");

    VerifyRenderObject(intArray.Where(i => i > 1), "{2, 3}");
    VerifyRenderObject(intArray.ToList(), "{1, 2, 3}");
    VerifyRenderObject(new ArrayList(intArray), "{1, 2, 3}");
    VerifyRenderObject(new List<int>(), "{}");

    Hashtable ht = new()
    {
      ["a"] = 1,
      ["b"] = 2,
      ["c"] = 3,
    };
    VerifyRenderObject(ht, s =>
    {
      // table entries may be rendered in arbitrary order, e.g. "{b=2, c=3, a=1}".
      Assert.That(s[0], Is.EqualTo('{'));
      Assert.That(s.EndsWith("}"));
      Assert.That(s, Does.Contain("a=1"));
      Assert.That(s, Does.Contain("b=2"));
      Assert.That(s, Does.Contain("c=3"));
    });

    Dictionary<string, int> dict = new()
    {
      ["a"] = 1,
      ["b"] = 2,
      ["c"] = 3,
    };
    VerifyRenderObject(dict, "{a=1, b=2, c=3}");

    VerifyRenderObject(new DictionaryEntry("a", 1), "a=1");
  }

  private void VerifyRenderObject(object? toRender, string expected)
  {
    VerifyRenderObject(toRender, s => Assert.That(s, Is.EqualTo(expected)));
  }

  private void VerifyRenderObject(object? toRender, Action<string> validate)
  {
    StringBuilder sb = new();
    using (StringWriter textWriter = new(sb))
    {
      _renderer.RenderObject(_rendererMap, toRender, textWriter);
    }

    validate(sb.ToString());
  }
}