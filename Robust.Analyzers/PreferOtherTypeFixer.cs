#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Robust.Roslyn.Shared.Diagnostics;

namespace Robust.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class PreferOtherTypeFixer : CodeFixProvider
{
    private const string PreferOtherTypeAttributeName = "PreferOtherTypeAttribute";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        IdPreferOtherType
    );

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case IdPreferOtherType:
                    return RegisterReplaceType(context, diagnostic);
            }
        }

        return Task.CompletedTask;
    }

    private static async Task RegisterReplaceType(CodeFixContext context, Diagnostic diagnostic)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var span = diagnostic.Location.SourceSpan;
        var token = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<VariableDeclarationSyntax>().First();

        if (token == null)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            "Replace type",
            c => ReplaceType(context.Document, token, c),
            "Replace type"
        ), diagnostic);
    }

    private static async Task<Document> ReplaceType(Document document, VariableDeclarationSyntax syntax, CancellationToken cancellation)
    {
        var root = (CompilationUnitSyntax?) await document.GetSyntaxRootAsync(cancellation);
        var model = await document.GetSemanticModelAsync(cancellation);

        if (model == null)
            return document;

        if (syntax.Type is not GenericNameSyntax genericNameSyntax)
            return document;
        var genericTypeSyntax = genericNameSyntax.TypeArgumentList.Arguments[0];
        if (model.GetSymbolInfo(genericTypeSyntax).Symbol is not {} genericTypeSymbol)
            return document;

        var symbolInfo = model.GetSymbolInfo(syntax.Type);
        if (symbolInfo.Symbol?.GetAttributes() is not { } attributes)
            return document;

        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass?.Name != PreferOtherTypeAttributeName)
                continue;

            if (attribute.ConstructorArguments[0].Value is not ITypeSymbol checkedTypeSymbol)
                continue;

            if (!SymbolEqualityComparer.Default.Equals(checkedTypeSymbol, genericTypeSymbol))
                continue;

            if (attribute.ConstructorArguments[1].Value is not ITypeSymbol replacementTypeSymbol)
                continue;

            var replacementIdentifier = SyntaxFactory.IdentifierName(replacementTypeSymbol.Name);
            var replacementSyntax = syntax.WithType(replacementIdentifier);

            root = root!.ReplaceNode(syntax, replacementSyntax);
            return document.WithSyntaxRoot(root);
        }

        return document;
    }
}
