using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Robust.Analyzers;

public static class Utils
{
   public static bool InheritsFromOrEquals(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        foreach (var otherType in GetBaseTypesAndThis(type))
        {
            if (SymbolEqualityComparer.Default.Equals(otherType, baseType))
                return true;
        }

        return false;
    }

    public static IEnumerable<INamedTypeSymbol> GetBaseTypesAndThis(INamedTypeSymbol namedType)
    {
        var current = namedType;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }
}
