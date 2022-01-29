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
    const string Attribute = "Robust.Shared.Analyzers.VirtualAttribute";

    [SuppressMessage("ReSharper", "RS2008")]
    private static readonly DiagnosticDescriptor Rule = new(
        Diagnostics.IdExplicitVirtual,
        "Class must be explicitly marked as [Virtual], abstract, or sealed",
        "Class must be explicitly marked as [Virtual], abstract, or sealed",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Class must be explicitly marked as [Virtual], abstract, or sealed.");

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
