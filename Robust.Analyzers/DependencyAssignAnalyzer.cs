using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DependencyAssignAnalyzer : DiagnosticAnalyzer
{
    private const string DependencyAttributeType = "Robust.Shared.IoC.DependencyAttribute";

    private static readonly DiagnosticDescriptor Rule = new (
        Diagnostics.IdDependencyFieldAssigned,
        "Assignment to dependency field",
        "Tried to assign to [Dependency] field '{0}'. Remove [Dependency] or inject it via field injection instead.",
        "Usage",
        DiagnosticSeverity.Warning,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(CheckAssignment, OperationKind.SimpleAssignment);
    }

    private static void CheckAssignment(OperationAnalysisContext context)
    {
        if (context.Operation is not ISimpleAssignmentOperation assignment)
            return;

        if (assignment.Target is not IFieldReferenceOperation fieldRef)
            return;

        var field = fieldRef.Field;
        var attributes = field.GetAttributes();
        if (attributes.Length == 0)
            return;

        var depAttribute = context.Compilation.GetTypeByMetadataName(DependencyAttributeType);
        if (!HasAttribute(attributes, depAttribute))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, assignment.Syntax.GetLocation(), field.Name));
    }

    private static bool HasAttribute(ImmutableArray<AttributeData> attributes, ISymbol symbol)
    {
        foreach (var attribute in attributes)
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbol))
                return true;
        }

        return false;
    }
}
