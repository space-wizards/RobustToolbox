using Microsoft.CodeAnalysis;

namespace Robust.Serialization.Generator;

public sealed record DataFieldAttribute(AttributeData Data, string Name, int Priority, bool Include);
