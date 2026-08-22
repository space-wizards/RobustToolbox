using Microsoft.CodeAnalysis;

namespace Robust.Analyzers;

public static  class ITypeSymbolExtensions
{
    public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }
}
