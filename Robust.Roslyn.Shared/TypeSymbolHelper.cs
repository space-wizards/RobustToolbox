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

    /// <summary>
    /// If <paramref name="type"/> is a Nullable{T}, returns the <see cref="ITypeSymbol"/> of the underlying type.
    /// Otherwise, returns <paramref name="type"/>.
    /// </summary>
    // Modified from https://www.meziantou.net/working-with-types-in-a-roslyn-analyzer.htm
    public static ITypeSymbol GetNullableUnderlyingTypeOrSelf(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return namedType.TypeArguments[0];
        }

        return type;
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
