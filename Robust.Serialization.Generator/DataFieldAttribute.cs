using Microsoft.CodeAnalysis;

namespace Robust.Serialization.Generator;

public sealed record DataFieldAttribute(
    AttributeData Data,
    string Tag,
    bool ReadOnly,
    int Priority,
    bool Include,
    bool IsDataFieldAttribute,
    bool Required,
    bool ServerOnly
);
