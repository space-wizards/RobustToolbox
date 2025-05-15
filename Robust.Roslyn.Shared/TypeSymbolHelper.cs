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
}
