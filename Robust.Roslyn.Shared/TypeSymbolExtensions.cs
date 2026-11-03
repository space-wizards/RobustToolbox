using Microsoft.CodeAnalysis;

namespace Robust.Roslyn.Shared;

public static class TypeSymbolExtensions
{
    extension (ITypeSymbol symbol)
    {
        /// <summary>
        /// Checks if this type matches another type by name.
        /// </summary>
        /// <remarks>
        /// Avoids the expense of doing a proper type symbol comparison, but will return false positives for
        /// different symbols that have the same name.
        /// </remarks>
        /// <param name="other"></param>
        /// <returns><see langword="true"/> if this type matches <paramref name="other"/>, otherwise <see langword="false"/></returns>
        public bool ShittyTypeMatch(string other)
        {
            // Doing it like this only allocates when the type actually matches, which is good enough for me right now.
            if (!other.EndsWith(symbol.Name))
                return false;

            return symbol.ToDisplayString() == other;
        }

        /// <summary>
        /// Enumerates all base types of this type.
        /// </summary>
        public IEnumerable<ITypeSymbol> GetBaseTypes()
        {
            var baseType = symbol.BaseType;
            while (baseType != null)
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }

        /// <summary>
        /// Gets all members of this symbol, including those that are inherited.
        /// </summary>
        /// <remarks>
        /// We need this because sometimes Components have abstract parents with autonetworked datafields.
        /// </remarks>
        public IEnumerable<ISymbol> GetAllMembersIncludingInherited()
        {
            var current = symbol;
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
        /// Checks this type inherits from <paramref name="other"/>.
        /// </summary>
        /// <returns><see langword="true"/> if this type inherits from <paramref name="other"/>, otherwise <see langword="false"/>.</returns>
        public bool InheritsFrom(ITypeSymbol other)
        {
            foreach (var baseType in GetBaseTypes(symbol))
            {
                if (SymbolEqualityComparer.Default.Equals(baseType, other))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if this type inherits from a type with the given metadata name.
        /// </summary>
        /// <returns><see langword="true"/> if this type inherits from <paramref name="otherTypeName"/>, otherwise <see langword="false"/>.</returns>
        public bool InheritsFrom(string otherTypeName)
        {
            foreach (var baseType in GetBaseTypes(symbol))
            {
                if (ShittyTypeMatch(baseType, otherTypeName))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if this type implements a specified interface.
        /// </summary>
        /// <returns><see langword="true"/> if this type implements <paramref name="interfaceType"/>, otherwise <see langword="false"/>.</returns>
        public bool ImplementsInterface(INamedTypeSymbol interfaceType)
        {
            foreach (var @interface in symbol.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(@interface, interfaceType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if this type implements an interface with the specified name.
        /// </summary>
        /// <returns><see langword="true"/> if this type implements <paramref name="interfaceType"/>, otherwise <see langword="false"/>.</returns>
        public bool ImplementsInterface(string interfaceTypeName)
        {
            foreach (var interfaceType in symbol.AllInterfaces)
            {
                if (ShittyTypeMatch(interfaceType, interfaceTypeName))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// If this type is a Nullable{T}, returns the <see cref="ITypeSymbol"/> of the underlying type.
        /// Otherwise, returns itself.
        /// </summary>
        /// <remarks>
        /// Modified from https://www.meziantou.net/working-with-types-in-a-roslyn-analyzer.htm
        /// </remarks>
        public ITypeSymbol GetNullableUnderlyingTypeOrSelf()
        {
            if (symbol is INamedTypeSymbol namedType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            {
                return namedType.TypeArguments[0];
            }

            return symbol;
        }
    }
}
