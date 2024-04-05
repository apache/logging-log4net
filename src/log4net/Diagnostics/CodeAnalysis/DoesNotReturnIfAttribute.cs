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

#if !NET6_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Specifies that the method will not return if the associated System.Boolean parameter is passed the specified value.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class DoesNotReturnIfAttribute : Attribute
{
  /// <summary>
  /// Initializes a new instance of the System.Diagnostics.CodeAnalysis.DoesNotReturnIfAttribute class
  /// with the specified parameter value.
  /// </summary>
  /// <param name="parameterValue">
  ///   The condition parameter value.
  ///   Code after the method is considered unreachable by diagnostics if the argument to the associated parameter
  ///   matches this value.
  /// </param>
  public DoesNotReturnIfAttribute(bool parameterValue) => ParameterValue = parameterValue;

  /// <summary>
  /// Gets the condition parameter value.
  /// </summary>
  /// <returns>The condition parameter value. Code after the method is considered unreachable
  /// by diagnostics if the argument to the associated parameter matches this value.
  /// </returns>
  public bool ParameterValue { get; }
}

#endif