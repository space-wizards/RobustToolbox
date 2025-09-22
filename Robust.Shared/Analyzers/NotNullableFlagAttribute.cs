using System;

#if ROBUST_ANALYZERS_IMPL
namespace Robust.Shared.Analyzers.Implementation;
#else
namespace Robust.Shared.Analyzers;
#endif

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.GenericParameter)]
public sealed class NotNullableFlagAttribute : Attribute
{
    public readonly string TypeParameterName;

    public NotNullableFlagAttribute(string typeParameterName)
    {
        TypeParameterName = typeParameterName;
    }
}
