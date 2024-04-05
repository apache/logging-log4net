#if !NET6_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Specifies that when a method returns System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute.ReturnValue,
/// the parameter may be null even if the corresponding type disallows it.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class MaybeNullWhenAttribute : Attribute
{
  /// <summary>
  /// Initializes the attribute with the specified return value condition.
  /// </summary>
  /// <param name="returnValue">The return value condition. If the method returns this value, the associated parameter may be null.</param>
  public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

  /// <summary>
  /// Gets the return value condition.
  /// </summary>
  public bool ReturnValue { get; }
}
#endif