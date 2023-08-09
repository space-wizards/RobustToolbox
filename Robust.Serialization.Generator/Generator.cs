using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Analyzers;

namespace Robust.Serialization.Generator;

[Generator]
public class Generator : IIncrementalGenerator
{
    private const string DataDefinitionNamespace = "Robust.Shared.Serialization.Manager.Attributes.DataDefinitionAttribute";

    private static readonly DiagnosticDescriptor DataDefinitionPartialRule = new(
        Diagnostics.IdDataDefinitionPartial,
        "Type must be partial",
        "Type {0} has a DataDefinition attribute but is not partial.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure to mark any type that is a data definition as partial."
    );

    private static readonly DiagnosticDescriptor NestedDataDefinitionPartialRule = new(
        Diagnostics.IdDataDefinitionPartial,
        "Type must be partial",
        "Type {0} contains nested data definition {1} but is not partial.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure to mark any type containing a nested data definition as partial."
    );

    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        Debugger.Launch();

        IncrementalValuesProvider<TypeDeclarationSyntax> dataDefinitions = initContext.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is TypeDeclarationSyntax { AttributeLists.Count: > 0 },
            static (context, _) =>
            {
                var type = (TypeDeclarationSyntax) context.Node;
                foreach (var attributeListSyntax in type.AttributeLists)
                {
                    foreach (var attributeSyntax in attributeListSyntax.Attributes)
                    {
                        var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
                        if (symbol is IMethodSymbol attributeSymbol &&
                            attributeSymbol.ContainingType.ToDisplayString() == DataDefinitionNamespace)
                        {
                            return type;
                        }
                    }
                }

                return null;
            }
        ).Where(static type => type != null)!;

        var comparer = new DataDefinitionComparer();
        initContext.RegisterSourceOutput(
            initContext.CompilationProvider.Combine(dataDefinitions.WithComparer(comparer).Collect()),
            static (sourceContext, source) =>
            {
                var (compilation, types) = source;
                var builder = new StringBuilder();
                var containingTypes = new Stack<INamedTypeSymbol>();

                foreach (var type in types)
                {
                    builder.Clear();
                    containingTypes.Clear();

                    var symbol = (ITypeSymbol) compilation.GetSemanticModel(type.SyntaxTree).GetDeclaredSymbol(type)!;

                    if (type.Modifiers.IndexOf(SyntaxKind.PartialKeyword) == -1)
                    {
                        sourceContext.ReportDiagnostic(Diagnostic.Create(DataDefinitionPartialRule, type.Keyword.GetLocation(), symbol.Name));
                        continue;
                    }

                    builder.AppendLine($"namespace {symbol.ContainingNamespace.ToDisplayString()};\n");

                    var containingType = symbol.ContainingType;
                    while (containingType != null)
                    {
                        containingTypes.Push(containingType);
                        containingType = containingType.ContainingType;
                    }

                    var nonPartial = false;
                    foreach (var parent in containingTypes)
                    {
                        var syntax = (ClassDeclarationSyntax) parent.DeclaringSyntaxReferences[0].GetSyntax();
                        if (syntax.Modifiers.IndexOf(SyntaxKind.PartialKeyword) == -1)
                        {
                            sourceContext.ReportDiagnostic(Diagnostic.Create(NestedDataDefinitionPartialRule, syntax.Keyword.GetLocation(), parent.Name, symbol.Name));
                            nonPartial = true;
                            continue;
                        }

                        builder.AppendLine($"{GetPartialTypeDefinitionLine(parent)}\n{{");
                    }

                    if (nonPartial)
                        continue;

                    builder.Append($$"""
{{GetPartialTypeDefinitionLine(symbol)}}
{

}
"""
                    );

                    for (var i = 0; i < containingTypes.Count; i++)
                    {
                        builder.AppendLine("}");
                    }

                    var fileName = symbol
                        .ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)
                        .Replace('<', '{')
                        .Replace('>', '}');

                    var sourceText = CSharpSyntaxTree
                        .ParseText(builder.ToString())
                        .GetRoot()
                        .NormalizeWhitespace()
                        .ToFullString();

                    sourceContext.AddSource($"{fileName}.g.cs", sourceText);
                }
            }
        );
    }

    private static string GetPartialTypeDefinitionLine(ITypeSymbol symbol)
    {
        var access = symbol.DeclaredAccessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "protected internal",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.Public => "public",
            _ => "public"
        };

        string typeKeyword;
        if (symbol.IsRecord)
        {
            typeKeyword = symbol.IsValueType ? "record struct" : "record";
        }
        else
        {
            typeKeyword = symbol.IsValueType ? "struct" : "class";
        }

        return $"{access} partial {typeKeyword} {symbol.Name}";
    }
}
