#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataDefinitionAnalyzer : DiagnosticAnalyzer
{
    private const string DataDefinitionNamespace = "Robust.Shared.Serialization.Manager.Attributes.DataDefinitionAttribute";
    private const string ImplicitDataDefinitionNamespace = "Robust.Shared.Serialization.Manager.Attributes.ImplicitDataDefinitionForInheritorsAttribute";

    private static readonly DiagnosticDescriptor DataDefinitionPartialRule = new(
        Diagnostics.IdDataDefinitionPartial,
        "Type must be partial",
        "Type {0} is a DataDefinition but is not partial.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure to mark any type that is a data definition as partial."
    );

    private static readonly DiagnosticDescriptor NestedDataDefinitionPartialRule = new(
        Diagnostics.IdNestedDataDefinitionPartial,
        "Type must be partial",
        "Type {0} contains nested data definition {1} but is not partial.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure to mark any type containing a nested data definition as partial."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DataDefinitionPartialRule, NestedDataDefinitionPartialRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.StructDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.RecordDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.RecordStructDeclaration);
    }

    private void AnalyzeType(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not TypeDeclarationSyntax declaration)
            return;

        var type = context.SemanticModel.GetDeclaredSymbol(declaration)!;
        if (!IsDataDefinition(type))
            return;

        if (!IsPartial(declaration))
        {
            context.ReportDiagnostic(Diagnostic.Create(DataDefinitionPartialRule, declaration.Keyword.GetLocation(), type.Name));
        }

        var containingType = type.ContainingType;
        while (containingType != null)
        {
            if (!IsPartial(declaration))
            {
                var syntax = (ClassDeclarationSyntax) containingType.DeclaringSyntaxReferences[0].GetSyntax();
                context.ReportDiagnostic(Diagnostic.Create(NestedDataDefinitionPartialRule, syntax.Keyword.GetLocation(), containingType.Name, type.Name));
            }

            containingType = containingType.ContainingType;
        }
    }

    private static bool IsPartial(TypeDeclarationSyntax type)
    {
        return type.Modifiers.IndexOf(SyntaxKind.PartialKeyword) != -1;
    }

    private static bool IsDataDefinition(ITypeSymbol? type)
    {
        if (type == null)
            return false;

        return HasAttribute(type, DataDefinitionNamespace) ||
               IsImplicitDataDefinition(type);
    }

    private static bool HasAttribute(ITypeSymbol type, string attributeName)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == attributeName)
                return true;
        }

        return false;
    }

    private static bool IsImplicitDataDefinition(ITypeSymbol type)
    {
        if (HasAttribute(type, ImplicitDataDefinitionNamespace))
            return true;

        foreach (var baseType in GetBaseTypes(type))
        {
            if (HasAttribute(baseType, ImplicitDataDefinitionNamespace))
                return true;
        }

        foreach (var @interface in type.AllInterfaces)
        {
            if (IsImplicitDataDefinitionInterface(@interface))
                return true;
        }

        return false;
    }

    private static bool IsImplicitDataDefinitionInterface(ITypeSymbol @interface)
    {
        if (HasAttribute(@interface, ImplicitDataDefinitionNamespace))
            return true;

        foreach (var subInterface in @interface.AllInterfaces)
        {
            if (HasAttribute(subInterface, ImplicitDataDefinitionNamespace))
                return true;
        }

        return false;
    }

    private static IEnumerable<ITypeSymbol> GetBaseTypes(ITypeSymbol type)
    {
        var baseType = type.BaseType;
        while (baseType != null)
        {
            yield return baseType;
            baseType = baseType.BaseType;
        }
    }
}
