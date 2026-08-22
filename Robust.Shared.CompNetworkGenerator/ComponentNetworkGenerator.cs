using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.SymbolDisplayFormat;
using static Microsoft.CodeAnalysis.SymbolDisplayMiscellaneousOptions;
using Robust.Roslyn.Shared;

// Yes dude I know this source generator isn't incremental, I'll fix it eventually.
#pragma warning disable RS1035

namespace Robust.Shared.CompNetworkGenerator
{
    [Generator]
#pragma warning disable RS1042
    public class ComponentNetworkGenerator : ISourceGenerator
#pragma warning restore RS1042
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
        private const string GlobalIRobustCloneableName = "global::Robust.Shared.Serialization.IRobustCloneable";

        private static readonly SymbolDisplayFormat FullNullableFormat =
            FullyQualifiedFormat.WithMiscellaneousOptions(IncludeNullableReferenceTypeModifier);

        private static string? GenerateSource(
            in GeneratorExecutionContext context,
            INamedTypeSymbol classSymbol,
            TypeDeclarationSyntax classSyntax,
            CSharpCompilation comp,
            bool raiseAfterAutoHandle,
            bool fieldDeltas,
            bool excludeReplays)
        {
            var partialInfo = PartialTypeInfo.FromSymbol(classSymbol, classSyntax);
            var componentName = classSymbol.Name;
            var stateName = $"{componentName}_AutoState";
            var componentDeltaStateName = $"{componentName}_AutoDeltaState";

            var members = TypeSymbolHelper.GetAllMembersIncludingInherited(classSymbol);
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
            var clientGetStateInit = new StringBuilder();

            // eg:
            //            component.Name = state.Name;
            //            component.Count = state.Count;
            var handleStateSetters = new StringBuilder();

            // Builds the string for duplicating a full component state, in preparation for applying a delta state state
            // without modifying the original.
            var shallowClone = new StringBuilder();

            // Delta field state generation.
            var deltaGetFields = new StringBuilder();
            var clientDeltaGetFields = new StringBuilder();

            var deltaHandleFields = new StringBuilder();
            var deltaStateFields = new StringBuilder();

            // Apply the delta field to the full state.
            var deltaApply = new List<string>();

            var index = -1;

            var fieldsStr = new StringBuilder();
            var fieldStates = new StringBuilder();

            var networkedTypes = new List<string>();
            var usesClientCollectionCopy = false;
            var collectionCopyMethods = new StringBuilder();

            void AppendShallowClone(string fieldName)
            {
                shallowClone.Append($@"
                {fieldName} = this.{fieldName},");
            }

            void AppendCollectionClone(string fieldName, bool nullable, string? copyMethodName = null)
            {
                var copy = copyMethodName == null
                    ? $"new(this.{fieldName})"
                    : $"{copyMethodName}(this.{fieldName})";
                var value = nullable
                    ? $"this.{fieldName} == null ? null! : {copy}"
                    : copy;
                shallowClone.Append($@"
                {fieldName} = {value},");
            }

            void AppendTypedCollectionClone(ITypeSymbol type, string fieldName, bool nullable, string? copyMethodName)
            {
                var copy = GetCollectionCopyExpression(type, $"this.{fieldName}", copyMethodName);
                var value = nullable
                    ? $"this.{fieldName} == null ? null! : {copy}"
                    : copy;
                shallowClone.Append($@"
                {fieldName} = {value},");
            }

            string GetClientCollectionField(ITypeSymbol type, string fieldName, bool nullable, string? copyMethodName)
            {
                usesClientCollectionCopy = true;
                var copy = GetCollectionCopyExpression(type, $"component.{fieldName}", copyMethodName);
                return nullable
                    ? $"component.{fieldName} == null ? null! : {copy}"
                    : copy;
            }

            string GetCollectionCopyExpression(ITypeSymbol type, string source, string? copyMethodName)
            {
                if (copyMethodName != null)
                    return $"{copyMethodName}({source})";

                var named = (INamedTypeSymbol) type;
                var typeName = type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(FullNullableFormat);
                return named.ConstructedFrom.ToDisplayString(FullyQualifiedFormat) switch
                {
                    GlobalDictionaryName => $"new {typeName}({source}, {source}.Comparer)",
                    GlobalHashSetName => $"new {typeName}({source}, {source}.Comparer)",
                    GlobalListName => $"new {typeName}({source})",
                    _ => throw new InvalidOperationException($"Unsupported collection type {type}")
                };
            }

            string GetCollectionRefill(ITypeSymbol type, string target, string source, string indentation)
            {
                var named = (INamedTypeSymbol) type;
                var constructed = named.ConstructedFrom.ToDisplayString(FullyQualifiedFormat);
                switch (constructed)
                {
                    case GlobalDictionaryName:
                    {
                        var key = GetCopyValueExpression(named.TypeArguments[0], "key");
                        var value = GetCopyValueExpression(named.TypeArguments[1], "value");
                        return $@"{target}.EnsureCapacity({source}.Count);
{indentation}foreach (var (key, value) in {source})
{indentation}    {target}.Add({key}, {value});";
                    }
                    case GlobalHashSetName:
                    {
                        var valueType = named.TypeArguments[0];
                        if (!NeedsCopyValue(valueType))
                            return $"{target}.UnionWith({source});";

                        var value = GetCopyValueExpression(valueType, "value");
                        return $@"{target}.EnsureCapacity({source}.Count);
{indentation}foreach (var value in {source})
{indentation}    {target}.Add({value});";
                    }
                    case GlobalListName:
                    {
                        var valueType = named.TypeArguments[0];
                        if (!NeedsCopyValue(valueType))
                            return $"{target}.AddRange({source});";

                        var value = GetCopyValueExpression(valueType, "value");
                        return $@"{target}.EnsureCapacity({source}.Count);
{indentation}foreach (var value in {source})
{indentation}    {target}.Add({value});";
                    }
                    default:
                        throw new InvalidOperationException($"Unsupported collection type {type}");
                }
            }

            void AppendCollectionCopyMethod(ITypeSymbol type, string methodName)
            {
                var named = (INamedTypeSymbol) type;
                var typeName = type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(FullNullableFormat);
                var constructed = named.ConstructedFrom.ToDisplayString(FullyQualifiedFormat);
                var refill = GetCollectionRefill(type, "copy", "source", "        ");

                switch (constructed)
                {
                    case GlobalDictionaryName:
                        collectionCopyMethods.Append($$"""
    private static {{typeName}} {{methodName}}({{typeName}} source)
    {
        var copy = new {{typeName}}(source.Count, source.Comparer);
        {{refill}}
        return copy;
    }

""");
                        break;
                    case GlobalHashSetName:
                        collectionCopyMethods.Append($$"""
    private static {{typeName}} {{methodName}}({{typeName}} source)
    {
        var copy = new {{typeName}}(source.Count, source.Comparer);
        {{refill}}
        return copy;
    }

""");
                        break;
                    case GlobalListName:
                        collectionCopyMethods.Append($$"""
    private static {{typeName}} {{methodName}}({{typeName}} source)
    {
        var copy = new {{typeName}}(source.Count);
        {{refill}}
        return copy;
    }

""");
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported collection type {type}");
                }
            }

            bool NeedsDeepCollectionCopy(ITypeSymbol type)
            {
                var named = (INamedTypeSymbol) type;
                switch (named.ConstructedFrom.ToDisplayString(FullyQualifiedFormat))
                {
                    case GlobalDictionaryName:
                        return NeedsCopyValue(named.TypeArguments[0]) || NeedsCopyValue(named.TypeArguments[1]);
                    case GlobalHashSetName:
                    case GlobalListName:
                        return NeedsCopyValue(named.TypeArguments[0]);
                    default:
                        throw new InvalidOperationException($"Unsupported collection type {type}");
                }
            }

            bool NeedsCopyValue(ITypeSymbol type)
            {
                return ImplementsInterface(GetCloneableType(type), GlobalIRobustCloneableName);
            }

            ITypeSymbol GetCloneableType(ITypeSymbol type)
            {
                if (type is INamedTypeSymbol named &&
                    named.OriginalDefinition.ToDisplayString() == "System.Nullable<T>")
                {
                    return named.TypeArguments[0];
                }

                return type.WithNullableAnnotation(NullableAnnotation.None);
            }

            string GetCopyValueExpression(ITypeSymbol type, string value)
            {
                if (!NeedsCopyValue(type))
                    return value;

                if (type is INamedTypeSymbol named &&
                    named.OriginalDefinition.ToDisplayString() == "System.Nullable<T>")
                {
                    return $"{value}.HasValue ? {value}.Value.Clone() : null";
                }

                if (type.NullableAnnotation == NullableAnnotation.Annotated && type.IsReferenceType)
                    return $"{value} == null ? null! : {value}.Clone()";

                return $"{value}.Clone()";
            }

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

                // The type used for networking, e.g. EntityUid -> NetEntity
                string networkedType;

                string getField;
                string? clientGetField = null;
                string? cast;
                // TODO: Uhh I just need casts or something.
                var castString = typeDisplayStr.Substring(8);
                var fieldMask = $"(1UL << {index})";

                deltaHandleFields.Append($@"
                    if ((deltaState.ChangedFields & {fieldMask}) != 0)
                    {{");

                var fieldHandleValue = $"deltaState.{name}!";

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

                        AppendShallowClone(name);

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

                        AppendShallowClone(name);

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

                        AppendCollectionClone(name, nullable);

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

                        AppendCollectionClone(name, nullable);

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

                                AppendCollectionClone(name, nullable);

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

                                AppendCollectionClone(name, nullable);

                                deltaApply.Add($@"fullState.{name} = {name};");

                                break;
                            }
                        }

                        networkedType = $"{typeDisplayStr}";

                        stateFields.Append($@"
        public {networkedType} {name} = default!;");

                        if (ImplementsInterface(type, GlobalIRobustCloneableName))
                        {
                            getField = $"component.{name}";
                            cast = $"({castString})";

                            var nullCast = nullable ? castString.Substring(0, castString.Length - 1) : castString;

                            if (nullable)
                            {
                                handleStateSetters.Append($@"
            component.{name} = state.{name} == null ? null! : state.{name}.Clone();");
                                deltaHandleFields.Append($@"
                    var {name}Value = {cast} {fieldHandleValue};
                    if ({name}Value == null)
                        component.{name} = null!;
                    else
                        component.{name} = ({nullCast})({name}Value.Clone());");
                                AppendShallowClone(name);
                                deltaApply.Add($"fullState.{name} = {name} == null ? null! : {name}.Clone();");
                            }
                            else
                            {
                                handleStateSetters.Append($@"
            component.{name} = state.{name}.Clone();");
                                deltaHandleFields.Append($@"
                    component.{name} = {cast}({fieldHandleValue}.Clone());");
                                AppendShallowClone(name);
                                deltaApply.Add($"fullState.{name} = {name}.Clone();");
                            }
                        }
                        else if (IsCloneType(type))
                        {
                            getField = $"component.{name}";
                            var copyMethodName = NeedsDeepCollectionCopy(type)
                                ? $"__AutoNetworkCopyCollection{index}"
                                : null;
                            if (copyMethodName != null)
                                AppendCollectionCopyMethod(type, copyMethodName);

                            clientGetField = GetClientCollectionField(type, name, nullable, copyMethodName);
                            cast = $"({castString})";

                            var handleRefill = GetCollectionRefill(type, $"component.{name}", $"state.{name}", "                ");
                            var deltaRefill = GetCollectionRefill(type, $"component.{name}", $"{name}Value", "                        ");
                            var stateCopy = GetCollectionCopyExpression(type, $"state.{name}", copyMethodName);
                            var valueCopy = GetCollectionCopyExpression(type, $"{name}Value", copyMethodName);
                            var deltaApplyCopy = GetCollectionCopyExpression(type, name, copyMethodName);

                            if (nullable)
                            {
                                handleStateSetters.Append($@"
            if (state.{name} == null)
                component.{name} = null!;
            else if (component.{name} == null)
                component.{name} = {stateCopy};
            else
            {{
                component.{name}.Clear();
                {handleRefill}
            }}");

                                deltaHandleFields.Append($@"
                    var {name}Value = {cast} {fieldHandleValue};
                    if ({name}Value == null)
                        component.{name} = null!;
                    else if (component.{name} == null)
                        component.{name} = {valueCopy};
                    else
                    {{
                        component.{name}.Clear();
                        {deltaRefill}
                    }}");

                                deltaApply.Add($"fullState.{name} = {name} == null ? null! : {deltaApplyCopy};");
                            }
                            else
                            {
                                handleStateSetters.Append($@"
            component.{name}.Clear();
            {handleRefill}");

                                deltaHandleFields.Append($@"
                    var {name}Value = {cast} {fieldHandleValue};
                    component.{name}.Clear();
                    {deltaRefill}");

                                deltaApply.Add($"fullState.{name} = {deltaApplyCopy};");
                            }

                            AppendTypedCollectionClone(type, name, nullable, copyMethodName);
                        }
                        else
                        {
                            getField = $"component.{name}";
                            cast = $"({castString})";

                            handleStateSetters.Append($@"
            component.{name} = state.{name};");

                            deltaHandleFields.Append($@"
                    component.{name} = {cast} {fieldHandleValue};");

                            AppendShallowClone(name);

                            deltaApply.Add($"fullState.{name} = {name};");
                        }

                        break;
                }

                /*
                 * End loop stuff
                 */

                networkedTypes.Add(networkedType);
                clientGetField ??= getField;

                deltaStateFields.Append($@"
        [NetworkedDeltaField({index})]
        public {networkedType} {name} = default!;");

                getStateInit.Append($@"
                {name} = {getField},");

                clientGetStateInit.Append($@"
                {name} = {clientGetField},");

                deltaGetFields.Append($@"
                    if ((aspects & {fieldMask}) != 0)
                        state.{name} = {getField};");

                deltaHandleFields.Append(@"
                    }
");

                clientDeltaGetFields.Append($@"
                    if ((aspects & {fieldMask}) != 0)
                        state.{name} = {clientGetField};");
            }

            var deltaGetState = "";
            var clientDeltaGetState = "";
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

                var deltaStateApply = new StringBuilder();
                for (var i = 0; i < fields.Count; i++)
                {
                    var fieldMask = $"(1UL << {i})";
                    var apply = deltaApply[i];

                    deltaStateApply.Append($@"
            if ((ChangedFields & {fieldMask}) != 0)
            {{
                {apply}
            }}
");
                }

                // Creates a single state that stores an arbitrary combination of dirty fields.
                fieldStates.Append($@"
    [Serializable, NetSerializable]
    public sealed class {componentDeltaStateName} : IAutoGeneratedComponentDeltaState, IComponentDeltaState<{stateName}>
    {{
        public ulong ChangedFields {{ get; set; }}

{TrimNewLines(deltaStateFields)}

        public void ApplyToFullState({stateName} fullState)
        {{
{TrimNewLines(deltaStateApply)}
        }}

        public {stateName} CreateNewFullState({stateName} fullState)
        {{
            var newState = fullState.ShallowClone();
            ApplyToFullState(newState);
            return newState;
        }}
    }}
");

                deltaNetRegister = $@"EntityManager.ComponentFactory.RegisterNetworkedFields<{classSymbol}>({fieldsStr});";

                deltaGetState = @$"// Delta state
            if (component is IComponentDelta delta && args.FromTick > component.CreationTick)
            {{
                var aspects = EntityManager.GetModifiedAspects(component, args.FromTick);

                if (aspects > 0 && aspects < DeltaAspect.Unclassified)
                {{
                    var state = new {componentDeltaStateName}
                    {{
                        ChangedFields = aspects,
                    }};
{deltaGetFields}
                    args.State = state;
                    return;
                }}
            }}";

                clientDeltaGetState = @$"// Delta state
            if (component is IComponentDelta delta && args.FromTick > component.CreationTick)
            {{
                var aspects = EntityManager.GetModifiedAspects(component, args.FromTick);

                if (aspects > 0 && aspects < DeltaAspect.Unclassified)
                {{
                    var state = new {componentDeltaStateName}
                    {{
                        ChangedFields = aspects,
                    }};
{clientDeltaGetFields}
                    args.State = state;
                    return;
                }}
            }}";

                deltaInterface = " : IComponentDelta";

                deltaCompFields = @$"/// <inheritdoc />
    public GameTick LastUnclassifiedDirty {{ get; set; }}
    /// <inheritdoc />
    public GameTick[] LastModifiedFields {{ get; set; }} = default!;";
            }

            string handleState;
            if (!fieldDeltas)
            {
                var eventRaise = "";
                var stateSetters = TrimNewLines(handleStateSetters);
                if (raiseAfterAutoHandle)
                {
                    eventRaise = @"

            var ev = new AfterAutoHandleStateEvent(args.Current);
            EntityManager.EventBus.RaiseComponentEvent(uid, component, ref ev);";
                }

                handleState = $@"
            if (args.Current is not {stateName} state)
                return;

{stateSetters}{eventRaise}";
            }
            else
            {
                // Re-indent handleStateSetters so it aligns with the switch block
                var stateSetters = TrimNewLines(handleStateSetters);
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
            {{
                case {componentDeltaStateName} deltaState:
                {{
{deltaHandleFields}
                    break;
                }}

                case {stateName} state:
                {{{stateSetters}
                    break;
                }}

                default:
                    return;
            }}{eventRaise}";
            }

            var excludeReplaysStr = string.Empty;
            if (excludeReplays)
            {
                excludeReplaysStr = @"
            if (args.ReplayState)
            {
                args.ExcludeReplays = true;
                return;
            }
";
            }

            var outSb = new StringBuilder();
            var stateFieldsText = TrimNewLines(stateFields);
            var getStateInitText = TrimNewLines(getStateInit);
            var clientGetStateInitText = TrimNewLines(clientGetStateInit);
            var cloneMethodText = TrimNewLines(cloneMethod);
            var excludeReplaysText = TrimNewLines(excludeReplaysStr);
            var deltaGetStateText = TrimNewLines(deltaGetState);
            var clientDeltaGetStateText = TrimNewLines(clientDeltaGetState);
            var deltaCompFieldsText = TrimNewLines(deltaCompFields);
            var fieldStatesText = TrimNewLines(fieldStates);
            var collectionCopyMethodsText = TrimNewLines(collectionCopyMethods);

            var netManagerDependency = usesClientCollectionCopy
                ? "[global::Robust.Shared.IoC.Dependency] private global::Robust.Shared.Network.INetManager _net = default!;"
                : string.Empty;
            var getStateSubscription = usesClientCollectionCopy
                ? $@"            if (_net.IsClient)
                SubscribeLocalEvent<{componentName}, ComponentGetState>(OnGetStateClient);
            else
                SubscribeLocalEvent<{componentName}, ComponentGetState>(OnGetState);"
                : $@"            SubscribeLocalEvent<{componentName}, ComponentGetState>(OnGetState);";

            outSb.Append("""
                // <auto-generated />
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

                """);

            partialInfo.WriteHeader(outSb);

            outSb.AppendLine(deltaInterface);
            outSb.AppendLine("{");

            if (collectionCopyMethodsText.Length != 0)
            {
                outSb.AppendLine(collectionCopyMethodsText);
                outSb.AppendLine();
            }

            if (deltaCompFieldsText.Length != 0)
            {
                outSb.AppendLine(deltaCompFieldsText);
                outSb.AppendLine();
            }

            outSb.AppendLine("    [System.Serializable, NetSerializable]");
            outSb.AppendLine("    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
            outSb.AppendLine("    [RobustAutoGenerated]");
            outSb.AppendLine($"    public sealed class {stateName} : IComponentState");
            outSb.AppendLine("    {");
            outSb.AppendLine(stateFieldsText);

            if (cloneMethodText.Length != 0)
            {
                outSb.AppendLine();
                outSb.AppendLine(cloneMethodText);
            }

            outSb.AppendLine("    }");
            outSb.AppendLine();
            outSb.AppendLine("    [RobustAutoGenerated]");
            outSb.AppendLine("    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
            outSb.AppendLine($"    public sealed class {componentName}_AutoNetworkSystem : EntitySystem");
            outSb.AppendLine("    {");

            if (netManagerDependency.Length != 0)
            {
                outSb.AppendLine($"        {netManagerDependency}");
                outSb.AppendLine();
            }

            outSb.AppendLine("        public override void Initialize()");
            outSb.AppendLine("        {");

            if (deltaNetRegister.Length != 0)
                outSb.AppendLine($"            {deltaNetRegister}");

            outSb.AppendLine(getStateSubscription);
            outSb.AppendLine($"            SubscribeLocalEvent<{componentName}, ComponentHandleState>(OnHandleState);");
            outSb.AppendLine("        }");
            outSb.AppendLine();
            outSb.AppendLine($"        private void OnGetState(EntityUid uid, {componentName} component, ref ComponentGetState args)");
            outSb.AppendLine("        {");

            if (excludeReplaysStr.Length != 0)
            {
                outSb.AppendLine(IndentFirstLine(excludeReplaysText, 12));
                outSb.AppendLine();
            }

            if (deltaGetStateText.Length != 0)
            {
                outSb.AppendLine(IndentFirstLine(deltaGetStateText, 12));
                outSb.AppendLine();
            }

            outSb.AppendLine("            // Get full state");
            outSb.AppendLine($"            args.State = new {stateName}");
            outSb.AppendLine("            {");
            outSb.AppendLine(getStateInitText);
            outSb.AppendLine("            };");
            outSb.AppendLine("        }");

            if (usesClientCollectionCopy)
            {
                outSb.AppendLine();
                outSb.AppendLine($"        private void OnGetStateClient(EntityUid uid, {componentName} component, ref ComponentGetState args)");
                outSb.AppendLine("        {");

                if (clientDeltaGetStateText.Length != 0)
                {
                    outSb.AppendLine(IndentFirstLine(clientDeltaGetStateText, 12));
                    outSb.AppendLine();
                }

                outSb.AppendLine("            // Get full state");
                outSb.AppendLine($"            args.State = new {stateName}");
                outSb.AppendLine("            {");
                outSb.AppendLine(clientGetStateInitText);
                outSb.AppendLine("            };");
                outSb.AppendLine("        }");
            }

            outSb.AppendLine();
            outSb.AppendLine($"        private void OnHandleState(EntityUid uid, {componentName} component, ref ComponentHandleState args)");
            outSb.AppendLine("        {");
            outSb.AppendLine(TrimNewLines(handleState));
            outSb.AppendLine("        }");
            outSb.AppendLine("    }");

            if (fieldStatesText.Length != 0)
            {
                outSb.AppendLine();
                outSb.AppendLine(fieldStatesText);
            }

            outSb.AppendLine("}");

            partialInfo.WriteFooter(outSb);

            return outSb.ToString();
        }

        private static string TrimNewLines(StringBuilder source)
        {
            return source.ToString().Trim('\r', '\n');
        }

        private static string TrimNewLines(string source)
        {
            return source.Trim('\r', '\n');
        }

        private static string IndentFirstLine(string source, int spaces)
        {
            if (source.Length == 0)
                return source;

            return new string(' ', spaces) + source;
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
            foreach (var (classType, classSyntax, attribute) in symbols)
            {
                try
                {
                    var raiseEv = false;
                    var fieldDeltas = false;
                    var excludeReplays = false;
                    if (attribute.ConstructorArguments is [{Value: bool raise}, {Value: bool fields}, {Value: bool exclude}])
                    {
                        // Get the afterautohandle bool, which is first constructor arg
                        raiseEv = raise;
                        fieldDeltas = fields;
                        excludeReplays = exclude;
                    }

                    var source = GenerateSource(context, classType, classSyntax, comp, raiseEv, fieldDeltas, excludeReplays);
                    // can be null if no members marked with network field, which already has a diagnostic, so
                    // just continue
                    if (source == null)
                        continue;

                    context.AddSource($"{classType.Name}_CompNetwork.g.cs", SourceText.From(source, Encoding.UTF8));
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
                            classType.Locations[0]));
                }
            }
        }

        private IReadOnlyList<(INamedTypeSymbol Type, TypeDeclarationSyntax Syntax, AttributeData Attribute)> GetAnnotatedTypes(
            in GeneratorExecutionContext context,
            CSharpCompilation comp,
            NameReferenceSyntaxReceiver receiver)
        {
            var symbols = new List<(INamedTypeSymbol, TypeDeclarationSyntax, AttributeData)>();
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
                    foreach (var mem in TypeSymbolHelper.GetAllMembersIncludingInherited(typeSymbol))
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
                    symbols.Add((typeSymbol, candidateClass, relevantAttribute));
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

        private static bool ImplementsInterface(ITypeSymbol type, string interfaceName)
        {
            foreach (var interfaceType in type.AllInterfaces)
            {
                if (interfaceType.ToDisplayString(FullyQualifiedFormat).Contains(interfaceName)
                    || interfaceType.ConstructedFrom.ToDisplayString(FullyQualifiedFormat).Contains(interfaceName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
