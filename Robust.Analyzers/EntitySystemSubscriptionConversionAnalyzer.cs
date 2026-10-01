#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EntitySystemSubscriptionConversionAnalyzer : DiagnosticAnalyzer
{
    private const string EntitySystemTypeName = "Robust.Shared.GameObjects.IEntitySystem";
    private const string SubscribeLocalEventAttributeTypeName = "Robust.Shared.Analyzers.SubscribeLocalEventAttribute";
    private const string SubscribeLocalEventMethodName = "SubscribeLocalEvent";
    private const string SubscribeNetworkEventMethodName = "SubscribeNetworkEvent";
    private const string SubscribeAllEventMethodName = "SubscribeAllEvent";
    private const string SubscribeAllEventAttributeName = "EventSubscription";
    private static readonly string[] SubscribeMethods =
    [
        SubscribeLocalEventMethodName,
        SubscribeNetworkEventMethodName,
        SubscribeAllEventMethodName,
    ];

    /// <summary>
    /// The key used to access the needed replacement attribute name in the diagnostic's Properties dictionary.
    /// </summary>
    public const string AttributeNameKey = "attribute";

    public static readonly DiagnosticDescriptor EntitySystemSubscriptionConversionPossible = new(
        Diagnostics.IdEntitySystemSubscriptionConversionPossible,
        "Convert to attribute-based subscription",
        "Event subscription using {0} can be converted to use {1}",
        "Usage",
        DiagnosticSeverity.Info,
        true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        EntitySystemSubscriptionConversionPossible,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            // If the subscription attributes aren't available in this compilation, we can't do anything.
            if (ctx.Compilation.GetTypeByMetadataName(SubscribeLocalEventAttributeTypeName) is null)
                return;

            if (ctx.Compilation.GetTypeByMetadataName(EntitySystemTypeName) is not { } entitySystemType)
                return;

            ctx.RegisterSymbolStartAction(symbolContext =>
            {
                // We only care about classes
                if (symbolContext.Symbol is not INamedTypeSymbol typeSymbol || typeSymbol.TypeKind != TypeKind.Class)
                    return;

                // Filter out anything that isn't an EntitySystem
                if (!typeSymbol.AllInterfaces.Contains(entitySystemType))
                    return;

                // Check each method definition in the class
                symbolContext.RegisterOperationAction(AnalyzeMethod, OperationKind.MethodBody);
            }, SymbolKind.NamedType);
        });
    }

    private static void AnalyzeMethod(OperationAnalysisContext context)
    {
        if (context.Operation is not IMethodBodyOperation method)
            return;

        if (method.BlockBody is null)
            return;

        // If the class contains any sort of conditional directives,
        // we consider it too complicated for automatic conversion.
        var classSyntax = method.Syntax.Ancestors().OfType<ClassDeclarationSyntax>().First();
        if (classSyntax.ContainsDirective(SyntaxKind.IfDirectiveTrivia | SyntaxKind.ElseDirectiveTrivia | SyntaxKind.ElifDirectiveTrivia | SyntaxKind.EndIfDirectiveTrivia))
            return;

        // Examine each operation within the Initialize method body
        foreach (var initOperation in method.BlockBody.ChildOperations)
        {
            // We only care about method invocations
            if (initOperation is not IExpressionStatementOperation expression
                || expression.Operation is not IInvocationOperation invocation)
                continue;

            // Check if the invoked method is one of the SubscribeWhateverEvent methods
            if (SubscribeMethods.Contains(invocation.TargetMethod.Name))
            {
                // If any of the type arguments of the invocation is a type parameter (rather than a distinct Type),
                // the attribute can't handle it, so we skip it.
                // For example, RaiseLocalEvent<TTreeComp, ComponentAdd>(), where TTreeComp is a type arg to the containing class.
                if (invocation.TargetMethod.TypeArguments.OfType<ITypeParameterSymbol>().Any())
                    continue;

                // Ignore anything that isn't a direct method reference, i.e. an anonymous delegate.
                if (invocation.Arguments.SingleOrDefault(arg => arg.Parameter?.Name == "handler") is not { } handlerArg
                    || handlerArg.Value.Syntax is not IdentifierNameSyntax)
                    continue;

                // Get the symbol for the event handler method.
                // We use OriginalDefinition to get the generic form if it's a generic method.
                // So we get MyEventHandler<MyComp, T> instead of MyEventHandler<MyComp, SomeSpecificEvent>.
                if (((handlerArg.Value as IDelegateCreationOperation)?.Target as IMethodReferenceOperation)?.Method.OriginalDefinition is not { } handlerMethod)
                    continue;

                // If the target method is generic, we can't subscribe using the attribute.
                if (handlerMethod.IsGenericMethod)
                    continue;

                // If the target method's event type is abstract, we can't subscribe using the attribute,
                // since the subscription needs the exact type.
                // This assumes that the event is the last parameter in the handler's signature,
                // which seems like a reasonable assumption at the time of writing.
                var handlerEventType = handlerMethod.Parameters.Last().Type;
                if (handlerEventType.IsAbstract)
                    continue;

                // If the handler is a virtual or abstract method, we can't use the attribute
                // since we would have to add it to the base class.
                if (handlerMethod.IsVirtual || handlerMethod.IsAbstract)
                    continue;

                // Find the name of the attribute we need to use to replace the invocation and
                // add it to the diagnostic so the code fixer can easily get it.
                var attributeName = ToAttributeName(invocation.TargetMethod.Name);
                var props = new Dictionary<string, string?>
                {
                    { AttributeNameKey, attributeName }
                };

                // Flag this subscription as elligible for conversion
                context.ReportDiagnostic(Diagnostic.Create(
                    EntitySystemSubscriptionConversionPossible,
                    invocation.Syntax.GetLocation(),
                    props.ToImmutableDictionary(),
                    invocation.TargetMethod.Name,
                    $"{attributeName}Attribute"
                ));
            }
        }
    }

    /// <summary>
    /// Returns the name of the appropriate attribute to replace the given subscription method.
    /// </summary>
    public static string ToAttributeName(string methodName)
    {
        return methodName switch
        {
            SubscribeAllEventMethodName => SubscribeAllEventAttributeName,
            _ => methodName
        };
    }
}
