using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Robust.Serialization.Generator
{
    /// <summary>
    /// Taken from https://github.com/AvaloniaUI/Avalonia.NameGenerator/blob/ecc9677a23de5cbc90af07ccac14e31c0da41d6a/src/Avalonia.NameGenerator/NameReferenceSyntaxReceiver.cs
    /// </summary>
    internal class NameReferenceSyntaxReceiver : ISyntaxReceiver
    {
        public HashSet<TypeDeclarationSyntax> Types { get; } = new HashSet<TypeDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TypeDeclarationSyntax type)
                Types.Add(type);
        }
    }
}
