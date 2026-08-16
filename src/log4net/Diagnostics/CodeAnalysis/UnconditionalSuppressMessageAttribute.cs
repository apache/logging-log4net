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

// inspired by https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/UnconditionalSuppressMessageAttribute.cs

#if !NET6_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Suppresses a trimming or single file warning, unconditionally: unlike
/// <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/> it is kept in the
/// assembly, so the trimmer can still see it.
/// </summary>
/// <remarks>
/// <para>
/// Neither <c>net462</c> nor <c>netstandard2.0</c> declares this attribute. The trimmer recognizes
/// it by full name, so log4net supplies its own.
/// </para>
/// </remarks>
/// <param name="category">the category of the suppressed warning</param>
/// <param name="checkId">the identifier of the suppressed warning</param>
[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
internal sealed class UnconditionalSuppressMessageAttribute(string category, string checkId) : Attribute
{
  /// <summary>
  /// Gets the category of the suppressed warning.
  /// </summary>
  public string Category { get; } = category;

  /// <summary>
  /// Gets the identifier of the suppressed warning.
  /// </summary>
  public string CheckId { get; } = checkId;

  /// <summary>
  /// Gets or sets the scope of the suppression.
  /// </summary>
  public string? Scope { get; set; }

  /// <summary>
  /// Gets or sets the fully qualified name of the suppression target.
  /// </summary>
  public string? Target { get; set; }

  /// <summary>
  /// Gets or sets why the warning is suppressed.
  /// </summary>
  public string? Justification { get; set; }

  /// <summary>
  /// Gets or sets an optional argument expanding on the suppression.
  /// </summary>
  public string? MessageId { get; set; }
}
#endif
