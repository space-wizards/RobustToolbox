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
using Robust.Roslyn.Shared;
using Document = Microsoft.CodeAnalysis.Document;

namespace Robust.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExplicitInterfaceAnalyzer : DiagnosticAnalyzer
    {
        public readonly SyntaxKind[] ExcludedModifiers =
        {
            SyntaxKind.VirtualKeyword,
            SyntaxKind.AbstractKeyword,
            SyntaxKind.OverrideKeyword
        };

        [SuppressMessage("ReSharper", "RS2008")] private static readonly DiagnosticDescriptor Rule = new(
            Diagnostics.IdExplicitInterface,
            "No explicit interface specified",
            "No explicit interface specified",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Make sure to specify the interface in your method-declaration.");

        private const string RequiresExplicitImplementationAttributeMetadataName =
            "Robust.Shared.Analyzers.RequiresExplicitImplementationAttribute";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var attrSymbol = compilationContext.Compilation.GetTypeByMetadataName(RequiresExplicitImplementationAttributeMetadataName);
                if (attrSymbol is null)
                    return;

                compilationContext.RegisterSymbolStartAction(symbolContext =>
                {
                    if (symbolContext.Symbol is not INamedTypeSymbol typeSymbol)
                        return;

                    var explicitInterfaceImplementations = GetExplicitInterfaceImplementations(typeSymbol, attrSymbol);
                    if (explicitInterfaceImplementations.Count == 0)
                        return;

                    symbolContext.RegisterSyntaxNodeAction(
                        nodeContext => AnalyzeNode(nodeContext, explicitInterfaceImplementations),
                        SyntaxKind.MethodDeclaration);
                    symbolContext.RegisterSyntaxNodeAction(
                        nodeContext => AnalyzeNode(nodeContext, explicitInterfaceImplementations),
                        SyntaxKind.PropertyDeclaration);
                }, SymbolKind.NamedType);
            });
        }

        private static HashSet<ISymbol> GetExplicitInterfaceImplementations(
            INamedTypeSymbol typeSymbol,
            INamedTypeSymbol attrSymbol)
        {
            var explicitInterfaceImplementations = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            foreach (var interfaceSymbol in typeSymbol.AllInterfaces)
            {
                if (!interfaceSymbol.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attrSymbol)))
                    continue;

                foreach (var member in interfaceSymbol.GetMembers())
                {
                    if (typeSymbol.FindImplementationForInterfaceMember(member) is { } implementation)
                        explicitInterfaceImplementations.Add(implementation);
                }
            }

            return explicitInterfaceImplementations;
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context, HashSet<ISymbol> explicitInterfaceImplementations)
        {
            ISymbol symbol;
            Location location;
            switch (context.Node)
            {
                //we already have a explicit interface specified, no need to check further
                case MethodDeclarationSyntax methodDecl when methodDecl.ExplicitInterfaceSpecifier != null || methodDecl.Modifiers.Any(m => ExcludedModifiers.Contains(m.Kind())):
                    return;
                case PropertyDeclarationSyntax propertyDecl when propertyDecl.ExplicitInterfaceSpecifier != null || propertyDecl.Modifiers.Any(m => ExcludedModifiers.Contains(m.Kind())):
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

            if (symbol != null && explicitInterfaceImplementations.Contains(symbol))
            {
                //we do not have an explicit interface specified. bad!
                var diagnostic = Diagnostic.Create(
                    Rule,
                    location);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
