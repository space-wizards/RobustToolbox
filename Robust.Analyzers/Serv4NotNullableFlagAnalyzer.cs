using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Serv4NotNullableFlagAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor NotNullableRule = new (
        Diagnostics.IdServ4NotNullableFlag,
        "Not Nullable Flag not set",
        "Class type parameter {0} is not annotated as nullable and notNullableOverride is not set to true",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Assign true to notNullableOverride or specify the type parameter as nullable.");

    private static readonly DiagnosticDescriptor InvalidNotNullableRule = new (
        Diagnostics.IdServ4NotNullableFlag,
        "Not Nullable Flag wrongfully set",
        "Class type parameter {0} is annotated as nullable but notNullableOverride is set to true",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Remove the true assignment to notNullableOverride or remove the nullable specifier of the type parameter.");

    private static readonly DiagnosticDescriptor NotNullableFlagMissingRule = new (
        Diagnostics.IdServ4NotNullableFlagNotPresent,
        "Not Nullable Flag parameter could not be found",
        "The bool parameter should be at the end of the method signature",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Move the notNullableOverride to the back of the method signature.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NotNullableRule, InvalidNotNullableRule, NotNullableFlagMissingRule);

    private static readonly string[] CheckedTypes = new[]
    {
        "Robust.Shared.Serialization.Manager.ISerializationManager",
        "Robust.Shared.Serialization.Manager.SerializationManager",
    };

    private static readonly string[] CheckedMethods = new []
    {
        "Read",
        "WriteValue",
        "CopyTo",
        "CreateCopy",
    };

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(CheckNotNullableFlag,
            OperationKind.Invocation);
    }

    private void CheckNotNullableFlag(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
            return;

        if(!invocationOperation.TargetMethod.IsGenericMethod || CheckedMethods.All(x => x != invocationOperation.TargetMethod.Name))
            return;

        var checkedTypesResolved = new INamedTypeSymbol[CheckedTypes.Length];
        for (int i = 0; i < CheckedTypes.Length; i++)
        {
            checkedTypesResolved[i] = context.Compilation.GetTypeByMetadataName(CheckedTypes[i]);
        }

        if (invocationOperation.Instance == null || checkedTypesResolved.All(x =>
                !SymbolEqualityComparer.Default.Equals(x, invocationOperation.Instance.Type)))
            return;

        var lastArgument = invocationOperation.Arguments.Last();
        if (!SymbolEqualityComparer.Default.Equals(lastArgument.Parameter?.Type,
                context.Compilation.GetSpecialType(SpecialType.System_Boolean)))
        {
            context.ReportDiagnostic(Diagnostic.Create(NotNullableFlagMissingRule,
                lastArgument.Parameter?.Locations.First()));
            return;
        }

        var genericParam = invocationOperation.TargetMethod.TypeArguments[0];
        if (genericParam.NullableAnnotation == NullableAnnotation.NotAnnotated)
        {
            if (lastArgument.ArgumentKind == ArgumentKind.DefaultValue || (lastArgument.ConstantValue.HasValue &&
                                                                           lastArgument.ConstantValue.Value as bool? ==
                                                                           false))
            {
                context.ReportDiagnostic(Diagnostic.Create(NotNullableRule,
                    invocationOperation.Syntax.GetLocation(),
                    genericParam));
            }
        }
        else
        {
            if (lastArgument.ConstantValue.HasValue && lastArgument.ConstantValue.Value as bool? == true)
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidNotNullableRule,
                    invocationOperation.Syntax.GetLocation(),
                    genericParam));
            }
        }
    }
}
