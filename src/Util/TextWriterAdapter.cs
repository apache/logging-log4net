#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
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
using System.IO;
using System.Globalization;

namespace log4net.Util
{
	/// <summary>
	/// Adapter that extends <see cref="TextWriter"/> and forwards all
	/// messages to an instance of <see cref="TextWriter"/>.
	/// </summary>
	/// <author>Nicko Cadell</author>
	public abstract class TextWriterAdapter : TextWriter
	{
		#region Protected Member Variables

		/// <summary>
		/// The writer to forward messages to
		/// </summary>
		private TextWriter m_writer;

		#endregion

		#region Constructors

		/// <summary>
		/// Create an instance of <see cref="TextWriterAdapter"/> that forwards all
		/// messages to a <see cref="TextWriter"/>.
		/// </summary>
		/// <param name="writer">The <see cref="TextWriter"/> to forward to</param>
		protected TextWriterAdapter(TextWriter writer) :  base(CultureInfo.InvariantCulture)
		{
			m_writer = writer;
		}

		#endregion

		#region Protected Instance Properties

		/// <summary>
		/// Gets the underlying <see cref="TextWriter" />.
		/// </summary>
		/// <value>
		/// The underlying <see cref="TextWriter" />.
		/// </value>
		protected TextWriter Writer 
		{
			get { return m_writer; }
			set { m_writer = value; }
		}

		#endregion Protected Instance Properties

		#region Public Properties
    
		/// <summary>
		/// The Encoding in which the output is written
		/// </summary>
		override public Encoding Encoding 
		{
			get { return m_writer.Encoding; }
		}

		/// <summary>
		/// Gets an object that controls formatting
		/// </summary>
		override public IFormatProvider FormatProvider 
		{
			get { return m_writer.FormatProvider; }
		}

		/// <summary>
		/// Gets or sets the line terminator string used by the TextWriter
		/// </summary>
		override public String NewLine 
		{
			get { return m_writer.NewLine; }
			set { m_writer.NewLine = value; }
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Closes the writer and releases any system resources associated with the writer
		/// </summary>
		override public void Close() 
		{
			m_writer.Close();
		}

		/// <summary>
		/// Dispose this writer
		/// </summary>
		/// <param name="disposing">flag indicating if we are being disposed</param>
		override protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				((IDisposable)m_writer).Dispose();
			}
		}

		/// <summary>
		/// Clears all buffers for the writer and causes any buffered data to be written 
		/// to the underlying device
		/// </summary>
		override public void Flush() 
		{
			m_writer.Flush();
		}

		/// <summary>
		/// Writes a character to the wrapped TextWriter
		/// </summary>
		/// <param name="value">the value to write to the TextWriter</param>
		override public void Write(char value) 
		{
			m_writer.Write(value);
		}
    
		/// <summary>
		/// Writes a character buffer to the wrapped TextWriter
		/// </summary>
		/// <param name="buffer">the data buffer</param>
		/// <param name="index">the start index</param>
		/// <param name="count">the number of characters to write</param>
		override public void Write(char[] buffer, int index, int count) 
		{
			m_writer.Write(buffer, index, count);
		}
    
		/// <summary>
		/// Writes a string to the wrapped TextWriter
		/// </summary>
		/// <param name="value">the value to write to the TextWriter</param>
		override public void Write(String value) 
		{
			m_writer.Write(value);
		}

		#endregion
	}
}
