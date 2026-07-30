using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Roslyn.Shared;
using Robust.Roslyn.Shared.Helpers;
using static Robust.Shared.EntitySystemSubscriptionsGenerator.KnownTypes;

namespace Robust.Shared.EntitySystemSubscriptionsGenerator;

/// <summary>
/// This generator implements <c>EntitySystem.AutoSubscriptions()</c> for all <c>EntitySystem</c>s with methods
/// annotated by auto-subscription attributes. In case any attributes are applied to methods incorrectly, this generator
/// just silently ignores them and expects <see cref="EntitySystemSubscriptionGeneratorErrorAnalyzer"/> will complain
/// on its behalf (except in the case of attempting to add generated code to a non-partial type -- we complain about
/// that here).
/// </summary>
/// <seealso cref="EntitySystemSubscriptionGeneratorErrorAnalyzer"/>
[Generator(LanguageNames.CSharp)]
public class EntitySystemSubscriptionGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor NotPartial = new(
        Diagnostics.IdNonPartialContainingTypeForGeneratedSubscription,
        "Containing class must be declared as Partial",
        "Method is declared in type \"{0}\" which is not Partial",
        "Usage",
        DiagnosticSeverity.Error,
        true
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var annotatedGenericEvents = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: GenericEventAttributeName,
                predicate: static (s, _) => s is TypeDeclarationSyntax,
                transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol)
            .Collect();

        var annotatedEntitySystems = Aggregate(
                GetEntityTypeCandidatesContainingAnnotatedMethods(context, AllSubscriptionMemberAttributeName),
                GetEntityTypeCandidatesContainingAnnotatedMethods(context, NetworkSubscriptionMemberAttributeName),
                GetEntityTypeCandidatesContainingAnnotatedMethods(context, LocalSubscriptionMemberAttributeName)
            ) // Get all candidate types containing subscription annotated methods
            .SelectMany((array, _) =>
                array.ToImmutableHashSet(PartialTypeInfo.WithoutLocationEqualityComparer)) // Dedupe
            .Combine(context.CompilationProvider)
            .Select((inputs, cancel) =>
                {
                    // For each EntitySystem we've identified as containing subscriptions...

                    var (partialTypeInfo, compilation) = inputs;
                    if (compilation.GetTypeByMetadataName(partialTypeInfo.GetMetadataName()) is not
                        { } entitySystemType)
                        return new EntitySystemInfo(partialTypeInfo, []);

                    // ... check all methods in the type to see if it's a subscription, assembling subscriptions into an array.
                    var subs = entitySystemType.GetMembers()
                        .OfType<IMethodSymbol>()
                        .Select(method =>
                        {
                            cancel.ThrowIfCancellationRequested();
                            return TryParseSubscriptions(method);
                        })
                        .OfType<SubscriptionInfo>()
                        .ToImmutableArray();

                    return new EntitySystemInfo(partialTypeInfo, subs);
                }
            );

        context.RegisterImplementationSourceOutput(
            // Only deal with types that have subscriptions.
            annotatedEntitySystems.Combine(annotatedGenericEvents).Where(tuple => !tuple.Left.Subscriptions.IsEmpty),
            (productionContext, tuple) =>
            {
                var (partialTypeInfo, subscriptions) = tuple.Left;
                if (partialTypeInfo.CheckPartialDiagnostic(productionContext, NotPartial))
                    return;

                var subscriptionsSyntax = new StringBuilder();
                foreach (var method in subscriptions)
                {
                    productionContext.CancellationToken.ThrowIfCancellationRequested();
                    var subscriptionMethod = method.Type.ToSubscriptionMethod();

                    // Handle methods with generic types
                    if (method.Generic != null)
                    {
                        var typeParameters = method.Generic.Value.TypeParameters;
                        var optionsPerTypeParam = new List<ImmutableArray<string>>(typeParameters.Length);

                        // Gather all valid types for each type parameter based on constraints
                        foreach (var (_, constraints) in typeParameters)
                        {
                            var filteredTypes = tuple.Right.Where(type => !constraints.Any(constraint =>
                                !TypeSymbolHelper.Inherits(type, constraint) &&
                                !TypeSymbolHelper.ImplementsInterface(type, constraint.ToString())));

                            var validSubstitutions = filteredTypes
                                .SelectMany(type => GetValidClosedTypes(type, tuple.Right))
                                .ToImmutableArray();

                            optionsPerTypeParam.Add(validSubstitutions);
                        }

                        // If any type parameter has no valid replacements, we can't generate generic combinations
                        if (optionsPerTypeParam.Any(opts => opts.Length == 0))
                            continue;

                        // Generate Cartesian Product of all substitutions to satisfy multi-parameter combinations
                        foreach (var combination in CartesianProduct(optionsPerTypeParam))
                        {
                            var combinationList = combination.ToImmutableArray();
                            IEnumerable<string> newArgs = method.TypeArgs;

                            // Replace each type parameter
                            for (int i = 0; i < typeParameters.Length; i++)
                            {
                                var typeParam = typeParameters[i].GenericParameterName;
                                var closedTypeStr = combinationList[i];

                                newArgs = newArgs.Select(arg =>
                                    arg == typeParam
                                        ? closedTypeStr
                                        : Regex.Replace(arg, $@"\b{typeParam}\b", closedTypeStr));
                            }

                            var genericTypeArgs = string.Join(", ", newArgs);
                            subscriptionsSyntax.AppendLine($"        {subscriptionMethod}<{genericTypeArgs}>({method.MethodName});");
                        }
                        continue;
                    }

                    var typeArgs = string.Join(", ", method.TypeArgs);
                    subscriptionsSyntax.AppendLine($"        {subscriptionMethod}<{typeArgs}>({method.MethodName});");
                }

                var builder = new StringBuilder(@"
// <auto-generated />

using Robust.Shared.GameObjects;
using JetBrains.Annotations;

#pragma warning disable CS0618 // Type or member is obsolete: event handlers will have their own obsoletion warnings, dont need to dupe them

");
                partialTypeInfo.WriteHeader(builder);
                builder.AppendLine($@"
{{
    /// <inheritdoc />
    [MustCallBase]
    public override void AutoSubscriptions()
    {{
        base.AutoSubscriptions();

{subscriptionsSyntax}
    }}
}}
");
                partialTypeInfo.WriteFooter(builder);

                productionContext.AddSource(partialTypeInfo.GetGeneratedFileName(), builder.ToString());
            }
        );
    }

    /// Returns <see cref="PartialTypeInfo"/>s for all types in the compilation that contain methods with the given
    /// <paramref name="attributeName">attribute</paramref>.
    private static IncrementalValuesProvider<PartialTypeInfo> GetEntityTypeCandidatesContainingAnnotatedMethods(
        IncrementalGeneratorInitializationContext context,
        string attributeName
    )
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
                attributeName,
                (node, _) => node is MethodDeclarationSyntax,
                (ctx, _) =>
                {
                    if (ctx.TargetSymbol is not IMethodSymbol symbol ||
                        ctx.TargetNode is not MethodDeclarationSyntax { Parent: TypeDeclarationSyntax parentSyntax })
                        return null;

                    return PartialTypeInfo.FromSymbol(symbol.ContainingType, parentSyntax);
                })
            .Where(it => it is not null)
            .Select((it, _) => it ?? throw new("Unreachable"));
    }

    // Helper method to recursively build all valid closed generic types
    private static IEnumerable<string> GetValidClosedTypes(
        INamedTypeSymbol type,
        ImmutableArray<INamedTypeSymbol> allTypes)
    {
        // If it's not generic, there's nothing to fill. Phew.
        if (!type.IsGenericType)
        {
            yield return type.ToString();
            yield break;
        }

        var optionsPerTypeParam = new List<ImmutableArray<string>>(type.TypeParameters.Length);

        // Gather all valid types for each type parameter based on constraints
        foreach (var typeParam in type.TypeParameters)
        {
            var constraints = typeParam.ConstraintTypes;
            var filteredTypes = allTypes.Where(t => !constraints.Any(constraint =>
                !TypeSymbolHelper.Inherits(t, constraint) &&
                !TypeSymbolHelper.ImplementsInterface(t, constraint.ToString())));

            var validSubstitutions = filteredTypes
                .SelectMany(t => GetValidClosedTypes(t, allTypes))
                .ToImmutableArray();

            optionsPerTypeParam.Add(validSubstitutions);
        }

        // If any type parameter has no valid replacements, we can't generate generic combinations
        if (optionsPerTypeParam.Any(opts => opts.Length == 0))
            yield break;

        // Isolate the base name
        var baseTypeName = type.OriginalDefinition.ToString().Split('<')[0];

        // Generate all possible combinations using a Cartesian product and yield the fully constructed strings
        foreach (var combination in CartesianProduct(optionsPerTypeParam))
        {
            yield return $"{baseTypeName}<{string.Join(", ", combination)}>";
        }
    }

    private static IEnumerable<IEnumerable<string>> CartesianProduct(IReadOnlyList<ImmutableArray<string>> sequences)
    {
        IEnumerable<IEnumerable<string>> emptyProduct = [[]];

        return sequences.Aggregate(
            emptyProduct,
            (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Concat([item]));
    }

    /// Tries to parse <paramref name="method"/>'s signature as an even subscription, returning the information required
    /// to make the subscription function call in the generated code. Returns <c>null</c> if the method is not a
    /// subscription, or is a subscription and its signature is invalid.
    private static SubscriptionInfo? TryParseSubscriptions(IMethodSymbol method)
    {
        return TryParseSubscription(
            method,
            AllSubscriptionMemberAttributeName,
            m => TryParseEntityEventHandler(m) ?? TryParseEntitySessionEventHandler(m)
        ) ?? TryParseSubscription(
            method,
            NetworkSubscriptionMemberAttributeName,
            m => TryParseEntityEventHandler(m) ?? TryParseEntitySessionEventHandler(m)
        ) ?? TryParseSubscription(
            method,
            LocalSubscriptionMemberAttributeName,
            m => TryParseEntityEventHandler(m) ??
                 TryParseEntitySessionEventHandler(m) ??
                 TryParseComponentEventHandler(m) ??
                 TryParseEntityEventRefHandler(m) ??
                 TryParseEntityEventRefHandlerGeneric(m)
        );
    }

    /// Tries to parse <paramref name="method"/>'s signature as <c>Robust.Shared.GameObjects.EntityEventHandler</c>.
    /// <returns>The type argument syntax to include in the subscription function call.</returns>
    public static ImmutableArray<string>? TryParseEntityEventHandler(IMethodSymbol method)
    {
        if (method.Parameters.Length != 1 ||
            method.Parameters[0].Type is not INamedTypeSymbol eventType)
            return null;

        return [eventType.ToString()];
    }

    /// Tries to parse <paramref name="method"/>'s signature as <c>Robust.Shared.GameObjects.EntitySessionEventHandler</c>.
    /// <returns>The type argument syntax to include in the subscription function call.</returns>
    public static ImmutableArray<string>? TryParseEntitySessionEventHandler(IMethodSymbol method)
    {
        if (method.Parameters.Length != 2 ||
            method.Parameters[0].Type is not INamedTypeSymbol eventType ||
            !TypeSymbolHelper.ShittyTypeMatch(
                method.Parameters[1].Type,
                EntitySessionEventArgsTypeName
            ))
            return null;

        return [eventType.ToString()];
    }

    /// Tries to parse <paramref name="method"/>'s signature as <c>Robust.Shared.GameObjects.EntityEventRefHandler</c>.
    /// <returns>The type argument syntax to include in the subscription function call.</returns>
    public static ImmutableArray<string>? TryParseEntityEventRefHandler(IMethodSymbol method)
    {
        if (method.Parameters.Length != 2 ||
            method.Parameters[0].Type is not INamedTypeSymbol entityType ||
            method.Parameters[1].Type is not INamedTypeSymbol eventType ||
            method.Parameters[1].RefKind != RefKind.Ref)
            return null;

        if (entityType.OriginalDefinition.ToDisplayString() != EntityTypeName ||
            entityType.TypeArguments is not [INamedTypeSymbol componentType] ||
            !TypeSymbolHelper.ImplementsInterface(componentType, IComponentTypeName))
            return null;

        return [componentType.ToString(), eventType.ToString()];
    }

    /// Tries to parse <paramref name="method"/>'s signature as <c>Robust.Shared.GameObjects.EntityEventRefHandler</c> with type parameters.
    /// <returns>The type argument syntax to include in the subscription function call.</returns>
    public static ImmutableArray<string>? TryParseEntityEventRefHandlerGeneric(IMethodSymbol method)
    {
        if (method.Parameters.Length != 2 ||
            method.Parameters[0].Type is not INamedTypeSymbol entityType ||
            method.Parameters[1].RefKind != RefKind.Ref)
            return null;

        if (method.Parameters[1].Type is not ITypeParameterSymbol &&
            method.Parameters[1].Type is not INamedTypeSymbol { IsGenericType: true })
            return null;

        if (entityType.OriginalDefinition.ToDisplayString() != EntityTypeName ||
            entityType.TypeArguments is not [INamedTypeSymbol componentType] ||
            !TypeSymbolHelper.ImplementsInterface(componentType, IComponentTypeName))
            return null;

        var eventType = method.Parameters[1].Type as ITypeParameterSymbol;
        var eventTypeNamed = method.Parameters[1].Type as INamedTypeSymbol;

        var evStr = string.Empty;

        if (eventType != null)
            evStr = eventType.ToString();
        if (eventTypeNamed != null)
            evStr = eventTypeNamed.ToString();

        return [componentType.ToString(), evStr];
    }

    /// Tries to parse <paramref name="method"/>'s signature as <c>Robust.Shared.GameObjects.ComponentEventHandler</c>.
    /// <returns>The type argument syntax to include in the subscription function call.</returns>
    public static ImmutableArray<string>? TryParseComponentEventHandler(IMethodSymbol method)
    {
        if (method.Parameters.Length != 3 ||
            method.Parameters[0].Type is not INamedTypeSymbol entityUidType ||
            method.Parameters[1].Type is not INamedTypeSymbol componentType ||
            method.Parameters[2].Type is not INamedTypeSymbol eventType ||
            !TypeSymbolHelper.ShittyTypeMatch(entityUidType, EntityUidTypeName) ||
            !TypeSymbolHelper.ImplementsInterface(componentType, IComponentTypeName))
            return null;

        return [componentType.ToString(), eventType.ToString()];
    }

    private static SubscriptionInfo? TryParseSubscription(
        IMethodSymbol method,
        string annotationName,
        Func<IMethodSymbol, ImmutableArray<string>?> parseFunc
    )
    {
        if (annotationName.ToSubscriptionType() is not { } subType ||
            !AttributeHelper.HasAttribute(method, annotationName, out _) ||
            parseFunc(method) is not { } parameters)
            return null;

        var genericInfo = TryParseGeneric(method);
        return new SubscriptionInfo(method.Name, subType, parameters, genericInfo);
    }

    private static GenericInfo? TryParseGeneric(IMethodSymbol method)
    {
        if (method.TypeParameters.Length < 1)
            return null;

        // Only works on type parameters that have constraints.
        // We don't turn constraint types into strings because when generating subscriptions
        // if any of the types will have type parameters, we will also need to fill them in.
        var result = method.TypeParameters
            .Where(typeParam => typeParam.ConstraintTypes.Length > 0)
            .Select(typeParam => (typeParam.ToString(), typeParam.ConstraintTypes))
            .ToImmutableArray();

        return new GenericInfo(result);
    }

    /// Aggregates all of the <typeparamref name="T"/>s across all the given providers into a single array value
    /// provided by the returned provider.
    private static IncrementalValueProvider<ImmutableArray<T>> Aggregate<T>(
        IncrementalValuesProvider<T> first,
        params IncrementalValuesProvider<T>[] more
    )
    {
        return more.Aggregate(
            first.Collect(),
            (acc, valuesProvider) =>
                acc.Combine(valuesProvider.Collect())
                    .Select((values, _) => values.Left.AddRange(values.Right))
        );
    }

    private record struct EntitySystemInfo(PartialTypeInfo Type, EquatableArray<SubscriptionInfo> Subscriptions);

    private record struct SubscriptionInfo(
        string MethodName,
        SubscriptionType Type,
        EquatableArray<string> TypeArgs,
        GenericInfo? Generic);

    private record struct GenericInfo(ImmutableArray<(string GenericParameterName, ImmutableArray<ITypeSymbol> GenericConstraintTypes)> TypeParameters);
}
