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
using System.IO;


namespace log4net.Util
{
	/// <summary>
	/// A StringWriter that can be <see cref="Reset"/> and reused
	/// </summary>
	/// <author>Nicko Cadell</author>
	public class ReusableStringWriter : StringWriter
	{
		/// <summary>
		/// Create an instance of <see cref="ReusableStringWriter"/>
		/// </summary>
		/// <param name="formatProvider">the format provider to use</param>
		public ReusableStringWriter(IFormatProvider formatProvider) : base(formatProvider) 
		{
		}

		/// <summary>
		/// Override Dispose to prevent closing of writer
		/// </summary>
		/// <param name="disposing">flag</param>
		protected override void Dispose(bool disposing)
		{
			// Do not close the writer
		}

		/// <summary>
		/// Reset this string witer so that it can be reused.
		/// The internal buffers are cleared and reset.
		/// </summary>
		/// <param name="maxCapacity">the maximum buffer capacity before it is trimmed</param>
		/// <param name="defaultSize">the default size to make the buffer</param>
		public void Reset(int maxCapacity, int defaultSize)
		{
			// Reset working string buffer
			StringBuilder sb = this.GetStringBuilder();

			sb.Length = 0;
			
			// Check if over max size
			if (sb.Capacity > maxCapacity) 
			{
				sb.Capacity = defaultSize;
			} 
		}
	}
}
