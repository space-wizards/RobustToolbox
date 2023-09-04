using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using static Robust.Analyzers.Diagnostics;

#nullable enable

namespace Robust.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class DependencyFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        IdDependencyNotPartial,
        IdDependencyNoInjectDependenciesAttribute);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case IdDependencyNotPartial:
                    await DefinitionFixer.RegisterPartialTypeFix(context, diagnostic);
                    break;
                case IdDependencyNoInjectDependenciesAttribute:
                    await RegisterNoInjectDependenciesFix(context, diagnostic);
                    break;
            }
        }
    }

    private static async Task RegisterNoInjectDependenciesFix(CodeFixContext context, Diagnostic diagnostic)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var span = diagnostic.Location.SourceSpan;
        var token = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

        if (token == null)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            "Give type [InjectDependencies]",
            c => AddInjectDependenciesAttribute(context.Document, token, c),
            "Give type [InjectDependencies]"
        ), diagnostic);
    }

    private static async Task<Document> AddInjectDependenciesAttribute(Document document, ClassDeclarationSyntax declaration, CancellationToken cancellation)
    {
        var root = (CompilationUnitSyntax?) await document.GetSyntaxRootAsync(cancellation);

        var generator = SyntaxGenerator.GetGenerator(document);

        var newDeclaration = (ClassDeclarationSyntax) generator.AddAttributes(
            declaration,
            generator.Attribute("InjectDependencies"));

        root = root!.ReplaceNode(declaration, newDeclaration);

        return document.WithSyntaxRoot(root);
    }

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }
}
