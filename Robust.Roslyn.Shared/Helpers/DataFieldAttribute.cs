using Microsoft.CodeAnalysis;

namespace Robust.Roslyn.Shared.Helpers;

public sealed record DataFieldAttribute(
    AttributeData? Data,
    string Tag,
    bool ReadOnly,
    int Priority,
    bool Include,
    bool IsDataFieldAttribute,
    bool Required,
    bool ServerOnly,
    string CamelCasedName,
    int InheritanceBehavior = 0
);
