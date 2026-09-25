#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Rename;
using static Robust.Roslyn.Shared.Diagnostics;

namespace Robust.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class EntitySystemNameCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
    [
        IdEntitySystemNameCompliance
    ];

    public override FixAllProvider? GetFixAllProvider()
    {
        return null;
    }

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case IdEntitySystemNameCompliance:
                    return RegisterRename(context, diagnostic);
            }
        }

        return Task.CompletedTask;
    }

    private async Task RegisterRename(CodeFixContext context, Diagnostic diagnostic)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken);

        var span = diagnostic.Location.SourceSpan;
        var token = root!.FindToken(span.Start);
        if (model?.GetDeclaredSymbol(token.Parent!, context.CancellationToken) is not INamedTypeSymbol classSymbol)
            return;

        // Get the name of the Attribute we need to add to the event handler method.
        if (diagnostic.Properties[EntitySystemNameAnalyzer.FixedNameKey] is not string fixedName)
            return;

        context.RegisterCodeFix(CodeAction.Create(
            "Rename EntitySystem",
            createChangedSolution:
            c => RenameSystem(context.Document, classSymbol, fixedName, c),
            "RenameEntitySystem"
        ), diagnostic);
    }

    private async Task<Solution> RenameSystem(Document document, INamedTypeSymbol symbol, string fixedName, CancellationToken c)
    {
        var solution = document.Project.Solution;

        var options = new SymbolRenameOptions
        {
            // If the file name matches the class name, we should also rename the file.
            RenameFile = Path.GetFileNameWithoutExtension(document.Name) == symbol.Name,
            RenameInComments = true,
        };

        var fixedSolution = await Renamer.RenameSymbolAsync(solution, symbol, options, fixedName);

        return fixedSolution;
    }
}


