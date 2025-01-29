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
        context.RegisterOperationAction(CheckForNonGenericVariant, OperationKind.Invocation);
    }

    private void CheckForNonGenericVariant(OperationAnalysisContext obj)
    {
        if (obj.Operation is not IInvocationOperation invocationOperation) return;

        var preferNonGenericAttribute = obj.Compilation.GetTypeByMetadataName(AttributeType);

        HashSet<ITypeSymbol> forTypes = [];
        foreach (var attribute in invocationOperation.TargetMethod.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, preferNonGenericAttribute))
                continue;

            foreach (var type in attribute.ConstructorArguments[0].Values)
                forTypes.Add((ITypeSymbol)type.Value);

            break;
        }

        if (forTypes == null)
            return;

        foreach (var typeArg in invocationOperation.TargetMethod.TypeArguments)
        {
            if (forTypes.Contains(typeArg))
            {
                obj.ReportDiagnostic(
                Diagnostic.Create(UseNonGenericVariantDescriptor,
                    invocationOperation.Syntax.GetLocation(), typeArg.Name));
            }
        }
    }
}
