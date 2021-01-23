using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Document = Microsoft.CodeAnalysis.Document;

namespace Robust.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExplicitInterfaceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RA0000";

        private const string Title = "No explicit interface specified";
        private const string MessageFormat = "No explicit interface specified";
        private const string Description = "Make sure to specify the interface in your method-declaration.";
        private const string Category = "Usage";

        [SuppressMessage("ReSharper", "RS2008")] private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        private const string RequiresExplicitImplementationAttributeMetadataName =
            "Robust.Shared.Analyzers.RequiresExplicitImplementationAttribute";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var methodDecl = (MethodDeclarationSyntax) context.Node;
            var attrSymbol = context.Compilation.GetTypeByMetadataName(RequiresExplicitImplementationAttributeMetadataName);

            //we already have a explicit interface specified, no need to check further
            if(methodDecl.ExplicitInterfaceSpecifier != null) return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(methodDecl);

            var @interface = symbol?.ContainingType.AllInterfaces.FirstOrDefault(
                i =>
                    i.GetMembers().OfType<IMethodSymbol>().Any(m => SymbolEqualityComparer.Default.Equals(symbol, symbol.ContainingType.FindImplementationForInterfaceMember(m)))
                    && i.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attrSymbol))
            );
            if (@interface != null)
            {
                //we do not have an explicit interface specified but are an interface method. bad!
                var diagnostic = Diagnostic.Create(
                    Rule,
                    methodDecl.Identifier.GetLocation(),
                    ImmutableDictionary.CreateRange(new Dictionary<string, string>(){{"interface", @interface.Name}}));
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class ExplicitInterfaceMethodCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Convert to explicit interface declaration";

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            foreach (var diagnostic in context.Diagnostics)
            {
                var span = diagnostic.Location.SourceSpan;
                var methodDecl = root.FindToken(span.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>()
                    .First();

                if(!diagnostic.Properties.TryGetValue("interface", out var @interface)) return;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        Title,
                        c => FixAsync(context.Document, methodDecl, c, @interface),
                        Title),
                    diagnostic);
            }
        }

        private async Task<Document> FixAsync(Document document, MethodDeclarationSyntax methodDecl,
            CancellationToken cancellationToken, string @interface)
        {
            var removableModifiers = new[]
            {
                SyntaxKind.PublicKeyword,
                SyntaxKind.OverrideKeyword
            };

            var keepMods =
                new SyntaxTokenList(methodDecl.Modifiers.Where(m => removableModifiers.All(rm => rm != m.Kind())));

            var leadingtrivia = methodDecl.Modifiers.Where(m => !keepMods.Contains(m)).SelectMany(m => m.LeadingTrivia);

            if (keepMods.Count != 0)
                keepMods = keepMods.Replace(keepMods[0], keepMods[0].WithLeadingTrivia(leadingtrivia));

            var newMethodDecl = methodDecl
                .WithExplicitInterfaceSpecifier(
                    SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.IdentifierName(@interface)))
                .WithModifiers(keepMods);

            if (keepMods.Count == 0)
            {
                newMethodDecl = newMethodDecl.WithLeadingTrivia(leadingtrivia);
            }
            //.WithAdditionalAnnotations(Formatter.Annotation);

            /*var formattedMethod = Formatter.Format(newMethodDecl, Formatter.Annotation,
                document.Project.Solution.Workspace, document.Project.Solution.Workspace.Options);*/

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(methodDecl, newMethodDecl);
            return document.WithSyntaxRoot(newRoot);
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ExplicitInterfaceAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
