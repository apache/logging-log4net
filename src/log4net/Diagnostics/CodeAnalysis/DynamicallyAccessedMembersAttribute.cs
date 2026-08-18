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

// inspired by https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/DynamicallyAccessedMembersAttribute.cs

#if !NET6_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// States which members of a <see cref="Type"/> are accessed dynamically, so that a trimmer keeps
/// them instead of removing them as unused.
/// </summary>
/// <remarks>
/// <para>
/// Neither <c>net462</c> nor <c>netstandard2.0</c> declares this attribute, but the trimmer
/// recognizes it by full name, so a library can supply its own and still be understood - which is
/// what lets log4net keep working when a consumer publishes with <c>PublishAot</c> or
/// <c>PublishTrimmed</c>.
/// </para>
/// </remarks>
/// <param name="memberTypes">The members that are dynamically accessed.</param>
[AttributeUsage(
  AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter
  | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method,
  Inherited = false)]
internal sealed class DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) : Attribute
{
  /// <summary>
  /// Gets the members that are dynamically accessed.
  /// </summary>
  public DynamicallyAccessedMemberTypes MemberTypes { get; } = memberTypes;
}
#endif
