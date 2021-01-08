using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Robust.Generators
{
    public class DeepCloneCandidates : ISyntaxReceiver
    {
        public readonly List<ClassDeclarationSyntax> Candidates = new List<ClassDeclarationSyntax>();
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
            {
                Candidates.Add(classDeclarationSyntax);
            }
        }
    }
}
