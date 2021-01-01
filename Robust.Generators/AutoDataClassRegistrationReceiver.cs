using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
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

            if(classDeclarationSyntax.AttributeLists.Any(al =>
                al.Attributes.Any(a => a.Name.ToString() == "AutoDataClass")))
            {
                Registrations.Add(classDeclarationSyntax);
            }

            if (classDeclarationSyntax.AttributeLists.Any(al =>
                al.Attributes.Any(a => a.Name.ToString() == "CustomDataClass")))
            {
                CustomDataClassRegistrations.Add(classDeclarationSyntax);
            }
        }
    }
}
