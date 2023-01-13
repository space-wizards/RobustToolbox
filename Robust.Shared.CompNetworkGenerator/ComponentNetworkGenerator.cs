using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Robust.Shared.CompNetworkGenerator
{
    [Generator]
    public class ComponentNetworkGenerator : ISourceGenerator
    {
        private const string ClassAttributeName = "Robust.Shared.GameObjects.AutoGenerateComponentStateAttribute";
        private const string MemberAttributeName = "Robust.Shared.GameObjects.AutoNetworkedFieldAttribute";

        private static string GenerateSource(in GeneratorExecutionContext context, INamedTypeSymbol classSymbol, CSharpCompilation comp)
        {
            var nameSpace = classSymbol.ContainingNamespace.ToDisplayString();
            var componentName = classSymbol.Name;
            var stateName = $"{componentName}_AutoState";

            var members = classSymbol.GetMembers();
            var fields = new List<(ITypeSymbol Type, string FieldName)>();
            var fieldAttr = comp.GetTypeByMetadataName(MemberAttributeName);

            foreach (var mem in members)
            {
                var attribute = mem.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass != null &&
                    a.AttributeClass.Equals(fieldAttr, SymbolEqualityComparer.Default));

                if (attribute == null)
                {
                    continue;
                }

                switch (mem)
                {
                    case IFieldSymbol field:
                        fields.Add((field.Type, field.Name));
                        break;
                    case IPropertySymbol prop:
                    {
                        if (prop.SetMethod == null || prop.SetMethod.DeclaredAccessibility != Accessibility.Public)
                        {
                            var msg = "Property is marked with [AutoNetworkField], but has no accessible setter method.";
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    new DiagnosticDescriptor(
                                        "RXN0008",
                                        msg,
                                        msg,
                                        "Usage",
                                        DiagnosticSeverity.Error,
                                        true),
                                    classSymbol.Locations[0]));
                            continue;
                        }

                        if (prop.GetMethod == null || prop.GetMethod.DeclaredAccessibility != Accessibility.Public)
                        {
                            var msg = "Property is marked with [AutoNetworkField], but has no accessible getter method.";
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    new DiagnosticDescriptor(
                                        "RXN0008",
                                        msg,
                                        msg,
                                        "Usage",
                                        DiagnosticSeverity.Error,
                                        true),
                                    classSymbol.Locations[0]));
                            continue;
                        }

                        fields.Add((prop.Type, prop.Name));
                        break;
                    }
                }
            }

            if (fields.Count == 0)
            {
                var msg = "Component is marked with [AutoGenerateComponentState], but has no members marked with [AutoNetworkField].";
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "RXN0007",
                            msg,
                            msg,
                            "Usage",
                            DiagnosticSeverity.Error,
                            true),
                        classSymbol.Locations[0]));

                return null;
            }

            // eg:
            //         public string Name = default!;
            //         public int Count = default!;
            var stateFields = new StringBuilder();

            // eg:
            //                 Name = component.Name,
            //                 Count = component.Count,
            var getStateInit = new StringBuilder();

            // eg:
            //            component.Name = state.Name;
            //            component.Count = state.Count;
            var handleStateSetters = new StringBuilder();

            foreach (var (type, name) in fields)
            {
                stateFields.Append($@"
        public global::{type.ContainingNamespace.ToDisplayString()}.{type.Name} {name} = default!;");

                getStateInit.Append($@"
                {name} = component.{name},");

                handleStateSetters.Append($@"
            component.{name} = state.{name};");
            }

            return $@"// <auto-generated />
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace {nameSpace};

public partial class {componentName}
{{
    [Serializable, NetSerializable]
    public class {stateName} : ComponentState
    {{{stateFields}
    }}

    public class {componentName}_AutoNetworkSystem : EntitySystem
    {{
        public override void Initialize()
        {{
            SubscribeLocalEvent<{componentName}, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<{componentName}, ComponentHandleState>(OnHandleState);
        }}

        private void OnGetState(EntityUid uid, {componentName} component, ref ComponentGetState args)
        {{
            args.State = new {stateName}
            {{{getStateInit}
            }};
        }}

        private void OnHandleState(EntityUid uid, {componentName} component, ref ComponentHandleState args)
        {{
            if (args.Current is not {stateName} state)
                return;
{handleStateSetters}
        }}
    }}
}}
";
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // Add attribute source
            var comp = (CSharpCompilation) context.Compilation;

            if (!(context.SyntaxReceiver is NameReferenceSyntaxReceiver receiver))
            {
                return;
            }

            var symbols = GetAnnotatedTypes(context, comp, receiver);

            // Generate component sources and add

            foreach (var type in symbols)
            {
                try
                {
                    var source = GenerateSource(context, type, comp);
                    context.AddSource($"{type.Name}_CompNetwork.g.cs", SourceText.From(source, Encoding.UTF8));
                }
                catch (Exception e)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "RXN0003",
                                "Unhandled exception occured while generating automatic component state handling.",
                                $"Unhandled exception occured while generating automatic component state handling: {e}",
                                "Usage",
                                DiagnosticSeverity.Error,
                                true),
                            type.Locations[0]));
                }
            }
        }

        private IReadOnlyList<INamedTypeSymbol> GetAnnotatedTypes(in GeneratorExecutionContext context,
            CSharpCompilation comp, NameReferenceSyntaxReceiver receiver)
        {
            var symbols = new List<INamedTypeSymbol>();
            var attributeSymbol = comp.GetTypeByMetadataName(ClassAttributeName);
            foreach (var candidateClass in receiver.CandidateClasses)
            {
                var model = comp.GetSemanticModel(candidateClass.SyntaxTree);
                var typeSymbol = model.GetDeclaredSymbol(candidateClass);
                var relevantAttribute = typeSymbol?.GetAttributes().FirstOrDefault(attr =>
                    attr.AttributeClass != null &&
                    attr.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));

                if (relevantAttribute == null)
                {
                    continue;
                }

                var isPartial = candidateClass.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

                if (isPartial)
                {
                    symbols.Add(typeSymbol);
                }
                else
                {
                    var missingPartialKeywordMessage =
                        $"The type {typeSymbol.Name} should be declared with the 'partial' keyword " +
                        "as it is annotated with the [AutoGenerateComponentState] attribute.";

                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "RXN0006",
                                missingPartialKeywordMessage,
                                missingPartialKeywordMessage,
                                "Usage",
                                DiagnosticSeverity.Error,
                                true),
                            Location.None));
                }
            }

            return symbols;
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new NameReferenceSyntaxReceiver());
        }
    }
}
