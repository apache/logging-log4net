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

namespace System.Diagnostics.CodeAnalysis;

// Not available until .NET 6, cloned from .NET code.

/// <summary>
/// Specifies that the output will be non-null if the named parameter is non-null.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue,
  AllowMultiple = true, Inherited = false)]
internal sealed class NotNullIfNotNullAttribute : Attribute
{
  /// <summary>
  /// Initializes the attribute with the associated parameter name.
  /// </summary>
  /// <param name="parameterName">
  /// The associated parameter name.
  /// The output will be non-null if the argument to the parameter specified is non-null.
  /// </param>
  public NotNullIfNotNullAttribute(string parameterName)
    => ParameterName = parameterName;

  /// <summary>
  /// Gets the associated parameter name.
  /// </summary>
  public string ParameterName { get; }
}
