using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.SymbolDisplayFormat;
using static Microsoft.CodeAnalysis.SymbolDisplayMiscellaneousOptions;

namespace Robust.Shared.CompNetworkGenerator
{
    [Generator]
    public class ComponentNetworkGenerator : ISourceGenerator
    {
        private const string ClassAttributeName = "Robust.Shared.Analyzers.AutoGenerateComponentStateAttribute";
        private const string MemberAttributeName = "Robust.Shared.Analyzers.AutoNetworkedFieldAttribute";

        private const string GlobalEntityUidName = "global::Robust.Shared.GameObjects.EntityUid";
        private const string GlobalNullableEntityUidName = "global::Robust.Shared.GameObjects.EntityUid?";

        private const string GlobalNetEntityName = "global::Robust.Shared.GameObjects.NetEntity";
        private const string GlobalNetEntityNullableName = "global::Robust.Shared.GameObjects.NetEntity?";

        private const string GlobalEntityCoordinatesName = "global::Robust.Shared.Map.EntityCoordinates";
        private const string GlobalNullableEntityCoordinatesName = "global::Robust.Shared.Map.EntityCoordinates?";

        private const string GlobalEntityUidSetName = "global::System.Collections.Generic.HashSet<global::Robust.Shared.GameObjects.EntityUid>";
        private const string GlobalNetEntityUidSetName = $"global::System.Collections.Generic.HashSet<{GlobalNetEntityName}>";

        private const string GlobalEntityUidListName = "global::System.Collections.Generic.List<global::Robust.Shared.GameObjects.EntityUid>";
        private const string GlobalNetEntityUidListName = $"global::System.Collections.Generic.List<{GlobalNetEntityName}>";

        private const string GlobalDictionaryName = "global::System.Collections.Generic.Dictionary<TKey, TValue>";
        private const string GlobalHashSetName = "global::System.Collections.Generic.HashSet<T>";
        private const string GlobalListName = "global::System.Collections.Generic.List<T>";

        private static readonly SymbolDisplayFormat FullNullableFormat =
            FullyQualifiedFormat.WithMiscellaneousOptions(IncludeNullableReferenceTypeModifier);

        private static string? GenerateSource(in GeneratorExecutionContext context, INamedTypeSymbol classSymbol, CSharpCompilation comp, bool raiseAfterAutoHandle)
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
                            var msg = "Property is marked with [AutoNetworkedField], but has no accessible setter method.";
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
                            var msg = "Property is marked with [AutoNetworkedField], but has no accessible getter method.";
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
                var msg = "Component is marked with [AutoGenerateComponentState], but has no valid members marked with [AutoNetworkedField].";
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
                var typeDisplayStr = type.ToDisplayString(FullNullableFormat);
                var nullable = type.NullableAnnotation == NullableAnnotation.Annotated;
                var nullableAnnotation = nullable ? "?" : string.Empty;

