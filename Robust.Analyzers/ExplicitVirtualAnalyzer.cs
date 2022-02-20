using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExplicitVirtualAnalyzer : DiagnosticAnalyzer
{
    internal const string Attribute = "Robust.Shared.Analyzers.VirtualAttribute";

    [SuppressMessage("ReSharper", "RS2008")]
    private static readonly DiagnosticDescriptor Rule = new(
        Diagnostics.IdExplicitVirtual,
        "Class must be explicitly marked as [Virtual], abstract, static or sealed",
        "Class must be explicitly marked as [Virtual], abstract, static or sealed",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Class must be explicitly marked as [Virtual], abstract, static or sealed.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
    }

    private static bool HasAttribute(INamedTypeSymbol namedTypeSymbol, INamedTypeSymbol attrSymbol)
    {
        return namedTypeSymbol.GetAttributes()
            .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attrSymbol));
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var attrSymbol = context.Compilation.GetTypeByMetadataName(Attribute);
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
        if (classSymbol == null)
            return;

        if (classSymbol.IsSealed || classSymbol.IsAbstract || classSymbol.IsStatic)
            return;

        if (HasAttribute(classSymbol, attrSymbol))
            return;

        var diag = Diagnostic.Create(Rule, classDecl.Keyword.GetLocation());
        context.ReportDiagnostic(diag);
    }
}

// Doesn't work as I'd hoped: Roslyn doesn't provide an API for global usings and I can't get batch changes to work.
/*
[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class ExplicitVirtualCodeFixProvider : CodeFixProvider
{
    private const string TitleSealed = "Annotate class as sealed.";
    private const string TitleVirtual = "Annotate class as [Virtual].";
    private const string TitleAbstract = "Annotate class as abstract.";
    private const string TitleStatic = "Annotate class as static.";

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

        foreach (var diagnostic in context.Diagnostics)
        {
            var span = diagnostic.Location.SourceSpan;
            var classDecl = root.FindToken(span.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>()
                .First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    TitleVirtual,
                    c => FixVirtualAsync(context.Document, classDecl, c),
                    TitleVirtual),
                diagnostic);

            context.RegisterCodeFix(
                CodeAction.Create(
                    TitleStatic,
                    c => FixStaticAsync(context.Document, classDecl, c),
                    TitleStatic),
                diagnostic);

            context.RegisterCodeFix(
                CodeAction.Create(
                    TitleSealed,
                    c => FixSealedAsync(context.Document, classDecl, c),
                    TitleSealed),
                diagnostic);

            context.RegisterCodeFix(
                CodeAction.Create(
                    TitleAbstract,
                    c => FixAbstractAsync(context.Document, classDecl, c),
                    TitleAbstract),
                diagnostic);
        }
    }

    private async Task<Document> FixVirtualAsync(
        Document document,
        ClassDeclarationSyntax classDecl,
        CancellationToken cancellationToken)
    {
        var ns = "Robust.Shared.Analyzers";
        var attrib = SyntaxFactory.Attribute(SyntaxFactory.ParseName("Virtual"));

        var newClassDecl = classDecl.AddAttributeLists(
            SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new[] { attrib })));

        var root = (CompilationUnitSyntax)await document.GetSyntaxRootAsync(cancellationToken);
        root = root.ReplaceNode(classDecl, newClassDecl);

        var options = await document.GetOptionsAsync(cancellationToken);

        if (root.Usings.All(u => u.Name.ToString() != ns))
        {
            root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns)));
        }

        return document.WithSyntaxRoot(root);
    }

    private async Task<Document> FixStaticAsync(
        Document document,
        ClassDeclarationSyntax classDecl,
        CancellationToken cancellationToken)
    {
        var newClassDecl = classDecl.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

        var root = (CompilationUnitSyntax)await document.GetSyntaxRootAsync(cancellationToken);
        root = root.ReplaceNode(classDecl, newClassDecl);

        return document.WithSyntaxRoot(root);
    }

    private async Task<Document> FixAbstractAsync(
        Document document,
        ClassDeclarationSyntax classDecl,
        CancellationToken cancellationToken)
    {
        var newClassDecl = classDecl.AddModifiers(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));

        var root = (CompilationUnitSyntax)await document.GetSyntaxRootAsync(cancellationToken);
        root = root.ReplaceNode(classDecl, newClassDecl);

        return document.WithSyntaxRoot(root);
    }

    private async Task<Document> FixSealedAsync(
        Document document,
        ClassDeclarationSyntax classDecl,
        CancellationToken cancellationToken)
    {
        var newClassDecl = classDecl.AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword));

        var root = (CompilationUnitSyntax)await document.GetSyntaxRootAsync(cancellationToken);
        root = root.ReplaceNode(classDecl, newClassDecl);

        return document.WithSyntaxRoot(root);
    }

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Diagnostics.IdExplicitVirtual);
}
*/
