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
using System.IO;

namespace log4net.Util.PatternStringConverters;

/// <summary>
/// Write an environment variable to the output
/// </summary>
/// <remarks>
/// <para>
/// Write an environment variable to the output writer.
/// The value of the <see cref="log4net.Util.PatternConverter.Option"/> determines 
/// the name of the variable to output.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
public sealed class EnvironmentPatternConverter : PatternConverter
{
  /// <summary>
  /// Write an environment variable to the output
  /// </summary>
  /// <param name="writer">the writer to write to</param>
  /// <param name="state">null, state is not set</param>
  /// <remarks>
  /// <para>
  /// Writes the environment variable to the output <paramref name="writer"/>.
  /// The name of the environment variable to output must be set
  /// using the <see cref="log4net.Util.PatternConverter.Option"/>
  /// property.
  /// </para>
  /// </remarks>
  public override void Convert(TextWriter writer, object? state)
  {
    try
    {
      if (Option?.Length > 0)
      {
        // Lookup the environment variable
        string? envValue = Environment.GetEnvironmentVariable(Option);

        // If we didn't see it for the process, try a user level variable.
        envValue ??= Environment.GetEnvironmentVariable(Option, EnvironmentVariableTarget.User);

        // If we still didn't find it, try a system level one.
        envValue ??= Environment.GetEnvironmentVariable(Option, EnvironmentVariableTarget.Machine);

        if (envValue?.Length > 0)
        {
          writer.Write(envValue);
        }
      }
    }
    catch (System.Security.SecurityException secEx)
    {
      // This security exception will occur if the caller does not have 
      // unrestricted environment permission. If this occurs the expansion 
      // will be skipped with the following warning message.
      LogLog.Debug(_declaringType, "Security exception while trying to expand environment variables. Error Ignored. No Expansion.", secEx);
    }
    catch (Exception e) when (!e.IsFatal())
    {
      LogLog.Error(_declaringType, "Error occurred while converting environment variable.", e);
    }
  }

  /// <summary>
  /// The fully qualified type of the EnvironmentPatternConverter class.
  /// </summary>
  /// <remarks>
  /// Used by the internal logger to record the Type of the
  /// log message.
  /// </remarks>
  private static readonly Type _declaringType = typeof(EnvironmentPatternConverter);
}
