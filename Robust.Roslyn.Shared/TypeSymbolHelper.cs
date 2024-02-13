using Microsoft.CodeAnalysis;

namespace Robust.Roslyn.Shared;

#nullable enable

public static class TypeSymbolHelper
{
    public static bool ShittyTypeMatch(INamedTypeSymbol type, string attributeMetadataName)
    {
        // Doing it like this only allocates when the type actually matches, which is good enough for me right now.
        if (!attributeMetadataName.EndsWith(type.Name))
            return false;

        return type.ToDisplayString() == attributeMetadataName;
    }

    public static bool ImplementsInterface(INamedTypeSymbol type, string interfaceTypeName)
    {
        foreach (var interfaceType in type.AllInterfaces)
        {
            if (ShittyTypeMatch(interfaceType, interfaceTypeName))
                return true;
        }

        return false;
    }
}
