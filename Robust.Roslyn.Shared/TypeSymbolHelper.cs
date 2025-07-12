using Microsoft.CodeAnalysis;

namespace Robust.Roslyn.Shared;

#nullable enable

public static class TypeSymbolHelper
{
    public static bool ShittyTypeMatch(ITypeSymbol type, string attributeMetadataName)
    {
        // Doing it like this only allocates when the type actually matches, which is good enough for me right now.
        if (!attributeMetadataName.EndsWith(type.Name))
            return false;

        return type.ToDisplayString() == attributeMetadataName;
    }

    public static bool ImplementsInterface(ITypeSymbol type, string interfaceTypeName)
    {
        foreach (var interfaceType in type.AllInterfaces)
        {
            if (ShittyTypeMatch(interfaceType, interfaceTypeName))
                return true;
        }

        return false;
    }

    public static bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol interfaceType)
    {
        foreach (var @interface in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(@interface, interfaceType))
                return true;
        }

        return false;
    }

    public static IEnumerable<ITypeSymbol> GetBaseTypes(ITypeSymbol type)
    {
        var baseType = type.BaseType;
        while (baseType != null)
        {
            yield return baseType;
            baseType = baseType.BaseType;
        }
    }

    public static bool Inherits(ITypeSymbol type, ITypeSymbol other)
    {
        foreach (var baseType in GetBaseTypes(type))
        {
            if (SymbolEqualityComparer.Default.Equals(baseType, other))
                return true;
        }
        return false;
    }
}
