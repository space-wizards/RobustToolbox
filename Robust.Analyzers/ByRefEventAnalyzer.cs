#nullable enable
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.SymbolEqualityComparer;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ByRefEventAnalyzer : DiagnosticAnalyzer
{
    private const string ByRefAttribute = "Robust.Shared.GameObjects.ByRefEventAttribute";

    private static readonly DiagnosticDescriptor ByRefEventSubscribedByValueRule = new(
        Diagnostics.IdByRefEventSubscribedByValue,
        "By-ref event subscribed to by value",
        "Tried to subscribe to a by-ref event '{0}' by value.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure that methods subscribing to a ref event have the ref keyword for the event argument."
    );

    private static readonly DiagnosticDescriptor ByValueEventSubscribedByRefRule = new(
        Diagnostics.IdValueEventRaisedByRef,
        "Value event subscribed to by-ref",
        "Tried to subscribe to a value event '{0}' by-ref.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure that methods subscribing to value events do not have the ref keyword for the event argument."
    );

    private static readonly DiagnosticDescriptor ByRefEventRaisedByValueRule = new(
        Diagnostics.IdByRefEventRaisedByValue,
        "By-ref event raised by value",
        "Tried to raise a by-ref event '{0}' by value.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure to use the ref keyword when raising ref events."
    );

    private static readonly DiagnosticDescriptor ByValueEventRaisedByRefRule = new(
        Diagnostics.IdValueEventRaisedByRef,
        "Value event raised by-ref",
        "Tried to raise a value event '{0}' by-ref.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure to not use the ref keyword when raising value events."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        ByRefEventSubscribedByValueRule,
        ByValueEventSubscribedByRefRule,
        ByRefEventRaisedByValueRule,
        ByValueEventRaisedByRefRule
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(CheckEventSubscription, OperationKind.Invocation);
        context.RegisterOperationAction(CheckEventRaise, OperationKind.Invocation);
    }

    private void CheckEventSubscription(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation operation)
            return;

        var subscribeMethods = context.Compilation
            .GetTypeByMetadataName("Robust.Shared.GameObjects.EntitySystem")?
            .GetMembers()
            .Where(m => m.Name.Contains("SubscribeLocalEvent"))
            .Cast<IMethodSymbol>();

        if (subscribeMethods == null)
            return;

        if (!subscribeMethods.Any(m => m.Equals(operation.TargetMethod.OriginalDefinition, Default)))
            return;

        var typeArguments = operation.TargetMethod.TypeArguments;
        if (typeArguments.Length < 1 || typeArguments.Length > 2)
            return;

        if (operation.Arguments.First().Value is not IDelegateCreationOperation delegateCreation)
            return;

        if (delegateCreation.Target is not IMethodReferenceOperation methodReference)
            return;

        var eventParameter = methodReference.Method.Parameters.LastOrDefault();
        if (eventParameter == null)
            return;

        ITypeSymbol eventArgument;
        switch (typeArguments.Length)
        {
            case 1:
                eventArgument = typeArguments[0];
                break;
            case 2:
                eventArgument = typeArguments[1];
                break;
            default:
                return;
        }

        var byRefAttribute = context.Compilation.GetTypeByMetadataName(ByRefAttribute);
        if (byRefAttribute == null)
            return;

        var isByRefEventType = eventArgument
            .GetAttributes()
            .Any(attribute => attribute.AttributeClass?.Equals(byRefAttribute, Default) ?? false);
        var parameterIsRef = eventParameter.RefKind == RefKind.Ref;

        if (isByRefEventType != parameterIsRef)
        {
            var descriptor = isByRefEventType ? ByRefEventSubscribedByValueRule : ByValueEventSubscribedByRefRule;
            var diagnostic = Diagnostic.Create(descriptor, operation.Syntax.GetLocation(), eventArgument);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void CheckEventRaise(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation operation)
            return;

        var raiseMethods = context.Compilation
            .GetTypeByMetadataName("Robust.Shared.GameObjects.EntitySystem")?
            .GetMembers()
            .Where(m => m.Name.Contains("RaiseLocalEvent") && m.Kind == SymbolKind.Method)
            .Cast<IMethodSymbol>();

        var busRaiseMethods = context.Compilation
            .GetTypeByMetadataName("Robust.Shared.GameObjects.EntityEventBus")?
            .GetMembers()
            .Where(m => m.Name.Contains("RaiseLocalEvent") && m.Kind == SymbolKind.Method)
            .Cast<IMethodSymbol>();

        if (raiseMethods == null)
            return;

        if (busRaiseMethods != null)
            raiseMethods = raiseMethods.Concat(busRaiseMethods);

        if (!raiseMethods.Any(m => m.Equals(operation.TargetMethod.OriginalDefinition, Default)))
        {
            // If you try to do this normally by concatenating like busRaiseMethods above
            // the analyzer does not run without any errors
            // I don't know man
            const string directedBusMethod = "Robust.Shared.GameObjects.IDirectedEventBus.RaiseLocalEvent";
            if (!operation.TargetMethod.ToString().StartsWith(directedBusMethod))
                return;
        }

        var arguments = operation.Arguments;
        IArgumentOperation eventArgument;
        switch (arguments.Length)
        {
            case 1:
                eventArgument = arguments[0];
                break;
            case 2:
            case 3:
                eventArgument = arguments[1];
                break;
            default:
                return;
        }

        var eventParameter = eventArgument.Parameter;
        // TODO have a way to check generic type parameters
        if (eventParameter == null ||
            eventParameter.Type.SpecialType == SpecialType.System_Object ||
            eventParameter.Type.TypeKind == TypeKind.TypeParameter)
        {
            return;
        }

        var byRefAttribute = context.Compilation.GetTypeByMetadataName(ByRefAttribute);
        if (byRefAttribute == null)
            return;

        var isByRefEventType = eventParameter.Type
            .GetAttributes()
            .Any(attribute => attribute.AttributeClass?.Equals(byRefAttribute, Default) ?? false);

        var parameterIsRef = eventParameter.RefKind == RefKind.Ref;

        if (isByRefEventType != parameterIsRef)
        {
            var descriptor = isByRefEventType ? ByRefEventRaisedByValueRule : ByValueEventRaisedByRefRule;
            var diagnostic = Diagnostic.Create(descriptor, eventArgument.Syntax.GetLocation(), eventParameter.Type);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
