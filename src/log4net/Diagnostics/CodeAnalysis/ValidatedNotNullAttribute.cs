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

/// <summary>
/// Attribute to tell Roslyn-Analyzers that a parameter will be checked for <see langword="null"/>
/// </summary>
// https://github.com/dotnet/roslyn-analyzers/issues/2215
// https://github.com/dotnet/roslyn-analyzers/blob/main/src/NetAnalyzers/UnitTests/Microsoft.CodeQuality.Analyzers/QualityGuidelines/ValidateArgumentsOfPublicMethodsTests.cs

[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class ValidatedNotNullAttribute : Attribute { }