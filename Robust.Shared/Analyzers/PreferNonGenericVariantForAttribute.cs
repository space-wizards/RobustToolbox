using System;

#if ROBUST_ANALYZERS_IMPL
namespace Robust.Shared.Analyzers.Implementation;
#else
namespace Robust.Shared.Analyzers;
#endif

[AttributeUsage(AttributeTargets.Method)]
public sealed class PreferNonGenericVariantForAttribute : Attribute
{
    public readonly Type[] ForTypes;

    public PreferNonGenericVariantForAttribute(params Type[] forTypes)
    {
        ForTypes = forTypes;
    }
}
