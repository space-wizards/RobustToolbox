using System;

#if ROBUST_ANALYZERS_IMPL
namespace Robust.Shared.Analyzers.Implementation;
#else
namespace Robust.Shared.Analyzers;
#endif

/// <summary>
/// Marks that use of a generic Type should be replaced with a specific other Type
/// when the type argument T is a certain Type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class PreferOtherTypeAttribute(Type genericType, Type replacementType) : Attribute
{
    public readonly Type GenericArgument = genericType;
    public readonly Type ReplacementType = replacementType;
}
