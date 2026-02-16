using Microsoft.CodeAnalysis;

namespace Robust.Serialization.Generator;

public sealed record DataField(
    ISymbol Symbol,
    ITypeSymbol Type,
    DataFieldAttribute Attribute,
    (INamedTypeSymbol Serializer, CustomSerializerType Type)? CustomSerializer
);

[Flags]
public enum CustomSerializerType
{
    None = 0,
    Copier = 1 << 0,
    CopyCreator = 1 << 1,
    MappingValidator = 1 << 2,
    SequenceValidator = 1 << 3,
    ValueValidator = 1 << 4,
    MappingReader = 1 << 5,
    SequenceReader = 1 << 6,
    ValueReader = 1 << 7,
}
