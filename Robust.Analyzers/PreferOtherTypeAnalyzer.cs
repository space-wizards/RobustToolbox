using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferOtherTypeAnalyzer : DiagnosticAnalyzer
{
    private const string AttributeType = "Robust.Shared.Analyzers.PreferOtherTypeAttribute";

    private static readonly DiagnosticDescriptor PreferOtherTypeDescriptor = new(
        Diagnostics.IdPreferOtherType,
        "Use the specific type",
        "Use the specific type {0} instead of {1} when T is {2}",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Use the specific type.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        PreferOtherTypeDescriptor
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics | GeneratedCodeAnalysisFlags.Analyze);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.VariableDeclaration);
    }

    private void AnalyzeField(SyntaxNodeAnalysisContext context)
    {
        var preferOtherTypeAttribute = context.Compilation.GetTypeByMetadataName(AttributeType);

        if (context.Node is not VariableDeclarationSyntax node)
            return;

        // Get the type of the generic being used
        if (node.Type is not GenericNameSyntax genericName)
            return;
        var genericSyntax = genericName.TypeArgumentList.Arguments[0];
        var genericType = context.SemanticModel.GetSymbolInfo(genericSyntax).Symbol;

        // Look for the PreferOtherTypeAttribute
        var symbolInfo = context.SemanticModel.GetSymbolInfo(node.Type);
        var attributes = symbolInfo.Symbol.GetAttributes();
        foreach (var attribute in attributes)
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, preferOtherTypeAttribute))
                continue;

            // See if the generic type argument matches the type the attribute specifies
            var checkedType = attribute.ConstructorArguments[0].Value as ITypeSymbol;
            if (!SymbolEqualityComparer.Default.Equals(checkedType, genericType))
                continue;

            var replacementType = attribute.ConstructorArguments[1].Value as ITypeSymbol;
            context.ReportDiagnostic(Diagnostic.Create(PreferOtherTypeDescriptor,
                context.Node.GetLocation(),
                replacementType.Name,
                symbolInfo.Symbol.Name,
                genericType.Name));

        }
    }
}
