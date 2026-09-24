#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;
using Robust.Shared.Analyzers;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EntitySystemNameAnalyzer : DiagnosticAnalyzer
{
    private const string AssemblySideAttributeType = "Robust.Shared.Analyzers.AssemblySideAttribute";
    private const string EntitySystemInterfaceType = "Robust.Shared.GameObjects.IEntitySystem";
    private const string EntitySystemType = "Robust.Shared.GameObjects.EntitySystem";

    private readonly struct TypeSymbols
    {
        public readonly INamedTypeSymbol AssemblySideAttribute { get; init; }
        public readonly INamedTypeSymbol EntitySystemInterface { get; init; }
        public readonly INamedTypeSymbol EntitySystem { get; init; }
    }

    public static readonly DiagnosticDescriptor EntitySystemNamingRule = new(
        Diagnostics.IdEntitySystemNameCompliance,
        "EntitySystem naming rule violation",
        "Naming rule violation: {0} should be named {1}",
        "Usage",
        DiagnosticSeverity.Warning,
        true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        EntitySystemNamingRule,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            if (ctx.Compilation.GetTypeByMetadataName(AssemblySideAttributeType) is not { } assemblySideAttributeType)
                return;

            if (!AttributeHelper.HasAttribute(ctx.Compilation.Assembly, assemblySideAttributeType, out var sideAttributeData))
                return;
            var side = (AssemblySide)sideAttributeData.ConstructorArguments[0].Value!;

            if (ctx.Compilation.GetTypeByMetadataName(EntitySystemInterfaceType) is not { } entitySystemInterfaceType)
                return;
            if (ctx.Compilation.GetTypeByMetadataName(EntitySystemType) is not { } entitySystemType)
                return;

            var typeSymbols = new TypeSymbols
            {
                AssemblySideAttribute = assemblySideAttributeType,
                EntitySystemInterface = entitySystemInterfaceType,
                EntitySystem = entitySystemType,
            };

            ctx.RegisterSymbolAction(symbolContext => AnalyzeNamedType(symbolContext, side, typeSymbols), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        AssemblySide side,
        TypeSymbols symbols
        )
    {
        // Filter out anything that isn't a class.
        if (context.Symbol is not INamedTypeSymbol symbol || symbol.TypeKind != TypeKind.Class)
            return;

        // Filter out anything that isn't an EntitySystem.
        if (!symbol.AllInterfaces.Contains(symbols.EntitySystemInterface))
            return;

        var expectedPrefix = GetExpectedPrefix(symbol, side);
        var actualPrefix = GetPrefix(symbol);

        if (actualPrefix != expectedPrefix)
        {
            var baseName = GetBaseName(symbol);
            var fixedName = $"{expectedPrefix}{baseName}";
            context.ReportDiagnostic(
                Diagnostic.Create(
                    EntitySystemNamingRule,
                    symbol.Locations[0],
                    symbol.Name,
                    fixedName
                )
            );
        }
    }

    private static string GetPrefix(INamedTypeSymbol symbol)
    {
        if (symbol.Name.StartsWith("Shared"))
            return "Shared";
        if (symbol.Name.StartsWith("Server"))
            return "Server";
        if (symbol.Name.StartsWith("Client"))
            return "Client";
        return string.Empty;
    }

    private static string GetExpectedPrefix(INamedTypeSymbol symbol, AssemblySide side)
    {
        // No prefixes used in Shared.
        if (side == AssemblySide.Shared)
            return string.Empty;

        // If our direct parent class has a different base name than us, we don't need a prefix.
        // For example, StationEventSystem is a Server system that inherits from GameRuleSystem
        // but it does not need the prefix to make it ServerStationEventSystem because there
        // is no StationEventSystem in Shared to distinguish it from.
        if (!InheritsFromSameBaseName(symbol))
            return string.Empty;

        // Return the appropriate prefix for the given side.
        return side switch
        {
            AssemblySide.Client => "Client",
            AssemblySide.Server => "Server",
            _ => throw new ArgumentException("Unexpected value", nameof(side))
        };
    }

    private static string GetBaseName(INamedTypeSymbol symbol)
    {
        return symbol.Name.Substring(GetPrefix(symbol).Length);
    }

    private static bool InheritsFromSameBaseName(INamedTypeSymbol symbol)
    {
        if (symbol.BaseType == null)
            return false;

        return GetBaseName(symbol) == GetBaseName(symbol.BaseType);
    }
}
