using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TaskResultAnalyzer : DiagnosticAnalyzer
{
    [SuppressMessage("ReSharper", "RS2008")]
    private static readonly DiagnosticDescriptor ResultRule = new DiagnosticDescriptor(
        Diagnostics.IdTaskResult,
        "Risk of deadlock from accessing Task<T>.Result",
        "Accessing Task<T>.Result is dangerous and can cause deadlocks in some contexts. If you understand how this works and are certain that you aren't causing a deadlock here, mute this error with #pragma.",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ResultRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Check, OperationKind.PropertyReference);
    }

    private static void Check(OperationAnalysisContext context)
    {
        var taskType = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

        var operation = (IPropertyReferenceOperation) context.Operation;
        var member = operation.Member;

        if (member.Name == "Result" && taskType.Equals(member.ContainingType.ConstructedFrom, SymbolEqualityComparer.Default))
        {
            var diag = Diagnostic.Create(ResultRule, operation.Syntax.GetLocation());
            context.ReportDiagnostic(diag);
        }
    }
}
