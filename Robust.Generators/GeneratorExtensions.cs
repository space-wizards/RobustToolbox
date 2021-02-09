using Microsoft.CodeAnalysis;
using System.Linq;

namespace Robust.Generators
{
    public static class GeneratorExtensions
    {
        public static AttributeData GetAttribute(this ISymbol symbol, INamedTypeSymbol attributeSymbol)
        {
            return symbol.GetAttributes()
                .FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol));
        }

        public static bool AssignableFrom(this INamedTypeSymbol symbol, INamedTypeSymbol other)
        {
            var currentSymbol = other;
            while (currentSymbol != null)
            {
                if (SymbolEqualityComparer.Default.Equals(currentSymbol, symbol)) return true;
                currentSymbol = currentSymbol.BaseType;
            }

            return false;
        }
    }
}
