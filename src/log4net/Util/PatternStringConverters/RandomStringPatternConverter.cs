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

using log4net.Core;

namespace log4net.Util.PatternStringConverters;

/// <summary>
/// A Pattern converter that generates a string of random characters
/// </summary>
/// <remarks>
/// <para>
/// The converter generates a string of random characters. By default
/// the string is length 4. This can be changed by setting the <see cref="PatternConverter.Option"/>
/// to the string value of the length required.
/// </para>
/// <para>
/// The random characters in the string are limited to uppercase letters and numbers only.
/// </para>
/// <para>
/// The random number generator used by this class is not cryptographically secure.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
public sealed class RandomStringPatternConverter : PatternConverter, IOptionHandler
{
  /// <summary>
  /// Shared random number generator
  /// </summary>
  private static readonly Random _sRandom = new();

  /// <summary>
  /// Length of random string to generate. Default length 4.
  /// </summary>
  private int _length = 4;

  /// <summary>
  /// Initialize the converter options
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is part of the <see cref="IOptionHandler"/> delayed object
  /// activation scheme. The <see cref="ActivateOptions"/> method must 
  /// be called on this object after the configuration properties have
  /// been set. Until <see cref="ActivateOptions"/> is called this
  /// object is in an undefined state and must not be used. 
  /// </para>
  /// <para>
  /// If any of the configuration properties are modified then 
  /// <see cref="ActivateOptions"/> must be called again.
  /// </para>
  /// </remarks>
  public void ActivateOptions()
  {
    string? optionStr = Option;
    if (!string.IsNullOrEmpty(optionStr))
    {
      if (SystemInfo.TryParse(optionStr!, out int lengthVal))
      {
        _length = lengthVal;
      }
      else
      {
        LogLog.Error(_declaringType, $"RandomStringPatternConverter: Could not convert Option [{optionStr}] to Length Int32");
      }
    }
  }

  /// <summary>
  /// Writes a random string to the output
  /// </summary>
  /// <param name="writer">the writer to write to</param>
  /// <param name="state">null, state is not set</param>
  public override void Convert(TextWriter writer, object? state)
  {
    try
    {
      lock (_sRandom)
      {
        for (int i = 0; i < _length; i++)
        {
          int randValue = _sRandom.Next(36);

          if (randValue < 26)
          {
            // Letter
            char ch = (char)('A' + randValue);
            writer.Write(ch);
          }
          else if (randValue < 36)
          {
            // Number
            char ch = (char)('0' + (randValue - 26));
            writer.Write(ch);
          }
          else
          {
            // Should not get here
            writer.Write('X');
          }
        }
      }
    }
    catch (Exception e) when (!e.IsFatal())
    {
      LogLog.Error(_declaringType, "Error occurred while converting.", e);
    }
  }

  /// <summary>
  /// The fully qualified type of the RandomStringPatternConverter class.
  /// </summary>
  /// <remarks>
  /// Used by the internal logger to record the Type of the
  /// log message.
  /// </remarks>
  private static readonly Type _declaringType = typeof(RandomStringPatternConverter);
}
