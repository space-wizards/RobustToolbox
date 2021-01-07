using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Robust.Generators
{
    public class AutoDataClassRegistrationReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> Registrations = new List<ClassDeclarationSyntax>();
        public List<ClassDeclarationSyntax> CustomDataClassRegistrations = new List<ClassDeclarationSyntax>();
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (!(syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)) return;

            Registrations.AddRange(YamlFieldFinder.GetAutoDataRegistrations(classDeclarationSyntax));

            if (classDeclarationSyntax.AttributeLists.Any(al =>
                al.Attributes.Any(a => a.Name.ToString() == "CustomDataClass")))
            {
                CustomDataClassRegistrations.Add(classDeclarationSyntax);
            }
        }

        public class YamlFieldFinder : CSharpSyntaxWalker
        {
            private YamlFieldFinder() {}

            public static ClassDeclarationSyntax[] GetAutoDataRegistrations(ClassDeclarationSyntax classDeclarationSyntax)
            {
                var finder = new YamlFieldFinder();
                finder.Visit(classDeclarationSyntax);
                return finder.foundDataRegistrations.ToArray();
            }

            private HashSet<ClassDeclarationSyntax> foundDataRegistrations = new HashSet<ClassDeclarationSyntax>();

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                base.VisitClassDeclaration(node);
            }

            public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (node.AttributeLists.Any(al => al.Attributes.Any(a => a.Name.ToString() == "YamlField")))
                {
                    foundDataRegistrations.Add((ClassDeclarationSyntax)node.Parent);
                }
            }

            public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (node.AttributeLists.Any(al => al.Attributes.Any(a => a.Name.ToString() == "YamlField")))
                {
                    foundDataRegistrations.Add((ClassDeclarationSyntax)node.Parent);
                }
            }
        }
    }
}
