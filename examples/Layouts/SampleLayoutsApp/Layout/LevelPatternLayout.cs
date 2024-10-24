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
using System.Collections;
using System.IO;
using log4net.Core;
using log4net.Layout;

namespace SampleLayoutsApp.Layout;

/// <inheritdoc/>
public sealed class LevelPatternLayout : PatternLayout
{
  private readonly Hashtable _levelToPatternLayout = [];

  /// <inheritdoc/>
  public override void Format(TextWriter writer, LoggingEvent loggingEvent)
  {
    ArgumentNullException.ThrowIfNull(loggingEvent);
    if (loggingEvent.Level is null 
        || _levelToPatternLayout[loggingEvent.Level] is not PatternLayout patternLayout)
    {
      base.Format(writer, loggingEvent);
    }
    else
    {
      patternLayout.Format(writer, loggingEvent);
    }
  }

  /// <inheritdoc/>
  public void AddLevelConversionPattern(LevelConversionPattern levelLayout)
  {
    ArgumentNullException.ThrowIfNull(levelLayout);
    _levelToPatternLayout[levelLayout.Level] = new PatternLayout(levelLayout.ConversionPattern);
  }
}