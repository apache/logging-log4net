#region Copyright
//
// This framework is based on log4j see http://jakarta.apache.org/log4j
// Copyright (C) The Apache Software Foundation. All rights reserved.
//
// This software is published under the terms of the Apache Software
// License version 1.1, a copy of which has been included with this
// distribution in the LICENSE.txt file.
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
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public sealed class Transform
	{
		#region Private Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Transform" /> class. 
		/// </summary>
		/// <remarks>
		/// Uses a private access modifier to prevent instantiation of this class.
		/// </remarks>
		private Transform()
		{
		}

		#endregion Private Instance Constructors

		#region Public Static Methods

		/// <summary>
		/// Write a string to an XmlWriter
		/// </summary>
		/// <param name="writer">the writer to write to</param>
		/// <param name="text">the string to write</param>
		/// <remarks>
		/// The test is escaped either using XML escape entities
		/// or using CDATA sections.
		/// </remarks>
		public static void WriteEscapedXmlString(XmlWriter writer, string text)
		{
			// Write either escaped text or CDATA sections

			int weightCData = 12 * (1 + CountSubstrings(text, CDATA_END));
			int weightStringEscapes = 3*(CountSubstrings(text, "<") + CountSubstrings(text, ">")) + 4*CountSubstrings(text, "&");

			if (weightStringEscapes <= weightCData)
			{
				// Write string using string escapes
				writer.WriteString(text);
			}
			else
			{
				// Write string using CDATA section

				int end = text.IndexOf(CDATA_END);
	
				if (end < 0) 
				{
					writer.WriteCData(text);
				}
				else
				{
					int start = 0;
					while (end > -1) 
					{
						writer.WriteCData(text.Substring(start, end - start));
						if (end == text.Length - 3)
						{
							start = text.Length;
							writer.WriteString(CDATA_END);
							break;
						}
						else
						{
							writer.WriteString(CDATA_UNESCAPABLE_TOKEN);
							start = end + 2;
							end = text.IndexOf(CDATA_END, start);
						}
					}
	
					if (start < text.Length)
					{
						writer.WriteCData(text.Substring(start));
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
		/// <returns>the nunmber of times the substring occurs in the text</returns>
		/// <remarks>
		/// The substring is assumed to be non repeating within itself.
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
