#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AdminLogSemanticAuthoringAnalyzer : DiagnosticAnalyzer
{
    private const string SharedAdminLogManagerType = "Content.Shared.Administration.Logs.ISharedAdminLogManager";
    private const string AdminLogEntityRefType = "Content.Shared.Administration.Logs.AdminLogEntityRef";
    private const string EntityUidType = "Robust.Shared.GameObjects.EntityUid";
    private const string NetEntityType = "Robust.Shared.GameObjects.NetEntity";
    private const string CommonSessionType = "Robust.Shared.Player.ICommonSession";

    private static readonly DiagnosticDescriptor RedundantExplicitEntitiesRule = new(
        Diagnostics.IdAdminLogRedundantExplicitEntities,
        "Remove redundant explicit admin log entities",
        "The interpolated admin log message already declares these entity semantics; remove redundant explicit 'entities:' unless this is a true Tier 3 exception",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        "Message-first semantic interpolation is the default authoring path for AddStructured().");

    private static readonly DiagnosticDescriptor UseSharedSemanticsHelperRule = new(
        Diagnostics.IdAdminLogUseSharedSemanticsHelper,
        "Use shared admin log semantics helper",
        "Use AdminLogHelpers for actor+tool, self-action, repeated actor/victim bundles, or multi-target explicit semantics instead of rebuilding 'players', 'entities', and 'playerRoles' inline",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        "Shared helpers are the preferred Tier 3 exception path for awkward self-action semantics.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [RedundantExplicitEntitiesRule, UseSharedSemanticsHelperRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(StartCompilation);
    }

    private static void StartCompilation(CompilationStartAnalysisContext context)
    {
        var sharedAdminLogManager = context.Compilation.GetTypeByMetadataName(SharedAdminLogManagerType);
        if (sharedAdminLogManager == null)
            return;

        var adminLogEntityRefType = context.Compilation.GetTypeByMetadataName(AdminLogEntityRefType);

        var analyzer = new AnalyzerState(sharedAdminLogManager, adminLogEntityRefType);
        context.RegisterOperationAction(analyzer.AnalyzeInvocation, OperationKind.Invocation);
    }

    private sealed class AnalyzerState(
        INamedTypeSymbol sharedAdminLogManager,
        INamedTypeSymbol? adminLogEntityRefType)
    {
        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation invocation)
                return;

            if (!IsAdminLogAddStructured(invocation.TargetMethod))
                return;

            var handlerArgument = invocation.Arguments.FirstOrDefault(arg => arg.Parameter?.Name == "handler");
            var interpolations = handlerArgument is null
                ? []
                : GetInterpolations(context, handlerArgument.Value).ToArray();

            AnalyzeRedundantExplicitEntities(context, invocation, interpolations);
            AnalyzeSelfActionHelperUsage(context, invocation);
        }

        private bool IsAdminLogAddStructured(IMethodSymbol method)
        {
            if (method.Name != "AddStructured")
                return false;

            if (SymbolEqualityComparer.Default.Equals(method.ContainingType, sharedAdminLogManager))
                return true;

            foreach (var iface in method.ContainingType.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface, sharedAdminLogManager))
                    return true;
            }

            return false;
        }

        private void AnalyzeRedundantExplicitEntities(
            OperationAnalysisContext context,
            IInvocationOperation invocation,
            IReadOnlyCollection<InterpolatedParticipant> interpolations)
        {
            var entitiesArgument = invocation.Arguments.FirstOrDefault(arg => arg.Parameter?.Name == "entities");
            if (entitiesArgument == null)
                return;

            var explicitEntities = ParseEntityRefs(entitiesArgument.Value).ToArray();
            if (explicitEntities.Length == 0)
                return;

            var playersArgument = invocation.Arguments.FirstOrDefault(arg => arg.Parameter?.Name == "players" && !arg.IsImplicit);
            var playerRolesArgument = invocation.Arguments.FirstOrDefault(arg => arg.Parameter?.Name == "playerRoles" && !arg.IsImplicit);
            if (playersArgument != null && playerRolesArgument != null && IsHelperEligibleBundle(explicitEntities))
                return;

            var semanticMap = BuildInterpolationRoleMap(interpolations);
            if (semanticMap.Count == 0)
                return;

            foreach (var explicitEntity in explicitEntities)
            {
                if (!semanticMap.TryGetValue(explicitEntity.ExpressionText, out var roles) || !roles.Contains(explicitEntity.Role))
                    return;
            }

            context.ReportDiagnostic(Diagnostic.Create(RedundantExplicitEntitiesRule, entitiesArgument.Syntax.GetLocation()));
        }

        private void AnalyzeSelfActionHelperUsage(OperationAnalysisContext context, IInvocationOperation invocation)
        {
            var entitiesArgument = invocation.Arguments.FirstOrDefault(arg => arg.Parameter?.Name == "entities" && !arg.IsImplicit);
            var playersArgument = invocation.Arguments.FirstOrDefault(arg => arg.Parameter?.Name == "players" && !arg.IsImplicit);
            var playerRolesArgument = invocation.Arguments.FirstOrDefault(arg => arg.Parameter?.Name == "playerRoles" && !arg.IsImplicit);

            if (entitiesArgument == null || playersArgument == null || playerRolesArgument == null)
                return;

            var explicitEntities = ParseEntityRefs(entitiesArgument.Value).ToArray();
            if (explicitEntities.Length == 0)
                return;

            var hasInlineActorToolBundle = HasInlineActorToolBundle(explicitEntities);
            var hasInlineActorTargetToolBundle = HasInlineActorTargetToolBundle(explicitEntities);
            var hasInlineActorVictimsBundle = HasInlineActorVictimsBundle(explicitEntities);
            var hasInlineSelfAction = explicitEntities
                .GroupBy(entity => entity.ExpressionText)
                .Any(group => group.Any(entry => entry.Role == "Actor") && group.Any(entry => entry.Role == "Victim"));

            if (!hasInlineSelfAction && !hasInlineActorToolBundle && !hasInlineActorTargetToolBundle && !hasInlineActorVictimsBundle)
                return;

            context.ReportDiagnostic(Diagnostic.Create(UseSharedSemanticsHelperRule, entitiesArgument.Syntax.GetLocation()));
        }

        private static bool IsHelperEligibleBundle(IReadOnlyCollection<ExplicitEntityRef> explicitEntities)
        {
            return HasInlineActorTargetToolBundle(explicitEntities)
                   || HasInlineActorToolBundle(explicitEntities)
                   || HasInlineActorVictimsBundle(explicitEntities)
                   || explicitEntities
                .GroupBy(entity => entity.ExpressionText)
                .Any(group => group.Any(entry => entry.Role == "Actor") && group.Any(entry => entry.Role == "Victim"));
        }

        private static bool HasInlineActorToolBundle(IReadOnlyCollection<ExplicitEntityRef> explicitEntities)
        {
            if (explicitEntities.Count != 2)
                return false;

            var roles = new HashSet<string>(explicitEntities.Select(entity => entity.Role), StringComparer.Ordinal);
            return roles.SetEquals(["Actor", "Tool"]);
        }

        private static bool HasInlineActorVictimsBundle(IReadOnlyCollection<ExplicitEntityRef> explicitEntities)
        {
            if (explicitEntities.Count < 3)
                return false;

            var actorCount = explicitEntities.Count(entity => entity.Role == "Actor");
            var toolCount = explicitEntities.Count(entity => entity.Role == "Tool");
            var victimCount = explicitEntities.Count(entity => entity.Role == "Victim");

            if (actorCount != 1 || victimCount < 2 || toolCount > 1)
                return false;

            return explicitEntities.All(entity => entity.Role is "Actor" or "Victim" or "Tool");
        }

        private static bool HasInlineActorTargetToolBundle(IReadOnlyCollection<ExplicitEntityRef> explicitEntities)
        {
            if (explicitEntities.Count != 3)
                return false;

            var roles = new HashSet<string>(explicitEntities.Select(entity => entity.Role), StringComparer.Ordinal);
            if (!roles.Contains("Actor") || !roles.Contains("Tool"))
                return false;

            return roles.Contains("Victim") || roles.Contains("Target");
        }

        private Dictionary<string, HashSet<string>> BuildInterpolationRoleMap(IReadOnlyCollection<InterpolatedParticipant> interpolations)
        {
            var map = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (var interpolation in interpolations)
            {
                if (interpolation.Role == null)
                    continue;

                if (!map.TryGetValue(interpolation.ExpressionText, out var roles))
                {
                    roles = new HashSet<string>(StringComparer.Ordinal);
                    map[interpolation.ExpressionText] = roles;
                }

                roles.Add(interpolation.Role);
            }

            return map;
        }

        private IEnumerable<InterpolatedParticipant> GetInterpolations(OperationAnalysisContext context, IOperation operation)
        {
            if (operation is IConversionOperation conversion)
                operation = conversion.Operand;

            if (operation is not IInterpolatedStringOperation interpolatedString)
                yield break;

            foreach (var part in interpolatedString.Parts)
            {
                if (part is not IInterpolationOperation interpolation)
                    continue;

                var syntax = interpolation.Syntax as InterpolationSyntax;
                var label = NormalizeInterpolationLabel(syntax?.FormatClause?.FormatStringToken.ValueText);
                var expressionText = NormalizeExpression(interpolation.Expression.Syntax);
                yield return new InterpolatedParticipant(expressionText, label);
            }
        }

        private IEnumerable<ExplicitEntityRef> ParseEntityRefs(IOperation operation)
        {
            foreach (var descendant in operation.DescendantsAndSelf())
            {
                if (descendant is not IObjectCreationOperation creation)
                    continue;

                if (adminLogEntityRefType == null || !SymbolEqualityComparer.Default.Equals(creation.Type, adminLogEntityRefType))
                    continue;

                if (creation.Arguments.Length < 2)
                    continue;

                var entityExpression = NormalizeExpression(creation.Arguments[0].Value.Syntax);
                var roleName = GetRoleName(creation.Arguments[1].Value);
                if (roleName == null)
                    continue;

                yield return new ExplicitEntityRef(entityExpression, roleName);
            }
        }

        private string? GetRoleName(IOperation operation)
        {
            if (operation is IConversionOperation conversion)
                operation = conversion.Operand;

            if (operation is IFieldReferenceOperation fieldReference)
                return fieldReference.Field.Name;

            return operation.ConstantValue.HasValue ? operation.ConstantValue.Value?.ToString() : null;
        }

        private static string NormalizeExpression(SyntaxNode syntax)
        {
            return syntax.WithoutTrivia().ToString().Replace(" ", string.Empty);
        }

        private static string? NormalizeInterpolationLabel(string? label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return null;

            var normalized = label!.Trim().ToLowerInvariant();

            return normalized switch
            {
                "actor" or "user" or "player" => "Actor",
                "victim" => "Victim",
                "target" => "Target",
                "tool" or "using" or "weapon" => "Tool",
                "subject" => "Subject",
                _ => null,
            };
        }
    }

    private readonly record struct InterpolatedParticipant(string ExpressionText, string? Role);
    private readonly record struct ExplicitEntityRef(string ExpressionText, string Role);
}
