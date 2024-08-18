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

using System.Diagnostics;
using log4net.Core;

namespace log4net.Util
{
  /// <summary>
  /// An abstract base class for types that are stored in the
  /// <see cref="LevelMapping"/> object.
  /// </summary>
  /// <author>Nicko Cadell</author>
  [DebuggerDisplay("{Level}")]
  public abstract class LevelMappingEntry : IOptionHandler
  {
    /// <summary>
    /// Default protected constructor
    /// </summary>
    protected LevelMappingEntry()
    { }

    /// <summary>
    /// Gets or sets the level that is the key for this mapping.
    /// </summary>
    public Level? Level { get; set; }

    /// <summary>
    /// Initialize any options defined on this entry
    /// </summary>
    /// <remarks>
    /// <para>
    /// Should be overridden by any classes that need to initialize based on their options
    /// </para>
    /// </remarks>
    public virtual void ActivateOptions()
    {
      // default implementation is to do nothing
    }
  }
}
