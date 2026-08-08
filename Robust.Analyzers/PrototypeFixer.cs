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
        var token = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<AttributeArgumentSyntax>().First();

        if (token == null)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            "Remove explicitly set type",
            c => RemoveType(context.Document, token, c),
            "Remove explicitly set type"
        ), diagnostic);
    }

    private static async Task<Document> RemoveType(Document document, AttributeArgumentSyntax syntax, CancellationToken cancellation)
    {
        var root = (CompilationUnitSyntax?) await document.GetSyntaxRootAsync(cancellation);

        if (syntax.Parent is not AttributeArgumentListSyntax argListSyntax)
            return document;

        if (argListSyntax.Arguments.Count == 1)
        {
            // If this is the only argument, remove the whole argument list so we don't leave empty parentheses
            if (argListSyntax.Parent is not AttributeSyntax attributeSyntax)
                return document;

            var newAttributeSyntax = attributeSyntax.RemoveNode(argListSyntax, SyntaxRemoveOptions.KeepNoTrivia);
            root = root!.ReplaceNode(attributeSyntax, newAttributeSyntax!);
        }
        else
        {
            // Otherwise just remove the argument
            var newArgListSyntax = argListSyntax.WithArguments(argListSyntax.Arguments.Remove(syntax));
            root = root!.ReplaceNode(argListSyntax, newArgListSyntax);
        }


        return document.WithSyntaxRoot(root);
    }
}
