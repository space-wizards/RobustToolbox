#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Robust.Roslyn.Shared.Diagnostics;

namespace Robust.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class ProxyForFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
    [
        IdPreferProxy,
        IdProxyForRedundantMethodName,
    ];

    public override FixAllProvider? GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case IdPreferProxy:
                    return RegisterUseProxy(context, diagnostic);
                case IdProxyForRedundantMethodName:
                    return RegisterRemoveRedundantMethodName(context, diagnostic);
            }
        }

        return Task.CompletedTask;
    }

    private async Task RegisterUseProxy(CodeFixContext context, Diagnostic diagnostic)
    {
        throw new NotImplementedException();
    }

    private async Task RegisterRemoveRedundantMethodName(CodeFixContext context, Diagnostic diagnostic)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var span = diagnostic.Location.SourceSpan;
        var token = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<AttributeArgumentSyntax>().First();

        if (token == null)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            "Remove method name parameter",
            c => RegisterRemoveRedundantMethodName(context.Document, token, c),
            "Remove method name parameter"
        ), diagnostic);
    }

    private async Task<Document> RegisterRemoveRedundantMethodName(Document document, AttributeArgumentSyntax token, CancellationToken cancellation)
    {
        var root = (CompilationUnitSyntax?)await document.GetSyntaxRootAsync(cancellation);
        var model = await document.GetSemanticModelAsync(cancellation);

        if (model == null)
            return document;

        if (token.Parent is not AttributeArgumentListSyntax listSyntax)
            return document;

        var newListSyntax = listSyntax.WithArguments(listSyntax.Arguments.Remove(token));
        root = root!.ReplaceNode(listSyntax, newListSyntax);
        return document.WithSyntaxRoot(root);
    }
}
