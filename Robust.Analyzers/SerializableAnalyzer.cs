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

namespace Robust.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SerializableAnalyzer : DiagnosticAnalyzer
    {
        // Metadata of the analyzer

        // You could use LocalizedString but it's a little more complicated for this sample

        private const string RequiresSerializableAttributeMetadataName = "Robust.Shared.Analyzers.RequiresSerializableAttribute";
        private const string SerializableAttributeMetadataName = "System.SerializableAttribute";
        private const string NetSerializableAttributeMetadataName = "Robust.Shared.Serialization.NetSerializableAttribute";

        [SuppressMessage("ReSharper", "RS2008")] private static readonly DiagnosticDescriptor Rule = new(
            Diagnostics.IdSerializable,
            "Class not marked as (Net)Serializable",
            "Class not marked as (Net)Serializable",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The class should be marked as (Net)Serializable.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
        }

        private bool Marked(INamedTypeSymbol namedTypeSymbol, INamedTypeSymbol attrSymbol)
        {
            if (namedTypeSymbol == null) return false;
            if (namedTypeSymbol.GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attrSymbol))) return true;
            return Marked(namedTypeSymbol.BaseType, attrSymbol);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var attrSymbol = context.Compilation.GetTypeByMetadataName(RequiresSerializableAttributeMetadataName);
            var classDecl = (ClassDeclarationSyntax) context.Node;
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
            if (classSymbol == null) return;

            if (Marked(classSymbol, attrSymbol))
            {
                var attributes = classSymbol.GetAttributes();
                var serAttr = context.Compilation.GetTypeByMetadataName(SerializableAttributeMetadataName);
                var netSerAttr = context.Compilation.GetTypeByMetadataName(NetSerializableAttributeMetadataName);

                var hasSerAttr = attributes.Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, serAttr));
                var hasNetSerAttr =
                    attributes.Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, netSerAttr));

                if (!hasSerAttr || !hasNetSerAttr)
                {
                    var requiredAttributes = new List<string>();
                    if(!hasSerAttr) requiredAttributes.Add(SerializableAttributeMetadataName);
                    if(!hasNetSerAttr) requiredAttributes.Add(NetSerializableAttributeMetadataName);

                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Rule,
                            classDecl.Identifier.GetLocation(),
                            ImmutableDictionary.CreateRange(new Dictionary<string, string>()
                            {
                                {
                                    "requiredAttributes", string.Join(",", requiredAttributes)
                                }
                            })));
                }
            }
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class SerializableCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Annotate class as (Net)Serializable.";

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            foreach (var diagnostic in context.Diagnostics)
            {
                var span = diagnostic.Location.SourceSpan;
                var classDecl = root.FindToken(span.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

                if(!diagnostic.Properties.TryGetValue("requiredAttributes", out var requiredAttributes)) return;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        Title,
                        c => FixAsync(context.Document, classDecl, requiredAttributes, c),
                        Title),
                    diagnostic);
            }
        }

        private async Task<Document> FixAsync(Document document, ClassDeclarationSyntax classDecl,
            string requiredAttributes, CancellationToken cancellationToken)
        {
            var attributes = new List<AttributeSyntax>();
            var namespaces = new List<string>();
            foreach (var attribute in requiredAttributes.Split(','))
            {
                var tempSplit = attribute.Split('.');
                namespaces.Add(string.Join(".",tempSplit.Take(tempSplit.Length-1)));
                var @class = tempSplit.Last();
                @class = @class.Substring(0, @class.Length - 9); //cut out "Attribute" at the end
                attributes.Add(SyntaxFactory.Attribute(SyntaxFactory.ParseName(@class)));
            }

            var newClassDecl =
                classDecl.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributes)));

            var root = (CompilationUnitSyntax) await document.GetSyntaxRootAsync(cancellationToken);
            root = root.ReplaceNode(classDecl, newClassDecl);

            foreach (var ns in namespaces)
            {
                if(root.Usings.Any(u => u.Name.ToString() == ns)) continue;
                root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns)));
            }
            return document.WithSyntaxRoot(root);
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(Diagnostics.IdSerializable);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

    }
}
