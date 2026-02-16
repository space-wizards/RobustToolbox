using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Robust.Serialization.Generator.CustomSerializerType;
using static Robust.Serialization.Generator.Types;

namespace Robust.Serialization.Generator;

[Generator]
public class Generator : IIncrementalGenerator
{
    private const string TypeCopierInterfaceNamespace = "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeCopier";
    private const string TypeCopyCreatorInterfaceNamespace = "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeCopyCreator";
    private const string TypeValidatorInterfaceNamespace = "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeValidator";
    private const string TypeReaderInterfaceNamespace = "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeReader";
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
                    if (!IsDataDefinition(symbol))
                        return null;

                    return GenerateForDataDefinition(type, symbol);
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
        ITypeSymbol type)
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

        var definition = GetDataDefinition(type);
        if (nonPartial || definition.InvalidFields)
            return null;

        builder.AppendLine($$"""
            #nullable enable
            using System;
            using System.Collections.Generic;
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
                {{GetConstructor(definition)}}

                {{GetCopyMethods(definition)}}

                {{GetInstantiators(definition)}}

                {{GetValidators(definition)}}

                {{GetReaders(definition)}}
            }

            {{containingTypesEnd}}
            """);

        return ($"{symbolName}.g.cs", builder.ToString());
    }

    private static DataDefinition GetDataDefinition(ITypeSymbol definition)
    {
        var fields = new List<DataField>();
        var symbols = new List<INamedTypeSymbol>();
        var invalidFields = false;

        foreach (var member in definition.GetMembers())
        {
            if (member is not IFieldSymbol && member is not IPropertySymbol)
                continue;

            if (member.IsStatic)
                continue;

            if (IsDataField(member, out var type, out var attribute))
            {
                if (attribute.Data.ConstructorArguments.FirstOrDefault(arg => arg.Kind == TypedConstantKind.Type).Value is INamedTypeSymbol customSerializer)
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

                    if (serializerType != None)
                    {
                        fields.Add(new DataField(member, type, attribute, (customSerializer, serializerType)));
                        continue;
                    }
                }

                fields.Add(new DataField(member, type, attribute, null));

                if (IsReadOnlyMember(definition, type))
                    invalidFields = true;
            }
        }

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

     private static string GetConstructor(DataDefinition definition)
     {
         if (definition.Type.TypeKind == TypeKind.Interface)
             return string.Empty;

         var builder = new StringBuilder();

         if (NeedsEmptyConstructor(definition.Type))
         {
             builder.AppendLine($$"""
                                  // Implicit constructor
                                  #pragma warning disable CS8618
                                  public {{definition.Type.Name}}()
                                  #pragma warning restore CS8618
                                  {
                                  }
                                  """);
         }

         return builder.ToString();
     }

    private static string GetCopyMethods(DataDefinition definition)
    {
        var builder = new StringBuilder();

        var modifiers = IsVirtualClass(definition.Type) ? "virtual " : string.Empty;
        var baseCall = string.Empty;
        string baseCopy;
        var baseType = definition.Type.BaseType;

        if (baseType != null && IsDataDefinition(definition.Type.BaseType))
        {
            var baseName = baseType.ToDisplayString();
            baseCall = $"""
                        var definitionCast = ({baseName}) target;
                        base.InternalCopy(ref definitionCast, serialization, hookCtx, context);
                        target = ({definition.GenericTypeName}) definitionCast;
                        """;

             baseCopy = $$"""
                          /// <seealso cref="ISerializationManager.CopyTo"/>
                          [Obsolete("Use ISerializationManager.CopyTo instead")]
                          public override void Copy(ref {{baseName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                          {
                              var cast = ({{definition.GenericTypeName}}) target;
                              Copy(ref cast, serialization, hookCtx, context);
                              target = cast!;
                          }

                          /// <seealso cref="ISerializationManager.CopyTo"/>
                          [Obsolete("Use ISerializationManager.CopyTo instead")]
                          public override void Copy(ref object target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                          {
                              var cast = ({{definition.GenericTypeName}}) target;
                              Copy(ref cast, serialization, hookCtx, context);
                              target = cast!;
                          }
                          """;
        }
        else
        {
            baseCopy = $$"""
                         /// <seealso cref="ISerializationManager.CopyTo"/>
                         [Obsolete("Use ISerializationManager.CopyTo instead")]
                         public {{modifiers}} void Copy(ref object target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                         {
                             var cast = ({{definition.GenericTypeName}}) target;
                             Copy(ref cast, serialization, hookCtx, context);
                             target = cast!;
                         }
                         """;
        }

        builder.AppendLine($$"""
                             /// <seealso cref="ISerializationManager.CopyTo"/>
                             [Obsolete("Use ISerializationManager.CopyTo instead")]
                             public {{modifiers}} void InternalCopy(ref {{definition.GenericTypeName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                             {
                                {{baseCall}}
                                {{CopyDataFields(definition)}}
                             }

                             /// <seealso cref="ISerializationManager.CopyTo"/>
                             [Obsolete("Use ISerializationManager.CopyTo instead")]
                             public {{modifiers}} void Copy(ref {{definition.GenericTypeName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                             {
                                 InternalCopy(ref target, serialization, hookCtx, context);
                             }

                             {{baseCopy}}
                             """);

        foreach (var interfaceName in InternalGetImplicitDataDefinitionInterfaces(definition.Type, true))
        {
            var interfaceModifiers = baseType != null &&
                                     baseType.AllInterfaces.Any(i => i.ToDisplayString() == interfaceName)
                ? "override "
                : modifiers;

            builder.AppendLine($$"""
                /// <seealso cref="ISerializationManager.CopyTo"/>
                [Obsolete("Use ISerializationManager.CopyTo instead")]
                public {{interfaceModifiers}} void InternalCopy(ref {{interfaceName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                {
                    var def = ({{definition.GenericTypeName}}) target;
                    Copy(ref def, serialization, hookCtx, context);
                    target = def;
                }

                /// <seealso cref="ISerializationManager.CopyTo"/>
                [Obsolete("Use ISerializationManager.CopyTo instead")]
                public {{interfaceModifiers}} void Copy(ref {{interfaceName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                {
                    InternalCopy(ref target, serialization, hookCtx, context);
                }
                """);
        }

        return builder.ToString();
    }

    private static string GetInstantiators(DataDefinition definition)
    {
        var builder = new StringBuilder();
        var modifiers = string.Empty;

        if (definition.Type.BaseType is { } baseType && IsDataDefinition(baseType))
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
            builder.AppendLine($$"""
                                 /// <seealso cref="ISerializationManager.CreateCopy"/>
                                 [Obsolete("Use ISerializationManager.CreateCopy instead")]
                                 public {{modifiers}} {{definition.GenericTypeName}} Instantiate()
                                 {
                                     return new {{definition.GenericTypeName}}();
                                 }
                                 """);
        }

        foreach (var interfaceName in InternalGetImplicitDataDefinitionInterfaces(definition.Type, false))
        {
            builder.AppendLine($$"""
                {{interfaceName}} {{interfaceName}}.Instantiate()
                {
                    return Instantiate();
                }

                {{interfaceName}} ISerializationGenerated<{{interfaceName}}>.Instantiate()
                {
                    return Instantiate();
                }
                """);
        }

        return builder.ToString();
    }

    private static string GetValidators(DataDefinition definition)
    {
        var builder = new StringBuilder();
        var validateBuilder = new StringBuilder();

        for (var i = 0; i < definition.Fields.Count; i++)
        {
            validateBuilder.Clear();

            var field = definition.Fields[i];
            var fieldTypeName = GetNonNullableNameForGenericParameter(field.Type);
            var tagName = field.Attribute.Name;
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

        var baseType = definition.Type.BaseType;
        if (IsDataDefinition(baseType))
            builder.AppendLine($"{baseType.ToDisplayString()}.Validate(nodes, node, serialization, context);");

        return $$"""
            public static void Validate(Dictionary<string, ValidationNode> nodes, MappingDataNode node, ISerializationManager serialization, ISerializationContext? context = null)
            {
                {{builder}}
            }

            public static ValidateAllFieldsDelegate RobustValidateDelegate()
            {
                return (nodes, node, serialization, context) => Validate(nodes, node, serialization, context);
            }
            """;
    }

    public static string GetReaders(DataDefinition definition)
    {
        var builder = new StringBuilder();
        var structCopier = new StringBuilder();
        for (var i = 0; i < definition.Fields.Count; i++)
        {
            var field = definition.Fields[i];
            var tempTypeName = field.Type.ToDisplayString();
            if (IsMultidimensionalArray(field.Type))
                tempTypeName = tempTypeName.Replace("*", "");

            if (field.Attribute.ServerOnly)
            {
                builder.AppendLine("""
                    if (serialization.IsServer)
                    {
                    """);
            }

            var fieldName = field.Symbol.Name;
            builder.AppendLine($"""
            {tempTypeName} {fieldName}Temp = target.{fieldName};
            """);
            if (field.Attribute.IsDataFieldAttribute)
            {
                builder.AppendLine($$"""
                    if (mappingDataNode.TryGet("{{field.Attribute.Name}}", out var node{{i}}))
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

            var (fieldTypeName, nonNullableFieldTypeName) = GetCleanNameForGenericType(field.Type, out var isNullableValueType);
            var tagName = field.Attribute.Name;
            var reader = field.CustomSerializer;
            var validatorName = reader?.Serializer.ToDisplayString();
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
                    ? $"{fieldName}Temp = EntityUid.Invalid;"
                    : nullable
                        ? $"{fieldName}Temp = null;"
                        : "throw new NullNotAllowedException();";

            builder.AppendLine($$"""
                if (node{{i}}.IsNull)
                {
                    {{nullExpression}}
                }
                else
                {
                """);

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
                            {fieldName}Temp = target.{field.Symbol.Name} = serialization.Read<{nonNullableFieldTypeName}, MappingDataNode, {validatorName}>(mapping, hookCtx, context, null{nullableString});
                            break;
                        """);
                }

                if ((reader.Value.Type & SequenceReader) != 0)
                {
                    builder.AppendLine($"""
                        case SequenceDataNode sequence:
                            {fieldName}Temp = serialization.Read<{nonNullableFieldTypeName}, SequenceDataNode, {validatorName}>(sequence, hookCtx, context, null{nullableString});
                            break;
                        """);
                }

                if ((reader.Value.Type & ValueReader) != 0)
                {
                    builder.AppendLine($"""
                        case ValueDataNode value:
                            {fieldName}Temp = serialization.Read<{nonNullableFieldTypeName}, ValueDataNode, {validatorName}>(value, hookCtx, context, null{nullableString});
                            break;
                        """);
                }

                builder.AppendLine($$"""
                    default:
                        throw new InvalidOperationException($"Unable to read node for {{field.Symbol.Name}}({{field.Attribute.Data.AttributeClass?.Name}}) as valid.");
                        break;
                    }
                    """);
            }
            else
            {
                builder.AppendLine($"{fieldName}Temp = serialization.Read<{fieldTypeName}>(node{i}, hookCtx, context, null{nullableString});");
            }

            builder.AppendLine("}");
            if (definition.Type.IsValueType)
                structCopier.AppendLine($"{fieldName} = {fieldName}Temp!,");
            else
                builder.AppendLine($"target.{fieldName} = {fieldName}Temp!;");

            builder.AppendLine("}");
            if (field.Attribute.IsDataFieldAttribute)
            {
                if (field.Attribute.Required)
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
            }

            if (field.Attribute.ServerOnly)
                builder.AppendLine("}");
        }

        var baseType = definition.Type.BaseType;
        if (IsDataDefinition(baseType))
        {
            var baseTypeName = baseType.ToDisplayString();
            builder.AppendLine($$"""
                {{baseTypeName}} baseInstance = target;
                {{baseTypeName}}.Read(ref baseInstance, mappingDataNode, serialization, hookCtx, context);
                """);
        }

        if (definition.Type.IsValueType)
        {
            builder.AppendLine($$"""
                target = target with
                {
                    {{structCopier}}
                };
                """);
        }

        return $$"""
            public static void Read(
                ref {{definition.GenericTypeName}} target,
                MappingDataNode mappingDataNode,
                ISerializationManager serialization,
                SerializationHookContext hookCtx,
                ISerializationContext? context)
            {
                {{builder}}
            }

            public static PopulateDelegateSignature<{{definition.GenericTypeName}}> RobustReadDelegate()
            {
                return (ref target, node, serialization, hookCtx, context) =>
                    Read(ref target, node, serialization, hookCtx, context);
            }
            """;
    }

    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    private static IEnumerable<string> InternalGetImplicitDataDefinitionInterfaces(
        ITypeSymbol type,
        bool all)
    {
        var symbols = GetImplicitDataDefinitionInterfaces(type, all);

        // TODO SOURCE GEN
        // fix this jank
        // The comp-state source generator will add an "IComponentDelta" interface to classes with the auto state
        // attribute, and this generator creates methods that those classes then have to implement because
        // IComponentDelta a DataDefinition via the ImplicitDataDefinitionForInheritorsAttribute on IComponent.
        if (!TryGetAttribute(type, AutoStateAttributeName, out var data))
            return symbols;

        // If it doesn't have field deltas then ignore
        if (data.ConstructorArguments[1] is not { Value: bool fields and true })
        {
            return symbols;
        }

        if (symbols.Any(x => x == ComponentDeltaInterfaceName))
            return symbols;

        return symbols.Append(ComponentDeltaInterfaceName);
    }

    // TODO serveronly? do we care? who knows!!
    private static StringBuilder CopyDataFields(DataDefinition definition)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"""
if (serialization.TryCustomCopy(this, ref target, hookCtx, {definition.HasHooks.ToString().ToLower()}, context))
    return;
""");

        var structCopier = new StringBuilder();
        foreach (var field in definition.Fields)
        {
            var type = field.Type;
            var (typeName, nonNullableTypeName) = GetCleanNameForGenericType(type, out var isNullableValueType);

            var isClass = type.IsReferenceType || type.SpecialType == SpecialType.System_String;
            var isNullable = type.NullableAnnotation == NullableAnnotation.Annotated;
            var nullableOverride = isClass && !isNullable ? ", true" : string.Empty;
            var name = field.Symbol.Name;
            var tempVarName = $"{name}Temp";
            var nullableValue = isNullableValueType ? ".Value" : string.Empty;
            var nullNotAllowed = isClass && !isNullable;

            if (field.CustomSerializer is { Serializer: var serializer, Type: var serializerType } &&
                ((serializerType & Copier) != 0 || (serializerType & CopyCreator) != 0))
            {
                if (nullNotAllowed)
                {
                    builder.AppendLine($$"""
                                         if ({{name}} == null)
                                         {
                                             throw new NullNotAllowedException();
                                         }
                                         """);
                }

                builder.AppendLine($$"""
                                     {{typeName}} {{tempVarName}} = default!;
                                     """);

                if (isNullable || isNullableValueType)
                {
                    builder.AppendLine($$"""
                                         if ({{name}} == null)
                                         {
                                             {{tempVarName}} = null!;
                                         }
                                         else
                                         {
                                         """);
                }

                var serializerName = serializer.ToDisplayString();

                // TODO ROBUST should these both be created if both are present?
                if ((serializerType & Copier) != 0)
                {
                    CopyToCustom(
                        builder,
                        nonNullableTypeName,
                        serializerName,
                        tempVarName,
                        name,
                        isNullable,
                        isClass,
                        isNullableValueType
                    );
                }
                else if ((serializerType & CopyCreator) != 0)
                {
                    CreateCopyCustom(
                        builder,
                        name,
                        tempVarName,
                        nonNullableTypeName,
                        serializerName,
                        nullableValue,
                        nullableOverride
                    );
                }

                if (isNullable || isNullableValueType)
                {
                    builder.AppendLine("}");
                }

                if (definition.Type.IsValueType)
                {
                    structCopier.AppendLine($"{name} = {tempVarName}!,");
                }
                else
                {
                    builder.AppendLine($"target.{name} = {tempVarName}!;");
                }
            }
            else
            {
                builder.AppendLine($$"""
                                     {{typeName}} {{tempVarName}} = default!;
                                     """);

                if (nullNotAllowed)
                {
                    builder.AppendLine($$"""
                                         if ({{name}} == null)
                                         {
                                             throw new NullNotAllowedException();
                                         }
                                         """);
                }

                var hasHooks = ImplementsInterface(type, SerializationHooksNamespace) || !type.IsSealed;
                builder.AppendLine($$"""
                                     if (!serialization.TryCustomCopy(this.{{name}}, ref {{tempVarName}}, hookCtx, {{hasHooks.ToString().ToLower()}}, context))
                                     {
                                     """);

                if (CanBeCopiedByValue(field.Symbol, field.Type))
                {
                    builder.AppendLine($"{tempVarName} = {name};");
                }
                else if (IsDataDefinition(type) && !type.IsAbstract &&
                         type is not INamedTypeSymbol { TypeKind: TypeKind.Interface })
                {
                    var nullable = !type.IsValueType || IsNullableType(type);

                    if (nullable)
                    {
                        builder.AppendLine($$"""
                                           if ({{name}} == null)
                                           {
                                               {{tempVarName}} = null!;
                                           }
                                           else
                                           {
                                           """);
                    }

                    builder.AppendLine($$"""
                                         serialization.CopyTo({{name}}, ref {{tempVarName}}, hookCtx, context{{nullableOverride}});
                                         """);

                    if (nullable)
                    {
                        builder.AppendLine("}");
                    }
                }
                else
                {
                    builder.AppendLine($"{tempVarName} = serialization.CreateCopy({name}, hookCtx, context);");
                }

                builder.AppendLine("}");

                if (definition.Type.IsValueType)
                {
                    structCopier.AppendLine($"{name} = {tempVarName}!,");
                }
                else
                {
                    builder.AppendLine($"target.{name} = {tempVarName}!;");
                }
            }
        }

        if (definition.Type.IsValueType)
        {
            builder.AppendLine($$"""
                                target = target with
                                {
                                    {{structCopier}}
                                };
                                """);
        }

        return builder;
    }

    private static void CopyToCustom(
        StringBuilder builder,
        string typeName,
        string serializerName,
        string tempVarName,
        string varName,
        bool isNullable,
        bool isClass,
        bool isNullableValueType)
    {
        var newTemp = isNullable && isClass ? $"{tempVarName} ??= new();" : string.Empty;
        var nullableOverride = isClass ? ", true" : string.Empty;
        var nullableValue = isNullableValueType ? ".Value" : string.Empty;
        var nonNullableTypeName = typeName.EndsWith("?") ? typeName.Substring(0, typeName.Length - 1) : typeName;

        builder.AppendLine($$"""
                             {{nonNullableTypeName}} {{tempVarName}}CopyTo = default!;
                             {{newTemp}}
                             serialization.CopyTo<{{typeName}}, {{serializerName}}>(this.{{varName}}{{nullableValue}}, ref {{tempVarName}}CopyTo, hookCtx, context{{nullableOverride}});
                             {{tempVarName}} = {{tempVarName}}CopyTo;
                             """);
    }

    private static void CreateCopyCustom(
        StringBuilder builder,
        string varName,
        string tempVarName,
        string nonNullableTypeName,
        string serializerName,
        string nullableValue,
        string nullableOverride)
    {
        builder.AppendLine($$"""
                             {{tempVarName}} = serialization.CreateCopy<{{nonNullableTypeName}}, {{serializerName}}>(this.{{varName}}{{nullableValue}}, hookCtx, context{{nullableOverride}});
                             """);
    }
}
