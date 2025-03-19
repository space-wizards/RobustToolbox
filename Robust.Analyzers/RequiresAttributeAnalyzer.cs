#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RequiresAttributeAnalyzer : DiagnosticAnalyzer
{
    private const string RequiresAttributeNamespace = "Robust.Shared.Analyzers.RequiresAttributeAttribute";

    public static DiagnosticDescriptor RequiresAttributeMissingAttributeRule = new(
        Diagnostics.IdRequiresAttributeMissingAttribute,
        "Type of passed value is missing required attribute",
        "Type ({0}) of passed value is missing required attribute {1}",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure the type of the passed value has the required attribute."
    );

    public static DiagnosticDescriptor RequiresAttributeTypeArgMissingAttributeRule = new(
        Diagnostics.IdRequiresAttributeTypeArgMissingAttribute,
        "Type argument is missing required attribute",
        "Type argument {0} of passed value is type {1} which is missing required attribute {2}",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure the type of the passed value has the required attribute."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        RequiresAttributeMissingAttributeRule,
        RequiresAttributeTypeArgMissingAttributeRule,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeArgument, SyntaxKind.Argument);
    }

    private void AnalyzeArgument(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ArgumentSyntax argumentSyntax)
            return;

        if (context.SemanticModel.GetOperation(argumentSyntax) is not IArgumentOperation argumentOperation)
            return;

        if (argumentOperation.Parameter is not { } parameterSymbol)
            return;

        if (parameterSymbol.ContainingSymbol is not IMethodSymbol methodSymbol)
            return;

        if (methodSymbol.IsGenericMethod)
        {
            for (var i = 0; i < methodSymbol.TypeArguments.Length; ++i)
            {
                var reqs = GetRequiredAttributes(methodSymbol.OriginalDefinition.TypeArguments[i], context);
                var passed = methodSymbol.TypeArguments[i];

                if (!passed.IsSealed)
                    continue;

                foreach (var req in reqs)
                {
                    if (!HasAttribute(passed, req))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(RequiresAttributeTypeArgMissingAttributeRule,
                            argumentSyntax.Expression.GetLocation(),
                            methodSymbol.TypeParameters[i].Name,
                            passed.Name,
                            req.Name));
                    }
                }
            }
        }

        var requiredAttributes = GetRequiredAttributes(parameterSymbol, context);
        if (requiredAttributes.Count == 0)
            return;

        // Get the type of the actual value that was passed in
        if (context.SemanticModel.GetTypeInfo(argumentSyntax.Expression).Type is not INamedTypeSymbol passedType)
            return;

        // If the passed type isn't sealed, we assume this is a proxy method or similar.
        if (!passedType.IsSealed)
            return;

        foreach (var requiredAttribute in requiredAttributes)
        {
            if (!HasAttribute(passedType, requiredAttribute))
            {
                context.ReportDiagnostic(Diagnostic.Create(RequiresAttributeMissingAttributeRule,
                    argumentSyntax.Expression.GetLocation(),
                    passedType.Name,
                    requiredAttribute.Name));
            }
        }
    }

    private List<INamedTypeSymbol> GetRequiredAttributes(ISymbol symbol, SyntaxNodeAnalysisContext context)
    {
        var requiresAttributeAttribute = context.Compilation.GetTypeByMetadataName(RequiresAttributeNamespace);

        List<INamedTypeSymbol> requiredAttributes = [];
        foreach (var attribute in symbol.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, requiresAttributeAttribute))
                continue;

            if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol requiredAttribute)
                continue;

            requiredAttributes.Add(requiredAttribute);
        }

        return requiredAttributes;
    }

    private bool HasAttribute(ISymbol symbol, INamedTypeSymbol attribute)
    {
        foreach (var passedTypeAttribute in GetAllAttributes(symbol))
        {
            if (SymbolEqualityComparer.Default.Equals(passedTypeAttribute.AttributeClass, attribute))
                return true;
        }
        return false;
    }

    private List<AttributeData> GetAllAttributes(ISymbol? symbol)
    {
        List<AttributeData> attributes = symbol?.GetAttributes().ToList() ?? [];

        while (symbol is ITypeSymbol typeSymbol)
        {
            foreach (var attribute in typeSymbol.GetAttributes())
            {
                if (AttributeIsInherited(attribute))
                    attributes.Add(attribute);
            }
            symbol = typeSymbol.BaseType;
        }
        return attributes;
    }

    private bool AttributeIsInherited(AttributeData attribute)
    {
        if (attribute.AttributeClass == null)
            return false;

        foreach (var attributeAttribute in attribute.AttributeClass.GetAttributes())
        {
            var attributeClass = attributeAttribute.AttributeClass;
            if (attributeClass != null
                && attributeClass.Name == nameof(AttributeUsageAttribute)
                && attributeClass.ContainingNamespace?.Name == "System")
            {
                foreach (var namedArgument in attributeAttribute.NamedArguments)
                {
                    if (namedArgument.Key == nameof(AttributeUsageAttribute.Inherited))
                        return (bool) namedArgument.Value.Value!;
                }

                // Default value of Inherited is true
                return true;
            }
        }

        // No AttributeUsage means inherited
        return true;
    }

}
