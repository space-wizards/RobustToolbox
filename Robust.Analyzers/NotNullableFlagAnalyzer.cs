using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

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
        "Invalid NotNullable flag implementation",
        "NotNullable flag is either not typed as bool, or does not have a default value equaling false",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Ensure that the notNullable flag is typed bool and has false set as a default value.");

    private static readonly DiagnosticDescriptor InvalidNotNullableTypeRule = new (
        Diagnostics.IdInvalidNotNullableFlagType,
        "Failed to resolve type parameter",
        "Failed to resolve type parameter \"{0}\"",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Use nameof to avoid typos.");

    private static readonly DiagnosticDescriptor NotNullableFlagValueTypeRule = new (
        Diagnostics.IdNotNullableFlagValueType,
        "NotNullable flag not supported for value types",
        "Value types as generic arguments are not supported for NotNullable flags",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Nullable value types are distinct at runtime when inspected with reflection. Therefore they are not supported for NotNullable flags.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            NotNullableNotSetRule,
            InvalidNotNullableValueRule,
            InvalidNotNullableImplementationRule,
            InvalidNotNullableTypeRule,
            NotNullableFlagValueTypeRule);

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

        foreach (var argument in invocationOperation.Arguments)
        {
            if(argument.Parameter == null) continue;

            foreach (var attributeData in argument.Parameter.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attribute))
                    continue;

                if (!SymbolEqualityComparer.Default.Equals(argument.Parameter.Type, @bool) ||
                    !argument.Parameter.HasExplicitDefaultValue ||
                    argument.Parameter.ExplicitDefaultValue as bool? != false)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidNotNullableImplementationRule,
                        argument.Parameter.Locations[0]));
                    break;
                }

                if (!TryGetTypeArgument(invocationOperation.TargetMethod,
                        attributeData.ConstructorArguments[0].Value as string, out var typeArgument))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidNotNullableTypeRule,
                        argument.Parameter.Locations[0],
                        attributeData.ConstructorArguments[0].Value as string));
                    break;
                }

                //until i find a way to implement it sanely, generic calls are exempt from this attribute
                if(typeArgument is ITypeParameterSymbol) break;

                //dont ask me why, argument.ConstantValue just straight up doesnt work.
                //i still kept it in here as a fallback, incase it ever starts working again lol -<paul
                var constantValue = (argument.Value as ILiteralOperation)?.ConstantValue ?? argument.ConstantValue;

                if (typeArgument.IsValueType)
                {
                    if (argument.ArgumentKind != ArgumentKind.DefaultValue)
                    {
                        //todo diagnostic only use for struct types
                        context.ReportDiagnostic(Diagnostic.Create(
                            NotNullableFlagValueTypeRule,
                            argument.Syntax.GetLocation()));
                    }
                    break;
                }

                if (typeArgument.NullableAnnotation == NullableAnnotation.None ||
                    (argument.ArgumentKind != ArgumentKind.DefaultValue && !constantValue.HasValue))
                    break;

                var flagValue = argument.ArgumentKind != ArgumentKind.DefaultValue ||
                                constantValue.Value as bool? == true;

                var nullable = typeArgument.NullableAnnotation == NullableAnnotation.Annotated;

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
