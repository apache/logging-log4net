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
using System.Text;

using log4net.Core;

namespace log4net.Util
{
	/// <summary>
	/// A <see cref="TextWriter"/> that ignores the <see cref="Close"/> message
	/// </summary>
	/// <remarks>
	/// Used in special cases where it is necessary to protect a writer
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
