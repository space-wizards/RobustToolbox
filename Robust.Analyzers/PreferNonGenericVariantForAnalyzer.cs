using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferNonGenericVariantForAnalyzer : DiagnosticAnalyzer
{
    private const string AttributeType = "Robust.Shared.Analyzers.PreferNonGenericVariantForAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        UseNonGenericVariantDescriptor
    );

    private static readonly DiagnosticDescriptor UseNonGenericVariantDescriptor = new(
        Diagnostics.IdUseNonGenericVariant,
        "Consider using the non-generic variant of this method",
        "Use the non-generic variant of this method for type {0}",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        "Use the generic variant of this method.");

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics | GeneratedCodeAnalysisFlags.Analyze);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var preferNonGenericAttribute = compilationContext.Compilation.GetTypeByMetadataName(AttributeType);
            if (preferNonGenericAttribute is null)
                return;

            compilationContext.RegisterOperationAction(
                operationContext => CheckForNonGenericVariant(operationContext, preferNonGenericAttribute),
                OperationKind.Invocation);
        });
    }

    private void CheckForNonGenericVariant(OperationAnalysisContext obj, INamedTypeSymbol preferNonGenericAttribute)
    {
        if (obj.Operation is not IInvocationOperation invocationOperation) return;

        AttributeData foundAttribute = null;
        foreach (var attribute in invocationOperation.TargetMethod.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, preferNonGenericAttribute))
                continue;

            foundAttribute = attribute;
            break;
        }

        if (foundAttribute == null)
            return;

        foreach (var typeArg in invocationOperation.TargetMethod.TypeArguments)
        {
            foreach (var type in foundAttribute.ConstructorArguments[0].Values)
            {
                if (type.Value is not ITypeSymbol forType ||
                    !SymbolEqualityComparer.Default.Equals(forType, typeArg))
                {
                    continue;
                }

                obj.ReportDiagnostic(
                    Diagnostic.Create(UseNonGenericVariantDescriptor,
                        invocationOperation.Syntax.GetLocation(), typeArg.Name));
                break;
            }
        }
    }
}
