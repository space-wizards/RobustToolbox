using System;

namespace Robust.Shared.Serialization.Manager.Definition;

[Obsolete("Only used in serialization source generators")]
public readonly record struct DataFieldDefinition(
    string? Tag,
    int Priority,
    bool IsDataField,
    bool IsIncludeDataField,
    object? DefaultValue,
    InheritanceBehavior InheritanceBehavior,
    string FieldInfoName,
    Type FieldType,
    bool FieldInfoNullable,
    string CamelCasedName,
    Type? CustomTypeSerializer
)
{
    public override string ToString()
    {
        return $"{FieldInfoName}({Tag ?? CamelCasedName})";
    }
}
