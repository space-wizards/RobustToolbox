#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Robust.Roslyn.Shared.Diagnostics;

namespace Robust.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class PrototypeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [IdPrototypeRedundantType];

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
                case IdPrototypeRedundantType:
                    return RegisterRemoveType(context, diagnostic);
            }
        }

        return Task.CompletedTask;
    }

    private static async Task RegisterRemoveType(CodeFixContext context, Diagnostic diagnostic)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var span = diagnostic.Location.SourceSpan;
        var token = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<AttributeSyntax>().First();

        if (token == null)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            "Remove explicitly set type",
            c => RemoveType(context.Document, token, c),
            "Remove explicitly set type"
        ), diagnostic);
    }

    private static async Task<Document> RemoveType(Document document, AttributeSyntax syntax, CancellationToken cancellation)
    {
        var root = (CompilationUnitSyntax?) await document.GetSyntaxRootAsync(cancellation);

        if (syntax.ArgumentList == null)
            return document;

        AttributeSyntax? newSyntax;
        if (syntax.ArgumentList.Arguments.Count == 1)
        {
            // If this is the only argument, delete the ArgumentList so we don't leave empty parentheses
            newSyntax = syntax.RemoveNode(syntax.ArgumentList, SyntaxRemoveOptions.KeepNoTrivia);
        }
        else
        {
            // Remove the first argument, which is the type
            var newArgs = syntax.ArgumentList.Arguments.RemoveAt(0);
            var newArgList = syntax.ArgumentList.WithArguments(newArgs);
            // Construct a new attribute with the type removed
            newSyntax = syntax.WithArgumentList(newArgList);
        }

        root = root!.ReplaceNode(syntax, newSyntax!);

        return document.WithSyntaxRoot(root);
    }
}
