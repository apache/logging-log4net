#if !NET6_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Specifies that an output may be null even if the corresponding type disallows it.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
internal sealed class MaybeNullAttribute : Attribute
{ }
#endif