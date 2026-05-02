using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class YamlTagShortenerAnalyzer : DiagnosticAnalyzer
{
    private const string YamlTagShortenerAttributeNamespace = "Robust.Shared.Serialization.Manager.Attributes.YamlTagShortenerAttribute";
    private const string CustomChildTagAttributeName = "CustomChildTagAttribute`1";

    [SuppressMessage("ReSharper", "RS2008")]
    private static readonly DiagnosticDescriptor ResultRuleBaseNameWrong = new DiagnosticDescriptor(
        Diagnostics.IdTypeEndsWithBase,
        "Type must end in 'Base' or have [CustomChildTag<T>]",
        "[YamlTagShortener] usage on type {0} is incorrect",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "The type must either be renamed to end in 'Base' or add at least one [CustomChildTag<T>].");

    [SuppressMessage("ReSharper", "RS2008")]
    private static readonly DiagnosticDescriptor ResultRuleChildNameWrong = new DiagnosticDescriptor(
        Diagnostics.IdYamlTagShortenerUnsupportedChildName,
        "Base type uses the YamlTagShortener but child name is invalid",
        "Base type uses the YamlTagShortener but the name of this type {0} is not supported",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Either rename this type to fit conventions or add [CustomChildTag<T>] to the base type.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ResultRuleBaseNameWrong, ResultRuleChildNameWrong);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolStartAction(symbolContext =>
            {
            if (symbolContext.Symbol is not INamedTypeSymbol typeSymbol)
                return;

            if (!HasYamlTagShortenerAttributes(typeSymbol))
                return;

            symbolContext.RegisterSyntaxNodeAction(CheckYamlTagShortener, SyntaxKind.ClassDeclaration);
            },
            SymbolKind.NamedType);

        context.RegisterSymbolStartAction(symbolContext =>
            {
                if (symbolContext.Symbol is not INamedTypeSymbol typeSymbol)
                    return;

                if (!HasYamlTagShortenerAttributes(typeSymbol.BaseType))
                    return;

                symbolContext.RegisterSyntaxNodeAction(CheckChildNamingConvention, SyntaxKind.ClassDeclaration);
            },
            SymbolKind.NamedType);

    }

    private static void CheckYamlTagShortener(SyntaxNodeAnalysisContext symbolContext)
    {
        if (symbolContext.Node is not TypeDeclarationSyntax declaration)
            return;

        if (symbolContext.ContainingSymbol is not INamedTypeSymbol type)
            return;

        var hasCustomChildTag = false;
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.MetadataName == CustomChildTagAttributeName)
                hasCustomChildTag = true;
        }

        if (!type.Name.EndsWith("Base") && !hasCustomChildTag)
        {
            symbolContext.ReportDiagnostic(Diagnostic.Create(ResultRuleBaseNameWrong, declaration.GetLocation(), type.Name));
        }
    }

    private static void CheckChildNamingConvention(SyntaxNodeAnalysisContext symbolContext)
    {
        if (symbolContext.Node is not TypeDeclarationSyntax declaration)
            return;

        if (symbolContext.ContainingSymbol is not INamedTypeSymbol type)
            return;

        if (type.BaseType is null)
            return;

        var childName = type.Name;
        var baseNameWithoutBase = YamlTagShortenerHelper.ReplaceLast(type.BaseType.Name, "Base", string.Empty);
        var shortFormChildName = childName.Replace(baseNameWithoutBase, string.Empty);
        var recombinedName = baseNameWithoutBase + shortFormChildName;

        if (recombinedName == childName)
            return;

        if (BaseTypeHasCustomChildTagForType(type, symbolContext))
            return;

        symbolContext.ReportDiagnostic(Diagnostic.Create(ResultRuleChildNameWrong, declaration.GetLocation(), type.Name));

    }

    private static bool BaseTypeHasCustomChildTagForType(ITypeSymbol type, SyntaxNodeAnalysisContext symbolContext)
    {
        if (type?.BaseType == null)
            return false;

        var childTagFound = false;

        foreach (var attr in type.BaseType.GetAttributes())
        {
            if (attr.AttributeClass?.MetadataName != CustomChildTagAttributeName)
                continue;

            if (attr.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax attributeSyntax)
                continue;

            if (attributeSyntax.Name is not GenericNameSyntax genericNameSyntax)
                continue;

            if (type.Name != genericNameSyntax.TypeArgumentList.Arguments[0].ToString())
                continue;

            childTagFound = true;
            break;
        }

        return childTagFound;
    }

    private static bool HasYamlTagShortenerAttributes(ITypeSymbol type)
    {
        if (type == null)
            return false;

        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == YamlTagShortenerAttributeNamespace)
                return true;
            if (attribute.AttributeClass?.MetadataName == CustomChildTagAttributeName)
                return true;
        }

        return false;
    }
}
