using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrototypeNetSerializableAnalyzer : DiagnosticAnalyzer
{
    private const string PrototypeInterfaceType = "Robust.Shared.Prototypes.IPrototype";
    private const string NetSerializableAttributeType = "Robust.Shared.Serialization.NetSerializableAttribute";

    public static readonly DiagnosticDescriptor RuleNetSerializable = new(
        Diagnostics.IdPrototypeNetSerializable,
        "Prototypes should not be [NetSerializable]",
        "Type {0} is a prototype and marked as [NetSerializable]. Prototypes should not be directly sent over the network, send their IDs instead.",
        "Usage",
        DiagnosticSeverity.Warning,
        true);


    public static readonly DiagnosticDescriptor RuleSerializable = new(
        Diagnostics.IdPrototypeSerializable,
        "Prototypes should not be [Serializable]",
        "Type {0} is a prototype and marked as [Serializable]. Prototypes should not be directly sent over the network, send their IDs instead.",
        "Usage",
        DiagnosticSeverity.Warning,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        RuleNetSerializable,
        RuleSerializable
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static ctx =>
        {
            var prototypeInterface = ctx.Compilation.GetTypeByMetadataName(PrototypeInterfaceType);
            var netSerializableAttribute = ctx.Compilation.GetTypeByMetadataName(NetSerializableAttributeType);

            if (prototypeInterface == null || netSerializableAttribute == null)
                return;

            ctx.RegisterSymbolAction(symbolContext => CheckClass(prototypeInterface, netSerializableAttribute, symbolContext), SymbolKind.NamedType);
        });
    }

    private static void CheckClass(
        INamedTypeSymbol prototypeInterface,
        INamedTypeSymbol netSerializableAttribute,
        SymbolAnalysisContext symbolContext)
    {
        if (symbolContext.Symbol is not INamedTypeSymbol symbol)
            return;

        if (!TypeSymbolHelper.ImplementsInterface(symbol, prototypeInterface))
            return;

        if (AttributeHelper.HasAttribute(symbol, netSerializableAttribute, out _))
        {
            symbolContext.ReportDiagnostic(
                Diagnostic.Create(RuleNetSerializable, symbol.Locations[0], symbol.ToDisplayString()));
        }

        if (symbol.IsSerializable)
        {
            symbolContext.ReportDiagnostic(
                Diagnostic.Create(RuleSerializable, symbol.Locations[0], symbol.ToDisplayString()));
        }
    }
}
