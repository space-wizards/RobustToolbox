#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

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
        {
            var autoGenStateAttribute = compilationContext.Compilation.GetTypeByMetadataName(AutoGenStateAttribute);
            // No attribute, no analyzer.
            if (autoGenStateAttribute is null)
                return;

            compilationContext.RegisterOperationAction(
                analysisContext => CheckEventSubscription(analysisContext, autoGenStateAttribute),
                OperationKind.Invocation);
        });
    }

    private static void CheckEventSubscription(OperationAnalysisContext context, ITypeSymbol autoGenStateAttribute)
    {
        if (context.Operation is not IInvocationOperation operation)
            return;

        // Check the method has the right name and has the right type args
        if (operation.TargetMethod is not
            { Name: SubscribeLocalEventName, TypeArguments: [var component, { Name: AfterAutoHandleStateEventName }] })
            return;

        // Search the component's attributes for something matching autoGenStateAttribute
        AttributeHelper.HasAttribute(component, autoGenStateAttribute, out var autoGenAttribute);

        // First argument is raiseAfterAutoHandleState—note it shouldn't ever
        // be null, since it has a default, but eh.
        if (autoGenAttribute?.ConstructorArguments[0].Value is true)
            return;

        context.ReportDiagnostic(Diagnostic.Create(autoGenAttribute is null ? MissingAttribute : MissingAttributeParam,
            operation.Syntax.GetLocation(),
            component.Name));
    }
}
