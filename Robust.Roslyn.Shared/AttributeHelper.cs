using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Robust.Roslyn.Shared;

#nullable enable

public static class AttributeHelper
{
    public static bool HasAttribute(ISymbol symbol, string attributeMetadataName, [NotNullWhen(true)] out AttributeData? matchedAttribute)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass == null)
                continue;

            if (TypeSymbolHelper.ShittyTypeMatch(attribute.AttributeClass, attributeMetadataName))
            {
                matchedAttribute = attribute;
                return true;
            }
        }

        matchedAttribute = null;
        return false;
    }

    public static bool GetNamedArgumentBool(AttributeData data, string name, bool defaultValue)
    {
        foreach (var kv in data.NamedArguments)
        {
            if (kv.Key != name)
                continue;

            if (kv.Value.Kind != TypedConstantKind.Primitive)
                continue;

            if (kv.Value.Value is not bool value)
                continue;

            return value;
        }

        return defaultValue;
    }
}
