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

using System.Xml;
using System.Text.RegularExpressions;

namespace log4net.Util;

/// <summary>
/// Utility class for transforming strings.
/// </summary>
/// <remarks>
/// <para>
/// Utility class for transforming strings.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
public static class Transform
{
  /// <summary>
  /// Write a string to an <see cref="XmlWriter"/>
  /// </summary>
  /// <param name="writer">the writer to write to</param>
  /// <param name="textData">the string to write</param>
  /// <param name="invalidCharReplacement">The string to replace non XML compliant chars with</param>
  /// <remarks>
  /// <para>
  /// The test is escaped either using XML escape entities
  /// or using CDATA sections.
  /// </para>
  /// </remarks>
  public static void WriteEscapedXmlString(XmlWriter writer, string textData, string invalidCharReplacement)
  {
    string stringData = MaskXmlInvalidCharacters(textData, invalidCharReplacement);
    // Write either escaped text or CDATA sections

    int weightCData = 12 * (1 + CountSubstrings(stringData, CdataEnd));
    int weightStringEscapes = 3 * (CountSubstrings(stringData, "<") + CountSubstrings(stringData, ">")) + 4 * CountSubstrings(stringData, "&");

    if (weightStringEscapes <= weightCData)
    {
      // Write string using string escapes
      writer.WriteString(stringData);
    }
    else
    {
      // Write string using CDATA section

      int end = stringData.IndexOf(CdataEnd);

      if (end < 0)
      {
        writer.WriteCData(stringData);
      }
      else
      {
        int start = 0;
        while (end > -1)
        {
          writer.WriteCData(stringData.Substring(start, end - start));
          if (end == stringData.Length - 3)
          {
            start = stringData.Length;
            writer.WriteString(CdataEnd);
            break;
          }
          else
          {
            writer.WriteString(CdataUnescapableToken);
            start = end + 2;
            end = stringData.IndexOf(CdataEnd, start);
          }
        }

        if (start < stringData.Length)
        {
          writer.WriteCData(stringData.Substring(start));
        }
      }
    }
  }

  /// <summary>
  /// Replace invalid XML characters in text string
  /// </summary>
  /// <param name="textData">the XML text input string</param>
  /// <param name="mask">the string to use in place of invalid characters</param>
  /// <returns>A string that does not contain invalid XML characters.</returns>
  /// <remarks>
  /// <para>
  /// Certain Unicode code points are not allowed in the XML InfoSet, for
  /// details see: <a href="http://www.w3.org/TR/REC-xml/#charsets">http://www.w3.org/TR/REC-xml/#charsets</a>.
  /// </para>
  /// <para>
  /// This method replaces any illegal characters in the input string
  /// with the mask string specified.
  /// </para>
  /// </remarks>
  public static string MaskXmlInvalidCharacters(string textData, string mask)
  {
    return _invalidchars.Replace(textData, mask);
  }

  /// <summary>
  /// Count the number of times that the substring occurs in the text
  /// </summary>
  /// <param name="text">the text to search</param>
  /// <param name="substring">the substring to find</param>
  /// <returns>the number of times the substring occurs in the text</returns>
  /// <remarks>
  /// <para>
  /// The substring is assumed to be non repeating within itself.
  /// </para>
  /// </remarks>
  private static int CountSubstrings(string text, string substring)
  {
    int count = 0;
    int offset = 0;
    int length = text.Length;
    int substringLength = substring.Length;

    if (length == 0)
    {
      return 0;
    }
    if (substringLength == 0)
    {
      return 0;
    }

    while (offset < length)
    {
      int index = text.IndexOf(substring, offset);

      if (index == -1)
      {
        break;
      }

      count++;
      offset = index + substringLength;
    }
    return count;
  }

  private const string CdataEnd = "]]>";
  private const string CdataUnescapableToken = "]]";

  /// <summary>
  /// Characters illegal in XML 1.0
  /// </summary>
  private static readonly Regex _invalidchars = new(@"[^\x09\x0A\x0D\x20-\uD7FF\uE000-\uFFFD]", RegexOptions.Compiled);
}
