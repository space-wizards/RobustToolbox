using Microsoft.CodeAnalysis;

namespace Robust.Serialization.Generator;

public sealed record DataField(ISymbol Field, AttributeData Attribute);
