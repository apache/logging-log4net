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

// inspired by https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/DynamicallyAccessedMemberTypes.cs

#if !NET6_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Specifies the types of members that are dynamically accessed.
/// </summary>
/// <remarks>
/// <para>
/// The trimmer matches this type by its full name rather than by identity, so the values have to
/// keep the numbering the framework uses. Only the members log4net annotates with are declared;
/// add further ones from the runtime source above as they are needed.
/// </para>
/// </remarks>
[Flags]
internal enum DynamicallyAccessedMemberTypes
{
  /// <summary>
  /// Specifies no members.
  /// </summary>
  None = 0,

  /// <summary>
  /// Specifies the default, parameterless public constructor.
  /// </summary>
  PublicParameterlessConstructor = 0x0001,

  /// <summary>
  /// Specifies all public constructors.
  /// </summary>
  PublicConstructors = 0x0002 | PublicParameterlessConstructor,
}
#endif
