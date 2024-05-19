using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoUncachedRegexAnalyzer : DiagnosticAnalyzer
{
    private const string RegexTypeName = "Regex";
    private const string RegexType = $"System.Text.RegularExpressions.{RegexTypeName}";

    private static readonly DiagnosticDescriptor Rule = new (
        Diagnostics.IdUncachedRegex,
        "Use of uncached static Regex function",
        "Usage of a static Regex function that takes in a pattern string. This can cause constant re-parsing of the pattern.",
        "Usage",
        DiagnosticSeverity.Warning,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public static readonly HashSet<string> BadFunctions =
    [
        "Count",
        "EnumerateMatches",
        "IsMatch",
        "Match",
        "Matches",
        "Replace",
        "Split"
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(CheckInvocation, OperationKind.Invocation);
    }

    private static void CheckInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
            return;

        // All Regex functions we care about are static.
        var targetMethod = invocation.TargetMethod;
        if (!targetMethod.IsStatic)
            return;

        // Bail early.
        if (targetMethod.ContainingType.Name != "Regex")
            return;

        var regexType = context.Compilation.GetTypeByMetadataName(RegexType);
        if (!SymbolEqualityComparer.Default.Equals(regexType, targetMethod.ContainingType))
            return;

        if (!BadFunctions.Contains(targetMethod.Name))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation()));
    }
}
