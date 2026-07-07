using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrototypeInstantiationAnalyzer : DiagnosticAnalyzer
{
    private const string PrototypeInterfaceType = "Robust.Shared.Prototypes.IPrototype";

    public static readonly DiagnosticDescriptor Rule = new(
        Diagnostics.IdPrototypeInstantiation,
        "Do not instantiate prototypes directly",
        "Do not instantiate prototypes directly. Prototypes should always be instantiated by the prototype manager.",
        "Usage",
        DiagnosticSeverity.Warning,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(static ctx =>
        {
            var prototypeInterface = ctx.Compilation.GetTypeByMetadataName(PrototypeInterfaceType);
            if (prototypeInterface == null)
                return;

            ctx.RegisterOperationAction(symContext => Check(prototypeInterface, symContext), OperationKind.ObjectCreation);
        });
    }

    private static void Check(INamedTypeSymbol prototypeInterface, OperationAnalysisContext ctx)
    {
        if (ctx.Operation is not IObjectCreationOperation { Type: { } resultType } creationOp)
            return;

        if (!TypeSymbolHelper.ImplementsInterface(resultType, prototypeInterface))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(Rule, creationOp.Syntax.GetLocation()));
    }
}
