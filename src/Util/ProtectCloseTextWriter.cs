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
using System.IO;
using System.Text;

using log4net.Core;

namespace log4net.Util
{
	/// <summary>
	/// A <see cref="TextWriter"/> that ignores the <see cref="Close"/> message
	/// </summary>
	/// <remarks>
	/// Used in special cases where it is nessasary to protect a writer
	/// from being closed by a client.
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public class ProtectCloseTextWriter : TextWriterAdapter
	{
		#region Public Instance Constructors

		/// <summary>
		/// Create a new ProtectCloseTextWriter using a writer
		/// </summary>
		/// <param name="writer">the writer to actually write to</param>
		public ProtectCloseTextWriter(TextWriter writer) : base(writer)
		{
		}

		#endregion Public Instance Constructors

		#region Public Properties

		/// <summary>
		/// Attach this instance to a different underlying <see cref="TextWriter"/>
		/// </summary>
		/// <param name="writer">the writer to attach to</param>
		public void Attach(TextWriter writer)
		{
			this.Writer = writer;
		}

		#endregion

		#region Override Implementation of TextWriter

		/// <summary>
		/// Does not close the underlying output writer.
		/// </summary>
		override public void Close()
		{
			// do nothing
		}

		#endregion Public Instance Methods
	}
}
