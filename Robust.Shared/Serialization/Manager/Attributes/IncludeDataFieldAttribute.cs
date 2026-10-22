using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
///     Marks a field or property as being serializable/deserializable, including all of the fields from its type into
///     the current data definition. Does not create a named field of its own, and should be used sparingly for
///     conciseness and readability only.
/// </summary>
/// <remarks>
///     This should never be used in a way where the included/collapsed fields conflict in name with other fields!
/// </remarks>
/// <example>
///     <code>
///       otherField: 42
///       myField:
///         subfield1: foo
///         subfield2: bar
///     </code>
///     becomes
///     <code>
///       otherField: 42
///       subfield1: foo
///       subfield2: bar
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
[MeansImplicitAssignment]
[MeansImplicitUse(ImplicitUseKindFlags.Assign)]
public sealed class IncludeDataFieldAttribute : DataFieldBaseAttribute
{
    /// <param name="readOnly">See <see cref="DataFieldBaseAttribute.ReadOnly"/>.</param>
    /// <param name="priority">See <see cref="DataFieldBaseAttribute.Priority"/>.</param>
    /// <param name="serverOnly">See <see cref="DataFieldBaseAttribute.ServerOnly"/>.</param>
    /// <param name="customTypeSerializer">See <see cref="DataFieldBaseAttribute.CustomTypeSerializer"/>.</param>
    public IncludeDataFieldAttribute(
            bool readOnly = false,
            int priority = 1,
            bool serverOnly = false,
            Type? customTypeSerializer = null
        ) : base(readOnly, priority, serverOnly, customTypeSerializer)
    {
    }

    public override string ToString()
    {
        return "[INCLUDE]";
    }
}
