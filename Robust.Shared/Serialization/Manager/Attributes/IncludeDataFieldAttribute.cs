using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
/// Inlines the datafield instead of putting it into its own node.
/// </summary>
/// <remarks>
/// mapping:
///   data1: 0
///   data2: 0
/// Becomes
/// data1: 0
/// data2: 0
/// </remarks>
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
