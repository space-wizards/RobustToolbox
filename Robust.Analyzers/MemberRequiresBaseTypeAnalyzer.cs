using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MemberRequiresBaseTypeAnalyzer : DiagnosticAnalyzer
{
    private const string AnalyzerAttribute = "Robust.Shared.Analyzers.MemberRequiresBaseTypeAttribute";

    [SuppressMessage("ReSharper", "RS2008")]
    private static readonly DiagnosticDescriptor Rule = new (
        Diagnostics.MemberRequiresBaseType,
        "Required basetype missing",
        "You can only use this attribute for members of types which are subtypes of \"{0}\"",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure to specify the accessing class in the friends attribute.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(CheckBaseType, SymbolKind.Field);
        context.RegisterSymbolAction(CheckBaseType, SymbolKind.Property);
        context.RegisterSymbolAction(CheckBaseType, SymbolKind.Method);
        context.RegisterSymbolAction(CheckBaseType, SymbolKind.Event);
    }

    private void CheckBaseType(SymbolAnalysisContext obj)
    {
        if(obj.Symbol.ContainingType == null) return;

        var analyzerAttribute = obj.Compilation.GetTypeByMetadataName(AnalyzerAttribute);

        INamedTypeSymbol requiredBaseType = null;
        foreach (var attribute in obj.Symbol.GetAttributes())
        {
            if(attribute.AttributeClass == null) continue;

            foreach (var attributeAttribute in attribute.AttributeClass.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(analyzerAttribute, attributeAttribute.AttributeClass))
                {
                    requiredBaseType = (INamedTypeSymbol) attributeAttribute.ConstructorArguments[0].Value;
                }
            }
        }

        if(requiredBaseType == null) return;

        if (!Utils.InheritsFromOrEquals(obj.Symbol.ContainingType, requiredBaseType))
        {
            obj.ReportDiagnostic(Diagnostic.Create(Rule, obj.Symbol.Locations.First(), requiredBaseType.Name));
        }
    }
}

