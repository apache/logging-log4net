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
/// Specifies that null is disallowed as an input even if the corresponding type allows it.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
internal sealed class DisallowNullAttribute : Attribute
{
  /// <summary>
  /// Initializes a new instance of the System.Diagnostics.CodeAnalysis.DisallowNullAttribute class.
  /// </summary>
  public DisallowNullAttribute() { }
}
#endif