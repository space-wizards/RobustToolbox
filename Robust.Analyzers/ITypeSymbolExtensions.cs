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


    /// <summary>
    /// Returns the "primary" <see cref="Location"/> of a TypeSymbol.
    /// </summary>
    /// <remarks>
    /// Types using <see langword="partial"/> can have multiple <see cref="Location"/>s.
    /// This helper method considers the primary partial to be the one with the shortest filename.
    /// For example, MyCoolClass.cs is shorter than MyCoolClass.Subscriptions.cs.
    /// </remarks>
    public static Location? GetPrimaryLocation(this ITypeSymbol symbol)
    {
        if (symbol.Locations.Length == 0)
            return null;

        var minVal = symbol.Locations[0].SourceTree?.FilePath.Length ?? int.MaxValue;
        var minIndex = 0;
        for (var i = 1; i < symbol.Locations.Length; i++)
        {
            if (!symbol.Locations[i].IsInSource)
                continue;

            if (symbol.Locations[i].SourceTree!.FilePath.Length >= minVal)
                continue;

            minVal = symbol.Locations[i].SourceTree!.FilePath.Length;
            minIndex = i;
        }

        return symbol.Locations[minIndex];
    }
}
