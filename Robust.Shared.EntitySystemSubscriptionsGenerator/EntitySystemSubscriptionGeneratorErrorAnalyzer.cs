using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Roslyn.Shared;
using Robust.Roslyn.Shared.Helpers;

namespace Robust.Shared.EntitySystemSubscriptionsGenerator;

[Generator(LanguageNames.CSharp)]
public class EntitySystemSubscriptionGeneratorErrorAnalyzer : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        VerifyAnnotatedMethodIsInPartialIEntitySystem(context, KnownTypes.AllSubscriptionMemberAttributeName);
        VerifyAnnotatedMethodIsInPartialIEntitySystem(context, KnownTypes.NetworkSubscriptionMemberAttributeName);
        VerifyAnnotatedMethodIsInPartialIEntitySystem(context, KnownTypes.LocalSubscriptionMemberAttributeName);
        VerifyAnnotatedMethodIsInPartialIEntitySystem(context, KnownTypes.CallAfterSubscriptionsAttributeName);


        VerifyAnnotatedMethodHasCorrectSignature(
            context,
            KnownTypes.AllSubscriptionMemberAttributeName,
            m => (EntitySystemSubscriptionGenerator.TryParseEntityEventHandler(m) ?? EntitySystemSubscriptionGenerator.TryParseEntitySessionEventHandler(m)) is not null,
            KnownTypes.NonComponentSubscriptionHandlerTypes
        );
        VerifyAnnotatedMethodHasCorrectSignature(
            context,
            KnownTypes.NetworkSubscriptionMemberAttributeName,
            m => (EntitySystemSubscriptionGenerator.TryParseEntityEventHandler(m) ?? EntitySystemSubscriptionGenerator.TryParseEntitySessionEventHandler(m)) is not null,
            KnownTypes.NonComponentSubscriptionHandlerTypes
        );
        VerifyAnnotatedMethodHasCorrectSignature(
            context,
            KnownTypes.LocalSubscriptionMemberAttributeName,
            m => (
                EntitySystemSubscriptionGenerator.TryParseEntityEventHandler(m) ??
                EntitySystemSubscriptionGenerator.TryParseEntitySessionEventHandler(m) ??
                EntitySystemSubscriptionGenerator.TryParseComponentEventHandler(m) ??
                EntitySystemSubscriptionGenerator.TryParseEntityEventRefHandler(m)
            ) is not null,
            string.Join(", ", KnownTypes.NonComponentSubscriptionHandlerTypes, KnownTypes.ComponentSubscriptionHandlerTypes)
        );
        VerifyAnnotatedMethodHasCorrectSignature(
            context,
            KnownTypes.CallAfterSubscriptionsAttributeName,
            EntitySystemSubscriptionGenerator.TakesNoParameters,
            KnownTypes.CallAfterSubscriptionsHandlerTypes
        );

        // TODO Complain when:
        //  - Annotations are in a class which already has an `Initialize` (Maybe? Maybe just let the compiler choke on this one)
    }

    private static void RegisterDiagnosticReporting(
        IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<Diagnostic?> diagnostics
    )
    {
        context.RegisterSourceOutput(
            diagnostics.Where(it => it is not null),
            (productionContext, diagnostic) => productionContext.ReportDiagnostic(diagnostic!)
        );
    }

    private static void VerifyAnnotatedMethodIsInPartialIEntitySystem(
        IncrementalGeneratorInitializationContext context,
        string annotationName
    )
    {
        var diagnostics = context.SyntaxProvider.ForAttributeWithMetadataName(
            annotationName,
            (node, _) => node is MethodDeclarationSyntax,
            (syntaxContext, _) =>
            {
                var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

                if (syntaxContext.TargetSymbol.ContainingType is not { } containingSymbol ||
                    !TypeSymbolHelper.ImplementsInterface(containingSymbol, KnownTypes.IEntitySystemTypeName))
                {
                    diagnostics.Add(Diagnostic.Create(
                        Diagnostics.NotIEntitySystem,
                        syntaxContext.TargetSymbol.Locations[0],
                        syntaxContext.TargetSymbol.ContainingType?.Name ?? "<unknown>"
                    ));
                }

                if (syntaxContext.TargetNode.Parent is not TypeDeclarationSyntax containingSyntax ||
                    !containingSyntax.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)))
                {
                    diagnostics.Add(Diagnostic.Create(
                        Diagnostics.NotPartial,
                        syntaxContext.TargetSymbol.Locations[0]
                    ));
                }

                return diagnostics.ToImmutable().AsEquatableArray();
            }
        );

        RegisterDiagnosticReporting(
            context,
            diagnostics.SelectMany((array, _) => array as IEnumerable<Diagnostic?>)
        );
    }

    private static void VerifyAnnotatedMethodHasCorrectSignature(
        IncrementalGeneratorInitializationContext context,
        string annotationName,
        Func<IMethodSymbol, bool> hasCorrectParameters,
        string acceptableHandlerTypes
    )
    {
        var diagnostics = context.SyntaxProvider.ForAttributeWithMetadataName(
            annotationName,
            (node, _) => node is MethodDeclarationSyntax,
            (syntaxContext, _) =>
            {
                if (syntaxContext.TargetSymbol is not IMethodSymbol method ||
                    !method.ReturnsVoid ||
                    !hasCorrectParameters(method))
                {
                    return Diagnostic.Create(
                        Diagnostics.BadMethodSignature,
                        syntaxContext.TargetSymbol.Locations[0],
                        annotationName,
                        acceptableHandlerTypes
                    );
                }

                return null;
            }
        );

        RegisterDiagnosticReporting(context, diagnostics);
    }
}
