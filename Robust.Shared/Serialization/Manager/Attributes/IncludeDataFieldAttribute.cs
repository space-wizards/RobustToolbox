using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
[MeansImplicitAssignment]
[MeansImplicitUse(ImplicitUseKindFlags.Assign)]
public sealed class IncludeDataFieldAttribute : DataFieldBaseAttribute
{
    public IncludeDataFieldAttribute(bool readOnly = false, int priority = 1, bool serverOnly = false,
        Type? customTypeSerializer = null) : base(readOnly, priority, serverOnly, customTypeSerializer)
    {
    }

    public override string ToString()
    {
        return "[INCLUDE]";
    }
}
