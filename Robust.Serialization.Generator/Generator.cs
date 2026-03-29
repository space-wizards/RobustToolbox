using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Robust.Roslyn.Shared.DataDefinitionHelper;
using static Robust.Serialization.Generator.CustomSerializerType;
using static Robust.Serialization.Generator.Types;

namespace Robust.Serialization.Generator;

[Generator]
public class Generator : IIncrementalGenerator
{
    private const string TypeCopierInterfaceNamespace =
        "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeCopier";

    private const string TypeCopyCreatorInterfaceNamespace =
        "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeCopyCreator";

    private const string TypeValidatorInterfaceNamespace =
        "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeValidator";

    private const string TypeReaderInterfaceNamespace =
        "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeReader";

    private const string TypeWriterInterfaceNamespace =
        "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeWriter";

    private const string SerializationHooksNamespace = "Robust.Shared.Serialization.ISerializationHooks";
    private const string AutoStateAttributeName = "Robust.Shared.Analyzers.AutoGenerateComponentStateAttribute";
    private const string ComponentDeltaInterfaceName = "Robust.Shared.GameObjects.IComponentDelta";
    private const string MappingDataNodeName = "Robust.Shared.Serialization.Markdown.Mapping.MappingDataNode";
    private const string SequenceDataNodeName = "Robust.Shared.Serialization.Markdown.Sequence.SequenceDataNode";
    private const string ValueDataNodeName = "Robust.Shared.Serialization.Markdown.Value.ValueDataNode";
    private const string EntityUidName = "Robust.Shared.GameObjects.EntityUid";

    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        IncrementalValuesProvider<(string name, string code)?> dataDefinitions = initContext.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax,
                static (context, _) =>
                {
                    var type = (TypeDeclarationSyntax)context.Node;
                    var symbol = (ITypeSymbol)context.SemanticModel.GetDeclaredSymbol(type)!;

                    if (symbol.TypeKind == TypeKind.Interface ||
                        !IsDataDefinition(symbol, out var isDataRecord))
                    {
                        return null;
                    }

                    return GenerateForDataDefinition(type, symbol, isDataRecord);
                }
            )
            .Where(static type => type != null);

        initContext.RegisterSourceOutput(
            dataDefinitions.Collect(),
            static (sourceContext, sources) =>
            {
                var done = new HashSet<string>();

                foreach (var source in sources)
                {
                    var (name, code) = source!.Value;

                    if (!done.Add(name))
                        continue;

                    sourceContext.AddSource(name, code);
                }
            }
        );
    }

    private static (string, string)? GenerateForDataDefinition(
        TypeDeclarationSyntax declaration,
        ITypeSymbol type,
        bool isDataRecord)
    {
        var builder = new StringBuilder();
        var containingTypes = new Stack<INamedTypeSymbol>();
        containingTypes.Clear();

        var symbolName = type
            .ToDisplayString()
            .Replace('<', '{')
            .Replace('>', '}');

        var nonPartial = !IsPartial(declaration);

        var namespaceString = type.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : $"namespace {type.ContainingNamespace.ToDisplayString()};";

        var containingType = type.ContainingType;
        while (containingType != null)
        {
            containingTypes.Push(containingType);
            containingType = containingType.ContainingType;
        }

        var containingTypesStart = new StringBuilder();
        var containingTypesEnd = new StringBuilder();
        foreach (var parent in containingTypes)
        {
            var syntax = (ClassDeclarationSyntax)parent.DeclaringSyntaxReferences[0].GetSyntax();
            if (!IsPartial(syntax))
            {
                nonPartial = true;
                continue;
            }

            containingTypesStart.AppendLine($"{GetPartialTypeDefinitionLine(parent)}\n{{");
            containingTypesEnd.AppendLine("}");
        }

        var definition = GetDataDefinition(type, isDataRecord);
        if (nonPartial || definition.InvalidFields)
            return null;

        builder.AppendLine($$"""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Collections.Immutable;
            using System.Diagnostics.CodeAnalysis;
            using Robust.Shared.Analyzers;
            using Robust.Shared.IoC;
            using Robust.Shared.GameObjects;
            using Robust.Shared.Serialization;
            using Robust.Shared.Serialization.Manager;
            using Robust.Shared.Serialization.Manager.Definition;
            using Robust.Shared.Serialization.Manager.Exceptions;
            using Robust.Shared.Serialization.Markdown;
            using Robust.Shared.Serialization.Markdown.Mapping;
            using Robust.Shared.Serialization.Markdown.Sequence;
            using Robust.Shared.Serialization.Markdown.Validation;
            using Robust.Shared.Serialization.Markdown.Value;
            using Robust.Shared.Serialization.TypeSerializers.Interfaces;
            #pragma warning disable CS0618 // Type or member is obsolete
            #pragma warning disable CS0612 // Type or member is obsolete
            #pragma warning disable CS0108 // Member hides inherited member; missing new keyword
            #pragma warning disable RA0002 // Robust access analyzer

            {{namespaceString}}

            {{containingTypesStart}}

            {{GetPartialTypeDefinitionLine(type)}} : ISerializationGenerated<{{definition.GenericTypeName}}>
            {
                {{GetConstructors(definition)}}

                {{GetInstantiators(definition)}}

                {{GetCopiers(definition)}}

                {{GetReader(definition)}}

                {{GetWriter(definition)}}

                {{GetValidator(definition)}}

                {{GetFieldDefinitions(definition)}}
            }

            {{containingTypesEnd}}
            """);

        return ($"{symbolName}.g.cs", builder.ToString());
    }

    private static void GetDataFields(
        ITypeSymbol definition,
        bool isDataRecord,
        List<DataField> fields,
        List<INamedTypeSymbol> symbols,
        ref bool invalidFields)
    {
        foreach (var (field, fieldType, attribute) in GetAllDataFields(definition, isDataRecord))
        {
            if (!IsDataDefinition(field.ContainingType, out _))
                invalidFields = true;

            if (attribute.Data?.ConstructorArguments.FirstOrDefault(arg => arg.Kind == TypedConstantKind.Type).Value is
                INamedTypeSymbol customSerializer)
            {
                var serializerType = None;
                if (ImplementsInterface(customSerializer, TypeCopierInterfaceNamespace))
                    serializerType |= Copier;
                else if (ImplementsInterface(customSerializer, TypeCopyCreatorInterfaceNamespace))
                    serializerType |= CopyCreator;

                if (ImplementsInterface(customSerializer, TypeValidatorInterfaceNamespace, symbols))
                {
                    foreach (var symbol in symbols)
                    {
                        if (symbol.IsGenericType &&
                            symbol.TypeArguments is { Length: >= 2 } arguments)
                        {
                            var nodeType = arguments[1];
                            if (nodeType.ToDisplayString().Contains(MappingDataNodeName))
                                serializerType |= MappingValidator;

                            if (nodeType.ToDisplayString().Contains(SequenceDataNodeName))
                                serializerType |= SequenceValidator;

                            if (nodeType.ToDisplayString().Contains(ValueDataNodeName))
                                serializerType |= ValueValidator;
                        }
                    }
                }

                if (ImplementsInterface(customSerializer, TypeReaderInterfaceNamespace, symbols))
                {
                    foreach (var symbol in symbols)
                    {
                        if (symbol.IsGenericType &&
                            symbol.TypeArguments is { Length: >= 2 } arguments)
                        {
                            var nodeType = arguments[1];
                            if (nodeType.ToDisplayString().Contains(MappingDataNodeName))
                                serializerType |= MappingReader;

                            if (nodeType.ToDisplayString().Contains(SequenceDataNodeName))
                                serializerType |= SequenceReader;

                            if (nodeType.ToDisplayString().Contains(ValueDataNodeName))
                                serializerType |= ValueReader;
                        }
                    }
                }

                if (ImplementsInterface(customSerializer, TypeWriterInterfaceNamespace, symbols))
                    serializerType |= Writer;

                if (serializerType != None)
                {
                    fields.Add(new DataField(field, fieldType, attribute, (customSerializer, serializerType)));
                    continue;
                }
            }

            fields.Add(new DataField(field, fieldType, attribute, null));

            if (IsReadOnlyMember(definition, fieldType))
                invalidFields = true;
        }
    }

    private static DataDefinition GetDataDefinition(ITypeSymbol definition, bool isDataRecord)
    {
        var fields = new List<DataField>();
        var symbols = new List<INamedTypeSymbol>();
        var invalidFields = false;

        GetDataFields(definition, isDataRecord, fields, symbols, ref invalidFields);

        var typeName = GetGenericTypeName(definition);
        var hasHooks = ImplementsInterface(definition, SerializationHooksNamespace);

        // Same as DataDefinition.cs
        fields.Sort((a, b) =>
        {
            var priority = b.Attribute.Priority.CompareTo(a.Attribute.Priority);
            if (priority != 0)
                return priority;

            return string.Compare(b.Symbol.Name, a.Symbol.Name, StringComparison.OrdinalIgnoreCase);
        });

        return new DataDefinition(definition, typeName, fields, hasHooks, invalidFields);
    }

    private static string GetConstructors(DataDefinition definition)
    {
        if (definition.Type.TypeKind == TypeKind.Interface)
            return string.Empty;

        var builder = new StringBuilder();
        var thisCall = new StringBuilder();
        var (needsEmpty, mustCall) = NeedsEmptyConstructor(definition.Type);
        if (mustCall != null)
        {
            thisCall.Append(" : this(");
            foreach (var parameter in mustCall.Parameters)
            {
                thisCall.Append($"({parameter.Type.ToDisplayString()}) default!,");
            }

            if (thisCall[thisCall.Length - 1] == ',')
                thisCall = thisCall.Remove(thisCall.Length - 1, 1);

            thisCall.Append(')');
        }

        var setsRequired = GetSetsRequiredAttributeOrEmpty(definition.Type);
        if (needsEmpty)
        {
            // There was one case in content of a content-defined constructor calling the source-generated
            // empty constructor, which would then call it again.
            // Instead of attempting to find loops like this, I changed content.
            // Because that's fucking stupid.
            builder.AppendLine($$"""
                // Implicit constructor
                #pragma warning disable CS8618
                {{setsRequired}}
                public {{definition.Type.Name}}(){{thisCall}}
                #pragma warning restore CS8618
                {
                }
                """);
        }

        var accessibility = definition.Type.IsValueType
            ? "public"
            : definition.Type.IsSealed
                ? "private"
                : "protected";

        var copyBaseCall = IsDataDefinition(definition.Type.BaseType, out _)
            ? "base(ISerializationGeneratedCopy, source, serialization, hookCtx, context)"
            : "this()";

        var readBaseCall = IsDataDefinition(definition.Type.BaseType, out _)
            ? "base(ISerializationGeneratedRead, mappingDataNode, serialization, hookCtx, context)"
            : "this()";

        builder.AppendLine($$"""
            [Obsolete("Used only in serialization source generation internally")]
            #pragma warning disable CS8618
            {{setsRequired}}
            {{accessibility}} {{definition.Type.Name}}(
                ISerializationGeneratedCopy ISerializationGeneratedCopy,
                {{definition.GenericTypeName}} source,
                ISerializationManager serialization,
                SerializationHookContext hookCtx,
                ISerializationContext? context
            ) : {{copyBaseCall}}
            #pragma warning restore CS8618
            {
                {{GetCopyBody(definition)}}
            }

            [Obsolete("Used only in serialization source generation internally")]
            #pragma warning disable CS8618
            {{setsRequired}}
            {{accessibility}} {{definition.Type.Name}}(
                ISerializationGeneratedRead ISerializationGeneratedRead,
                MappingDataNode mappingDataNode,
                ISerializationManager serialization,
                SerializationHookContext hookCtx,
                ISerializationContext? context
            ) : {{readBaseCall}}
            #pragma warning restore CS8618
            {
                {{GetReadBody(definition)}}
            }
            """);

        return builder.ToString();
    }

    private static string GetReadBody(DataDefinition definition)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < definition.Fields.Count; i++)
        {
            var field = definition.Fields[i];
            if (!definition.Type.Equals(field.Symbol.ContainingType, SymbolEqualityComparer.Default))
                continue;

            if (field.Attribute.ServerOnly)
            {
                builder.AppendLine("""
                    if (serialization.IsServer)
                    {
                    """);
            }

            var fieldName = field.Symbol.Name;
            if (field.Attribute.IsDataFieldAttribute)
            {
                builder.AppendLine($$"""
                    if (mappingDataNode.TryGet("{{field.Attribute.Tag}}", out var node{{i}}))
                    {
                    """);
            }
            else
            {
                builder.AppendLine($$"""
                    {
                        var node{{i}} = mappingDataNode;
                    """);
            }

            var (fieldTypeName, nonNullableFieldTypeName) = GetCleanNameForGenericType(field.Type, out _);
            var tagName = field.Attribute.Tag;
            var reader = field.CustomSerializer;
            var readerName = reader?.Serializer.ToDisplayString();
            var nullable = field.Type.NullableAnnotation == NullableAnnotation.Annotated ||
                           field.Type.ToDisplayString().EndsWith("?");
            var nullableString = string.Empty;
            if (!field.Type.IsValueType)
            {
                nullableString = $", {(!nullable).ToString().ToLowerInvariant()}";
                if (fieldTypeName.EndsWith("?"))
                    fieldTypeName = fieldTypeName.Substring(0, fieldTypeName.Length - 1);
            }

            var nullExpression =
                field.Type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString().Equals(EntityUidName)
                    ? $"this.{fieldName} = EntityUid.Invalid;"
                    : nullable
                        ? $"this.{fieldName} = default!;"
                        : "throw new NullNotAllowedException();";

            builder.AppendLine($$"""
                if (node{{i}}.IsNull)
                {
                    {{nullExpression}}
                }
                else
                {
                """);

            var method = $"Read<{fieldTypeName}>";
            if (field.Type.TypeKind == TypeKind.Enum)
            {
                method = $"ReadEnum<{fieldTypeName}>";
                nullableString = string.Empty;
            }
            else if (field.Type.IsValueType && IsDataDefinition(field.Type, out _))
            {
                method = $"ReadStructDefinition<{fieldTypeName}>";
                nullableString = string.Empty;
            }
            else if (field.Type.TypeKind == TypeKind.Array &&
                     field.Type is IArrayTypeSymbol { Rank: 1 } arrayTypeSymbol) // [*,*] goes the regular way
            {
                var elementType = arrayTypeSymbol.ElementType;
                method = $"ReadArray<{elementType}>";

                if (elementType.NullableAnnotation != NullableAnnotation.Annotated &&
                    !elementType.ToDisplayString().EndsWith("?") &&
                    !elementType.IsValueType)
                {
                    nullableString = $", {(!nullable).ToString().ToLowerInvariant()}";
                }
                else
                {
                    nullableString = string.Empty;
                }
            }
            else if (IsDataDefinition(field.Type, out _) && field.Type.TypeKind != TypeKind.Interface)
            {
                method = $"ReadDefinition<{fieldTypeName}>";
            }

            if (reader is { Type: var type } &&
                (type & (MappingReader | SequenceReader | ValueReader)) != 0)
            {
                builder.AppendLine($$"""
                    switch (node{{i}})
                    {
                    """);

                if ((reader.Value.Type & MappingReader) != 0)
                {
                    builder.AppendLine($"""
                        case MappingDataNode mapping:
                            this.{fieldName} = serialization.Read<{nonNullableFieldTypeName}, MappingDataNode, {readerName}>(mapping, hookCtx, context, null{nullableString});
                            break;
                        """);
                }

                if ((reader.Value.Type & SequenceReader) != 0)
                {
                    builder.AppendLine($"""
                        case SequenceDataNode sequence:
                            this.{fieldName} = serialization.Read<{nonNullableFieldTypeName}, SequenceDataNode, {readerName}>(sequence, hookCtx, context, null{nullableString});
                            break;
                        """);
                }

                if ((reader.Value.Type & ValueReader) != 0)
                {
                    builder.AppendLine($"""
                        case ValueDataNode value:
                            this.{fieldName} = serialization.Read<{nonNullableFieldTypeName}, ValueDataNode, {readerName}>(value, hookCtx, context, null{nullableString});
                            break;
                        """);
                }

                builder.AppendLine($$"""
                    default:
                        throw new InvalidOperationException($"Unable to read node for {{field.Symbol.Name}}({{field.Attribute.Data?.AttributeClass?.Name}}) as valid.");
                        break;
                    }
                    """);
            }
            else
            {
                builder.AppendLine(
                    $"this.{fieldName} = serialization.{method}(node{i}, hookCtx, context, null{nullableString});");
            }

            builder.AppendLine("}");
            builder.AppendLine("}");

            if (field.Attribute is { IsDataFieldAttribute: true, Required: true })
            {
                if (field.Type.IsReferenceType && fieldTypeName.EndsWith("?"))
                    fieldTypeName = fieldTypeName.Substring(0, fieldTypeName.Length - 1);

                builder.AppendLine($$"""
                    else
                    {
                        throw new RequiredFieldNotMappedException(typeof({{fieldTypeName}}), "{{tagName}}", typeof({{definition.Type.ToDisplayString()}}));
                    }
                    """);
            }

            if (field.Attribute.ServerOnly)
                builder.AppendLine("}");
        }

        return builder.ToString();
    }

    private static string GetInstantiators(DataDefinition definition)
    {
        var builder = new StringBuilder();
        var modifiers = string.Empty;

        if (GetFirstDataDefinitionBaseType(definition.Type) != null)
            modifiers = "override ";
        else if (IsVirtualClass(definition.Type))
            modifiers = "virtual ";

        if (definition.Type.IsAbstract)
        {
            // TODO make abstract once data definitions are forced to be partial
            builder.AppendLine($$"""
                /// <seealso cref="ISerializationManager.CreateCopy"/>
                [Obsolete("Use ISerializationManager.CreateCopy instead")]
                public {{modifiers}} {{definition.GenericTypeName}} Instantiate()
                {
                    throw new NotImplementedException();
                }
                """);
        }
        else
        {
            var requiredFields = GetRequiredFieldsPropertiesAssigners(definition.Type, string.Empty);
            builder.AppendLine($$"""
                /// <seealso cref="ISerializationManager.CreateCopy"/>
                [Obsolete("Use ISerializationManager.CreateCopy instead")]
                public {{modifiers}} {{definition.GenericTypeName}} Instantiate()
                {
                    return new {{definition.GenericTypeName}}(){{requiredFields}};
                }

                public static {{definition.GenericTypeName}} StaticInstantiate()
                {
                    return new {{definition.GenericTypeName}}();
                }

                public static object StaticInstantiateObject()
                {
                    return (object) {{definition.GenericTypeName}}.StaticInstantiate();
                }
                """);
        }

        return builder.ToString();
    }

    private static string GetValidator(DataDefinition definition)
    {
        var builder = new StringBuilder();
        var validateBuilder = new StringBuilder();

        for (var i = 0; i < definition.Fields.Count; i++)
        {
            validateBuilder.Clear();

            var field = definition.Fields[i];
            if (!definition.Type.Equals(field.Symbol.ContainingType, SymbolEqualityComparer.Default))
                continue;

            var fieldTypeName = GetNonNullableNameForGenericParameter(field.Type);
            var tagName = field.Attribute.Tag;
            if (field.Attribute.Include)
            {
                builder.AppendLine($"var node{i} = node;");
            }
            else
            {
                builder.AppendLine($$"""
                    if (node.TryGetValue("{{tagName}}", out var node{{i}}))
                    {
                    """);
            }

            var validator = field.CustomSerializer;
            var validatorName = validator?.Serializer.ToDisplayString();
            if (validator != null && (validator.Value.Type & MappingValidator) != 0)
            {
                validateBuilder.AppendLine($"""
                    case MappingDataNode mapping:
                        nodes["{tagName}"] = serialization.ValidateNode<{fieldTypeName}, MappingDataNode, {validatorName}>(mapping, context);
                        break;
                    """);
            }

            if (validator != null && (validator.Value.Type & SequenceValidator) != 0)
            {
                validateBuilder.AppendLine($"""
                    case SequenceDataNode sequence:
                        nodes["{tagName}"] = serialization.ValidateNode<{fieldTypeName}, SequenceDataNode, {validatorName}>(sequence, context);
                        break;
                    """);
            }

            if (validator != null && (validator.Value.Type & ValueValidator) != 0)
            {
                validateBuilder.AppendLine($"""
                    case ValueDataNode value:
                        nodes["{tagName}"] = serialization.ValidateNode<{fieldTypeName}, ValueDataNode, {validatorName}>(value, context);
                        break;
                    """);
            }

            builder.AppendLine($$"""
                switch (node{{i}})
                {
                    {{validateBuilder}}
                    default:
                        nodes["{{tagName}}"] = serialization.ValidateNode<{{fieldTypeName}}>(node{{i}}, context);
                        break;
                }
                """);

            if (!field.Attribute.Include)
                builder.AppendLine("}");
        }

        if (GetFirstDataDefinitionBaseType(definition.Type) is { } baseType)
            builder.AppendLine($"{baseType.ToDisplayString()}.Validate(nodes, node, serialization, context);");

        return $$"""
            public static void Validate(Dictionary<string, ValidationNode> nodes, MappingDataNode node, ISerializationManager serialization, ISerializationContext? context = null)
            {
                {{builder}}
            }
            """;
    }

    private static string GetCopiers(DataDefinition definition)
    {
        var builder = new StringBuilder();
        var requiredFields = GetRequiredFieldsPropertiesAssigners(definition.Type, string.Empty);
        var type = definition.Type;
        var baseType = type.BaseType;
        var baseDefinition = false;
        while (baseType != null)
        {
            if (!baseDefinition && IsDataDefinition(baseType, out _))
                baseDefinition = true;

            GetCopierMethod(definition, baseType, baseType.ToDisplayString(), true, builder, requiredFields);
            baseType = baseType.BaseType;
        }

        GetCopierMethod(definition, definition.Type, "object", baseDefinition, builder, requiredFields);
        GetCopierMethod(definition, definition.Type, GetGenericTypeName(type), false, builder, requiredFields);
        return builder.ToString();
    }

    private static void GetCopierMethod(
        DataDefinition definition,
        ITypeSymbol type,
        string targetType,
        bool forceOverride,
        StringBuilder builder,
        string requiredFields)
    {
        if (!IsDataDefinition(type, out _))
            return;

        var sameType = definition.Type.Equals(type, SymbolEqualityComparer.Default);
        var isSealedOrStruct = definition.Type.IsSealed || definition.Type.IsValueType;
        var isAbstract = definition.Type.IsAbstract;
        var isInterface = definition.Type.TypeKind == TypeKind.Interface;
        var modifier = (sameType, isSealedOrStruct, isAbstract, isInterface) switch
        {
            (true, true, _, _) => string.Empty,
            (true, false, false, _) => "virtual ",
            (false, _, _, true) => string.Empty,
            (false, _, false, false) => "override ",
            (_, _, true, _) => "abstract ",
        };

        if (forceOverride && modifier is "" or "abstract " or "virtual ")
        {
            if (modifier is "" or "abstract ")
                modifier += "override ";
            else if (modifier == "virtual ")
                modifier = "override ";
        }

        builder.AppendLine($"""
            public {modifier}void Copy(
                ref {targetType} target,
                ISerializationManager serialization,
                SerializationHookContext hookCtx,
                ISerializationContext? context = null)
            """);

        if (isAbstract)
        {
            builder.Append(";");
        }
        else
        {
            builder.AppendLine($$"""
                {
                    if (serialization.TryCustomCopy(this, ref target, hookCtx, {{definition.HasHooks.ToString().ToLower()}}, context))
                        return;

                    target = new {{definition.GenericTypeName}}(
                        ISerializationGeneratedCopy.Default,
                        this,
                        serialization,
                        hookCtx,
                        context
                    ){{requiredFields}};
                }
                """);
        }
    }

    private static string GetReader(DataDefinition definition)
    {
        if (definition.Type.IsAbstract)
            return string.Empty;

        var requiredFields = GetRequiredFieldsPropertiesAssigners(definition.Type, "target.");
        return $$"""
            public static void Read(
                ref {{definition.GenericTypeName}} target,
                MappingDataNode mappingDataNode,
                ISerializationManager serialization,
                SerializationHookContext hookCtx,
                ISerializationContext? context)
            {
                target = new {{definition.GenericTypeName}}(
                    ISerializationGeneratedRead.Default,
                    mappingDataNode,
                    serialization,
                    hookCtx,
                    context
                ){{requiredFields}};
            }
            """;
    }

    private static string GetWriter(DataDefinition definition)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < definition.Fields.Count; i++)
        {
            var field = definition.Fields[i];
            if (!definition.Type.Equals(field.Symbol.ContainingType, SymbolEqualityComparer.Default))
                continue;

            if (field.Attribute.ReadOnly)
                continue;

            var fieldType = field.Type.ToDisplayString();
            if (IsMultidimensionalArray(field.Type))
                fieldType = fieldType.Replace("*", "");

            if (field.Type.NullableAnnotation == NullableAnnotation.Annotated &&
                !fieldType.EndsWith("?"))
            {
                fieldType += "?";
            }

            var nonNullableFieldType = GetNonNullableNameForGenericParameter(field.Type);
            var nullable = fieldType.EndsWith("?");
            var nullableString = string.Empty;
            if (!field.Type.IsValueType)
            {
                if (!nullable)
                    nullableString = ", true";

                if (nonNullableFieldType.EndsWith("?"))
                    nonNullableFieldType = nonNullableFieldType.Substring(0, nonNullableFieldType.Length - 1);
            }

            if (field.Attribute.ServerOnly)
            {
                builder.AppendLine("""
                    if (serialization.IsServer)
                    {
                    """);
            }

            if (!field.Attribute.IsDataFieldAttribute || !field.Attribute.Required)
            {
                builder.AppendLine($$"""
                    if (alwaysWrite || !EqualityComparer<{{fieldType}}>.Default.Equals(obj.{{field.Symbol.Name}}, ({{fieldType}}) defaultValues["{{field.Attribute.Tag}}"]!))
                    {
                    """);
            }

            builder.AppendLine($"""DataNode node{i};""");

            if (field.Attribute.IsDataFieldAttribute)
            {
                builder.AppendLine($$"""
                    if (!mapping.Has("{{field.Attribute.Tag}}"))
                    {
                    """);
            }

            if (field.CustomSerializer is { } serializer &&
                (serializer.Type & Writer) != 0)
            {
                var nullableValueType = field.Type.IsValueType && nullable;
                if (nullableValueType)
                {
                    builder.AppendLine($$"""
                        if (obj.{{field.Symbol.Name}} == null)
                        {
                            node{{i}} = ValueDataNode.Null();
                        }
                        else
                        {
                        """);
                }

                var nullableValueTypeString = nullableValueType ? ".Value" : string.Empty;
                var writerName = serializer.Serializer.ToDisplayString();
                builder.AppendLine($"""
                    #pragma warning disable RA0008 notNullableOverride
                    node{i} = serialization.WriteValue<{nonNullableFieldType}, {writerName}>(obj.{field.Symbol.Name}{nullableValueTypeString}!, alwaysWrite, context{nullableString});
                    #pragma warning enable RA0008
                    """);

                if (nullableValueType)
                    builder.Append("}");
            }
            else
            {
                builder.AppendLine(
                    $"node{i} = serialization.WriteValue<{fieldType}>(obj.{field.Symbol.Name}, alwaysWrite, context{nullableString});");
            }

            if (field.Attribute.IsDataFieldAttribute)
            {
                builder.AppendLine($$"""
                        mapping.Add("{{field.Attribute.Tag}}", node{{i}});
                    }
                    """);
            }
            else
            {
                builder.AppendLine($$"""
                        if (node{{i}} is MappingDataNode mapping{{i}})
                        {
                            mapping.Insert(mapping{{i}}, true);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Writing field {{field.Symbol.Name}} for type {typeof({{definition.GenericTypeName}})} did not return a {nameof(MappingDataNode)} but was annotated to be included.");
                        }
                    """);
            }

            if (!field.Attribute.IsDataFieldAttribute || !field.Attribute.Required)
                builder.AppendLine("}");

            if (field.Attribute.ServerOnly)
                builder.AppendLine("}");

            if (GetFirstDataDefinitionBaseType(definition.Type) is { } baseType)
            {
                var baseTypeName = baseType.ToDisplayString();
                builder.AppendLine(
                    $"{baseTypeName}.Write(obj, mapping, serialization, context, alwaysWrite, defaultValues);");
            }
        }

        return $$"""
            public static void Write(
                {{definition.GenericTypeName}} obj,
                MappingDataNode mapping,
                ISerializationManager serialization,
                ISerializationContext? context,
                bool alwaysWrite,
                ImmutableDictionary<string, object?> defaultValues)
            {
                {{builder}}
            }
            """;
    }

    private static string GetFieldDefinitions(DataDefinition definition)
    {
        var builder = new StringBuilder();
        var nullConditional = definition.Type.IsValueType ? string.Empty : "?";
        var fieldTags = new List<string>(definition.Fields.Count);
        foreach (var field in definition.Fields)
        {
            if (!definition.Type.Equals(field.Symbol.ContainingType, SymbolEqualityComparer.Default))
                continue;

            var (fieldType, _) = GetCleanNameForGenericType(field.Type, out var isNullableValueType);
            var nullable = field.Type.NullableAnnotation == NullableAnnotation.Annotated ||
                           field.Type.ToDisplayString().EndsWith("?");

            if (!isNullableValueType && fieldType.EndsWith("?"))
                fieldType = fieldType.Substring(0, fieldType.Length - 1);

            builder.AppendLine($$"""
                if (fieldsParsed == null || !fieldsParsed.Contains("{{field.Attribute.Tag}}"))
                {
                    fields.Add(new DataFieldDefinition(
                        "{{field.Attribute.Tag}}",
                        {{field.Attribute.Priority}},
                        {{field.Attribute.IsDataFieldAttribute.ToString().ToLowerInvariant()}},
                        {{field.Attribute.Include.ToString().ToLowerInvariant()}},
                        instance{{nullConditional}}.{{field.Symbol.Name}},
                        (InheritanceBehavior) {{field.Attribute.InheritanceBehavior}},
                        "{{field.Symbol.Name}}",
                        typeof({{fieldType}}),
                        {{nullable.ToString().ToLowerInvariant()}},
                        "{{field.Attribute.CamelCasedName}}",
                        {{(field.CustomSerializer == null ? "null" : $"typeof({field.CustomSerializer.Value.Serializer.ToDisplayString()})")}}
                    ));
                }
                """);

            fieldTags.Add($"\"{field.Attribute.Tag}\"");
        }

        if (GetFirstDataDefinitionBaseType(definition.Type) is { } baseType)
            builder.AppendLine(
                $"{baseType.ToDisplayString()}.GetFieldDefinitions(instance, fields, [{string.Join(", ", fieldTags)}]);");

        var instance = definition.Type.IsAbstract
            ? string.Empty
            : $"instance = {definition.GenericTypeName}.StaticInstantiate();";

        return $$"""
            public static void GetFieldDefinitions({{definition.GenericTypeName}}{{nullConditional}} instance, List<DataFieldDefinition> fields, string[]? fieldsParsed = null)
            {
                {{instance}}
                {{builder}}
            }
            """;
    }

    // TODO serveronly? do we care? who knows!!
    private static StringBuilder GetCopyBody(DataDefinition definition)
    {
        var builder = new StringBuilder();
        foreach (var field in definition.Fields)
        {
            if (!definition.Type.Equals(field.Symbol.ContainingType, SymbolEqualityComparer.Default))
                continue;

            var type = field.Type;
            var (typeName, nonNullableTypeName) = GetCleanNameForGenericType(type, out var isNullableValueType);

            var isClass = type.IsReferenceType || type.SpecialType == SpecialType.System_String;
            var isNullable = type.NullableAnnotation == NullableAnnotation.Annotated ||
                             field.Type.ToDisplayString().EndsWith("?");
            var nullableOverride = isClass && !isNullable ? ", true" : string.Empty;
            var name = field.Symbol.Name;
            var nullableValue = isNullableValueType ? ".Value" : string.Empty;
            var nullNotAllowed = isClass && !isNullable;

            if (field.CustomSerializer is { Serializer: var serializer, Type: var serializerType } &&
                ((serializerType & Copier) != 0 || (serializerType & CopyCreator) != 0))
            {
                if (nullNotAllowed)
                {
                    builder.AppendLine($$"""
                        if (source.{{name}} == null)
                        {
                            throw new NullNotAllowedException();
                        }
                        """);
                }

                if (isNullable || isNullableValueType)
                {
                    builder.AppendLine($$"""
                        if (source.{{name}} == null)
                        {
                            {{name}} = null!;
                        }
                        else
                        {
                        """);
                }

                var serializerName = serializer.ToDisplayString();

                // TODO ROBUST should these both be created if both are present?
                if ((serializerType & Copier) != 0)
                {
                    builder.AppendLine($"""
                        #pragma warning disable RA0008 notNullableOverride
                        {nonNullableTypeName} {name}GeneratedTemp = default!;
                        serialization.CopyTo<{nonNullableTypeName}, {serializerName}>(source.{name}{nullableValue}, ref {name}GeneratedTemp, hookCtx, context{nullableOverride});
                        #pragma warning enable RA0008
                        {name} = {name}GeneratedTemp;
                        """);
                }
                else if ((serializerType & CopyCreator) != 0)
                {
                    builder.AppendLine(
                        $"{name} = serialization.CreateCopy<{nonNullableTypeName}, {serializerName}>(source.{name}{nullableValue}, hookCtx, context{nullableOverride});");
                }

                if (isNullable || isNullableValueType)
                    builder.AppendLine("}");
            }
            else
            {
                if (nullNotAllowed)
                {
                    builder.AppendLine($$"""
                        if (source.{{name}} == null)
                        {
                            throw new NullNotAllowedException();
                        }
                        """);
                }

                var hasHooks = ImplementsInterface(type, SerializationHooksNamespace) || !type.IsSealed;
                builder.AppendLine($$"""
                    {{typeName}} {{name}}GeneratedTemp = default!;
                    if (serialization.TryCustomCopy(source.{{name}}, ref {{name}}GeneratedTemp, hookCtx, {{hasHooks.ToString().ToLower()}}, context))
                    {
                        {{name}} = {{name}}GeneratedTemp;
                    }
                    else
                    {
                    """);

                if (CanBeCopiedByValue(field.Symbol, field.Type))
                {
                    builder.AppendLine($"{name} = source.{name};");
                }
                else if (IsDataDefinition(type, out _) && !type.IsAbstract &&
                         type is not INamedTypeSymbol { TypeKind: TypeKind.Interface })
                {
                    var nullable = !type.IsValueType || IsNullableType(type);

                    if (nullable)
                    {
                        builder.AppendLine($$"""
                            if (source.{{name}} == null)
                            {
                                {{name}} = null!;
                            }
                            else
                            {
                            """);
                    }

                    builder.AppendLine($"""
                        serialization.CopyTo(source.{name}, ref {name}GeneratedTemp, hookCtx, context{nullableOverride});
                        {name} = {name}GeneratedTemp;
                        """);

                    if (nullable)
                        builder.AppendLine("}");
                }
                else
                {
                    builder.AppendLine($"{name} = serialization.CreateCopy(source.{name}, hookCtx, context);");
                }

                builder.AppendLine("}");
            }
        }

        return builder;
    }
}
