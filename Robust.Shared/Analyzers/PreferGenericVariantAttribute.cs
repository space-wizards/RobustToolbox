using System;

#if ROBUST_ANALYZERS_IMPL
namespace Robust.Shared.Analyzers.Implementation;
#else
namespace Robust.Shared.Analyzers;
#endif

[AttributeUsage(AttributeTargets.Method)]
public sealed class PreferGenericVariantAttribute : Attribute
{
    public readonly string GenericVariant;

    public PreferGenericVariantAttribute(string genericVariant = null!)
    {
        GenericVariant = genericVariant;
    }
}
