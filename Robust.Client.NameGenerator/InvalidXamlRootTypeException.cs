using System;
using Microsoft.CodeAnalysis;

namespace Robust.Client.NameGenerator
{
    public class InvalidXamlRootTypeException : Exception
    {
        public readonly INamedTypeSymbol ExpectedType;
        public readonly INamedTypeSymbol ExpectedBaseType;
        public readonly INamedTypeSymbol Actual;

        public InvalidXamlRootTypeException(INamedTypeSymbol actual, INamedTypeSymbol expectedType, INamedTypeSymbol expectedBaseType)
        {
            Actual = actual;
            ExpectedType = expectedType;
            ExpectedBaseType = expectedBaseType;
        }
    }
}
