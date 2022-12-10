using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NotNullableFlagAnalyzer : DiagnosticAnalyzer
{
    private const string Attribute = "Robust.Shared.Analyzers.NotNullableFlagAttribute";

    private static readonly DiagnosticDescriptor NotNullableNotSetRule = new (
        Diagnostics.IdNotNullableFlagNotSet,
        "Not Nullable Flag not set",
        "Class type parameter {0} is not annotated as nullable and notNullableOverride is not set to true",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Assign true to notNullableOverride or specify the type parameter as nullable.");

    private static readonly DiagnosticDescriptor InvalidNotNullableValueRule = new (
        Diagnostics.IdInvalidNotNullableFlagValue,
        "Not Nullable Flag wrongfully set",
        "Class type parameter {0} is annotated as nullable but notNullableOverride is set to true",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Remove the true assignment to notNullableOverride or remove the nullable specifier of the type parameter.");

    private static readonly DiagnosticDescriptor InvalidNotNullableImplementationRule = new (
        Diagnostics.IdInvalidNotNullableFlagImplementation,
        "Invalid NotNullable flag implementation.",
        "NotNullable flag is either not typed as bool, or does not have a default value equaling false",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Ensure that the notNullable flag is typed bool and has false set as a default value.");

    private static readonly DiagnosticDescriptor InvalidNotNullableTypeRule = new (
        Diagnostics.IdInvalidNotNullableFlagType,
        "Failed to resolve type parameter",
        "Failed to resolve type parameter \"{0}\".",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Use nameof to avoid typos.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            NotNullableNotSetRule,
            InvalidNotNullableValueRule,
            InvalidNotNullableImplementationRule,
            InvalidNotNullableTypeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(CheckNotNullableFlag, OperationKind.Invocation);
    }

    private bool TryGetTypeArgument(IMethodSymbol methodSymbol, string typeParamName, out ITypeSymbol typeArgument)
    {
        for (var index = 0; index < methodSymbol.TypeParameters.Length; index++)
        {
            if (methodSymbol.TypeParameters[index].Name != typeParamName)
                continue;

            typeArgument = methodSymbol.TypeArguments[index];
            return true;
        }

        typeArgument = null;
        return false;
    }

    private void CheckNotNullableFlag(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocationOperation || !invocationOperation.TargetMethod.IsGenericMethod)
            return;

        var attribute = context.Compilation.GetTypeByMetadataName(Attribute);
        var @bool = context.Compilation.GetSpecialType(SpecialType.System_Boolean);

        for (var paramIndex = 0; paramIndex < invocationOperation.TargetMethod.Parameters.Length; paramIndex++)
        {
            var param = invocationOperation.TargetMethod.Parameters[paramIndex];
            foreach (var attributeData in param.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attribute))
                    continue;

                if (!SymbolEqualityComparer.Default.Equals(param.Type, @bool) || !param.HasExplicitDefaultValue || param.ExplicitDefaultValue as bool? != false)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidNotNullableImplementationRule,
                        param.Locations[0]));
                    break;
                }

                if (!TryGetTypeArgument(invocationOperation.TargetMethod,
                        attributeData.ConstructorArguments[0].Value as string, out var typeArgument))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidNotNullableTypeRule,
                        param.Locations[0],
                        attributeData.ConstructorArguments[0].Value as string));
                    break;
                }

                var argument = invocationOperation.Arguments[paramIndex];
                if (typeArgument.NullableAnnotation == NullableAnnotation.None || typeArgument.IsValueType || !argument.ConstantValue.HasValue) break;

                var nullable = typeArgument.NullableAnnotation == NullableAnnotation.Annotated;
                var flagValue = argument.ArgumentKind == ArgumentKind.DefaultValue ||
                                argument.ConstantValue.Value as bool? == true;

                if (nullable && flagValue)
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidNotNullableValueRule,
                        argument.Syntax.GetLocation(),
                        typeArgument));
                }
                else if (!nullable && !flagValue)
                {
                    context.ReportDiagnostic(Diagnostic.Create(NotNullableNotSetRule,
                        argument.Syntax.GetLocation(),
                        typeArgument));
                }

                break;
            }
        }
    }
}
