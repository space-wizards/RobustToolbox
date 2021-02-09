using System.Collections.Generic;
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

        public static IEnumerable<T> RemoveDuplicates<T>(this IEnumerable<T> enumerable)
        {
            var res = new List<T>();
            foreach (var variable in enumerable)
            {
                if(!res.Contains(variable))
                    res.Add(variable);
            }

            return res;
        }
    }
}
