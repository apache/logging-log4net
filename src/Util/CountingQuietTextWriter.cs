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
using System.IO;

using log4net.Core;

namespace log4net.Util
{
	/// <summary>
	/// Subclass of <see cref="QuietTextWriter"/> that maintains a count of 
	/// the number of bytes written.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class CountingQuietTextWriter : QuietTextWriter 
	{
		#region Public Instance Constructors

		/// <summary>
		/// Creates a new instance of the <see cref="CountingQuietTextWriter" /> class 
		/// with the specified <see cref="TextWriter" /> and <see cref="IErrorHandler" />.
		/// </summary>
		/// <param name="writer">The <see cref="TextWriter" /> to actually write to.</param>
		/// <param name="errorHandler">The <see cref="IErrorHandler" /> to report errors to.</param>
		public CountingQuietTextWriter(TextWriter writer, IErrorHandler errorHandler) : base(writer, errorHandler)
		{
			m_countBytes = 0;
		}

		#endregion Public Instance Constructors

		#region Override implementation of QuietTextWriter
  
		/// <summary>
		/// Writes a string to the output and counts the number of bytes written.
		/// </summary>
		/// <param name="str">The string data to write to the output.</param>
		override public void Write(string str) 
		{
			if (str != null && str.Length > 0)
			{
				try 
				{
					base.Write(str);

					// get the number of bytes needed to represent the 
					// string using the supplied encoding.
					m_countBytes += this.Encoding.GetByteCount(str);
				}
				catch(Exception e) 
				{
					this.ErrorHandler.Error("Failed to write [" + str + "].", e, ErrorCode.WriteFailure);
				}
			}
		}

		#endregion Override implementation of QuietTextWriter

		#region Public Instance Properties

		/// <summary>
		/// Gets or sets the total number of bytes written.
		/// </summary>
		/// <value>
		/// The total number of bytes written.
		/// </value>
		public long Count 
		{
			get { return m_countBytes; }
			set { m_countBytes = value; }
		}

		#endregion Public Instance Properties
  
		#region Private Instance Fields

		/// <summary>
		/// Total number of bytes written.
		/// </summary>
		private long m_countBytes;

		#endregion Private Instance Fields
	}
}
