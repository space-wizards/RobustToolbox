using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Roslyn.Shared;
using System.Collections.Immutable;

namespace Robust.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class HasDependencyCodeFixProvider : CodeFixProvider
{
    private const string TitleOptional = "Make Nullable";
    private const string TitleRequired = "Remove Nullable";

    public override ImmutableArray<string> FixableDiagnosticIds =>
    [
        Diagnostics.IdDependencyNullable, Diagnostics.IdDependencyNotNullable
    ];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case Diagnostics.IdDependencyNotNullable:
                {
                    var diagnosticSpan = diagnostic.Location.SourceSpan;
                    var declaration = root?.FindToken(diagnosticSpan.Start).Parent?.FirstAncestorOrSelf<FieldDeclarationSyntax>();
                    if (declaration == null) continue;
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            TitleOptional,
                            c => FixOptionalField(context.Document, declaration, c),
                            TitleOptional),
                        diagnostic);
                    break;
                }
                case Diagnostics.IdDependencyNullable:
                {
                    var diagnosticSpan = diagnostic.Location.SourceSpan;
                    var declaration = root?.FindToken(diagnosticSpan.Start).Parent!.FirstAncestorOrSelf<FieldDeclarationSyntax>();
                    if (declaration == null) continue;
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            TitleRequired,
                            c => FixRequiredField(context.Document, declaration, c),
                            TitleRequired),
                        diagnostic);
                    break;
                }
            }
        }
    }

    private static async Task<Document> FixRequiredField(
        Document document,
        FieldDeclarationSyntax field,
        CancellationToken cancel)
    {
        var root = (await document.GetSyntaxRootAsync(cancel))!;
        var oldType = field.Declaration.Type;
        if (field.Declaration.Type is not NullableTypeSyntax nullableType)
            return document;
        var newType =  nullableType.ElementType
            .WithoutTrivia()
            .WithTriviaFrom(nullableType);

        var newField = field.ReplaceNode(oldType, newType);
        return document.WithSyntaxRoot(root.ReplaceNode(field, newField));
    }

    private static async Task<Document> FixOptionalField(
        Document document,
        FieldDeclarationSyntax field,
        CancellationToken cancel)
    {
        var root = (await document.GetSyntaxRootAsync(cancel))!;
        var oldType = field.Declaration.Type;
        if (oldType is NullableTypeSyntax)
            return document;

        var newType = SyntaxFactory.NullableType(
                oldType.WithoutTrivia())
            .WithTriviaFrom(oldType);

        var newField = field.ReplaceNode(oldType, newType);
        return document.WithSyntaxRoot(root.ReplaceNode(field, newField));
    }

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }
}
