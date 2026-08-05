#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using static Robust.Roslyn.Shared.Diagnostics;

namespace Robust.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class EntitySystemSubscriptionConversionFixer : CodeFixProvider
{
    private const string AttributeNamespace = "Robust.Shared.Analyzers";

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
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

        var span = diagnostic.Location.SourceSpan;
        var invocationSyntax = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
        var classSyntax = invocationSyntax?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

        if (invocationSyntax is null || classSyntax is null)
            return;

        // Get the name of the Attribute we need to add to the event handler method.
        if (diagnostic.Properties[EntitySystemSubscriptionConversionAnalyzer.AttributeNameKey] is not string attributeName)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            "Convert subscription to attribute",
            c => ConvertSubscription(context.Document, invocationSyntax, classSyntax, attributeName, c),
            "Convert subscription to attribute"
        ), diagnostic);
    }

    private static async Task<Solution> ConvertSubscription(
        Document document,
        InvocationExpressionSyntax invocationSyntax,
        ClassDeclarationSyntax classSyntax,
        string attributeName,
        CancellationToken c)
    {
        // Get the identifier of the event handler method.
        if (invocationSyntax.ArgumentList.Arguments[0].Expression is not IdentifierNameSyntax handlerMethodIdentifer)
            throw new InvalidOperationException($"Exception determining event handler method identifier for {invocationSyntax}");

        var model = await document.GetSemanticModelAsync(c);
        if (model.GetSymbolInfo(handlerMethodIdentifer, c).Symbol is not IMethodSymbol handlerMethodSymbol)
            throw new InvalidOperationException($"Failed to find event handler method {handlerMethodIdentifer}");

        if (model.GetDeclaredSymbol(classSyntax) is not { } classSymbol)
            throw new InvalidOperationException($"Failed to find symbol for class {classSyntax.Identifier}");

        if (model?.GetOperation(invocationSyntax) is not IInvocationOperation invocationOperation)
            throw new InvalidOperationException($"Failed to find invocation operation");

        var beforeTypes = GetTypesList(invocationOperation, "before");
        var afterTypes = GetTypesList(invocationOperation, "after");

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
        // If the event handler is in the same document as the Initialize method, just reuse the same editor.
        var handlerEditor = (handlerDocId == document.Id) ? initializeEditor : await editor.GetDocumentEditorAsync(handlerDocId, c);
        // Make our changes to the document containing the event handler method.
        ModifyHandler(handlerEditor, handlerMethodSymbol, attributeName, beforeTypes, afterTypes);

        // Make sure the class is marked as partial.
        EnsureClassPartial(initializeEditor, classSymbol, classSyntax);

        // Return the modified solution.
        return editor.GetChangedSolution();
    }

    /// <summary>
    /// Edits the document containing the Intialize method.
    /// Removes the SubscribeWhateverEvent method invocation.
    /// </summary>
    /// <param name="editor">An editor for the document containing the Initialize method.</param>
    /// <param name="invocationSyntax">The SyntaxNode for the invocation of the Initialize method.</param>
    private static void ModifyInitialize(
        DocumentEditor editor,
        InvocationExpressionSyntax invocationSyntax)
    {
        // Remove the SubscribeWhateverEvent invocation from the Initialize method.
        editor.RemoveNode(invocationSyntax.Parent!, SyntaxRemoveOptions.KeepUnbalancedDirectives);
    }

    /// <summary>
    /// Edits the document containing the event handler method.
    /// Adds the SubscribeWhateverEventAttribute to the method.
    /// </summary>
    /// <param name="editor">An editor for the document containing the event handler method.</param>
    /// <param name="handlerMethodSymbol">The symbol for the event handler method.</param>
    /// <param name="attributeName">The name of the Attribute to be added.</param>
    private static void ModifyHandler(
        DocumentEditor editor,
        IMethodSymbol handlerMethodSymbol,
        string attributeName,
        IEnumerable<ExpressionSyntax> beforeTypes,
        IEnumerable<ExpressionSyntax> afterTypes
        )
    {
        // Get the syntax node for the event handler method.
        var handlerMethodSyntax = handlerMethodSymbol.DeclaringSyntaxReferences.First().GetSyntax() as MethodDeclarationSyntax;

        // Generate an annotation containing the full name of the attribute we're adding.
        // The magic string "SymbolId" makes this a SymbolAnnotation for Simplifier.AddImportsAnnotation to use.
        var symbolAnnotation = new SyntaxAnnotation("SymbolId", $"{AttributeNamespace}.{attributeName}Attribute");

        // Create the identifier for the attribute, annotating it with the full class name and AddImportsAnnotation.
        // When Roslyn applies this code fix, AddImportsAnnotation tells it to add any missing using directives,
        // but it needs the full name of the class to be able to do so.
        var identifier = editor.Generator.IdentifierName(attributeName).WithAdditionalAnnotations(symbolAnnotation, Simplifier.AddImportsAnnotation);

        // Generate the SubscribeWhateverEvent attribute.
        var attr = editor.Generator.Attribute(identifier);

        var before = BuildArgument(beforeTypes, "before");
        var after = BuildArgument(afterTypes, "after");

        attr = editor.Generator.AddAttributeArguments(attr, [before, after]);

        // Add the attribute to the event handler method.
        editor.AddAttribute(handlerMethodSyntax!, attr);
    }

    /// <summary>
    /// Marks the class as partial if it's not already.
    /// </summary>
    private static void EnsureClassPartial(
        DocumentEditor editor,
        INamedTypeSymbol classSymbol,
        ClassDeclarationSyntax classSyntax)
    {
        // Use the current modifiers as a base.
        var oldModifiers = DeclarationModifiers.From(classSymbol);
        // Add the partial modifier if it's not already there.
        editor.SetModifiers(classSyntax, oldModifiers.WithPartial(true));
    }

    private static IEnumerable<ExpressionSyntax> GetTypesList(IInvocationOperation invocationOperation, string parameter)
    {
        var arg = invocationOperation.Arguments.Where(arg => arg.Parameter?.Name == parameter).SingleOrDefault();
        if (arg.Value is IDefaultValueOperation or null)
            return [];
        var expression = (arg.Syntax as ArgumentSyntax)?.Expression;
        return expression switch
        {
            CollectionExpressionSyntax collection => collection.Elements.OfType<ExpressionElementSyntax>().Select(e => e.Expression),
            ArrayCreationExpressionSyntax arrayCreation => arrayCreation.Initializer?.Expressions ?? [],
            ImplicitArrayCreationExpressionSyntax implicitArrayCreation => implicitArrayCreation.Initializer.Expressions,
            _ => throw new InvalidOperationException("Invalid types list")
        };
        //var node = arg.Value.Syntax;
        //var node = invocationOperation.Arguments.Where(arg => arg.Parameter?.Name == parameter).Select(arg => arg.Value.Syntax).SingleOrDefault();
        // return node switch
        // {
        //     ImplicitArrayCreationExpressionSyntax implicitArrayCreation => implicitArrayCreation.Initializer.Expressions.Cast<TypeOfExpressionSyntax>(),
        //     CollectionExpressionSyntax collection => collection.Elements.Select(el => (el as ExpressionElementSyntax)?.Expression).Cast<TypeOfExpressionSyntax>(),
        //     null => [],
        //     _ => throw new InvalidOperationException("Invalid types list")
        // };
    }

    private static AttributeArgumentSyntax BuildArgument(IEnumerable<ExpressionSyntax> types, string name)
    {
        var nameColon = SyntaxFactory.NameColon(name);
        var syntaxList = SyntaxFactory.SeparatedList<CollectionElementSyntax>(types.Select(SyntaxFactory.ExpressionElement));
        var collection = SyntaxFactory.CollectionExpression(syntaxList);
        return SyntaxFactory.AttributeArgument(null, nameColon, collection);
    }
}
