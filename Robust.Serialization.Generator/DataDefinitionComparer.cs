using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Robust.Serialization.Generator;

public sealed class DataDefinitionComparer : IEqualityComparer<TypeDeclarationSyntax>
{
    public bool Equals(TypeDeclarationSyntax x, TypeDeclarationSyntax y)
    {
        return x.Equals(y);
    }

    public int GetHashCode(TypeDeclarationSyntax type)
    {
        return type.GetHashCode();
    }
}
