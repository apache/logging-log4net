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
using System.Text.RegularExpressions;
using System.Xml;

namespace log4net.Util;

/// <summary>
/// Utility class for transforming strings.
/// </summary>
/// <remarks>
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
  public static void WriteEscapedXmlString(this XmlWriter writer, string textData, string invalidCharReplacement)
  {
    writer.EnsureNotNull();
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
  /// <para>
  /// Supplementary characters (U+10000–U+10FFFF) encoded as valid UTF-16 surrogate
  /// pairs are preserved; only lone surrogates and other illegal code units are masked.
  /// </para>
  /// </remarks>
  public static string MaskXmlInvalidCharacters(string textData, string mask)
    => _invalidChars.Replace(textData, m =>
        // A valid surrogate pair represents a legal supplementary character — preserve it.
        m.Value.Length == 2 ? m.Value : mask);

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
    if (text.Length == 0 || substring.Length == 0)
    {
      return 0;
    }

    // Use the char overload for single-character substrings — avoids string
    // comparison overhead; all current callers pass single ASCII characters.
    if (substring.Length == 1)
    {
      int charCount = 0;
      char target = substring[0];
      foreach (char c in text)
      {
        if (c == target)
        {
          charCount++;
        }
      }
      return charCount;
    }

    int count = 0;
    int offset = 0;
    int substringLength = substring.Length;
    while (offset < text.Length)
    {
      // Ordinal avoids culture-sensitive comparison for these ASCII-only tokens.
      int index = text.IndexOf(substring, offset, StringComparison.Ordinal);
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
  /// Matches either a valid UTF-16 surrogate pair (preserved) or a single character
  /// that is illegal in XML 1.0 (replaced). The surrogate-pair alternative must come
  /// first so the engine consumes both code units together before the single-char
  /// alternative can match them individually.
  /// </summary>
  private static readonly Regex _invalidChars = new(
    @"[\uD800-\uDBFF][\uDC00-\uDFFF]|[^\x09\x0A\x0D\x20-\uD7FF\uE000-\uFFFD]",
    RegexOptions.Compiled);
}