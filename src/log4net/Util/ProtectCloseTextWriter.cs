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

using System.IO;

namespace log4net.Util;

/// <summary>
/// A <see cref="TextWriter"/> that ignores the <see cref="Close"/> message
/// </summary>
/// <remarks>
/// This writer is used in special cases where it is necessary 
/// to protect a writer from being closed by a client.
/// </remarks>
/// <author>Nicko Cadell</author>
public class ProtectCloseTextWriter : TextWriterAdapter
{
  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="writer">the writer to actually write to</param>
  /// <remarks>
  /// Create a new ProtectCloseTextWriter using a writer
  /// </remarks>
  public ProtectCloseTextWriter(TextWriter writer) : base(writer)
  {
  }

  /// <summary>
  /// Attaches this instance to a different underlying <see cref="TextWriter"/>.
  /// </summary>
  /// <param name="writer">the writer to attach to</param>
  public void Attach(TextWriter writer) => Writer = writer;

  /// <summary>
  /// Does not close the underlying output writer.
  /// </summary>
  public override void Close()
  {
    // do nothing
  }
}