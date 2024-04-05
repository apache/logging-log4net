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
/// Specifies that the method or property will ensure that the listed field and property members have non-null values
/// when returning with the specified return value condition.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
internal sealed class MemberNotNullWhenAttribute : Attribute
{
  /// <summary>
  /// Initializes the attribute with the specified return value condition and a field or property member.
  /// </summary>
  /// <param name="returnValue">The return value condition. If the method returns this value, the associated parameter will not be null.</param>
  /// <param name="members">The list of field and property members that are promised to be non-null.</param>
  [SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
  public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
  {
    ReturnValue = returnValue;
    Members = members;
  }

  /// <summary>
  /// Initializes the attribute with the specified return value condition and a field or property member.
  /// </summary>
  /// <param name="returnValue">The return value condition. If the method returns this value, the associated parameter will not be null.</param>
  /// <param name="member">The field or property member that is promised to be non-null.</param>
  [SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
  public MemberNotNullWhenAttribute(bool returnValue, string member)
    : this(returnValue, new[] { member })
  { }

  /// <summary>
  /// Gets field or property member names.
  /// </summary>
  public string[] Members { get; }

  /// <summary>
  /// Gets the return value condition.
  /// </summary>
  public bool ReturnValue { get; }
}
#endif