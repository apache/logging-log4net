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

using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Util;

/// <summary>
/// Guards the annotations that keep log4net working when a consumer publishes with
/// <c>PublishAot</c> or <c>PublishTrimmed</c>.
/// </summary>
/// <remarks>
/// <para>
/// Dropping one of these annotations breaks nothing in an ordinary build: the failure only shows up
/// as a <see cref="MissingMethodException"/> inside a consumer's published application. These tests
/// fail on the spot instead. The attribute is matched by full name, because log4net polyfills it as
/// an internal type, which is also how the trimmer recognises it.
/// </para>
/// </remarks>
[TestFixture]
public class AotCompatibilityTest
{
  private const string DynamicallyAccessedMembers
    = "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute";

  /// <summary>
  /// The public entry point a caller passes a repository type to.
  /// </summary>
  [Test]
  public void LogManagerCreateRepositoryAnnotatesTheRepositoryType()
    => AssertAnnotated(Parameter(typeof(LogManager), nameof(LogManager.CreateRepository), typeof(Type)));

  /// <summary>
  /// The step between <see cref="LogManager"/> and the repository selector.
  /// </summary>
  [Test]
  public void LoggerManagerCreateRepositoryAnnotatesTheRepositoryType()
    => AssertAnnotated(Parameter(typeof(LoggerManager), nameof(LoggerManager.CreateRepository),
      typeof(Assembly), typeof(Type)));

  /// <summary>
  /// The interface, so a custom selector keeps the annotation too.
  /// </summary>
  [Test]
  public void RepositorySelectorCreateRepositoryAnnotatesTheRepositoryType()
    => AssertAnnotated(Parameter(typeof(IRepositorySelector), nameof(IRepositorySelector.CreateRepository),
      typeof(Assembly), typeof(Type)));

  /// <summary>
  /// Where <c>typeof(Hierarchy)</c> enters, and the default repository is created from.
  /// </summary>
  [Test]
  public void DefaultRepositorySelectorAnnotatesItsDefaultRepositoryType()
  {
    ConstructorInfo constructor = typeof(DefaultRepositorySelector).GetConstructor([typeof(Type)])
      ?? throw new InvalidOperationException("DefaultRepositorySelector(Type) no longer exists.");
    AssertAnnotated(constructor.GetParameters()[0]);
  }

  /// <summary>
  /// The assembly attribute that lets an application choose its own repository type.
  /// </summary>
  [Test]
  public void RepositoryAttributeAnnotatesItsRepositoryType()
    => AssertAnnotated(typeof(RepositoryAttribute).GetProperty(nameof(RepositoryAttribute.RepositoryType))
      ?? throw new InvalidOperationException("RepositoryAttribute.RepositoryType no longer exists."));

  /// <summary>
  /// What carries a converter type through the registries, where a bare <see cref="Type"/> would lose the annotation.
  /// </summary>
  [Test]
  public void ConverterInfoAnnotatesItsType()
    => AssertAnnotated(typeof(ConverterInfo).GetProperty(nameof(ConverterInfo.Type))
      ?? throw new InvalidOperationException("ConverterInfo.Type no longer exists."));

  /// <summary>
  /// The built-in converters are only ever created reflectively, so a lost constructor would
  /// surface only once a consumer trims their application.
  /// </summary>
  [Test]
  public void BuiltInPatternLayoutConvertersAreConstructible()
    => AssertConvertersAreConstructible(typeof(PatternLayout));

  /// <summary>
  /// The same guarantee for the <see cref="PatternString"/> registry.
  /// </summary>
  [Test]
  public void BuiltInPatternStringConvertersAreConstructible()
    => AssertConvertersAreConstructible(typeof(PatternString));

  /// <summary>
  /// Every registered name really produces a converter rather than the error a missing
  /// constructor would give.
  /// </summary>
  [Test]
  public void EveryBuiltInPatternLayoutConverterCanBeCreated()
  {
    foreach (string name in GlobalRules(typeof(PatternLayout)).Keys)
    {
      PatternLayout layout = new($"%{name}");
      Assert.DoesNotThrow(layout.ActivateOptions, $"converter [{name}] could not be activated");
    }
  }

  private static void AssertConvertersAreConstructible(Type owner)
  {
    Dictionary<string, ConverterInfo> rules = GlobalRules(owner);
    Assert.That(rules, Is.Not.Empty);

    foreach (KeyValuePair<string, ConverterInfo> rule in rules)
    {
      Type converter = rule.Value.Type
        ?? throw new InvalidOperationException($"converter [{rule.Key}] has no type");
      Assert.That(converter.GetConstructor(Type.EmptyTypes), Is.Not.Null,
        $"converter [{rule.Key}] ({converter.FullName}) has no public parameterless constructor");
    }
  }

  private static Dictionary<string, ConverterInfo> GlobalRules(Type owner)
  {
    FieldInfo field = owner.GetField("_sGlobalRulesRegistry", BindingFlags.Static | BindingFlags.NonPublic)
      ?? throw new InvalidOperationException($"{owner.Name}._sGlobalRulesRegistry no longer exists - update this test along with it.");
    return (Dictionary<string, ConverterInfo>)field.GetValue(null)!;
  }

  private static ParameterInfo Parameter(Type owner, string methodName, params Type[] signature)
  {
    MethodInfo method = owner.GetMethod(methodName, signature)
      ?? throw new InvalidOperationException($"{owner.Name}.{methodName} no longer has the expected signature.");
    return method.GetParameters().Single(p => p.ParameterType == typeof(Type));
  }

  private static void AssertAnnotated(ParameterInfo parameter)
    => Assert.That(IsAnnotated(parameter.GetCustomAttributesData()), Is.True,
      $"parameter [{parameter.Name}] of [{parameter.Member}] lost its {DynamicallyAccessedMembers}");

  private static void AssertAnnotated(MemberInfo member)
    => Assert.That(IsAnnotated(member.GetCustomAttributesData()), Is.True,
      $"[{member.DeclaringType?.Name}.{member.Name}] lost its {DynamicallyAccessedMembers}");

  private static bool IsAnnotated(IEnumerable<CustomAttributeData> attributes)
    => attributes.Any(attribute => attribute.AttributeType.FullName == DynamicallyAccessedMembers);
}
