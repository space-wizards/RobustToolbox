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
    private const string EntitySystemTypeName = "Robust.Shared.GameObjects.EntitySystem";
    private const string InitializeMethodName = "Initialize";
    private const string SubscribeLocalEventMethodName = "SubscribeLocalEvent";
    private const string SubscribeNetworkEventMethodName = "SubscribeNetworkEvent";
    private const string SubscribeAllEventMethodName = "SubscribeAllEvent";
    private static readonly string[] SubscribeMethods =
    [
        SubscribeLocalEventMethodName,
        SubscribeNetworkEventMethodName,
        SubscribeAllEventMethodName,
    ];

    public const string AttributeNameKey = "attribute";

    public static readonly DiagnosticDescriptor EntitySystemSubscriptionConversionPossible = new(
        Diagnostics.IdEntitySystemSubscriptionConversionPossible,
        "Convert to attribute-based subscription",
        "Initialize-based event subscription can be converted to attribute-based",
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
            if (ctx.Compilation.GetTypeByMetadataName(EntitySystemTypeName) is not { } entitySystemType)
                return;

            ctx.RegisterSymbolStartAction(symbolContext =>
            {
                // We only care about classes
                if (symbolContext.Symbol is not INamedTypeSymbol typeSymbol || typeSymbol.TypeKind != TypeKind.Class)
                    return;

                // Must inherit from EntitySystem
                if (!TypeSymbolHelper.Inherits(typeSymbol, entitySystemType))
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

        // We're only looking for the Initialize method
        if (context.ContainingSymbol.Name != InitializeMethodName)
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

                // We (currently) don't support the before and after parameters with attribute subscriptions
                // so we skip any invocations that use them.
                // If we do support them in the future (and the code fixer is improved to convert to them), this check should be removed.
                if (invocation.Arguments.Any(
                    arg => (arg.Parameter?.Name == "before" || arg.Parameter?.Name == "after")
                    && arg.Value is not IDefaultValueOperation))
                    continue;

                // Ignore anything that isn't a direct method reference, i.e. an anonymous delegate.
                if (invocation.Arguments.SingleOrDefault(arg => arg.Parameter?.Name == "handler") is not { } handlerArg
                    || handlerArg.Value.Syntax is not IdentifierNameSyntax)
                    continue;

                var props = new Dictionary<string, string?>
                {
                    { AttributeNameKey, ToAttributeName(invocation.TargetMethod.Name) }
                };

                // Flag this subscription as elligible for conversion
                context.ReportDiagnostic(Diagnostic.Create(
                    EntitySystemSubscriptionConversionPossible,
                    invocation.Syntax.GetLocation(),
                    props.ToImmutableDictionary()
                ));
            }
        }
    }

    /// <summary>
    /// Returns the name of the appropriate attribute to replace the given subscription method.
    /// </summary>
    public static string ToAttributeName(string methodName)
    {
        // This is currently a 1:1 match, but if it weren't,
        // this would be the place to implement the remapping.
        return methodName;
    }
}
