#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;
using Robust.Shared.Analyzers;

namespace Robust.Analyzers;

/// <summary>
/// A <see cref="DiagnosticAnalyzer"/> that checks EntitySystem names for compliance with established rules.
/// </summary>
/// <remarks>
/// The rules are:
/// <list type="bullet">
/// <item>An EntitySystem in Shared should not have a side prefix.</item>
/// <item>An EntitySystem in Client or Server that inherits from an EntitySystem with the same name should be prefixed with the appropriate side name.</item>
/// <item>An EntitySystem in Client or Server that does not meet the previous criterion should not have a side prefix.</item>
/// <item>An EntitySystem not in Client, Server, or Shared can do whatever it wants.</item>
/// </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EntitySystemNameAnalyzer : DiagnosticAnalyzer
{
    private const string AssemblySideAttributeType = "Robust.Shared.Analyzers.AssemblySideAttribute";
    private const string EntitySystemInterfaceType = "Robust.Shared.GameObjects.IEntitySystem";
    private const string EntitySystemType = "Robust.Shared.GameObjects.EntitySystem";

    /// <summary>
    /// Simple struct to pass multiple type symbols between methods without making the parameter list huge.
    /// </summary>
    private readonly struct TypeSymbols
    {
        public readonly INamedTypeSymbol AssemblySideAttribute { get; init; }
        public readonly INamedTypeSymbol EntitySystemInterface { get; init; }
        public readonly INamedTypeSymbol EntitySystem { get; init; }
    }

    /// <summary>
    /// The key used to access the correct replacement name in the diagnostic's Properties dictionary.
    /// </summary>
    public const string FixedNameKey = "fixedName";

    public static readonly DiagnosticDescriptor EntitySystemNamingRule = new(
        Diagnostics.IdEntitySystemNamingViolation,
        "EntitySystem naming rule violation",
        "Naming rule violation: {0} should be named {1}",
        "Usage",
        DiagnosticSeverity.Info,
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

            // If this assembly isn't marked with our attribute, we can ignore it.
            if (!AttributeHelper.HasAttribute(ctx.Compilation.Assembly, assemblySideAttributeType, out var sideAttributeData))
                return;
            // Get the Side of this assembly.
            var side = (AssemblySide)sideAttributeData.ConstructorArguments[0].Value!;

            if (ctx.Compilation.GetTypeByMetadataName(EntitySystemInterfaceType) is not { } entitySystemInterfaceType)
                return;
            if (ctx.Compilation.GetTypeByMetadataName(EntitySystemType) is not { } entitySystemType)
                return;

            // Pack our type symbols into a struct to make them easier to pass to other methods.
            var typeSymbols = new TypeSymbols
            {
                AssemblySideAttribute = assemblySideAttributeType,
                EntitySystemInterface = entitySystemInterfaceType,
                EntitySystem = entitySystemType,
            };

            // Inspect each named type defined in the assembly.
            ctx.RegisterSymbolAction(symbolContext => AnalyzeNamedType(symbolContext, side, ref typeSymbols), SymbolKind.NamedType);
        });
    }

    /// <summary>
    /// Examines an <see cref="INamedTypeSymbol"/> representing a class for compliance.
    /// </summary>
    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        AssemblySide side,
        ref TypeSymbols symbols
        )
    {
        // Filter out anything that isn't a class.
        if (context.Symbol is not INamedTypeSymbol symbol || symbol.TypeKind != TypeKind.Class)
            return;

        // Filter out anything that isn't an EntitySystem.
        if (!symbol.AllInterfaces.Contains(symbols.EntitySystemInterface))
            return;

        // Compare the prefix of the name written in the code with the correct name.
        var expectedPrefix = GetExpectedPrefix(symbol, side);
        var actualPrefix = GetPrefix(symbol);
        if (actualPrefix != expectedPrefix)
        {
            var baseName = GetBaseName(symbol);
            var fixedName = $"{expectedPrefix}{baseName}";

            var props = new Dictionary<string, string?>
            {
                // Include the correct replacement name in the diagnostic,
                // so the CodeFix can use it.
                { FixedNameKey, fixedName }
            };

            context.ReportDiagnostic(
                Diagnostic.Create(
                    EntitySystemNamingRule,
                    symbol.Locations[0],
                    props.ToImmutableDictionary(),
                    symbol.Name,
                    fixedName
                )
            );
        }
    }

    /// <summary>
    /// Returns the side prefix of the given symbol, extracted from its name.
    /// </summary>
    /// <returns>
    /// "Shared", "Server", or "Client", if the symbol's name starts with one of those.
    /// Otherwise, an empty string.
    /// </returns>
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

    /// <summary>
    /// Returns the correct side prefix for the given symbol, based on its name, the
    /// <see cref="AssemblySide"/> of the defining assembly, and the name of its immediate parent class.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="side"/> is not <see cref="AssemblySide.Client"/> or <see cref="AssemblySide.Server"/>.
    /// </exception>
    private static string GetExpectedPrefix(INamedTypeSymbol symbol, AssemblySide side)
    {
        // No prefixes should be used in Shared.
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

    /// <summary>
    /// Returns the name of the given symbol without any prefix.
    /// </summary>
    private static string GetBaseName(INamedTypeSymbol symbol)
    {
        return symbol.Name.Substring(GetPrefix(symbol).Length);
    }

    /// <summary>
    /// Checks if a symbol has the same base name as its immediate parent class.
    /// </summary>
    /// <seealso cref="GetBaseName"/>
    private static bool InheritsFromSameBaseName(INamedTypeSymbol symbol)
    {
        if (symbol.BaseType == null)
            return false;

        return GetBaseName(symbol) == GetBaseName(symbol.BaseType);
    }
}
