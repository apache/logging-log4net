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
using System.Collections;

using log4net.Core;
using log4net.Util;
using log4net.Repository;

namespace log4net.Util
{
	/// <summary>
	/// Abstract class that provides the formatting functionality that 
	/// derived classes need.
	/// </summary>
	/// <remarks>
	/// Conversion specifiers in a conversion patterns are parsed to
	/// individual PatternConverters. Each of which is responsible for
	/// converting a logging event in a converter specific manner.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public abstract class PatternConverter
	{
		#region Protected Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="PatternConverter" /> class.
		/// </summary>
		protected PatternConverter() 
		{  
		}

		#endregion Protected Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// the next patter converter in the chain
		/// </summary>
		public virtual PatternConverter Next
		{
			get { return m_next; }
		}

		/// <summary>
		/// The formatting info for this converter
		/// </summary>
		public virtual FormattingInfo FormattingInfo
		{
			get { return new FormattingInfo(m_min, m_max, m_leftAlign); }
			set
			{
				m_min = value.Min;
				m_max = value.Max;
				m_leftAlign = value.LeftAlign;
			}
		}

		/// <summary>
		/// The option for this converter
		/// </summary>
		public virtual string Option
		{
			get { return m_option; }
			set { m_option = value; }
		}

		#endregion Public Instance Properties

		#region Protected Abstract Methods

		/// <summary>
		/// Derived pattern converters must override this method in order to
		/// convert conversion specifiers in the correct way.
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="state">The state objet on which the pattern converter should be executed.</param>
		abstract protected void Convert(TextWriter writer, object state);

		#endregion Protected Abstract Methods

		#region Public Instance Methods

		/// <summary>
		/// Set the next pattern converter in the chains
		/// </summary>
		/// <param name="pc">the pattern converter that should follow this converter in the chain</param>
		/// <returns>the next converter</returns>
		/// <remarks>
		/// The PatternConverter can merge with its neighbour during this method (or a sub class).
		/// Therefore the return value may or may not be the value of the argument passed in.
		/// </remarks>
		public virtual PatternConverter SetNext(PatternConverter pc)
		{
			m_next = pc;
			return m_next;
		}

		/// <summary>
		/// A template method for formatting in a converter specific way.
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
		/// <param name="state">The state object on which the pattern converter should be executed.</param>
		virtual public void Format(TextWriter writer, object state) 
		{
			if (m_min < 0 && m_max == int.MaxValue)
			{
				// Formatting options are not in use
				Convert(writer, state);
			}
			else
			{
				m_formatWriter.Reset(c_renderBufferMaxCapacity, c_renderBufferSize);

				Convert(m_formatWriter, state);

				StringBuilder sb = m_formatWriter.GetStringBuilder();
				int len = sb.Length;

				if (len > m_max)
				{
					writer.Write(sb.ToString(len - m_max, m_max));
				}
				else if (len < m_min) 
				{
					if (m_leftAlign) 
					{	
						writer.Write(sb.ToString());
						SpacePad(writer, m_min - len);
					}
					else 
					{
						SpacePad(writer, m_min - len);
						writer.Write(sb.ToString());
					}
				}
				else
				{
					writer.Write(sb.ToString());
				}
			}
		}	

		static readonly string[] SPACES = {	" ", "  ", "    ", "        ",			// 1,2,4,8 spaces
											"                ",						// 16 spaces
											"                                " };	// 32 spaces

		/// <summary>
		/// Fast space padding method.
		/// </summary>
		/// <param name="writer"><see cref="TextWriter" /> to which the spaces will be appended.</param>
		/// <param name="length">The number of spaces to be padded.</param>
		protected static void SpacePad(TextWriter writer, int length) 
		{
			while(length >= 32) 
			{
				writer.Write(SPACES[5]);
				length -= 32;
			}
    
			for(int i = 4; i >= 0; i--) 
			{	
				if ((length & (1<<i)) != 0) 
				{
					writer.Write(SPACES[i]);
				}
			}
		}	

		#endregion Public Instance Methods

		#region Private Instance Fields

		private PatternConverter m_next;
		private int m_min = -1;
		private int m_max = int.MaxValue;
		private bool m_leftAlign = false;

		/// <summary>
		/// The option string to the converter
		/// </summary>
		private string m_option = null;

		private ReusableStringWriter m_formatWriter = new ReusableStringWriter(System.Globalization.CultureInfo.InvariantCulture);

		#endregion Private Instance Fields

		#region Constants

		/// <summary>
		/// Initial buffer size
		/// </summary>
		private const int c_renderBufferSize = 256;

		/// <summary>
		/// Maximum buffer size before it is recycled
		/// </summary>
		private const int c_renderBufferMaxCapacity = 1024;

		#endregion

		#region Static Methods

		/// <summary>
		/// Write an dictionary to a <see cref="TextWriter"/>
		/// </summary>
		/// <param name="writer">the writer to write to</param>
		/// <param name="repository">a <see cref="ILoggerRepository"/> to use for object conversion</param>
		/// <param name="value">the value to write to the writer</param>
		/// <remarks>
		/// <para>
		/// Writes the <see cref="IDictionary"/> to a writer in the form:
		/// </para>
		/// <code>
		/// {key1=value1, key2=value2, key3=value3}
		/// </code>
		/// <para>
		/// If the <see cref="ILoggerRepository"/> specified
		/// is not null then it is used to render the key and value to text, otherwise
		/// the object's ToString method is called.
		/// </para>
		/// </remarks>
		protected static void WriteDictionary(TextWriter writer, ILoggerRepository repository, IDictionary value)
		{
			writer.Write("{");

			bool first = true;

			// Write out all the MDC key value pairs
			foreach(DictionaryEntry entry in value)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					writer.Write(", ");
				}
				WriteObject(writer, repository, entry.Key);
				writer.Write("=");
				WriteObject(writer, repository, entry.Value);
			}

			writer.Write("}");
		}

		/// <summary>
		/// Write an object to a <see cref="TextWriter"/>
		/// </summary>
		/// <param name="writer">the writer to write to</param>
		/// <param name="repository">a <see cref="ILoggerRepository"/> to use for object conversion</param>
		/// <param name="value">the value to write to the writer</param>
		/// <remarks>
		/// <para>
		/// Writes the Object to a writer. If the <see cref="ILoggerRepository"/> specified
		/// is not null then it is used to render the object to text, otherwise
		/// the object's ToString method is called.
		/// </para>
		/// </remarks>
		protected static void WriteObject(TextWriter writer, ILoggerRepository repository, object value)
		{
			if (repository != null)
			{
				repository.RendererMap.FindAndRender(value, writer);
			}
			else
			{
				// Don't have a repository to render with so just have to rely on ToString
				if (value == null)
				{
					writer.Write( "(null)" );
				}
				else
				{
					writer.Write( value.ToString() );
				}
			}
		}

		#endregion
	}
}
