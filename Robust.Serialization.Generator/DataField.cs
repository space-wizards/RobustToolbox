using Microsoft.CodeAnalysis;

namespace Robust.Serialization.Generator;

public sealed record DataField(ISymbol Symbol, ITypeSymbol Type, AttributeData Attribute);
