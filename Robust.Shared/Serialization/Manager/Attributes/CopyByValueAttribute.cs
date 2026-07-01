using System;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
///     Marks a value type as safe to copy by direct assignment.
///     This attribute is not inherited.
/// </summary>
/// <remarks>
///     This is intended for immutable value-type structs who can be trivially copied by assignment.
///     Reference types should use <see cref="CopyByRefAttribute"/> instead.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Struct |
    AttributeTargets.Enum,
    Inherited = false)]
public sealed class CopyByValueAttribute : Attribute
{
}
