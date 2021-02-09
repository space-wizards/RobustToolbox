using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Robust.Generators
{
    public class AutoDataClassRegistrationReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> AllClasses = new List<ClassDeclarationSyntax>();
        public List<ClassDeclarationSyntax> CustomDataClassRegistrations = new List<ClassDeclarationSyntax>();
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (!(syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)) return;

            AllClasses.Add(classDeclarationSyntax);

            CustomDataClassRegistrations.AddRange(ExplicitRegFinder.GetAutoDataRegistrations(classDeclarationSyntax));
        }

        public class ExplicitRegFinder : CSharpSyntaxWalker
        {
            private ExplicitRegFinder() {}

            public static ClassDeclarationSyntax[] GetAutoDataRegistrations(ClassDeclarationSyntax classDeclarationSyntax)
            {
                var finder = new ExplicitRegFinder();
                finder.Visit(classDeclarationSyntax);
                return finder.foundDataRegistrations.ToArray();
            }

            private HashSet<ClassDeclarationSyntax> foundDataRegistrations = new HashSet<ClassDeclarationSyntax>();

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                base.VisitClassDeclaration(node);
                if (node.AttributeLists.Any(al =>
                    al.Attributes.Any(a => a.Name.ToString() == "DataClass")))
                {
                    foundDataRegistrations.Add(node);
                }
            }
        }
    }
}
