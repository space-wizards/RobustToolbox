using Microsoft.CodeAnalysis;

namespace Robust.Serialization.Generator;

public sealed record DataField(
    ISymbol Symbol,
    ITypeSymbol Type,
    (INamedTypeSymbol Serializer, CustomSerializerType Type)? CustomSerializer);

public enum CustomSerializerType
{
    Copier,
    CopyCreator
}
