using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Robust.Serialization.Generator.Diagnostics;

namespace Robust.Serialization.Generator;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class DefinitionFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        IdDataDefinitionPartial, IdNestedDataDefinitionPartial
    );

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case IdDataDefinitionPartial:
                    return RegisterPartialTypeFix(context, diagnostic);
                case IdNestedDataDefinitionPartial:
                    return RegisterPartialTypeFix(context, diagnostic);
                // case IdDataFieldWritable:
                    // return RegisterDataFieldFix(context, diagnostic);
            }
        }

        return Task.CompletedTask;
    }

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    private static async Task RegisterPartialTypeFix(CodeFixContext context, Diagnostic diagnostic)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var span = diagnostic.Location.SourceSpan;
        var token = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

        if (token == null)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            "Make type partial",
            c => MakeDataDefinitionPartial(context.Document, token, c),
            "Make type partial"
        ), diagnostic);
    }

    private static async Task<Document> MakeDataDefinitionPartial(Document document, TypeDeclarationSyntax declaration, CancellationToken cancellation)
    {
        var root = (CompilationUnitSyntax?) await document.GetSyntaxRootAsync(cancellation);
        var token = SyntaxFactory.Token(SyntaxKind.PartialKeyword);
        var newDeclaration = declaration.AddModifiers(token);

        root = root!.ReplaceNode(declaration, newDeclaration);

        return document.WithSyntaxRoot(root);
    }

    // TODO
    private static async Task RegisterDataFieldFix(CodeFixContext context, Diagnostic diagnostic)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var span = diagnostic.Location.SourceSpan;
        var field = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();

        if (field != null)
        {
            context.RegisterCodeFix(CodeAction.Create(
                "Make field writable",
                c => MakeFieldWritable(context.Document, field, c),
                "Make field writable"
            ), diagnostic);
            return;
        }

        var property = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().First();

        if (property != null)
        {
            context.RegisterCodeFix(CodeAction.Create(
                "Make property writable",
                c => MakePropertyWritable(context.Document, property, c),
                "Make property writable"
            ), diagnostic);
        }
    }

    private static async Task<Document> MakeFieldWritable(Document document, FieldDeclarationSyntax declaration, CancellationToken cancellation)
    {
        var root = (CompilationUnitSyntax?) await document.GetSyntaxRootAsync(cancellation);
        var token = SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
        var newDeclaration = declaration.WithModifiers(declaration.Modifiers.Remove(token));

        root = root!.ReplaceNode(declaration, newDeclaration);

        return document.WithSyntaxRoot(root);
    }

    private static async Task<Document> MakePropertyWritable(Document document, PropertyDeclarationSyntax declaration, CancellationToken cancellation)
    {
        Debugger.Launch();
        var root = (CompilationUnitSyntax?) await document.GetSyntaxRootAsync(cancellation);
        var token = SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
        var newDeclaration = declaration.WithModifiers(declaration.Modifiers.Remove(token));

        root = root!.ReplaceNode(declaration, newDeclaration);

        return document.WithSyntaxRoot(root);
    }
}
