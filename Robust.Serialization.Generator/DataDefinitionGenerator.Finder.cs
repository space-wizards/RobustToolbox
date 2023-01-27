using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.SymbolEqualityComparer;
using Diagnostic = Microsoft.CodeAnalysis.Diagnostic;
using DiagnosticDescriptor = Microsoft.CodeAnalysis.DiagnosticDescriptor;
using DiagnosticSeverity = Microsoft.CodeAnalysis.DiagnosticSeverity;
using GeneratorExecutionContext = Microsoft.CodeAnalysis.GeneratorExecutionContext;
using INamedTypeSymbol = Microsoft.CodeAnalysis.INamedTypeSymbol;

namespace Robust.Serialization.Generator
{
    public partial class DataDefinitionGenerator
    {
        private FindResults FindDataDefinitions(in GeneratorExecutionContext context,
            CSharpCompilation comp, NameReferenceSyntaxReceiver receiver)
        {
            var results = new FindResults();
            var dataDefinition = comp.GetTypeByMetadataName(DataDefinitionName);
            var implicitDataDefinition = comp.GetTypeByMetadataName(ImplicitDataDefinitionForInheritorsName);

            foreach (var candidateClass in receiver.Types)
            {
                var model = comp.GetSemanticModel(candidateClass.SyntaxTree);
                var typeSymbol = model.GetDeclaredSymbol(candidateClass);
                var dataDefinitionAttribute = GetAttribute(typeSymbol, dataDefinition);
                var implicitDataDefinitionAttribute = GetAttribute(typeSymbol, implicitDataDefinition);

                if (dataDefinitionAttribute != null)
                {
                    var isPartial = candidateClass.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

                    if (!isPartial)
                    {
                        var missingPartialKeywordMessage =
                            $"The type {typeSymbol.Name} and its nesting types should be declared with the 'partial' keyword as it is annotated with the [DataDefinition] attribute.";

                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "RSN0001",
                                    missingPartialKeywordMessage,
                                    missingPartialKeywordMessage,
                                    "Usage",
                                    DiagnosticSeverity.Error,
                                    true),
                                typeSymbol.Locations.First()));
                    }
                    else
                    {
                        results.DataDefinitions.Add(typeSymbol);
                    }
                }

                if (implicitDataDefinitionAttribute != null)
                {
                    results.ImplicitDataDefinitions.Add(typeSymbol);
                }
            }

            return results;
        }

        private IEnumerable<ISymbol> FindDefinitionFields(INamedTypeSymbol definition)
        {
            return definition.GetMembers()
                .Where(member =>
                    member.Kind == SymbolKind.Field ||
                    member.Kind == SymbolKind.Property)
                .Where(field => GetBaseDataFieldAttribute(field) != null);
        }

        private bool NestsType(INamedTypeSymbol parent, INamedTypeSymbol child)
        {
            var containingType = child.ContainingType;
            while (containingType != null)
            {
                if (parent.Equals(containingType, Default))
                    return true;

                containingType = containingType.ContainingType;
            }

            return false;
        }

        private bool CanInstantiateParameterlessType(INamedTypeSymbol parent, INamedTypeSymbol child)
        {
            if (child.IsValueType)
                return true;

            var parameterlessConstructor = child.InstanceConstructors.FirstOrDefault(c => c.Parameters.IsEmpty);
            if (parameterlessConstructor == null)
                return false;

            switch (parameterlessConstructor.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return true;
                case Accessibility.Private:
                    return false;
            }

            var containingType = child.ContainingType;
            while (containingType != null)
            {
                if (parent.Equals(containingType, Default))
                    return true;

                containingType = containingType.ContainingType;
            }

            return false;
        }

        private sealed class FindResults
        {
            public readonly HashSet<INamedTypeSymbol> DataDefinitions = new HashSet<INamedTypeSymbol>(Default);
            public readonly HashSet<INamedTypeSymbol> ImplicitDataDefinitions = new HashSet<INamedTypeSymbol>(Default);
        }
    }
}