                switch (typeDisplayStr)
                {
                    case GlobalEntityUidName:
                    case GlobalNullableEntityUidName:
                        stateFields.Append($@"
        public NetEntity{nullableAnnotation} {name} = default!;");

                        getStateInit.Append($@"
                {name} = GetNetEntity(component.{name}),");
                        handleStateSetters.Append($@"
            component.{name} = EnsureEntity<{componentName}>(state.{name}, uid);");

                        break;
                    case GlobalEntityCoordinatesName:
                    case GlobalNullableEntityCoordinatesName:
                        stateFields.Append($@"
        public NetCoordinates{nullableAnnotation} {name} = default!;");

                        getStateInit.Append($@"
                {name} = GetNetCoordinates(component.{name}),");
                        handleStateSetters.Append($@"
            component.{name} = EnsureCoordinates<{componentName}>(state.{name}, uid);");

                        break;
                    case GlobalEntityUidSetName:
                        stateFields.Append($@"
        public {GlobalNetEntityUidSetName} {name} = default!;");

                        getStateInit.Append($@"
                {name} = GetNetEntitySet(component.{name}),");
                        handleStateSetters.Append($@"
            EnsureEntitySet<{componentName}>(state.{name}, uid, component.{name});");

                        break;
                    case GlobalEntityUidListName:
                        stateFields.Append($@"
        public {GlobalNetEntityUidListName} {name} = default!;");

                        getStateInit.Append($@"
                {name} = GetNetEntityList(component.{name}),");
                        handleStateSetters.Append($@"
            EnsureEntityList<{componentName}>(state.{name}, uid, component.{name});");

                        break;
                    default:
                        if (type is INamedTypeSymbol { TypeArguments.Length: 2 } named &&
                            named.ConstructedFrom.ToDisplayString(FullyQualifiedFormat) == GlobalDictionaryName)
                        {
                            var key = named.TypeArguments[0].ToDisplayString(FullNullableFormat);
                            var keyNullable = key.EndsWith("?");

                            var value = named.TypeArguments[1].ToDisplayString(FullNullableFormat);
                            var valueNullable = value.EndsWith("?");

                            if (key is GlobalEntityUidName or GlobalNullableEntityUidName)
                            {
                                key = keyNullable ? GlobalNetEntityNullableName : GlobalNetEntityName;

                                var ensureGeneric = $"{componentName}, {value}";
                                if (value is GlobalEntityUidName or GlobalNullableEntityUidName)
                                {
                                    value = valueNullable ? GlobalNetEntityNullableName : GlobalNetEntityName;
                                    ensureGeneric = componentName;
                                }

                                stateFields.Append($@"
        public Dictionary<{key}, {value}> {name} = default!;");

                                getStateInit.Append($@"
                {name} = GetNetEntityDictionary(component.{name}),");

                                if (valueNullable && value is not GlobalNetEntityName and not GlobalNetEntityNullableName)
                                {
                                    handleStateSetters.Append($@"
            EnsureEntityDictionaryNullableValue<{componentName}, {value}>(state.{name}, uid, component.{name});");
                                }
                                else
                                {
                                    handleStateSetters.Append($@"
            EnsureEntityDictionary<{ensureGeneric}>(state.{name}, uid, component.{name});");
                                }

                                break;
                            }

                            if (value is GlobalEntityUidName or GlobalNullableEntityUidName)
                            {
                                value = valueNullable ? GlobalNetEntityNullableName : GlobalNetEntityName;

                                stateFields.Append($@"
        public Dictionary<{key}, {value}> {name} = default!;");

                                getStateInit.Append($@"
                {name} = GetNetEntityDictionary(component.{name}),");
                                handleStateSetters.Append($@"
            EnsureEntityDictionary<{componentName}, {key}>(state.{name}, uid, component.{name});");

                                break;
                            }
                        }

                        stateFields.Append($@"
        public {typeDisplayStr} {name} = default!;");

                        if (IsCloneType(type))
                        {
                            // get first ctor arg of the field attribute, which determines whether the field should be cloned
                            // (like if its a dict or list)
                            getStateInit.Append($@"
                {name} = component.{name},");

                            handleStateSetters.Append($@"
            if (state.{name} == null)
                component.{name} = null!;
            else
                component.{name} = new(state.{name});");
                        }
                        else
                        {
                            getStateInit.Append($@"
                {name} = component.{name},");

                            handleStateSetters.Append($@"
            component.{name} = state.{name};");
                        }

                        break;
                }
            }

            var eventRaise = "";
            if (raiseAfterAutoHandle)
            {
                eventRaise = @"
            var ev = new AfterAutoHandleStateEvent(args.Current);
            EntityManager.EventBus.RaiseComponentEvent(component, ref ev);";
            }

            return $@"// <auto-generated />
#nullable enable
using System;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace {nameSpace};

public partial class {componentName}
{{
    [System.Serializable, NetSerializable]
    public sealed class {stateName} : IComponentState
    {{{stateFields}
    }}

    [RobustAutoGenerated]
    public sealed class {componentName}_AutoNetworkSystem : EntitySystem
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
{handleStateSetters}{eventRaise}
        }}
    }}
}}
";
        }

        public void Execute(GeneratorExecutionContext context)
        {
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
                    var attr = type.Attribute;
                    var raiseEv = false;
                    if (attr.ConstructorArguments is [{Value: bool raise}])
                    {
                        // Get the afterautohandle bool, which is first constructor arg
                        raiseEv = raise;
                    }

                    var source = GenerateSource(context, type.Type, comp, raiseEv);
                    // can be null if no members marked with network field, which already has a diagnostic, so
                    // just continue
                    if (source == null)
                        continue;

                    context.AddSource($"{type.Type.Name}_CompNetwork.g.cs", SourceText.From(source, Encoding.UTF8));
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
                            type.Type.Locations[0]));
                }
            }
        }

        private IReadOnlyList<(INamedTypeSymbol Type, AttributeData Attribute)> GetAnnotatedTypes(in GeneratorExecutionContext context,
            CSharpCompilation comp, NameReferenceSyntaxReceiver receiver)
        {
            var symbols = new List<(INamedTypeSymbol, AttributeData)>();
            var attributeSymbol = comp.GetTypeByMetadataName(ClassAttributeName);
            var fieldAttr = comp.GetTypeByMetadataName(MemberAttributeName);

            foreach (var candidateClass in receiver.CandidateClasses)
            {
                var model = comp.GetSemanticModel(candidateClass.SyntaxTree);
                var typeSymbol = model.GetDeclaredSymbol(candidateClass);
                var relevantAttribute = typeSymbol?.GetAttributes().FirstOrDefault(attr =>
                    attr.AttributeClass != null &&
                    attr.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));

                if (typeSymbol == null)
                    continue;

                if (relevantAttribute == null)
                {
                    foreach (var mem in typeSymbol.GetMembers())
                    {
                        var attribute = mem.GetAttributes().FirstOrDefault(a =>
                            a.AttributeClass != null &&
                            a.AttributeClass.Equals(fieldAttr, SymbolEqualityComparer.Default));

                        if (attribute == null)
                            continue;

                        var msg = "Field is marked with [AutoNetworkedField], but its class has no [AutoGenerateComponentState] attribute.";
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "RXN0007",
                                    msg,
                                    msg,
                                    "Usage",
                                    DiagnosticSeverity.Error,
                                    true),
                                candidateClass.Keyword.GetLocation()));
                    }

                    continue;
                }

                var isPartial = candidateClass.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

                if (isPartial)
                {
                    symbols.Add((typeSymbol, relevantAttribute));
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
                            candidateClass.Keyword.GetLocation()));
                }
            }

            return symbols;
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            if (!Debugger.IsAttached)
            {
                //Debugger.Launch();
            }
            context.RegisterForSyntaxNotifications(() => new NameReferenceSyntaxReceiver());
        }

        private static bool IsCloneType(ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol named || !named.IsGenericType)
            {
                return false;
            }

            var constructed = named.ConstructedFrom.ToDisplayString(FullyQualifiedFormat);
            return constructed switch
            {
                GlobalDictionaryName or GlobalHashSetName or GlobalListName => true,
                _ => false
            };
        }
    }
}
