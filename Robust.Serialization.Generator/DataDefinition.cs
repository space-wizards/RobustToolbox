using Microsoft.CodeAnalysis;

using Robust.Roslyn.Shared;

namespace Robust.Serialization.Generator;

public sealed record DataDefinition(
    ITypeSymbol Type,
    string GenericTypeName,
    List<DataField> Fields,
    bool HasHooks,
    bool InvalidFields,
    bool IsRecord)
{
    private readonly Dictionary<ITypeSymbol, (bool Definition, bool Record)> _classificationCache =
        new(SymbolEqualityComparer.Default);

    private bool _firstDataDefinitionBaseTypeResolved;
    private ITypeSymbol? _firstDataDefinitionBaseType;

    internal bool IsDataDefinition(ITypeSymbol? type, out bool isDataRecord)
    {
        if (type == null)
        {
            isDataRecord = false;
            return false;
        }

        if (!_classificationCache.TryGetValue(type, out var result))
        {
            result = (DataDefinitionHelper.IsDataDefinition(type, out var record), record);
            _classificationCache.Add(type, result);
        }

        isDataRecord = result.Record;
        return result.Definition;
    }

    internal ITypeSymbol? GetFirstDataDefinitionBaseType()
    {
        if (_firstDataDefinitionBaseTypeResolved)
            return _firstDataDefinitionBaseType;

        _firstDataDefinitionBaseTypeResolved = true;
        var parent = Type;
        while ((parent = parent.BaseType) != null)
        {
            if (IsDataDefinition(parent, out _))
                return _firstDataDefinitionBaseType = parent;
        }

        return null;
    }
}
