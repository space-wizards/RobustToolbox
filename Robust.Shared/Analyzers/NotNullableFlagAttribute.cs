using System;

#if ROBUST_ANALYZERS_IMPL
namespace Robust.Shared.Analyzers.Implementation;
#else
namespace Robust.Shared.Analyzers;
#endif

/// <summary>
///     Indicates the given boolean field must be set to true when the provided type parameter is not nullable.
///     An analyzer then enforces this as an error.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.GenericParameter)]
public sealed class NotNullableFlagAttribute : Attribute
{
    public readonly string TypeParameterName;

    public NotNullableFlagAttribute(string typeParameterName)
    {
        TypeParameterName = typeParameterName;
    }
}
