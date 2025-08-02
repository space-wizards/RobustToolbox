#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;
using static Microsoft.CodeAnalysis.SymbolEqualityComparer;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AfterAutoHandleStateAnalyzer : DiagnosticAnalyzer
{
    private const string AfterAutoHandleStateEventName = "AfterAutoHandleStateEvent";
    private const string AutoGenStateAttribute = "Robust.Shared.Analyzers.AutoGenerateComponentStateAttribute";
    private const string SubscribeLocalEventName = "SubscribeLocalEvent";

    public static readonly DiagnosticDescriptor MissingAttribute = new(
        Diagnostics.IdAutoGenStateAttributeMissing,
        "Unreachable AfterAutoHandleState subscription",
        "Tried to subscribe to AfterAutoHandleStateEvent for '{0}' which doesn't have an "
        + "AutoGenerateComponentState attribute",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        // Does this even show up anywhere in Rider? >:(
        "You must mark your component with '[AutoGenerateComponentState(true)]' to subscribe to this event."
    );

    public static readonly DiagnosticDescriptor MissingAttributeParam = new(
        Diagnostics.IdAutoGenStateParamMissing,
        "Unreachable AfterAutoHandleState subscription",
        "Tried to subscribe to AfterAutoHandleStateEvent for '{0}' which doesn't have "
        + "raiseAfterAutoHandleState set",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "The AutoGenerateComponentState attribute must be passed 'true' in order to subscribe to this event."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [MissingAttribute, MissingAttributeParam];

    public override void Initialize(AnalysisContext context)
    {
        // This is more to stop user error rather than code generation error
        // (Plus this shouldn't affect code gen anyway)
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
            compilationContext.RegisterOperationAction(CheckEventSubscription, OperationKind.Invocation));
    }

    private static void CheckEventSubscription(
        OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation operation)
            return;

        if (!operation.TargetMethod.Name.Contains(SubscribeLocalEventName))
            return;

        var subscriptionTypes = operation.TargetMethod.TypeArguments;
        // Check second arg of SubscribeLocalEvent is AfterAutoHandleStateEvent
        if (subscriptionTypes.ElementAtOrDefault(1)?.Name.Contains(AfterAutoHandleStateEventName) != true)
            return;

        var autoGenStateAttribute = context.Compilation.GetTypeByMetadataName(AutoGenStateAttribute);
        if (autoGenStateAttribute == null)
            return;

        // If we have a second type arg, we definitely have a first.
        // Get the attributes for whatever that is, and then get its attributes.
        var component = subscriptionTypes[0];
        var autoGenAttribute = component
            .GetAttributes()
            .FirstOrDefault(attribute => attribute.AttributeClass?.Equals(autoGenStateAttribute, Default) ?? false);

        // First argument is raiseAfterAutoHandleState—note it shouldn't ever
        // be null, since it has a default, but eh.
        if (autoGenAttribute?.ConstructorArguments[0].Value is true)
            return;

        context.ReportDiagnostic(Diagnostic.Create(autoGenAttribute is null ? MissingAttribute : MissingAttributeParam,
            operation.Syntax.GetLocation(),
            component.Name));
    }
}
