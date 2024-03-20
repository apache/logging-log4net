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

namespace log4net.Tests.ObjectRenderer
{
  [TestFixture]
  public class DefaultRendererTest
  {
    readonly RendererMap m_rendererMap = new();
    readonly DefaultRenderer m_renderer = new();

    [Test]
    public void DefaultRendererForDifferentObjectTypes()
    {
      VerifyRenderObject(null, SystemInfo.NullText);

      VerifyRenderObject(new object[] { 1, 2, 3 }, "Object[] {1, 2, 3}");
      VerifyRenderObject(
        new object[3][] { new object[] { 1, 2, 3 }, new object[] { 4, 5, 6 }, new object[] { 7, 8, 9 } },
        "Object[][] {Object[] {1, 2, 3}, Object[] {4, 5, 6}, Object[] {7, 8, 9}}");

      var intArray = new[] { 1, 2, 3 };
      VerifyRenderObject(intArray, "Int32[] {1, 2, 3}");
      VerifyRenderObject(intArray.GetEnumerator(), "{1, 2, 3}");

      VerifyRenderObject(intArray.Where(i => i > 1), "{2, 3}");
      VerifyRenderObject(intArray.ToList(), "{1, 2, 3}");
      VerifyRenderObject(new ArrayList(intArray), "{1, 2, 3}");
      VerifyRenderObject(new List<int>(), "{}");

      var ht = new Hashtable()
      {
        ["a"] = 1,
        ["b"] = 2,
        ["c"] = 3,
      };
      VerifyRenderObject(ht, s =>
      {
        // table entries may be rendered in arbitrary order, e.g. "{b=2, c=3, a=1}".
        Assert.AreEqual('{', s[0]);
        Assert.IsTrue(s.EndsWith("}"));
        Assert.IsTrue(s.Contains("a=1"));
        Assert.IsTrue(s.Contains("b=2"));
        Assert.IsTrue(s.Contains("c=3"));
      });

      var dict = new Dictionary<string, int>()
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
      VerifyRenderObject(toRender, s => { Assert.AreEqual(expected, s); });
    }

    private void VerifyRenderObject(object? toRender, Action<string> validate)
    {
      var sb = new StringBuilder();
      using (var textWriter = new StringWriter(sb))
      {
        m_renderer.RenderObject(m_rendererMap, toRender, textWriter);
      }

      validate(sb.ToString());
    }
  }
}
