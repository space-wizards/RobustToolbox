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
        var token = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<GenericNameSyntax>().First();

        if (token == null)
            return;

        var replacement = diagnostic.Properties[PreferOtherTypeAnalyzer.ReplacementType];
        if (replacement == null)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            "Replace type",
            c => ReplaceType(context.Document, token, replacement, c),
            "Replace type"
        ), diagnostic);
    }

    private static async Task<Document> ReplaceType(Document document, GenericNameSyntax syntax, string replacement, CancellationToken cancellation)
    {
        var root = (CompilationUnitSyntax?) await document.GetSyntaxRootAsync(cancellation);
        var model = await document.GetSemanticModelAsync(cancellation);

        if (model == null)
            return document;

        var replacementSyntax = SyntaxFactory.IdentifierName(replacement);

        root = root!.ReplaceNode(syntax, replacementSyntax);
        return document.WithSyntaxRoot(root);
    }
}
