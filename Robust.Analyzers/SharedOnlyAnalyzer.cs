#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SharedOnlyAnalyzer : DiagnosticAnalyzer
{
    private const string SharedOnlyAttributeType = "Robust.Shared.Analyzers.SharedOnlyAttribute";

    public static readonly DiagnosticDescriptor SharedOnlyRule = new(
        Diagnostics.IdSharedOnly,
        "Use is forbidden outside of Shared assemblies",
        "{0} should only be used in Shared assemblies",
        "Usage",
        DiagnosticSeverity.Warning,
        true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        SharedOnlyRule,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(ctx =>
        {
            // Don't do anything in Shared assemblies
            if (ctx.Compilation.AssemblyName?.Contains("Shared") == true)
                return;

            // Get the type symbol for the attribute
            if (ctx.Compilation.GetTypeByMetadataName(SharedOnlyAttributeType) is not { } attributeType)
                return;

            ctx.RegisterOperationAction(operationContext => AnalyzeOperation(operationContext, attributeType), OperationKind.Invocation);
            ctx.RegisterOperationAction(operationContext => AnalyzeOperation(operationContext, attributeType), OperationKind.PropertyReference);
            ctx.RegisterOperationAction(operationContext => AnalyzeOperation(operationContext, attributeType), OperationKind.FieldReference);

            // Attribute usage check
            ctx.RegisterOperationAction(operationContext => AnalyzeAttribute(operationContext, attributeType), OperationKind.Attribute);
        });
    }

    private void AnalyzeOperation(OperationAnalysisContext context, INamedTypeSymbol attributeType)
    {
        ISymbol target = context.Operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IPropertyReferenceOperation propertyRef => propertyRef.Property,
            IFieldReferenceOperation fieldRef => fieldRef.Field,
            _ => throw new InvalidOperationException()
        };

        if (!AttributeHelper.HasAttribute(target, attributeType, out _))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            SharedOnlyRule,
            context.Operation.Syntax.GetLocation(),
            target.Name
        ));
    }

    /// <summary>
    /// Checks for correct usage of the attribute itself.
    /// </summary>
    private void AnalyzeAttribute(OperationAnalysisContext context, INamedTypeSymbol attributeType)
    {
        if (context.Operation is not IAttributeOperation operation)
            return;

        if (operation.Operation is not IObjectCreationOperation creationOperation)
            return;

        if (SymbolEqualityComparer.Default.Equals(creationOperation.Type, attributeType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
            SharedOnlyRule,
            context.Operation.Syntax.GetLocation(),
            attributeType.Name
        ));
        }
    }
}
