#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using static Robust.Roslyn.Shared.Diagnostics;

namespace Robust.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class DefinitionFixer : CodeFixProvider
{
    private const string DataFieldAttributeName = "DataField";

    private const string ViewVariablesAttributeName = "ViewVariables";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        IdDataDefinitionPartial, IdNestedDataDefinitionPartial, IdDataFieldWritable, IdDataFieldPropertyWritable,
        IdDataFieldRedundantTag, IdDataFieldNoVVReadWrite
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
                case IdDataFieldWritable:
                    return RegisterDataFieldFix(context, diagnostic);
                case IdDataFieldPropertyWritable:
                    return RegisterDataFieldPropertyFix(context, diagnostic);
                case IdDataFieldRedundantTag:
                    return RegisterRedundantTagFix(context, diagnostic);
                case IdDataFieldNoVVReadWrite:
                    return RegisterVVReadWriteFix(context, diagnostic);
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
        var token = SyntaxFactory.Token(PartialKeyword);
        var newDeclaration = declaration.AddModifiers(token);

        root = root!.ReplaceNode(declaration, newDeclaration);

        return document.WithSyntaxRoot(root);
    }

    private static async Task RegisterRedundantTagFix(CodeFixContext context, Diagnostic diagnostic)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var span = diagnostic.Location.SourceSpan;
        var token = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().First();

        if (token == null)
            return;

        // Find the DataField attribute
        AttributeSyntax? dataFieldAttribute = null;
        foreach (var attributeList in token.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.Name.ToString() == DataFieldAttributeName)
                {
                    dataFieldAttribute = attribute;
                    break;
                }
            }
            if (dataFieldAttribute != null)
                break;
        }

        if (dataFieldAttribute == null)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            "Remove explicitly set tag",
            c => RemoveRedundantTag(context.Document, dataFieldAttribute, c),
            "Remove explicitly set tag"
        ), diagnostic);
    }

    private static async Task<Document> RemoveRedundantTag(Document document, AttributeSyntax syntax, CancellationToken cancellation)
    {
        var root = (CompilationUnitSyntax?) await document.GetSyntaxRootAsync(cancellation);

        if (syntax.ArgumentList == null)
            return document;

        AttributeSyntax? newSyntax;
        if (syntax.ArgumentList.Arguments.Count == 1)
        {
            // If this is the only argument, delete the ArgumentList so we don't leave empty parentheses
            newSyntax = syntax.RemoveNode(syntax.ArgumentList, SyntaxRemoveOptions.KeepNoTrivia);
        }
        else
        {
            // Remove the first argument, which is the tag
            var newArgs = syntax.ArgumentList.Arguments.RemoveAt(0);
            var newArgList = syntax.ArgumentList.WithArguments(newArgs);
            // Construct a new attribute with the tag removed
            newSyntax = syntax.WithArgumentList(newArgList);
        }

        root = root!.ReplaceNode(syntax, newSyntax!);

        return document.WithSyntaxRoot(root);
    }

    private static async Task RegisterVVReadWriteFix(CodeFixContext context, Diagnostic diagnostic)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var span = diagnostic.Location.SourceSpan;
        var token = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().First();

        if (token == null)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            "Remove ViewVariables attribute",
            c => RemoveVVAttribute(context.Document, token, c),
            "Remove ViewVariables attribute"
        ), diagnostic);
    }

    private static async Task<Document> RemoveVVAttribute(Document document, MemberDeclarationSyntax syntax, CancellationToken cancellation)
    {
        var root = (CompilationUnitSyntax?) await document.GetSyntaxRootAsync(cancellation);

        var newLists = new SyntaxList<AttributeListSyntax>();
        foreach (var attributeList in syntax.AttributeLists)
        {
            var attributes = new SeparatedSyntaxList<AttributeSyntax>();
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.Name.ToString() != ViewVariablesAttributeName)
                {
                    attributes = attributes.Add(attribute);
                }
            }
            // Don't add empty lists []
            if (attributes.Count > 0)
                newLists = newLists.Add(attributeList.WithAttributes(attributes));
        }
        var newSyntax = syntax.WithAttributeLists(newLists);

        root = root!.ReplaceNode(syntax, newSyntax);

        return document.WithSyntaxRoot(root);
    }

    private static async Task RegisterDataFieldFix(CodeFixContext context, Diagnostic diagnostic)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var span = diagnostic.Location.SourceSpan;
        var field = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().FirstOrDefault();

        if (field == null)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            "Make data field writable",
            c => MakeFieldWritable(context.Document, field, c),
            "Make data field writable"
        ), diagnostic);
    }

    private static async Task RegisterDataFieldPropertyFix(CodeFixContext context, Diagnostic diagnostic)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var span = diagnostic.Location.SourceSpan;
        var property = root?.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();

        if (property == null)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            "Make data field writable",
            c => MakePropertyWritable(context.Document, property, c),
            "Make data field writable"
        ), diagnostic);
    }

    private static async Task<Document> MakeFieldWritable(Document document, FieldDeclarationSyntax declaration, CancellationToken cancellation)
    {
        var root = (CompilationUnitSyntax?) await document.GetSyntaxRootAsync(cancellation);
        var token = declaration.Modifiers.First(t => t.IsKind(ReadOnlyKeyword));
        var newDeclaration = declaration.WithModifiers(declaration.Modifiers.Remove(token));

        root = root!.ReplaceNode(declaration, newDeclaration);

        return document.WithSyntaxRoot(root);
    }

    private static async Task<Document> MakePropertyWritable(Document document, PropertyDeclarationSyntax declaration, CancellationToken cancellation)
    {
        var root = (CompilationUnitSyntax?) await document.GetSyntaxRootAsync(cancellation);
        var newDeclaration = declaration;
        var privateSet = newDeclaration
            .AccessorList?
            .Accessors
            .FirstOrDefault(s => s.IsKind(SetAccessorDeclaration) || s.IsKind(InitAccessorDeclaration));

        if (newDeclaration.AccessorList != null && privateSet != null)
        {
            newDeclaration = newDeclaration.WithAccessorList(
                newDeclaration.AccessorList.WithAccessors(
                    newDeclaration.AccessorList.Accessors.Remove(privateSet)
                )
            );
        }

        AccessorDeclarationSyntax setter;
        if (declaration.Modifiers.Any(m => m.IsKind(PrivateKeyword)))
        {
            setter = SyntaxFactory.AccessorDeclaration(
                SetAccessorDeclaration,
                default,
                default,
                SyntaxFactory.Token(SetKeyword),
                default,
                default,
                SyntaxFactory.Token(SemicolonToken)
            );
        }
        else
        {
            setter = SyntaxFactory.AccessorDeclaration(
                SetAccessorDeclaration,
                default,
                SyntaxFactory.TokenList(SyntaxFactory.Token(PrivateKeyword)),
                SyntaxFactory.Token(SetKeyword),
                default,
                default,
                SyntaxFactory.Token(SemicolonToken)
            );
        }

        newDeclaration = newDeclaration.AddAccessorListAccessors(setter);

        root = root!.ReplaceNode(declaration, newDeclaration);

        return document.WithSyntaxRoot(root);
    }
}
