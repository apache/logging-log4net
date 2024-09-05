#if NET462_OR_GREATER
global using Log4NetSerializableAttribute = System.SerializableAttribute;
global using ILog4NetSerializable = System.Runtime.Serialization.ISerializable;
#else
global using Log4NetSerializableAttribute = log4net.Core.EmptyAttribute;
global using ILog4NetSerializable = log4net.Core.IEmptyInterface;
using System;
#endif

namespace log4net.Core;
#if !NET462_OR_GREATER
/// <summary>
/// Empty Interface (as replacement for <see cref="System.Runtime.Serialization.ISerializable"/>)
/// </summary>
internal interface IEmptyInterface
{ }

/// <summary>
/// Empty Attribute (as replacement for <see cref="SerializableAttribute"/>)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
internal sealed class EmptyAttribute : Attribute
{ }
#endif