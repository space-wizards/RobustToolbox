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
public sealed class ValidateMemberAnalyzer : DiagnosticAnalyzer
{
    private const string ValidateMemberType = "Robust.Shared.Analyzers.ValidateMemberAttribute";

    private static readonly DiagnosticDescriptor ValidateMemberDescriptor = new(
        Diagnostics.IdValidateMember,
        "Invalid member name",
        "{0} is not a member of {1}",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Be sure the type and member name are correct.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ValidateMemberDescriptor];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeExpression(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax node)
            return;

        if (context.SemanticModel.GetSymbolInfo(node.Expression).Symbol is not IMethodSymbol methodSymbol)
            return;

        // We need at least one type argument for context
        if (methodSymbol.TypeArguments.Length < 1)
            return;

        // We'll be checking members of the first type argument
        if (methodSymbol.TypeArguments[0] is not INamedTypeSymbol targetType)
            return;

        // Lookup the ValidateMemberAttribute symbol
        if (context.Compilation.GetTypeByMetadataName(ValidateMemberType) is not { } validateMemberAttribute)
            return;

        // We defer building this set until we need it later, so we don't have to build it for every single method invocation!
        ImmutableHashSet<ISymbol>? members = null;

        // Check each parameter of the method
        foreach (var parameterContext in node.ArgumentList.Arguments)
        {

            // Get the symbol for this parameter
            if (context.SemanticModel.GetOperation(parameterContext) is not IArgumentOperation op || op.Parameter is null)
                continue;
            var parameterSymbol = op.Parameter.OriginalDefinition;

            // Make sure the parameter has the ValidateMember attribute
            if (!HasAttribute(parameterSymbol, validateMemberAttribute))
                continue;

            // Find the value passed for this parameter.
            // We use GetConstantValue to resolve compile-time values - i.e. the result of nameof()
            if (context.SemanticModel.GetConstantValue(parameterContext.Expression).Value is not string fieldName)
                continue;

            // Get a set containing all the members of the target type and its ancestors
            members ??= targetType.GetBaseTypesAndThis().SelectMany(n => n.GetMembers()).ToImmutableHashSet(SymbolEqualityComparer.Default);

            // Check each member of the target type to see if it matches our passed in value
            var found = false;
            foreach (var member in members)
            {
                if (member.Name == fieldName)
                {
                    found = true;
                    continue;
                }
            }
            // If we didn't find it, report the violation
            if (!found)
                context.ReportDiagnostic(Diagnostic.Create(
                    ValidateMemberDescriptor,
                    parameterContext.GetLocation(),
                    fieldName,
                    targetType.Name
                    ));
        }
    }

    private bool HasAttribute(ISymbol type, INamedTypeSymbol attributeType)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                return true;
        }
        return false;
    }
}
