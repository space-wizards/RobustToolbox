using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
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
        "You can only use this attribute for members of types which are subtypes of one of the following types: {0}",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure to inherit the correct basetype.");

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

        //get all attributes on the member
        foreach (var attribute in obj.Symbol.GetAttributes())
        {
            if(attribute.AttributeClass == null) continue;

            //get all attributes of the attribute
            foreach (var attributeAttribute in attribute.AttributeClass.GetAttributes())
            {
                //is it our analyzerattribute?
                if (SymbolEqualityComparer.Default.Equals(analyzerAttribute, attributeAttribute.AttributeClass))
                {
                    //it is, lets check if we are inheriting one of the required basetypes
                    bool matchedType = false;

                    var allowedTypes = new List<INamedTypeSymbol>();
                    foreach (var constant in attributeAttribute.ConstructorArguments[0].Values)
                    {
                        // Check if the value is a type...
                        if (constant.Value is not INamedTypeSymbol t)
                            continue;

                        allowedTypes.Add(t);
                        // If we find that the containing class is specified in the attribute, return! All is good.
                        matchedType |= Utils.InheritsFromOrEquals(obj.Symbol.ContainingType, t);
                    }

                    if (!matchedType)
                    {
                        obj.ReportDiagnostic(Diagnostic.Create(Rule, obj.Symbol.Locations.First(), string.Join(",", allowedTypes)));
                    }
                }

            }
        }
    }
}

