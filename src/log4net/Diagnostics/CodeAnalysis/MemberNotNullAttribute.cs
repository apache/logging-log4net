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

// inspired by https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/NullableAttributes.cs


#if !NET6_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Specifies that the method or property will ensure that the listed field and property members have values that aren't null.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
internal sealed class MemberNotNullAttribute : Attribute
{
  /// <summary>
  /// Initializes the attribute with list of field or property members.
  /// </summary>
  /// <param name="members">The list of field and property members that are promised to be non-null.</param>
  public MemberNotNullAttribute(params string[] members) => Members = members;

  /// <summary>
  /// Initializes the attribute with a field or property member.
  /// </summary>
  /// <param name="member">The field or property member that is promised to be non-null.</param>
  [SuppressMessage("Design", "CA1019:Define accessors for attribute arguments", Justification = "LuGe: 1-zu-1 von DotNet geklaut")]
  public MemberNotNullAttribute(string member)
    : this(new[] { member })
  { }

  /// <summary>
  /// Gets field or property member names.
  /// </summary>
  public string[] Members { get; }
}
#endif