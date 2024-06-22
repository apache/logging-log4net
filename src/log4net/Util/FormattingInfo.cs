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

namespace log4net.Util
{
  /// <summary>
  /// Contain the information obtained when parsing formatting modifiers 
  /// in conversion modifiers.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Holds the formatting information extracted from the format string by
  /// the <see cref="PatternParser"/>. This is used by the <see cref="PatternConverter"/>
  /// objects when rendering the output.
  /// </para>
  /// </remarks>
  /// <author>Nicko Cadell</author>
  /// <author>Gert Driesen</author>
  public class FormattingInfo
  {
    /// <summary>
    /// Defaut Constructor
    /// </summary>
    /// <remarks>
    /// <para>
    /// Initializes a new instance of the <see cref="FormattingInfo" /> class.
    /// </para>
    /// </remarks>
    public FormattingInfo()
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <remarks>
    /// <para>
    /// Initializes a new instance of the <see cref="FormattingInfo" /> class
    /// with the specified parameters.
    /// </para>
    /// </remarks>
    public FormattingInfo(int min, int max, bool leftAlign)
    {
      Min = min;
      Max = max;
      LeftAlign = leftAlign;
    }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public int Min { get; set; } = -1;

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public int Max { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets a flag indicating whether left align is enabled.
    /// or not.
    /// </summary>
    public bool LeftAlign { get; set; } = false;
  }
}
