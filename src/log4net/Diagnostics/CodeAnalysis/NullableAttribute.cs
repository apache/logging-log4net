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

using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

/// <inheritdoc/>
[AttributeUsage(AttributeTargets.GenericParameter | AttributeTargets.ReturnValue | AttributeTargets.Parameter | AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class NullableAttribute : Attribute
{
  /// <inheritdoc/>
  [SuppressMessage("Style", "KR1010:Class field names should be camelCased")]
  [SuppressMessage("Design", "CA1051:Do not declare visible instance fields")]
  public readonly byte[] NullableFlags;

  /// <inheritdoc/>
  [SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
  public NullableAttribute(byte nullableFlags) => NullableFlags = new[] { nullableFlags };

  /// <inheritdoc/>
  public NullableAttribute(byte[] nullableFlags) => NullableFlags = nullableFlags;
}