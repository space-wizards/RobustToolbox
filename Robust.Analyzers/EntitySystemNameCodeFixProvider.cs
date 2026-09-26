#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Rename;
using static Robust.Roslyn.Shared.Diagnostics;

namespace Robust.Analyzers;

/// <summary>
/// A <see cref="CodeFixProvider"/> which enables automatic correction of <see cref="IdEntitySystemNamingViolation"/> diagnostics.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class EntitySystemNameCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
    [
        IdEntitySystemNamingViolation
    ];

    public override FixAllProvider? GetFixAllProvider()
    {
        return new EntitySystemNameFixAllProvider();
    }

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case IdEntitySystemNamingViolation:
                    return RegisterRename(context, diagnostic);
            }
        }

        return Task.CompletedTask;
    }

    private async Task RegisterRename(CodeFixContext context, Diagnostic diagnostic)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken);

        // Find the symbol for the type that triggered this diagnostic.
        var span = diagnostic.Location.SourceSpan;
        var token = root!.FindToken(span.Start);
        if (model?.GetDeclaredSymbol(token.Parent!, context.CancellationToken) is not INamedTypeSymbol classSymbol)
            return;

        // Extract the correct replacement name for the system from the diagnostic's properties.
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
            // Keep our docs up to date.
            RenameInComments = true,
        };

        // Here's where we express our gratitude to the dotnet developers for providing this API.
        var fixedSolution = await Renamer.RenameSymbolAsync(solution, symbol, options, fixedName);

        return fixedSolution;
    }

    /// <summary>
    /// Enables bulk application of <see cref="EntitySystemNameCodeFixProvider"/>.
    /// </summary>
    /// <remarks>
    /// Annoyingly, this does not work with <c>dotnet format</c>, because it cannot rename files.
    /// It does work in IDEs that support FixAll code actions.
    /// </remarks>
    private sealed class EntitySystemNameFixAllProvider : FixAllProvider
    {
        public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            // Group all of our diagnostics by the document containing them.
            var diagnosticsToFix = new Dictionary<Document, ImmutableArray<Diagnostic>>();
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                {
                    var document = fixAllContext.Document!;
                    var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                    diagnosticsToFix.Add(document, diagnostics);
                    break;
                }
                case FixAllScope.Project:
                {
                    var project = fixAllContext.Project;
                    foreach (var document in project.Documents)
                    {
                        var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                        diagnosticsToFix.Add(document, diagnostics);
                    }
                    break;
                }
                case FixAllScope.Solution:
                {
                    var solution = fixAllContext.Solution;
                    foreach (var project in solution.Projects)
                    {
                        foreach (var document in project.Documents)
                        {
                            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                            diagnosticsToFix.Add(document, diagnostics);
                        }
                    }
                    break;
                }
            }

            return CodeAction.Create(
                "Rename all EntitySystems",
                cancellationToken => FixAllDocuments(fixAllContext, diagnosticsToFix, cancellationToken)
            );
        }

        private async Task<Solution> FixAllDocuments(
            FixAllContext context,
            Dictionary<Document, ImmutableArray<Diagnostic>> diagnosticsToFix,
            CancellationToken cancellationToken)
        {
            var solution = context.Solution;
            // For batch fixes, we'll wait and do the file renames at the end.
            var documentsToRename = new List<KeyValuePair<DocumentId, string>>();

            foreach (var documentDiagnostics in diagnosticsToFix)
            {
                foreach (var diagnostic in documentDiagnostics.Value)
                {
                    if (diagnostic.Properties[EntitySystemNameAnalyzer.FixedNameKey] is not string fixedName)
                        continue;

                    // Find the symbol for the type that triggered the diagnostic.
                    var document = documentDiagnostics.Key;
                    var root = await document.GetSyntaxRootAsync(context.CancellationToken);
                    var model = await document.GetSemanticModelAsync(context.CancellationToken);

                    var span = diagnostic.Location.SourceSpan;
                    var token = root!.FindToken(span.Start);
                    var symbol = model!.GetDeclaredSymbol(token.Parent!);

                    solution = await Renamer.RenameSymbolAsync(
                        solution,
                        symbol!,
                        new SymbolRenameOptions(), // No automatic file renaming this time; we're doing it ourselves!
                        fixedName,
                        cancellationToken);

                    // Check if the symbol we renamed matched the name of the file it's in.
                    if (symbol?.Name == Path.GetFileNameWithoutExtension(document.FilePath))
                    {
                        // If it did, make a note to rename the file to match later.
                        var newDocName = $"{fixedName}.cs";
                        documentsToRename.Add(new(document.Id, newDocName));
                    }
                }
            }

            // Apply all the file renames
            foreach (var pair in documentsToRename)
            {
                solution = solution.WithDocumentName(pair.Key, pair.Value);
            }

            return solution;
        }
    }
}


