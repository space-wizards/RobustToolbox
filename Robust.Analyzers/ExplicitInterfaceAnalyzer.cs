using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Document = Microsoft.CodeAnalysis.Document;

namespace Robust.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExplicitInterfaceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RA0000";

        private const string Title = "No explicit interface specified";
        private const string MessageFormat = "No explicit interface specified";
        private const string Description = "Make sure to specify the interface in your method-declaration.";
        private const string Category = "Usage";

        [SuppressMessage("ReSharper", "RS2008")] private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        private const string RequiresExplicitImplementationAttributeMetadataName =
            "Robust.Shared.Analyzers.RequiresExplicitImplementationAttribute";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.PropertyDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            ISymbol symbol;
            Location location;
            switch (context.Node)
            {
                //we already have a explicit interface specified, no need to check further
                case MethodDeclarationSyntax methodDecl when methodDecl.ExplicitInterfaceSpecifier != null:
                    return;
                case PropertyDeclarationSyntax propertyDecl when propertyDecl.ExplicitInterfaceSpecifier != null:
                    return;

                case MethodDeclarationSyntax methodDecl:
                    symbol = context.SemanticModel.GetDeclaredSymbol(methodDecl);
                    location = methodDecl.Identifier.GetLocation();
                    break;
                case PropertyDeclarationSyntax propertyDecl:
                    symbol = context.SemanticModel.GetDeclaredSymbol(propertyDecl);
                    location = propertyDecl.Identifier.GetLocation();
                    break;

                default:
                    return;
            }

            var attrSymbol = context.Compilation.GetTypeByMetadataName(RequiresExplicitImplementationAttributeMetadataName);

            var isInterfaceMember = symbol?.ContainingType.AllInterfaces.Any(
                i =>
                    i.GetMembers().Any(m => SymbolEqualityComparer.Default.Equals(symbol, symbol.ContainingType.FindImplementationForInterfaceMember(m)))
                    && i.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attrSymbol))
            ) ?? false;

            if (isInterfaceMember)
            {
                //we do not have an explicit interface specified but are an interface/property. bad!
                var diagnostic = Diagnostic.Create(
                    Rule,
                    location);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
