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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace log4net.Tests;

/// <summary>
/// Reaches the private and internal members that <c>log4net</c> does not expose, since it grants no
/// <c>InternalsVisibleTo</c>.
/// </summary>
/// <remarks>
/// <para>
/// The receiver decides between static and instance: the <see cref="Type"/> members are the static
/// ones, the <see cref="object"/> members the instance ones. A variable statically typed
/// <see cref="object"/> that holds a <see cref="Type"/> would therefore bind to the instance member.
/// </para>
/// <para>
/// The lookup includes public members, so <c>NonPublic</c> in a name says what the call sites are
/// for, not what the flags exclude. Arguments are passed as an explicit <c>[a, b]</c>: a C# 14
/// extension block miscomputes the nullability of an expanded <c>params</c> argument (CS8620),
/// which <c>WarningsAsErrors=nullable</c> turns into a build error.
/// </para>
/// </remarks>
internal static class ReflectionExtensions
{
  private const BindingFlags AnyMember =
    BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

  extension(Type owner)
  {
    /// <summary>Finds a method, optionally disambiguating an overload by its parameter types.</summary>
    internal MethodInfo NonPublicMethod(string name, params Type[] signature)
      => FindMethod(owner, name, signature);

    /// <summary>Finds a field.</summary>
    internal FieldInfo NonPublicField(string name) => FindField(owner, name);

    /// <summary>Finds a nested type.</summary>
    internal Type NonPublicNestedType(string name)
      => owner.GetNestedType(name, AnyMember) ?? throw Missing(owner, name);

    /// <summary>Calls a static method and casts its result.</summary>
    internal T Invoke<T>(string name, params object?[] arguments)
      => (T)FindOverload(owner, name, arguments).Invoke(null, arguments)!;

    /// <summary>Calls a static method and discards its result.</summary>
    internal void Invoke(string name, params object?[] arguments)
      => FindOverload(owner, name, arguments).Invoke(null, arguments);

    /// <summary>Reads a static field.</summary>
    internal T GetFieldValue<T>(string name) => (T)FindField(owner, name).GetValue(null)!;

    /// <summary>Writes a static field.</summary>
    internal void SetFieldValue(string name, object? value) => FindField(owner, name).SetValue(null, value);

    /// <summary>Reads a static property.</summary>
    internal T? GetPropertyValue<T>(string name)
      => (T?)(owner.GetProperty(name, AnyMember) ?? throw Missing(owner, name)).GetValue(null);

    /// <summary>Constructs an instance through a non-public constructor and casts it.</summary>
    internal T Construct<T>(params object?[] arguments)
      => (T)(Activator.CreateInstance(owner, AnyMember, null, arguments, null)
        ?? throw new InvalidOperationException($"could not construct {owner.FullName}"));
  }

  extension(object target)
  {
    /// <summary>Calls an instance method and casts its result.</summary>
    internal T Invoke<T>(string name, params object?[] arguments)
      => (T)FindOverload(target.GetType(), name, arguments).Invoke(target, arguments)!;

    /// <summary>Calls an instance method and discards its result.</summary>
    internal void Invoke(string name, params object?[] arguments)
      => FindOverload(target.GetType(), name, arguments).Invoke(target, arguments);

    /// <summary>Reads an instance field.</summary>
    internal T GetFieldValue<T>(string name) => (T)FindField(target.GetType(), name).GetValue(target)!;

    /// <summary>Writes an instance field.</summary>
    internal void SetFieldValue(string name, object? value)
      => FindField(target.GetType(), name).SetValue(target, value);
  }

  extension(Assembly assembly)
  {
    /// <summary>Finds a type that is not visible outside its assembly.</summary>
    internal Type NonPublicType(string fullName)
      => assembly.GetType(fullName)
        ?? throw new InvalidOperationException(
          $"{fullName} no longer exists - update this test along with it.");
  }

  /// <summary>Picks the overload the arguments fit, so no call site has to spell out a signature.</summary>
  private static MethodInfo FindOverload(Type owner, string name, object?[] arguments)
  {
    List<MethodInfo> candidates = [];
    for (Type? type = owner; type is not null; type = type.BaseType)
    {
      candidates.AddRange(type.GetMethods(AnyMember)
        .Where(method => method.Name == name && method.GetParameters().Length == arguments.Length));
    }
    if (candidates.Count == 0)
    {
      throw Missing(owner, name);
    }
    if (candidates.Count > 1)
    {
      candidates = candidates.Where(method => Fits(method, arguments)).ToList();
    }
    return candidates.Count == 1
      ? candidates[0]
      : throw new InvalidOperationException(
        $"{owner.FullName}.{name} matches {candidates.Count} overloads - pass a signature instead.");
  }

  /// <summary>Tells whether every argument is assignable to the parameter in its place.</summary>
  private static bool Fits(MethodInfo method, object?[] arguments)
    => method.GetParameters().Zip(arguments, (parameter, argument) => argument is null
      ? !parameter.ParameterType.IsValueType || Nullable.GetUnderlyingType(parameter.ParameterType) is not null
      : parameter.ParameterType.IsInstanceOfType(argument)).All(fits => fits);

  /// <summary>Walks the base types, since a private member is not inherited.</summary>
  private static MethodInfo FindMethod(Type owner, string name, Type[] signature)
  {
    for (Type? type = owner; type is not null; type = type.BaseType)
    {
      // the three argument GetMethod overload with a signature only exists from net5.0 on
      MethodInfo? found = signature.Length == 0
        ? type.GetMethod(name, AnyMember)
        : type.GetMethod(name, AnyMember, null, signature, null);
      if (found is not null)
      {
        return found;
      }
    }
    throw Missing(owner, name);
  }

  /// <summary>Walks the base types, since a private member is not inherited.</summary>
  private static FieldInfo FindField(Type owner, string name)
  {
    for (Type? type = owner; type is not null; type = type.BaseType)
    {
      if (type.GetField(name, AnyMember) is FieldInfo found)
      {
        return found;
      }
    }
    throw Missing(owner, name);
  }

  private static InvalidOperationException Missing(Type owner, string name)
    => new($"{owner.FullName}.{name} no longer exists - update this test along with it.");
}
