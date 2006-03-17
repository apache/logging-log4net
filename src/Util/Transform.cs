#region Copyright & License
//
// Copyright 2001-2006 The Apache Software Foundation
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
using System.Text.RegularExpressions;

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

		#region XML String Methods

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
			return INVALIDCHARS.Replace(textData, mask);
		}

		#endregion XML String Methods

		#region StringFormat

		/// <summary>
		/// Replaces the format item in a specified <see cref="System.String"/> with the text equivalent 
		/// of the value of a corresponding <see cref="System.Object"/> instance in a specified array.
		/// A specified parameter supplies culture-specific formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="System.IFormatProvider"/> that supplies culture-specific formatting information.</param>
		/// <param name="format">A <see cref="System.String"/> containing zero or more format items.</param>
		/// <param name="args">An <see cref="System.Object"/> array containing zero or more objects to format.</param>
		/// <returns>
		/// A copy of format in which the format items have been replaced by the <see cref="System.String"/> 
		/// equivalent of the corresponding instances of <see cref="System.Object"/> in args.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method does not throw exceptions. If an exception thrown while formatting the result the
		/// exception and arguments are returned in the result string.
		/// </para>
		/// </remarks>
		public static string StringFormat(IFormatProvider provider, string format, params object[] args)
		{
			try
			{
				// The format is missing, log null value
				if (format == null)
				{
					return null;
				}

				// The args are missing - should not happen unless we are called explicitly with a null array
				if (args == null)
				{
					return format;
				}

				// Try to format the string
				return String.Format(provider, format, args);
			}
			catch(Exception ex)
			{
				log4net.Util.LogLog.Error("StringFormat: Exception while rendering format ["+format+"]", ex);
				return StringFormatError(ex, format, args);
			}
			catch
			{
				log4net.Util.LogLog.Error("StringFormat: Exception while rendering format ["+format+"]");
				return StringFormatError(null, format, args);
			}
		}

		/// <summary>
		/// Process an error during StringFormat
		/// </summary>
		private static string StringFormatError(Exception formatException, string format, object[] args)
		{
			try
			{
				StringBuilder buf = new StringBuilder("<log4net.Error>");

				if (formatException != null)
				{
					buf.Append("Exception during StringFormat: ").Append(formatException.Message);
				}
				else
				{
					buf.Append("Exception during StringFormat");
				}
				buf.Append(" <format>").Append(format).Append("</format>");
				buf.Append("<args>");
				RenderArray(args, buf);
				buf.Append("</args>");
				buf.Append("</log4net.Error>");

				return buf.ToString();
			}
			catch(Exception ex)
			{
				log4net.Util.LogLog.Error("StringFormat: INTERNAL ERROR during StringFormat error handling", ex);
				return "<log4net.Error>Exception during StringFormat. See Internal Log.</log4net.Error>";
			}
			catch
			{
				log4net.Util.LogLog.Error("StringFormat: INTERNAL ERROR during StringFormat error handling");
				return "<log4net.Error>Exception during StringFormat. See Internal Log.</log4net.Error>";
			}
		}

		/// <summary>
		/// Dump the contents of an array into a string builder
		/// </summary>
		private static void RenderArray(Array array, StringBuilder buffer)
		{
			if (array == null)
			{
				buffer.Append(SystemInfo.NullText);
			}
			else
			{
				if (array.Rank != 1)
				{
					buffer.Append(array.ToString());
				}
				else
				{
					buffer.Append("{");
					int len = array.Length;

					if (len > 0)
					{
						RenderObject(array.GetValue(0), buffer);
						for (int i = 1; i < len; i++)
						{
							buffer.Append(", ");
							RenderObject(array.GetValue(i), buffer);
						}
					}
					buffer.Append("}");
				}
			}
		}

		/// <summary>
		/// Dump an object to a string
		/// </summary>
		private static void RenderObject(Object obj, StringBuilder buffer)
		{
			if (obj == null)
			{
				buffer.Append(SystemInfo.NullText);
			}
			else
			{
				try
				{
					buffer.Append(obj);
				}
				catch(Exception ex)
				{
					buffer.Append("<Exception: ").Append(ex.Message).Append(">");
				}
				catch
				{
					buffer.Append("<Exception>");
				}
			}
		}

		#endregion StringFormat
		
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

		private static Regex INVALIDCHARS=new Regex(@"[^\x09\x0A\x0D\x20-\xFF\u00FF-\u07FF\uE000-\uFFFD]",RegexOptions.Compiled);
		#endregion Private Static Fields
	}
}
