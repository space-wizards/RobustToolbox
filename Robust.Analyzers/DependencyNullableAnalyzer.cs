using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DependencyNullableAnalyzer : DiagnosticAnalyzer
{
    private const string DependencyAttributeType = "Robust.Shared.IoC.DependencyAttribute";

    private static readonly DiagnosticDescriptor Rule = new (
        Diagnostics.IdDependencyNullable,
        "Dependencies should not be nullable types",
        "[Dependency] field '{0}' is a nullable type. This has no effect and will be disallowed in the future.",
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
            var attr = ctx.Compilation.GetTypeByMetadataName(DependencyAttributeType);
            if (attr == null)
                return;

            ctx.RegisterSymbolAction(c => CheckField(c, attr), SymbolKind.Field);
        });
    }

    private static void CheckField(SymbolAnalysisContext ctx, INamedTypeSymbol attrSymbol)
    {
        if (ctx.Symbol is not IFieldSymbol symbol)
            return;

        if (!AttributeHelper.HasAttribute(symbol, DependencyAttributeType, out _))
            return;

        if (symbol.Type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            if (symbol.DeclaringSyntaxReferences.Length == 0)
                return;

            var declarator = symbol.DeclaringSyntaxReferences[0]
                .GetSyntax()
                .FirstAncestorOrSelf<VariableDeclarationSyntax>();

            if (declarator == null)
                return;

            ctx.ReportDiagnostic(
                Diagnostic.Create(Rule, declarator.Type.GetLocation(), symbol.Name));
        }
    }
}
