using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;

namespace Robust.Shared.EntitySystemSubscriptionsGenerator;

/// <summary>
/// This analyzer ensures that all methods annotated with the relevant subscription attributes are:<ul>
///   <li>In an EntitySystem</li>
///   <li>Have the correct signature for their subscription type</li>
/// </ul></summary>
/// <seealso cref="EntitySystemSubscriptionGenerator"/>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EntitySystemSubscriptionGeneratorErrorAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor BadMethodSignature = new(
        Diagnostics.IdInvalidAMethodSignatureForGeneratedSubscription,
        "Invalid method signature",
        "Method signature is incompatible with required delegate type(s) for \"{0}\". Compatible types are: {1}.",
        "Usage",
        DiagnosticSeverity.Error,
        true
    );

    private static readonly DiagnosticDescriptor NotEntitySystem = new(
        Diagnostics.IdInvalidContainingTypeForGeneratedSubscription,
        $"Method not in {KnownTypes.EntitySystemTypeName}",
        $"Method is declared in type \"{{0}}\" which does not extend {KnownTypes.EntitySystemTypeName}",
        "Usage",
        DiagnosticSeverity.Error,
        true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [BadMethodSignature, NotEntitySystem];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics
        );

        EnsureAnnotatedSubscriptionMethodsAreInAnEntitySystem(context);
        EnsureAnnotatedSubscriptionMethodsHaveCorrectSignatures(context);
    }

    private static void EnsureAnnotatedSubscriptionMethodsAreInAnEntitySystem(AnalysisContext context)
    {
        List<string> attributeNames =
        [
            KnownTypes.AllSubscriptionMemberAttributeName,
            KnownTypes.NetworkSubscriptionMemberAttributeName,
            KnownTypes.LocalSubscriptionMemberAttributeName,
        ];

        context.RegisterCompilationStartAction(c =>
        {
            if (c.Compilation.GetTypeByMetadataName(KnownTypes.EntitySystemTypeName) is not { } entitySystemType)
                return;

            var attributeSymbols = attributeNames
                .Select(attributeName => c.Compilation.GetTypeByMetadataName(attributeName))
                .OfType<INamedTypeSymbol>()
                .ToList();

            c.RegisterSymbolStartAction(
                c =>
                {
                    if (!c.Symbol.GetAttributes()
                            .Select(it => it.AttributeClass)
                            .Intersect(attributeSymbols, SymbolEqualityComparer.IncludeNullability)
                            .Any() ||
                        IsSubtypeOf(c.Symbol.ContainingType, entitySystemType))
                        return;

                    c.RegisterSymbolEndAction(c => c.ReportDiagnostic(Diagnostic.Create(
                        NotEntitySystem,
                        c.Symbol.Locations[0],
                        c.Symbol.ContainingType?.Name ?? "<unknown>"
                    )));
                },
                SymbolKind.Method
            );
        });
    }

    private static bool IsSubtypeOf(ITypeSymbol subtype, INamedTypeSymbol supertype)
    {
        return SymbolEqualityComparer.Default.Equals(subtype.BaseType, supertype) ||
               (subtype.BaseType is not null && IsSubtypeOf(subtype.BaseType, supertype));
    }

    private static void EnsureAnnotatedSubscriptionMethodsHaveCorrectSignatures(AnalysisContext context)
    {
        EnsureAnnotatedSubscriptionMethodHasCorrectSignature(
            context,
            KnownTypes.AllSubscriptionMemberAttributeName,
            m => (EntitySystemSubscriptionGenerator.TryParseEntityEventHandler(m) ??
                  EntitySystemSubscriptionGenerator.TryParseEntitySessionEventHandler(m)) is not null,
            KnownTypes.NonComponentSubscriptionHandlerTypes
        );
        EnsureAnnotatedSubscriptionMethodHasCorrectSignature(
            context,
            KnownTypes.NetworkSubscriptionMemberAttributeName,
            m => (EntitySystemSubscriptionGenerator.TryParseEntityEventHandler(m) ??
                  EntitySystemSubscriptionGenerator.TryParseEntitySessionEventHandler(m)) is not null,
            KnownTypes.NonComponentSubscriptionHandlerTypes
        );
        EnsureAnnotatedSubscriptionMethodHasCorrectSignature(
            context,
            KnownTypes.LocalSubscriptionMemberAttributeName,
            m => (
                EntitySystemSubscriptionGenerator.TryParseEntityEventHandler(m) ??
                EntitySystemSubscriptionGenerator.TryParseEntitySessionEventHandler(m) ??
                EntitySystemSubscriptionGenerator.TryParseComponentEventHandler(m) ??
                EntitySystemSubscriptionGenerator.TryParseEntityEventRefHandler(m)
            ) is not null,
            string.Join(", ",
                KnownTypes.NonComponentSubscriptionHandlerTypes,
                KnownTypes.ComponentSubscriptionHandlerTypes)
        );
    }

    /// Checks that any methods annotated with <paramref name="annotationName"/> have the correct signature as
    /// determined by <paramref name="hasCorrectParameters"/>. If not, a <see cref="BadMethodSignature"/>
    /// diagnostic is emitted, describing how the signature should instead conform to
    /// <paramref name="acceptableHandlerTypes"/>.
    private static void EnsureAnnotatedSubscriptionMethodHasCorrectSignature(
        AnalysisContext context,
        string annotationName,
        Func<IMethodSymbol, bool> hasCorrectParameters,
        string acceptableHandlerTypes
    )
    {
        context.RegisterCompilationStartAction(c =>
        {
            if (c.Compilation.GetTypeByMetadataName(annotationName) is not { } annotationSymbol)
                return;

            context.RegisterSymbolStartAction(
                c =>
                {
                    // The `symbolKind` arg to `RegisterSymbolStartAction` should make this never fail.
                    if (c.Symbol is not IMethodSymbol symbol)
                        throw new Exception($"Expected {nameof(IMethodSymbol)} but got {c.Symbol.GetType().FullName}");

                    if (!symbol.GetAttributes()
                            .Select(it => it.AttributeClass)
                            .Contains(annotationSymbol, SymbolEqualityComparer.IncludeNullability) ||
                        hasCorrectParameters(symbol))
                        return;

                    c.RegisterSymbolEndAction(c => c.ReportDiagnostic(Diagnostic.Create(
                        BadMethodSignature,
                        symbol.Locations[0],
                        annotationName,
                        acceptableHandlerTypes
                    )));
                },
                SymbolKind.Method
            );
        });
    }
}
