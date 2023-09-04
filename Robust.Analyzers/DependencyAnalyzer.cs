using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DependencyAnalyzer : DiagnosticAnalyzer
{
    private const string DependencyAttributeName = "Robust.Shared.IoC.DependencyAttribute";
    private const string InjectDependenciesAttributeName = "Robust.Shared.IoC.InjectDependenciesAttribute";

    private static readonly DiagnosticDescriptor DependencyPartialRule = new(
        Diagnostics.IdDependencyNotPartial,
        "Type must be partial",
        "Type {0} has [Dependency] fields but is not partial",
        "Usage",
        DiagnosticSeverity.Error,
        true
    );

    private static readonly DiagnosticDescriptor DependencyNoInjectDependenciesAttributeRule = new(
        Diagnostics.IdDependencyNoInjectDependenciesAttribute,
        "Type misses [InjectDependencies]",
        "Type {0} has [Dependency] fields but is not marked as [InjectDependencies]",
        "Usage",
        DiagnosticSeverity.Error,
        true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DependencyPartialRule, DependencyNoInjectDependenciesAttributeRule
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeDependencyOwner, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeDependencyOwner(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not TypeDeclarationSyntax declaration)
            return;

        var depAttribute = context.Compilation.GetTypeByMetadataName(DependencyAttributeName);
        var injDepAttribute = context.Compilation.GetTypeByMetadataName(InjectDependenciesAttributeName);
        if (depAttribute == null || injDepAttribute == null)
            return;

        var type = context.SemanticModel.GetDeclaredSymbol(declaration)!;
        if (!IsDependencyOwner(type, depAttribute))
            return;

        if (!HasAttribute(type, injDepAttribute))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DependencyNoInjectDependenciesAttributeRule,
                declaration.Identifier.GetLocation(),
                type.Name));
        }

        if (!DataDefinitionAnalyzer.IsPartial(declaration))
        {
            context.ReportDiagnostic(Diagnostic.Create(DependencyPartialRule, declaration.Keyword.GetLocation(), type.Name));
        }

        var containingType = type.ContainingType;
        while (containingType != null)
        {
            var containingTypeDeclaration = (TypeDeclarationSyntax) containingType.DeclaringSyntaxReferences[0].GetSyntax();
            if (!DataDefinitionAnalyzer.IsPartial(containingTypeDeclaration))
            {
                context.ReportDiagnostic(Diagnostic.Create(DependencyPartialRule, declaration.Keyword.GetLocation(), type.Name));
                break;
            }

            containingType = containingType.ContainingType;
        }
    }

    private static bool IsDependencyOwner(INamedTypeSymbol type, INamedTypeSymbol depAttribute)
    {
        foreach (var member in type.GetMembers())
        {
            if (member is not IFieldSymbol or IPropertySymbol)
                continue;

            if (HasAttribute(member, depAttribute))
                return true;
        }

        return false;
    }

    private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attributeType)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                return true;
        }

        return false;
    }

}
