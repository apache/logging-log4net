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

using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace log4net.Tests.Layout;

/// <summary>
/// Reads back the records of the XML layouts the way a consumer does.
/// </summary>
/// <remarks>
/// Uses <see cref="XDocument.Parse(string)"/>, which normalizes line ends and attribute whitespace;
/// a default <c>XmlDocument</c> or <c>XmlTextReader</c> keeps them and would hide the defects.
/// </remarks>
internal static class XmlRecords
{
  /// <summary>
  /// Parses the records and returns the text of the one element named <paramref name="localName"/>.
  /// </summary>
  /// <remarks>net462 writes the prefixes undeclared, so a wrapper declares them.</remarks>
  internal static string ElementText(string records, string localName)
    => XDocument.Parse($"<events xmlns:log4net=\"log4net\" xmlns:log4j=\"log4j\">{records}</events>")
      .Descendants().Single(element => element.Name.LocalName == localName).Value;

  /// <summary>
  /// Returns the content of the one element named <paramref name="localName"/> as written.
  /// </summary>
  internal static string RawContent(string records, string localName)
    => Regex.Matches(records, $"<(?:\\w+:)?{localName}>(.*?)</(?:\\w+:)?{localName}>", RegexOptions.Singleline)
      .Cast<Match>().Single().Groups[1].Value;
}
