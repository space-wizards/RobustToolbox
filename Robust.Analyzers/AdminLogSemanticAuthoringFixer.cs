#nullable enable
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Robust.Roslyn.Shared.Diagnostics;

namespace Robust.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class AdminLogSemanticAuthoringFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [IdAdminLogRedundantExplicitEntities];

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Id != IdAdminLogRedundantExplicitEntities)
                continue;

            return RegisterRemoveRedundantEntitiesAsync(context, diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task RegisterRemoveRedundantEntitiesAsync(CodeFixContext context, Diagnostic diagnostic)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var argument = root?
            .FindToken(diagnostic.Location.SourceSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<ArgumentSyntax>()
            .FirstOrDefault();

        if (argument == null || argument.NameColon?.Name.Identifier.ValueText != "entities")
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Remove redundant entities argument",
                c => RemoveEntitiesArgumentAsync(context.Document, argument, c),
                "Remove redundant entities argument"),
            diagnostic);
    }

    private static async Task<Document> RemoveEntitiesArgumentAsync(Document document, ArgumentSyntax argument, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root == null || argument.Parent is not BaseArgumentListSyntax argumentList)
            return document;

        var updatedList = argumentList.WithArguments(argumentList.Arguments.Remove(argument));
        var updatedRoot = root.ReplaceNode(argumentList, updatedList);
        return document.WithSyntaxRoot(updatedRoot);
    }
}
