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

#if !NET7_0_OR_GREATER
namespace System.Runtime.CompilerServices;

/// <summary>
/// Indicates that compiler support for a particular feature is required for the location where this attribute is applied
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
internal sealed class CompilerFeatureRequiredAttribute : Attribute
{
  /// <summary>
  /// The <see cref="FeatureName" /> used for the ref structs C# feature
  /// </summary>
  public const string RefStructs = nameof(RefStructs);

  /// <summary>
  /// The <see cref="FeatureName" /> used for the required members C# feature
  /// </summary>
  public const string RequiredMembers = nameof(RequiredMembers);

  /// <summary>
  /// The name of the compiler feature
  /// </summary>
  public string FeatureName { get; }

  ///  <summary>
  ///  Gets a value that indicates whether the compiler can choose to allow access to the location
  ///  where this attribute is applied if it does not understand <see cref="FeatureName" />
  ///  </summary>
  public bool IsOptional { get; init; }

  /// <summary>
  /// Initializes a <see cref="CompilerFeatureRequiredAttribute" /> instance for the passed in compiler feature
  /// </summary>
  /// <param name="featureName">The name of the compiler feature</param>
  public CompilerFeatureRequiredAttribute(string featureName) => FeatureName = featureName;
}
#endif