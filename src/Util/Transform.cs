#region Copyright & License
//
// Copyright 2001-2005 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
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
using System.Text;
using System.Xml;

namespace log4net.Util
{
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
	public sealed class Transform
	{
		#region Private Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Transform" /> class. 
		/// </summary>
		/// <remarks>
		/// <para>
		/// Uses a private access modifier to prevent instantiation of this class.
		/// </para>
		/// </remarks>
		private Transform()
		{
		}

		#endregion Private Instance Constructors

		#region Public Static Methods

		/// <summary>
		/// Write a string to an <see cref="XmlWriter"/>
		/// </summary>
		/// <param name="writer">the writer to write to</param>
		/// <param name="stringData">the string to write</param>
		/// <remarks>
		/// <para>
		/// The test is escaped either using XML escape entities
		/// or using CDATA sections.
		/// </para>
		/// </remarks>
		public static void WriteEscapedXmlString(XmlWriter writer, string stringData)
		{
			// Write either escaped text or CDATA sections

			int weightCData = 12 * (1 + CountSubstrings(stringData, CDATA_END));
			int weightStringEscapes = 3*(CountSubstrings(stringData, "<") + CountSubstrings(stringData, ">")) + 4*CountSubstrings(stringData, "&");

			if (weightStringEscapes <= weightCData)
			{
				// Write string using string escapes
				writer.WriteString(stringData);
			}
			else
			{
				// Write string using CDATA section

				int end = stringData.IndexOf(CDATA_END);
	
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
							writer.WriteString(CDATA_END);
							break;
						}
						else
						{
							writer.WriteString(CDATA_UNESCAPABLE_TOKEN);
							start = end + 2;
							end = stringData.IndexOf(CDATA_END, start);
						}
					}
	
					if (start < stringData.Length)
					{
						writer.WriteCData(stringData.Substring(start));
					}
				}
			}
		}

		#endregion Public Static Methods

		#region Private Helper Methods

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

			while(offset < length)
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

		#endregion

		#region Private Static Fields

		private const string CDATA_END	= "]]>";
		private const string CDATA_UNESCAPABLE_TOKEN	= "]]";

		#endregion Private Static Fields
	}
}
