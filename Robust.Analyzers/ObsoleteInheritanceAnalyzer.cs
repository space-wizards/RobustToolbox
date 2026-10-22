#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ObsoleteInheritanceAnalyzer : DiagnosticAnalyzer
{
    private const string Attribute = "Robust.Shared.Analyzers.ObsoleteInheritanceAttribute";

    public static readonly DiagnosticDescriptor Rule = new(
        Diagnostics.IdObsoleteInheritance,
        "Parent type has obsoleted inheritance",
        "Type '{0}' inherits from '{1}', which has obsoleted inheriting from itself",
        "Usage",
        DiagnosticSeverity.Warning,
        true);

    public static readonly DiagnosticDescriptor RuleWithMessage = new(
        Diagnostics.IdObsoleteInheritanceWithMessage,
        "Parent type has obsoleted inheritance",
        "Type '{0}' inherits from '{1}', which has obsoleted inheriting from itself: \"{2}\"",
        "Usage",
        DiagnosticSeverity.Warning,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule, RuleWithMessage];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(CheckClass, SymbolKind.NamedType);
    }

    private static void CheckClass(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol typeSymbol)
            return;

        if (typeSymbol.IsValueType || typeSymbol.BaseType is not { } baseType)
            return;

        if (!AttributeHelper.HasAttribute(baseType, Attribute, out var data))
            return;

        var location = context.Symbol.Locations[0];

        if (GetMessageFromAttributeData(data) is { } message)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                RuleWithMessage,
                location,
                [typeSymbol.Name, baseType.Name, message]));
        }
        else
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                location,
                [typeSymbol.Name, baseType.Name]));
        }
    }

    private static string? GetMessageFromAttributeData(AttributeData data)
    {
        if (data.ConstructorArguments is not [var message, ..])
            return null;

        return message.Value as string;
    }
}
