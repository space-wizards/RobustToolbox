using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Robust.Client.NameGenerator
{
    /// <summary>
    /// Taken from https://github.com/AvaloniaUI/Avalonia.NameGenerator/blob/ecc9677a23de5cbc90af07ccac14e31c0da41d6a/src/Avalonia.NameGenerator/NameReferenceSyntaxReceiver.cs
    /// </summary>
    internal class NameReferenceSyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax &&
                classDeclarationSyntax.AttributeLists.Count > 0)
                CandidateClasses.Add(classDeclarationSyntax);
        }
    }
}
