using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HasDependenciesAnalyzer : DiagnosticAnalyzer
{
    private const string DependencyAttributeName = "Robust.Shared.IoC.DependencyAttribute";

    public static readonly DiagnosticDescriptor DiagnosticNotPartial = new(
        Diagnostics.IdHasDependenciesNotPartial,
        "Type has dependencies but is not partial",
        "Type '{0}' has [Dependency] fields but is not partial. This will be required in the future.",
        "Usage",
        DiagnosticSeverity.Warning,
        true);

    public static readonly DiagnosticDescriptor DiagnosticNotPartialParent = new(
        Diagnostics.IdHasDependenciesNotPartialParent,
        "Type has dependencies but is not in a partial type",
        "Type '{0}' has [Dependency] fields but is nested in a non-partial type. The parent being partial will be required in the future.",
        "Usage",
        DiagnosticSeverity.Warning,
        true);

    public static readonly DiagnosticDescriptor DiagnosticReadOnly = new(
        Diagnostics.IdHasDependenciesReadOnly,
        "Dependency field is readonly",
        "Field '{0}' is a [Dependency] but is readonly. This will be an error in the future.",
        "Usage",
        DiagnosticSeverity.Warning,
        true);

    public static readonly DiagnosticDescriptor DiagnosticPropertyField = new(
        Diagnostics.IdHasDependenciesPropertyField,
        "Property backing fields cannot be a dependency",
        "Property '{0}' has a backing field marked with [Dependency]. This will be an error in the future.",
        "Usage",
        DiagnosticSeverity.Warning,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticNotPartial, DiagnosticNotPartialParent, DiagnosticReadOnly, DiagnosticPropertyField];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static ctx =>
        {
            var attr = ctx.Compilation.GetTypeByMetadataName(DependencyAttributeName);
            if (attr == null)
                return;

            ctx.RegisterSymbolAction(ctx => AnalyzeType(ctx, attr), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeType(SymbolAnalysisContext ctx, INamedTypeSymbol dependencyAttr)
    {
        if (ctx.Symbol is not INamedTypeSymbol typeSymbol)
            return;

        var hasDependencies = false;
        foreach (var fieldSymbol in typeSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (!AttributeHelper.HasAttribute(fieldSymbol, dependencyAttr, out _))
                continue;

            hasDependencies = true;

            if (!fieldSymbol.CanBeReferencedByName)
            {
                if (fieldSymbol.AssociatedSymbol is IPropertySymbol prop)
                {
                    // I wanted to make this work, but ForAttributeWithMetadataName doesn't work with
                    // backing field attributes.
                    // https://github.com/dotnet/roslyn/issues/80511
                    // Putting [Dependency] on the property directly isn't backwards-compatible with the old system.
                    ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticPropertyField,
                        prop.DeclaringSyntaxReferences[0].GetSyntax().GetLocation(),
                        prop.Name));
                }

                continue;
            }

            if (fieldSymbol.IsReadOnly)
            {
                var fieldSyntax =
                    (FieldDeclarationSyntax)fieldSymbol.DeclaringSyntaxReferences[0].GetSyntax().Parent!.Parent!;
                foreach (var modifier in fieldSyntax.Modifiers)
                {
                    if (modifier.IsKind(SyntaxKind.ReadOnlyKeyword))
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticReadOnly,
                            modifier.GetLocation(),
                            fieldSymbol.Name));
                        break;
                    }
                }
            }
        }

        if (!hasDependencies)
            return;

        var origSyntax = (TypeDeclarationSyntax)typeSymbol.DeclaringSyntaxReferences[0].GetSyntax();
        var syntax = origSyntax;

        while (syntax != null)
        {
            var foundPartial = false;
            foreach (var modifier in syntax.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.PartialKeyword))
                {
                    foundPartial = true;
                    break;
                }
            }

            if (!foundPartial)
            {
                var diag = syntax == origSyntax
                    ? DiagnosticNotPartial
                    : DiagnosticNotPartialParent;

                ctx.ReportDiagnostic(Diagnostic.Create(diag,
                    origSyntax.Keyword.GetLocation(),
                    typeSymbol.ToDisplayString()));

                break;
            }

            syntax = syntax.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        }
    }
}
