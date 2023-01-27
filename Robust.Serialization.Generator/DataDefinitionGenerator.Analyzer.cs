using System.Linq;
using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.SymbolEqualityComparer;

namespace Robust.Serialization.Generator
{
    public sealed partial class DataDefinitionGenerator
    {
        private void AnalyzeDefinition(in GeneratorExecutionContext context, INamedTypeSymbol definition)
        {
            foreach (var field in FindDefinitionFields(definition))
            {
                var fieldType = (INamedTypeSymbol) GetFieldType(field);
                if (fieldType.NullableAnnotation == NullableAnnotation.Annotated &&
                    fieldType.IsReferenceType &&
                    !CanInstantiateParameterlessType(definition, fieldType))
                {
                    var missingPartialKeywordMessage =
                        $"Nullable class-type field {field.ToDisplayString()} must be instantiable by its defining type {definition.ToDisplayString()}. Make it not null, not a class or add a public constructor to it.";

                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "RSN0001",
                                missingPartialKeywordMessage,
                                missingPartialKeywordMessage,
                                "Usage",
                                DiagnosticSeverity.Error,
                                true),
                            field.Locations.First()));
                }
            }
        }
    }
}
