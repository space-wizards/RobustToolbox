using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Robust.Serialization.Generator;

public sealed record DataDefinition(ITypeSymbol Type, string GenericTypeName, List<DataField> Fields, bool HasHooks, bool InvalidFields);
