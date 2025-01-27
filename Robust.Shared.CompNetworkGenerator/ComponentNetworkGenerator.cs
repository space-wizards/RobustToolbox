using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.SymbolDisplayFormat;
using static Microsoft.CodeAnalysis.SymbolDisplayMiscellaneousOptions;

// Yes dude I know this source generator isn't incremental, I'll fix it eventually.
#pragma warning disable RS1035

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

        private static string? GenerateSource(in GeneratorExecutionContext context, INamedTypeSymbol classSymbol, CSharpCompilation comp,
            bool raiseAfterAutoHandle,
            bool fieldDeltas)
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

            // Builds the string for duplicating a full component state, in preparation for applying a delta state state
            // without modifying the original. Note that this will not do a proper clone of any collections, under the
            // assumption that nothing should ever try to modify them. Applying the delta state should just override the
            // referenced collection, not modify it.
            var shallowClone = new StringBuilder();

            // Delta field states
            var deltaGetFields = new StringBuilder();

            var deltaHandleFields = new StringBuilder();

            // Apply the delta field to the full state.
            var deltaApply = new List<string>();

            var index = -1;

            var fieldsStr = new StringBuilder();
            var fieldStates = new StringBuilder();

            var networkedTypes = new List<string>();

            foreach (var (type, name) in fields)
            {
                index++;

                if (index == 0)
                {
                    fieldsStr.Append(@$"""{name}""");
                }
                else
                {
                    fieldsStr.Append(@$", ""{name}""");
                }

                var typeDisplayStr = type.ToDisplayString(FullNullableFormat);
                var nullable = type.NullableAnnotation == NullableAnnotation.Annotated;
                var nullableAnnotation = nullable ? "?" : string.Empty;

                string deltaStateName = $"{name}_FieldComponentState";

                // The type used for networking, e.g. EntityUid -> NetEntity
                string networkedType;

                string getField;
                string? cast;
                // TODO: Uhh I just need casts or something.
                var castString = typeDisplayStr.Substring(8);

                deltaGetFields.Append(@$"
                    case {Math.Pow(2, index)}:
                        args.State = new {deltaStateName}()
                        {{
                        ");

                deltaHandleFields.Append(@$"
                case {deltaStateName} {deltaStateName}_State:
                {{");

                var fieldHandleValue = $"{deltaStateName}_State.{name}!";

                switch (typeDisplayStr)
                {
                    case GlobalEntityUidName:
                    case GlobalNullableEntityUidName:
                        networkedType = $"NetEntity{nullableAnnotation}";

                        stateFields.Append($@"
        public {networkedType} {name} = default!;");

                        getField = $"GetNetEntity(component.{name})";
                        cast = $"(NetEntity{nullableAnnotation})";

                        handleStateSetters.Append($@"
            component.{name} = EnsureEntity<{componentName}>(state.{name}, uid);");

                        deltaHandleFields.Append($@"
                    component.{name} = EnsureEntity<{componentName}>({cast} {fieldHandleValue}, uid);");

                        shallowClone.Append($@"
                {name} = this.{name},");

                        deltaApply.Add($"fullState.{name} = {name};");

                        break;
                    case GlobalEntityCoordinatesName:
                    case GlobalNullableEntityCoordinatesName:
                        networkedType = $"NetCoordinates{nullableAnnotation}";

                        stateFields.Append($@"
        public {networkedType} {name} = default!;");

                        getField = $"GetNetCoordinates(component.{name})";
                        cast = $"(NetCoordinates{nullableAnnotation})";

                        handleStateSetters.Append($@"
            component.{name} = EnsureCoordinates<{componentName}>(state.{name}, uid);");

                        deltaHandleFields.Append($@"
                    component.{name} = EnsureCoordinates<{componentName}>({cast} {fieldHandleValue}, uid);");

                        shallowClone.Append($@"
                {name} = this.{name},");

                        deltaApply.Add($@"fullState.{name} = {name};");

                        break;
                    case GlobalEntityUidSetName:
                        networkedType = $"{GlobalNetEntityUidSetName}";

                        stateFields.Append($@"
        public {networkedType} {name} = default!;");

                        getField = $"GetNetEntitySet(component.{name})";
                        cast = $"({GlobalNetEntityUidSetName})";

                        handleStateSetters.Append($@"
            EnsureEntitySet<{componentName}>(state.{name}, uid, component.{name});");

                        deltaHandleFields.Append($@"
                    EnsureEntitySet<{componentName}>({cast} {fieldHandleValue}, uid, component.{name});");

                        shallowClone.Append($@"
                {name} = this.{name},");

                        deltaApply.Add($@"fullState.{name} = {name};");

                        break;
                    case GlobalEntityUidListName:
                        networkedType = $"{GlobalNetEntityUidListName}";

                        stateFields.Append($@"
                        public {networkedType} {name} = default!;");

                        getField = $"GetNetEntityList(component.{name})";
                        cast = $"({GlobalNetEntityUidListName})";

                        handleStateSetters.Append($@"
            EnsureEntityList<{componentName}>(state.{name}, uid, component.{name});");

                        deltaHandleFields.Append($@"
                    EnsureEntityList<{componentName}>({cast} {fieldHandleValue}, uid, component.{name});");

                        shallowClone.Append($@"
                {name} = this.{name},");

                        deltaApply.Add($@"fullState.{name} = {name};");

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

                                networkedType = $"Dictionary<{key}, {value}>";

                                stateFields.Append($@"
        public {networkedType} {name} = default!;");

                                getField = $"GetNetEntityDictionary(component.{name})";

                                if (valueNullable && value is not GlobalNetEntityName and not GlobalNetEntityNullableName)
                                {
                                    cast = $"(Dictionary<{key}, {value}>)";

                                    handleStateSetters.Append($@"
            EnsureEntityDictionaryNullableValue<{componentName}, {value}>(state.{name}, uid, component.{name});");

                                    deltaHandleFields.Append($@"
                    EnsureEntityDictionaryNullableValue<{componentName}, {value}>({cast} {fieldHandleValue}, uid, component.{name});");
                                }
                                else
                                {
                                    cast = $"({castString})";

                                    handleStateSetters.Append($@"
            EnsureEntityDictionary<{ensureGeneric}>(state.{name}, uid, component.{name});");

                                    deltaHandleFields.Append($@"
                    EnsureEntityDictionary<{ensureGeneric}>({cast} {fieldHandleValue}, uid, component.{name});");
                                }

                                shallowClone.Append($@"
                {name} = this.{name},");

                                deltaApply.Add($@"fullState.{name} = {name};");

                                break;
                            }

                            if (value is GlobalEntityUidName or GlobalNullableEntityUidName)
                            {
                                value = valueNullable ? GlobalNetEntityNullableName : GlobalNetEntityName;
                                networkedType = $"Dictionary<{key}, {value}>";

                                stateFields.Append($@"
        public {networkedType} {name} = default!;");

                                getField = $"GetNetEntityDictionary(component.{name})";
                                cast = $"(Dictionary<{key}, {value}>)";

                                handleStateSetters.Append($@"
            EnsureEntityDictionary<{componentName}, {key}>(state.{name}, uid, component.{name});");

                                deltaHandleFields.Append($@"
                    EnsureEntityDictionary<{componentName}, {key}>({cast} {fieldHandleValue}, uid, component.{name});");

                                shallowClone.Append($@"
                {name} = this.{name},");

                                deltaApply.Add($@"fullState.{name} = {name};");

                                break;
                            }
                        }

                        networkedType = $"{typeDisplayStr}";

                        stateFields.Append($@"
        public {networkedType} {name} = default!;");

                        if (IsCloneType(type))
                        {
                            getField = $"component.{name}";
                            cast = $"({castString})";

                            var nullCast = nullable ? castString.Substring(0, castString.Length - 1) : castString;

                            handleStateSetters.Append($@"
            component.{name} = state.{name} == null ? null! : new(state.{name});");

                            deltaHandleFields.Append($@"
                    var {name}Value = {cast} {fieldHandleValue};
                    if ({name}Value == null)
                        component.{name} = null!;
                    else
                        component.{name} = new {nullCast}({name}Value);");

                            shallowClone.Append($@"
                {name} = this.{name},");

                            deltaApply.Add($"fullState.{name} = {name} == null ? null! : new({name});");
                        }
                        else
                        {
                            getField = $"component.{name}";
                            cast = $"({castString})";

                            handleStateSetters.Append($@"
            component.{name} = state.{name};");

                            deltaHandleFields.Append($@"
                    component.{name} = {cast} {fieldHandleValue};");

                            shallowClone.Append($@"
                {name} = this.{name},");

                            deltaApply.Add($"fullState.{name} = {name};");
                        }

                        break;
                }

                /*
                 * End loop stuff
                 */

                networkedTypes.Add(networkedType);

                getStateInit.Append($@"
                {name} = {getField},");

                deltaGetFields.Append(@$"    {name} = {getField}
                        }};
                        return;");

                deltaHandleFields.Append(@"
                    break;
                }
");
            }

            var deltaGetState = "";
            var deltaInterface = "";
            var deltaCompFields = "";
            var deltaNetRegister = "";

            var cloneMethod = "";
            if (fieldDeltas)
            {
                cloneMethod = $@"
        public {stateName} ShallowClone()
        {{
            return new {stateName}()
            {{{shallowClone}
            }};
        }}
";

                for (var i = 0; i < fields.Count; i++)
                {
                    var name = fields[i].FieldName;
                    string deltaStateName = $"{name}_FieldComponentState";
                    var networkedType = networkedTypes[i];
                    var apply = deltaApply[i];

                    // Creates a state per field
                    fieldStates.Append($@"
    [Serializable, NetSerializable]
    public sealed class {deltaStateName} : IComponentDeltaState<{stateName}>
    {{
        public {networkedType} {name} = default!;

        public void ApplyToFullState({stateName} fullState)
        {{
            {apply}
        }}

        public {stateName} CreateNewFullState({stateName} fullState)
        {{
            var newState = fullState.ShallowClone();
            ApplyToFullState(newState);
            return newState;
        }}
    }}
");
                }

                deltaNetRegister = $@"EntityManager.ComponentFactory.RegisterNetworkedFields<{classSymbol}>({fieldsStr});";

                deltaGetState = @$"// Delta state
            if (component is IComponentDelta delta && args.FromTick > component.CreationTick && delta.LastFieldUpdate >= args.FromTick)
            {{
                var fields = EntityManager.GetModifiedFields(component, args.FromTick);

                // Try and get a matching delta state for the relevant dirty fields, otherwise fall back to full state.
                switch (fields)
                {{{deltaGetFields}
                    default:
                        break;
                }}
            }}";

                deltaInterface = " : IComponentDelta";

                deltaCompFields = @$"/// <inheritdoc />
    public GameTick LastFieldUpdate {{ get; set; }} = GameTick.Zero;

    /// <inheritdoc />
    public GameTick[] LastModifiedFields {{ get; set; }} = Array.Empty<GameTick>();";
            }

            string handleState;
            if (!fieldDeltas)
            {
                var eventRaise = "";
                if (raiseAfterAutoHandle)
                {
                    eventRaise = @"

            var ev = new AfterAutoHandleStateEvent(args.Current);
            EntityManager.EventBus.RaiseComponentEvent(uid, component, ref ev);";
                }

                handleState = $@"
            if (args.Current is not {stateName} state)
                return;
{handleStateSetters}{eventRaise}";
            }
            else
            {
                // Re-indent handleStateSetters so it aligns with the switch block
                var stateSetters = handleStateSetters.ToString();
                stateSetters = stateSetters.Replace("            ", "                    ");


                var eventRaise = "";
                if (raiseAfterAutoHandle)
                {
                    eventRaise = @"

            if (args.Current is not {} current)
                return;

            var ev = new AfterAutoHandleStateEvent(current);
            EntityManager.EventBus.RaiseComponentEvent(uid, component, ref ev);";
                }

                handleState = $@"
            switch(args.Current)
            {{{deltaHandleFields}
                case {stateName} state:
                {{{stateSetters}
                    break;
                }}

                default:
                    return;
            }}{eventRaise}";
            }

            return $@"// <auto-generated />
#nullable enable
using System;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.Collections;
using Robust.Shared.Serialization;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Collections.Generic;

namespace {nameSpace};

public partial class {componentName}{deltaInterface}
{{
    {deltaCompFields}

    [System.Serializable, NetSerializable]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class {stateName} : IComponentState
    {{{stateFields}
{cloneMethod}
    }}

    [RobustAutoGenerated]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    public sealed class {componentName}_AutoNetworkSystem : EntitySystem
    {{
        public override void Initialize()
        {{
            {deltaNetRegister}
            SubscribeLocalEvent<{componentName}, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<{componentName}, ComponentHandleState>(OnHandleState);
        }}

        private void OnGetState(EntityUid uid, {componentName} component, ref ComponentGetState args)
        {{
            {deltaGetState}

            // Get full state
            args.State = new {stateName}
            {{{getStateInit}
            }};
        }}

        private void OnHandleState(EntityUid uid, {componentName} component, ref ComponentHandleState args)
        {{{handleState}
        }}
    }}

    {fieldStates}
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
                    var fieldDeltas = false;
                    if (attr.ConstructorArguments is [{Value: bool raise}, {Value: bool fields}])
                    {
                        // Get the afterautohandle bool, which is first constructor arg
                        raiseEv = raise;
                        fieldDeltas = fields;
                    }

                    var source = GenerateSource(context, type.Type, comp, raiseEv, fieldDeltas);
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
