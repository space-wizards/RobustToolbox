using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class HasDependenciesCodeFixProvider : CodeFixProvider
{
    private const string TitleRemoveReadonly = "Remove readonly from dependency";
    private const string TitlePartial = "Add partial";

    public override ImmutableArray<string> FixableDiagnosticIds =>
    [
        Diagnostics.IdHasDependenciesReadOnly, Diagnostics.IdHasDependenciesNotPartialParent,
        Diagnostics.IdHasDependenciesNotPartial
    ];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case Diagnostics.IdHasDependenciesReadOnly:
                {
                    var diagnosticSpan = diagnostic.Location.SourceSpan;
                    var declaration = root!.FindToken(diagnosticSpan.Start).Parent!.AncestorsAndSelf()
                        .OfType<FieldDeclarationSyntax>()
                        .First();

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            TitleRemoveReadonly,
                            c => FixReadOnlyAsync(context.Document, declaration, c),
                            TitleRemoveReadonly),
                        diagnostic);
                    break;
                }
                case Diagnostics.IdHasDependenciesNotPartial:
                case Diagnostics.IdHasDependenciesNotPartialParent:
                {
                    var diagnosticSpan = diagnostic.Location.SourceSpan;
                    var declaration = root!.FindToken(diagnosticSpan.Start).Parent!.AncestorsAndSelf()
                        .OfType<TypeDeclarationSyntax>()
                        .First();

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            TitlePartial,
                            c => FixPartialAsync(context.Document, declaration, c),
                            TitlePartial),
                        diagnostic);
                    break;
                }
            }
        }
    }

    private static async Task<Document> FixReadOnlyAsync(
        Document document,
        FieldDeclarationSyntax origDeclaration,
        CancellationToken cancel)
    {
        var readonlyModifier = origDeclaration.Modifiers.Single(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));
        var newDeclaration = origDeclaration.WithModifiers(origDeclaration.Modifiers.Remove(readonlyModifier));
        var formattedField = newDeclaration.WithAdditionalAnnotations(Formatter.Annotation);

        var oldRoot = await document.GetSyntaxRootAsync(cancel);
        var newRoot = oldRoot!.ReplaceNode(origDeclaration, formattedField);

        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> FixPartialAsync(
        Document document,
        TypeDeclarationSyntax declaration,
        CancellationToken cancel)
    {
        var root = (await document.GetSyntaxRootAsync(cancel))!;

        var nodesToPartial = new List<SyntaxNode>();

        do
        {
            if (!declaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                nodesToPartial.Add(declaration);

            declaration = declaration.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        } while (declaration != null);

        root = root.TrackNodes(nodesToPartial.ToArray());

        foreach (var node in nodesToPartial)
        {
            var old = (TypeDeclarationSyntax)root.GetCurrentNode(node)!;
            root = root.ReplaceNode(old, old.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword)));
        }

        return document.WithSyntaxRoot(root);
    }

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }
}
