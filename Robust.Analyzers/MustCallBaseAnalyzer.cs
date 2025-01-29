using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

#nullable enable

/// <summary>
/// Enforces <c>MustCallBaseAttribute</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MustCallBaseAnalyzer : DiagnosticAnalyzer
{
    private const string Attribute = "Robust.Shared.Analyzers.MustCallBaseAttribute";

    private static readonly DiagnosticDescriptor Rule = new(
        Diagnostics.IdMustCallBase,
        "No base call in overriden function",
        "Overriders of this function must always call the base function",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol { IsOverride: true } method)
            return;

        var attrSymbol = context.Compilation.GetTypeByMetadataName(Attribute);
        if (attrSymbol == null)
            return;

        if (DoesMethodOverriderHaveAttribute(method, attrSymbol) is not { } data)
            return;

        if (data is { onlyOverrides: true, depth: < 2 })
            return;

        var syntax = (MethodDeclarationSyntax) method.DeclaringSyntaxReferences[0].GetSyntax();
        if (HasBaseCall(syntax))
            return;

        var diag = Diagnostic.Create(Rule, syntax.Identifier.GetLocation());
        context.ReportDiagnostic(diag);
    }

    private static (int depth, bool onlyOverrides)? DoesMethodOverriderHaveAttribute(
        IMethodSymbol method,
        INamedTypeSymbol attributeSymbol)
    {
        var depth = 0;
        while (method.OverriddenMethod != null)
        {
            depth += 1;
            method = method.OverriddenMethod;
            if (GetAttribute(method, attributeSymbol) is not { } attribute)
                continue;

            var onlyOverrides = attribute.ConstructorArguments is [{Kind: TypedConstantKind.Primitive, Value: true}];
            return (depth, onlyOverrides);
        }

        return null;
    }

    private static bool HasBaseCall(MethodDeclarationSyntax syntax)
    {
        return syntax.Accept(new BaseCallLocator());
    }

    private static AttributeData? GetAttribute(ISymbol namedTypeSymbol, INamedTypeSymbol attrSymbol)
    {
        return namedTypeSymbol.GetAttributes()
            .SingleOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attrSymbol));
    }

    private sealed class BaseCallLocator : CSharpSyntaxVisitor<bool>
    {
        public override bool VisitBaseExpression(BaseExpressionSyntax node)
        {
            return true;
        }

        public override bool DefaultVisit(SyntaxNode node)
        {
            foreach (var childNode in node.ChildNodes())
            {
                if (childNode is not CSharpSyntaxNode cSharpSyntax)
                    continue;

                if (cSharpSyntax.Accept(this))
                    return true;
            }

            return false;
        }
    }
}
