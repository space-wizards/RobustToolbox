using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ByRefEventAnalyzer : DiagnosticAnalyzer
{
    private const string ByRefAttribute = "Robust.Shared.GameObjects.ByRefEventAttribute";

    private static readonly DiagnosticDescriptor ByRefRaisedByValueRule = new(
        Diagnostics.IdByRefEventRaisedByValue,
        "Invalid by-refness",
        "Tried to raise a by-ref event '{0}' by value.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure that by-ref events are raised with the ref keyword."
    );

    private ISymbol _subscribeLocalEventMethod = default!;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ByRefRaisedByValueRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(CheckByRefEvents, OperationKind.Invocation);
    }

    private void CheckByRefEvents(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation operation)
            return;

        operation.TargetMethod.Equals()
    }
}
