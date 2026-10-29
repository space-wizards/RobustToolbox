using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;
using Robust.Shared.IoC;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DependencyNullableAnalyzer : DiagnosticAnalyzer
{
    private const string DependencyAttributeType = "Robust.Shared.IoC.DependencyAttribute";

    private static readonly DiagnosticDescriptor RequiredRule = new (
        Diagnostics.IdDependencyNullable,
        "Required dependencies should not be nullable types",
        "[Dependency] field '{0}' is a nullable type",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor OptionalRule = new (
        Diagnostics.IdDependencyNotNullable,
        "Optional dependencies must be nullable types",
        "[Dependency(IoCMode.Optional)] field '{0}' is not a nullable type",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [RequiredRule, OptionalRule];

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
        if (ctx.Symbol is not IFieldSymbol symbol || !AttributeHelper.HasAttribute(symbol, attrSymbol, out var attribute))
            return;
        var mode = IoCMode.Required;
        if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is int posValue)
        {
            mode = (IoCMode)posValue;
        }
        else if (attribute.NamedArguments.Any(na => na.Key == "Mode"))
        {
            var modeArg = attribute.NamedArguments.First(na => na.Key == "Mode").Value;
            if (modeArg.Value is int namedValue)
            {
                mode = (IoCMode)namedValue;
            }
        }
        DiagnosticDescriptor descriptor;
        switch (mode)
        {
            case IoCMode.Required:
            case IoCMode.Differed:
                if (symbol.Type.NullableAnnotation != NullableAnnotation.Annotated)
                    return;
                descriptor = RequiredRule;
                break;
            case IoCMode.Optional:
            case IoCMode.DifferedOptional:
                if (symbol.Type.NullableAnnotation == NullableAnnotation.Annotated)
                    return;
                descriptor = OptionalRule;
                break;
            default:
                return;
        }
        if (symbol.DeclaringSyntaxReferences.Length == 0)
            return;
        var declarator = symbol.DeclaringSyntaxReferences[0]
            .GetSyntax()
            .FirstAncestorOrSelf<VariableDeclarationSyntax>();
        if (declarator == null)
            return;
        ctx.ReportDiagnostic(Diagnostic.Create(descriptor, declarator.Type.GetLocation(), symbol.Name));
    }
}
