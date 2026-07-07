#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Robust.Roslyn.Shared.Diagnostics;

namespace Robust.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class EntitySystemSubscriptionConversionFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
    [
        IdEntitySystemSubscriptionConversionPossible
    ];

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
                case IdEntitySystemSubscriptionConversionPossible:
                    return RegisterSubscriptionConversion(context, diagnostic);
            }
        }

        return Task.CompletedTask;
    }

    private static async Task RegisterSubscriptionConversion(CodeFixContext context, Diagnostic diagnostic)
    {
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
        var root = await semanticModel!.SyntaxTree.GetRootAsync(context.CancellationToken);
        //var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

        var span = diagnostic.Location.SourceSpan;
        var invocationSyntax = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
        var classSyntax = invocationSyntax?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
        var classSymbol = semanticModel.GetDeclaredSymbol(classSyntax!);

        if (invocationSyntax is null || classSyntax is null || classSymbol is null)
            return;

        if (diagnostic.Properties[EntitySystemSubscriptionConversionAnalyzer.AttributeNameKey] is not string attributeName)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            "Convert subscription to attribute",
            c => ConvertSubscription(context.Document, invocationSyntax, classSymbol, classSyntax, attributeName, c),
            "Convert subscription to attribute"
        ), diagnostic);
    }

    private static async Task<Solution> ConvertSubscription(
        Document document,
        InvocationExpressionSyntax invocationSyntax,
        INamedTypeSymbol classSymbol,
        ClassDeclarationSyntax classSyntax,
        string attributeName,
        CancellationToken c)
    {
        // Get the identifier of the event handler method.
        if (invocationSyntax.ArgumentList.Arguments[0].Expression is not IdentifierNameSyntax handlerMethodIdentifer)
            throw new InvalidOperationException();

        // Use the identifier to get the symbol for the event handler method.
        var handlerMethodSymbol = classSymbol.GetMembers(handlerMethodIdentifer.Identifier.Text).Single();

        // Create a SolutionEditor to edit multiple documents without worrying about immutability.
        // The Initialize method might be in a different document than the handler, thanks to partial classes.
        var editor = new SolutionEditor(document.Project.Solution);

        // Get an editor for the document containing the Initialize method.
        var initializeEditor = await editor.GetDocumentEditorAsync(document.Id, c);
        // Make our changes to the document containing the Initialize method.
        ModifyInitialize(initializeEditor, invocationSyntax);

        // Find the ID for the document containing the event handler method.
        var handlerDocId = editor.OriginalSolution.GetDocumentId(handlerMethodSymbol.DeclaringSyntaxReferences.First().SyntaxTree);

        // Get an editor for the document containing the event handler method.
        var handlerEditor = await editor.GetDocumentEditorAsync(handlerDocId, c);
        // Make our changes to the document containing the event handler method.
        ModifyHandler(handlerEditor, handlerMethodSymbol, attributeName);

        //
        EnsureClassPartial(initializeEditor, classSymbol, classSyntax);

        // Return the modified solution
        return editor.GetChangedSolution();
    }

    private static void ModifyInitialize(
        DocumentEditor editor,
        InvocationExpressionSyntax token)
    {
        // Remove the SubscribeWhateverEvent invocation from the Initialize method
        editor.RemoveNode(token.Parent!);
    }

    private static void ModifyHandler(
        DocumentEditor editor,
        ISymbol handlerMethodSymbol,
        string attributeName)
    {
        // Get the syntax node for the event handler method
        var handlerMethodSyntax = handlerMethodSymbol.DeclaringSyntaxReferences.First().GetSyntax() as MethodDeclarationSyntax;

        // Generate the SubscribeWhateverEvent attribute
        var attr = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attributeName));
        // Add the attribute to the event handler method
        editor.AddAttribute(handlerMethodSyntax!, attr);
    }

    private static void EnsureClassPartial(
        DocumentEditor editor,
        INamedTypeSymbol classSymbol,
        ClassDeclarationSyntax classSyntax)
    {
        var oldModifiers = DeclarationModifiers.From(classSymbol);
        editor.SetModifiers(classSyntax, oldModifiers.WithPartial(true));
    }
}
