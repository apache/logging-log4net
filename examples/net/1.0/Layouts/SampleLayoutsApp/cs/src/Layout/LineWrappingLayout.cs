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
using System.IO;
using System.Text;
using log4net.Layout;
using log4net.Core;
using log4net.Util;

namespace SampleLayoutsApp.Layout
{
	/// <summary>
	/// The LineWrappingLayout wraps the output of a nested layout
	/// </summary>
	/// <remarks>
	/// The output of the nested layout is wrapped at
	/// <see cref="LineWidth"/>. Each of the continuation lines
	/// is prefixed with a number of spaces specified by <see cref="Indent"/>.
	/// </remarks>
	public class LineWrappingLayout : ForwardingLayout
	{
		private int m_lineWidth = 76;
		private int m_indent = 4;

		public LineWrappingLayout()
		{
		}

		public int LineWidth
		{
			get { return m_lineWidth; }
			set { m_lineWidth = value; }
		}
		public int Indent
		{
			get { return m_indent; }
			set { m_indent = value; }
		}

		override public void Format(TextWriter writer, LoggingEvent loggingEvent)
		{
			StringWriter stringWriter = new StringWriter();

			base.Format(stringWriter, loggingEvent);

			string formattedString = stringWriter.ToString();

			WrapText(writer, formattedString);
		}

		private void WrapText(TextWriter writer, string text)
		{
			if (text.Length <= m_lineWidth)
			{
				writer.Write(text);
			}
			else
			{
				// Do the first line
				writer.WriteLine(text.Substring(0, m_lineWidth));
				string rest = text.Substring(m_lineWidth);

				string indentString = new String(' ', m_indent);
				int continuationLineWidth = m_lineWidth - m_indent;

				// Do the continuation lines
				while(true)
				{
					writer.Write(indentString);

					if (rest.Length > continuationLineWidth)
					{
						writer.WriteLine(rest.Substring(0, continuationLineWidth));
						rest = rest.Substring(continuationLineWidth);
					}
					else
					{
						writer.Write(rest);
						break;
					}
				}
			}
		}
	}
}
