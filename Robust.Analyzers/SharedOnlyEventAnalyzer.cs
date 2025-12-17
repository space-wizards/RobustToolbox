using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SharedOnlyEventAnalyzer : DiagnosticAnalyzer
{
    private const string SharedOnlyEventAttributeName = "Robust.Shared.Analyzers.SharedOnlyEventAttribute";
    private const string SubscribeLocalEventMethodName = "SubscribeLocalEvent";

    public static DiagnosticDescriptor Rule = new(
        Diagnostics.IdSharedOnlyEventNotShared,
        "Shared event subscribed in non-shared code",
        "The event {0} should only be subscribed to in Shared",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        "Move the event subscription to Shared code."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        Rule,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(ctx =>
        {
            if (ctx.Compilation.GetTypeByMetadataName(SharedOnlyEventAttributeName) is not { } sharedSubscribeAttribute)
                return;

            ctx.RegisterOperationAction(opContext => AnalyzeInvocation(opContext, sharedSubscribeAttribute), OperationKind.Invocation);
        });
    }

    public static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol sharedSubscribeAttribute)
    {
        // If the subscription is in Shared code, then we don't care about it
        if (context.ContainingSymbol.ContainingNamespace.ToString().Contains("Shared"))
            return;

        if (context.Operation is not IInvocationOperation operation)
            return;

        // Check if the method being invoked is SubscribeLocalEvent
        if (operation.TargetMethod.OriginalDefinition.Name != SubscribeLocalEventMethodName)
            return;

        // Sanity check: make sure the method takes 2 type args
        if (operation.TargetMethod.TypeArguments.Length != 2)
            return;

        // Get the second type arg (TEvent)
        if (operation.TargetMethod.TypeArguments[1] is not INamedTypeSymbol eventType)
            return;

        // Make sure the event has our attribute
        if (!AttributeHelper.HasAttribute(eventType, sharedSubscribeAttribute, out var attributeData))
            return;

        // Check if the attribute also allows Client-only subscriptions
        var allowClientOnly = (bool)attributeData.ConstructorArguments[0].Value;
        if (allowClientOnly && context.ContainingSymbol.ContainingNamespace.ToString().Contains("Client"))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            operation.Syntax.GetLocation(),
            eventType.Name
        ));
    }
}
