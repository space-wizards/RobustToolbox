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

    /// <summary>
    /// Gets all Members of a symbol, including those that are inherited.
    /// We need this because sometimes Components have abstract parents with autonetworked datafields.
    /// </summary>
    public static IEnumerable<ISymbol> GetAllMembersIncludingInherited(INamedTypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            foreach (var member in current.GetMembers())
            {
                yield return member;
            }

            current = current.BaseType;
        }
    }
}
