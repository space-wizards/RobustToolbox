using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;

namespace Robust.Serialization.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EntityRelationsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor NotComponentDiagnostic = new(
        Diagnostics.IdComponentRelationNotComponent,
        "Class must be an IComponent to use AutoGenerateEntityRelations",
        "Class '{0}' must implement IComponent to be used with [AutoGenerateEntityRelations]",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor NoFieldsDiagnostic = new(
        Diagnostics.IdComponentRelationNoFields,
        "AutoGenerateEntityRelations has no fields",
        "Class '{0}' has [AutoGenerateEntityRelations] but has no fields or properties with [AutoRelationField]",
        "Usage",
        DiagnosticSeverity.Warning,
        true);

    private static readonly DiagnosticDescriptor NoParentAttributeDiagnostic = new(
        Diagnostics.IdComponentRelationNoParentAttribute,
        "AutoRelationField on type of field without AutoGenerateEntityRelations",
        "Field '{0}' has [AutoRelationField] but its containing type does not have [AutoGenerateEntityRelations]",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor WrongTypeAttributeDiagnostic = new(
        Diagnostics.IdComponentRelationWrongTypeAttribute,
        "AutoRelationField has wrong type",
        "Field '{0}' has [AutoRelationField] but is not of type EntityRelation",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        NotComponentDiagnostic,
        NoFieldsDiagnostic,
        NoParentAttributeDiagnostic,
        WrongTypeAttributeDiagnostic
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeFieldOrProperty, SymbolKind.Field, SymbolKind.Property);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol symbol)
            return;

        if (!AttributeHelper.HasAttribute(symbol, EntityRelationsGenerator.AutoGenerateEntityRelationsAttributeName))
            return;

        var syntax = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as TypeDeclarationSyntax;
        var location = syntax?.Identifier.GetLocation() ?? symbol.Locations.FirstOrDefault();

        // Check if it implements IComponent
        if (!TypeSymbolHelper.ImplementsInterface(symbol, EntityRelationsGenerator.IComponentTypeName))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(NotComponentDiagnostic, location, symbol.Name)
            );
        }

        // Check if it has any fields with [AutoRelationField]
        var hasFields = false;
        foreach (var member in symbol.GetMembers())
        {
            if (member is not IFieldSymbol and not IPropertySymbol
                || !AttributeHelper.HasAttribute(member, EntityRelationsGenerator.AutoRelationFieldAttributeName))
            {
                continue;
            }

            hasFields = true;
            break;
        }

        if (!hasFields)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(NoFieldsDiagnostic, location, symbol.Name));
        }
    }

    private static void AnalyzeFieldOrProperty(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;

        if (!AttributeHelper.HasAttribute(symbol, EntityRelationsGenerator.AutoRelationFieldAttributeName))
            return;

        var syntax = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        var location = syntax switch
        {
            PropertyDeclarationSyntax prop => prop.Identifier.GetLocation(),
            VariableDeclaratorSyntax varDecl => varDecl.Identifier.GetLocation(),
            _ => symbol.Locations.FirstOrDefault()
        };

        // Check if parent has [AutoGenerateEntityRelations]
        if (!AttributeHelper.HasAttribute(symbol.ContainingType, EntityRelationsGenerator.AutoGenerateEntityRelationsAttributeName))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(NoParentAttributeDiagnostic, location, symbol.Name));
        }

        // Check type validity
        var type = symbol switch
        {
            IPropertySymbol property => property.Type,
            IFieldSymbol field => field.Type,
            _ => null
        };

        if (type is not INamedTypeSymbol namedType)
            return;

        if (namedType.Name == "EntityRelation")
            return;

        switch (namedType)
        {
            case { Name: "Nullable", TypeArguments: [{ Name: "EntityRelation" }] }:
            case { Name: "List", TypeArguments: [{ Name: "EntityRelation" }] }:
            case { Name: "HashSet", TypeArguments: [{ Name: "EntityRelation" }] }:
            case { Name: "Dictionary", TypeArguments: [{ Name: "EntityRelation" }, _] }:
            case { Name: "Dictionary", TypeArguments: [_, { Name: "EntityRelation" }] }:
                return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(WrongTypeAttributeDiagnostic, symbol.Locations[0], symbol.Name)
        );
    }
}
