using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.SymbolDisplayFormat;
using static Microsoft.CodeAnalysis.SymbolEqualityComparer;

namespace Robust.Serialization.Generator
{
    [Generator]
    public partial class DataDefinitionGenerator : ISourceGenerator
    {
        private const string DataDefinitionName =
            "Robust.Shared.Serialization.Manager.Attributes.DataDefinitionAttribute";

        private const string ImplicitDataDefinitionForInheritorsName =
            "Robust.Shared.Serialization.Manager.Attributes.ImplicitDataDefinitionForInheritorsAttribute";

        private const string DataFieldName =
            "Robust.Shared.Serialization.Manager.Attributes.DataFieldAttribute";

        private const string IncludeDataFieldName =
            "Robust.Shared.Serialization.Manager.Attributes.IncludeDataFieldAttribute";

        private const string CopyByRefName = "Robust.Shared.Serialization.Manager.Attributes.CopyByRefAttribute";
        private const string SelfSerializeName = "Robust.Shared.Serialization.ISelfSerialize";

        private const string ReaderName = "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeReader";
        private const string CopierName = "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeCopier";

        private const string CopyCreatorName =
            "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeCopyCreator";

        private const string ValueNodeName = "Robust.Shared.Serialization.Markdown.Value.ValueDataNode";
        private const string SequenceNodeName = "Robust.Shared.Serialization.Markdown.Sequence.SequenceDataNode";
        private const string MappingNodeName = "Robust.Shared.Serialization.Markdown.Mapping.MappingDataNode";

        private INamedTypeSymbol _dataDefinitionSymbol;
        private INamedTypeSymbol _implicitDataDefinitionSymbol;
        private INamedTypeSymbol _dataFieldSymbol;
        private INamedTypeSymbol _includeDataFieldSymbol;
        private INamedTypeSymbol _copyByRefSymbol;
        private INamedTypeSymbol _selfSerializeSymbol;

        private INamedTypeSymbol _readerSymbol;
        private INamedTypeSymbol _copierSymbol;
        private INamedTypeSymbol _copyCreatorSymbol;

        private INamedTypeSymbol _valueNodeSymbol;
        private INamedTypeSymbol _sequenceNodeSymbol;
        private INamedTypeSymbol _mappingNodeSymbol;

        private readonly SymbolDisplayFormat _displayFormat = FullyQualifiedFormat
            .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new NameReferenceSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var comp = (CSharpCompilation)context.Compilation;
            if (!(context.SyntaxReceiver is NameReferenceSyntaxReceiver receiver))
                return;

            _dataDefinitionSymbol = comp.GetTypeByMetadataName(DataDefinitionName);
            _implicitDataDefinitionSymbol = comp.GetTypeByMetadataName(ImplicitDataDefinitionForInheritorsName);
            _dataFieldSymbol = comp.GetTypeByMetadataName(DataFieldName);
            _includeDataFieldSymbol = comp.GetTypeByMetadataName(IncludeDataFieldName);
            _copyByRefSymbol = comp.GetTypeByMetadataName(CopyByRefName);

            _selfSerializeSymbol = comp.GetTypeByMetadataName(SelfSerializeName);
            _readerSymbol = comp.GetTypeByMetadataName(ReaderName);
            _copierSymbol = comp.GetTypeByMetadataName(CopierName);
            _copyCreatorSymbol = comp.GetTypeByMetadataName(CopyCreatorName);

            _valueNodeSymbol = comp.GetTypeByMetadataName(ValueNodeName);
            _sequenceNodeSymbol = comp.GetTypeByMetadataName(SequenceNodeName);
            _mappingNodeSymbol = comp.GetTypeByMetadataName(MappingNodeName);

            var results = FindDataDefinitions(context, comp, receiver);

            if (results == null)
                return;

            var types = receiver.Types
                .Select(type => comp.GetSemanticModel(type.SyntaxTree).GetDeclaredSymbol(type))
                .ToImmutableHashSet(Default);

            foreach (var symbol in types)
            {
                var type = (INamedTypeSymbol) symbol;
                if (results.DataDefinitions.Contains(type, Default))
                {
                    GenerateDefinition(type, context);
                }
                else
                {
                    var baseType = type?.BaseType;
                    while (baseType != null)
                    {
                        if (GetAttribute(baseType, _implicitDataDefinitionSymbol) != null)
                        {
                            GenerateDefinition(type, context);
                            break;
                        }

                        baseType = baseType.BaseType;
                    }
                }
            }
        }

        private void GenerateDefinition(INamedTypeSymbol definition, GeneratorExecutionContext context)
        {
            if (definition.IsAbstract || definition.TypeKind == TypeKind.Interface)
                return;

            var namespaces = new HashSet<INamespaceSymbol>(Default);

            try
            {
                // TODO nullability
                // TODO check for parameterless constructor
                // TODO init structs zeroed

                var hasEmptyConstructor = definition.InstanceConstructors.Any(c => c.Parameters.IsEmpty);
                var generateEmptyConstructor = !definition.InstanceConstructors.Any(c => !c.IsImplicitlyDeclared);

                var readConstructor = new StringBuilder(
                    $@"    private {definition.Name}(MappingDataNode mapping, IDependencyCollection dependencies, SerializationHookContext hooks, ISerializationContext? context = null)
    {{
        var serialization = dependencies.Resolve<ISerializationManager>();
");
                var copyConstructor = new StringBuilder(
                    $@"    private {definition.Name}({definition.Name} instance, IDependencyCollection dependencies, SerializationHookContext hooks, ISerializationContext? context = null){(hasEmptyConstructor || generateEmptyConstructor ? " : this()" : "")}
    {{
        var serialization = dependencies.Resolve<ISerializationManager>();
");
                var reader = new StringBuilder(@"        // TODO check is server
        var isServer = dependencies.Resolve<INetManager>().IsServer;");
                var writer = new StringBuilder("        var mapping = new MappingDataNode();\n");
                foreach (var field in FindDefinitionFields(definition))
                {
                    // Constructors
                    readConstructor.AppendLine(GetReadConstructorAssigner(namespaces, definition, field));
                    copyConstructor.AppendLine(GetCopyConstructorAssigner(namespaces, field));

                    // Read
                    // TODO serializers
                    // TODO others
                    // TODO IsServer for all

                    // Write
                    // TODO serializers
                    // TODO same/different type
                    // TODO IsServer for all
                    var fieldType = GetFieldType(field);
                    var dataDef = IsDataDefinition(fieldType);
                    var dataFieldId = GetDataFieldId(field);

                    if (fieldType.TypeKind == TypeKind.Enum)
                    {
                        writer.AppendLine(
                            $@"        mapping.Add(""{dataFieldId}"", WriteEnum<{fieldType.Name}>({field.Name});");
                    }
                    else if (fieldType.TypeKind == TypeKind.Array)
                    {
                        writer.AppendLine($@"        var {field.Name}Node = new SequenceDataNode();
        foreach (var val in this.{field.Name})
        {{
            var serializedVal = val{(dataDef ? ".Write()" : "")};
        }}

        mapping.Add(""{dataFieldId}"", {field.Name}Node);");
                    }
                    else if (IsSelfSerialize(fieldType))
                    {
                        reader.AppendLine($@"        var {field.Name}Node = mapping[""{dataFieldId}""];
        if ({field.Name}Node is not ValueDataNode {field.Name}ValueNode)
        {{
            throw new InvalidNodeTypeException(""Cannot read {{nameof(ISelfSerialize)}} from node type {{node.GetType()}}. Expected {{nameof(ValueDataNode)}}""); 
        }}
    
        this.{field.Name} = new {field.ToDisplayString(_displayFormat)}();
        this.{field.Name}.Deserialize({field.Name}ValueNode.Value);
        ");
                        writer.AppendLine(
                            $@"        var {field.Name}Node = new ValueDataNode({field.Name}.Serialize());
        mapping.Add(""{dataFieldId}"", {field.Name}Node);");
                    }
                    else if (IsDataDefinition(fieldType))
                    {
                        reader.AppendLine($@"        var {field.Name}Node = mapping[""{dataFieldId}""];
        // TODO check nullability of data field
        this.{field.Name} = {field.Name}Node.Read()");
                        writer.AppendLine($@"        mapping.Add(""{dataFieldId}"", {field.Name}.Write());");

                        // TODO type tag
                    }
                    else
                    {
                        // TODO
                        writer.AppendLine($@"        // TODO write non data definition with serializers");
                    }
                }

                writer.Append($"\n        return mapping;");

                // TODO fix indents for nested data definitions

                readConstructor.AppendLine($"    }}");
                copyConstructor.AppendLine($"    }}");

                var optionalEmptyConstructor = string.Empty;

                if (generateEmptyConstructor)
                {
                    if (definition.IsReferenceType)
                    {
                        optionalEmptyConstructor = $@"
    public {definition.Name}() {{ }}
";
                    }
                    else
                    {
                        optionalEmptyConstructor = $@"
#pragma warning disable CS8618
    public {definition.Name}() {{ }}
#pragma warning enable CS8618
";
                    }
                }

                string accessibility;
                switch (definition.DeclaredAccessibility)
                {
                    case Accessibility.Private:
                        accessibility = "private ";
                        break;
                    case Accessibility.ProtectedAndInternal:
                        accessibility = "protected internal ";
                        break;
                    case Accessibility.Protected:
                        accessibility = "protected ";
                        break;
                    case Accessibility.Internal:
                        accessibility = "internal ";
                        break;
                    case Accessibility.Public:
                        accessibility = "public ";
                        break;
                    case Accessibility.ProtectedOrInternal:
                    case Accessibility.NotApplicable:
                    default:
                        accessibility = "";
                        break;
                }

                var source = new StringBuilder($@"// <auto-generated/>
#nullable enable
#pragma warning disable CS0612
#pragma warning disable CS0618
{string.Join("\n", namespaces.Select(n => $"using {n};").Concat(new[]
{
    "using System;",
    "using System.Globalization;",
    "using Robust.Shared.IoC;",
    "using Robust.Shared.Network;",
    "using Robust.Shared.Serialization;",
    "using Robust.Shared.Serialization.Manager;",
    "using Robust.Shared.Serialization.Markdown;",
    "using Robust.Shared.Serialization.Markdown.Mapping;",
    "using Robust.Shared.Serialization.Markdown.Sequence;",
    "using Robust.Shared.Serialization.Markdown.Value;",
    "using Robust.Shared.Analyzers;"
}).Distinct().OrderBy(n => n))}

{(definition.ContainingNamespace.IsGlobalNamespace ? "" : $"namespace {definition.ContainingNamespace.ToDisplayString()};")}
");

                var containingTypes = new List<INamedTypeSymbol>();
                var containingType = definition.ContainingType;
                while (containingType != null)
                {
                    containingTypes.Add(containingType);
                    containingType = containingType.ContainingType;
                }

                containingTypes.Reverse();
                foreach (var baseType in containingTypes)
                {
                    source.AppendLine($"partial {(baseType.IsReferenceType ? "class" : "struct")} {baseType.Name}\n{{");
                }

                source.AppendLine($@"
{accessibility}partial{(definition.IsRecord ? " record" : "")} {(definition.IsReferenceType ? "class" : "struct")} {definition.Name} : ISerializationGenerated<{definition.Name}>
{{{optionalEmptyConstructor}
{readConstructor}
{copyConstructor}
    public static {definition.Name} Read(MappingDataNode mapping, IDependencyCollection dependencies, SerializationHookContext hooks, ISerializationContext? context = null)
    {{
        return new {definition.Name}(mapping, dependencies, hooks, context);
    }}

    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public {definition.Name} Copy(IDependencyCollection dependencies, SerializationHookContext hooks, ISerializationContext? context = null)
    {{
        return new {definition.Name}(this, dependencies, hooks, context);
    }}

    object ISerializationGenerated.Copy(IDependencyCollection dependencies, SerializationHookContext hooks, ISerializationContext? context)
    {{
        return Copy(dependencies, hooks, context);
    }}
}}
#pragma warning restore CS0612
#pragma warning restore CS0618
");

                for (var i = 0; i < containingTypes.Count; i++)
                {
                    source.AppendLine("}");
                }

                var typeName = definition.Name;
                if (!definition.ContainingNamespace.IsGlobalNamespace)
                    typeName = $"{definition.ContainingNamespace}.{typeName}";

                context.AddSource($"{typeName}.g.cs", source.ToString());
            }
            catch (Exception e)
            {
                var message = $"Error generating serialization code for type {definition.ToDisplayString()}: {e.Message}";

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "RSN0002",
                            message,
                            message,
                            "Usage",
                            DiagnosticSeverity.Error,
                            true),
                        definition.Locations.First()));
            }
        }

        private AttributeData GetAttribute(ISymbol type, INamedTypeSymbol attribute)
        {
            return type.GetAttributes().FirstOrDefault(data =>
                data.AttributeClass != null &&
                data.AttributeClass.Equals(attribute, Default));
        }

        private AttributeData GetBaseDataFieldAttribute(ISymbol type)
        {
            return GetAttribute(type, _dataFieldSymbol) ?? GetAttribute(type, _includeDataFieldSymbol);
        }

        private string GetDataFieldId(ISymbol field)
        {
            return (string)GetAttribute(field, _dataFieldSymbol)?.ConstructorArguments[0].Value;
        }

        private INamedTypeSymbol GetBaseDataFieldCustomSerializer(ISymbol field)
        {
            var dataField = GetAttribute(field, _dataFieldSymbol);
            if (dataField != null)
            {
                return (INamedTypeSymbol)dataField.ConstructorArguments[5].Value;
            }

            return (INamedTypeSymbol)GetAttribute(field, _includeDataFieldSymbol).ConstructorArguments[3].Value;
        }

        private bool TryGetCustomTypeSerializer(
            ISymbol field,
            out INamedTypeSymbol serializer,
            out bool valueReader,
            out bool sequenceReader,
            out bool mappingReader,
            out bool copier,
            out bool copyCreator)
        {
            serializer = GetBaseDataFieldCustomSerializer(field);

            if (serializer == null)
            {
                valueReader = false;
                sequenceReader = false;
                mappingReader = false;
                copier = false;
                copyCreator = false;
                return false;
            }

            var readers = serializer.AllInterfaces
                .Where(@interface => @interface.Equals(_readerSymbol, Default))
                .ToArray();

            valueReader = readers.Any(reader => reader.TypeParameters[1].Equals(_valueNodeSymbol, Default));
            sequenceReader = readers.Any(reader => reader.TypeParameters[1].Equals(_sequenceNodeSymbol, Default));
            mappingReader = readers.Any(reader => reader.TypeParameters[1].Equals(_mappingNodeSymbol, Default));

            var fieldType = GetFieldType(field);
            var copiers = serializer.AllInterfaces
                .Where(@interface => @interface.Equals(_copierSymbol, Default))
                .Where(@interface => @interface.TypeParameters[0].Equals(fieldType, Default));

            copier = copiers.Any();

            var copyCreators = serializer.AllInterfaces
                .Where(@interface => @interface.Equals(_copyCreatorSymbol, Default))
                .Where(@interface => @interface.TypeParameters[0].Equals(fieldType, Default));
            copyCreator = copyCreators.Any();

            return true;
        }

        private bool ShouldReturnSource(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Char:
                case SpecialType.System_Decimal:
                case SpecialType.System_Double:
                case SpecialType.System_Single:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_String:
                    return true;
            }

            if (type.TypeKind == TypeKind.Enum)
                return true;

            if (GetAttribute(type, _copyByRefSymbol) != null)
                return true;

            return false;
        }

        private ITypeSymbol GetFieldType(ISymbol symbol)
        {
            return (symbol as IFieldSymbol)?.Type ?? (symbol as IPropertySymbol)?.Type;
        }

        private string GetReadConstructorAssigner(HashSet<INamespaceSymbol> namespaces, ITypeSymbol definition,
            ISymbol field)
        {
            var type = GetFieldType(field);

            var customResolved = new HashSet<ITypeSymbol>(Default);
            // TODO move this below custom serializer
            if (type.TypeKind == TypeKind.Array)
            {
                var assigner = new StringBuilder();
                assigner.AppendLine(GetArrayReader(customResolved, namespaces, definition, field));

                return assigner.ToString();
            }

            // TODO
            if (type.IsAbstract) /* || type.IsInterface) */
                return $"// TODO abstract/interface ${field.Name}";

            var reader = new StringBuilder();
            var interfaceName = $"ISerializationGenerated<{definition.Name}>";

            // If IncludeDataField is used instead of DataField, then we need to use MappingDataNode as the
            // type parameter for serializers, instead of what the node type would have usually been
            // after getting it from the mapping
            bool mappingNodeOverride;

            if (GetAttribute(field, _dataFieldSymbol) != null)
            {
                reader.AppendLine($@"        var {field.Name}Node = mapping[""{GetDataFieldId(field)}""];");
                mappingNodeOverride = false;
            }
            else
            {
                // Attribute has to be IncludeDataField
                mappingNodeOverride = true;
            }

            if (TryGetCustomTypeSerializer(
                    field,
                    out var serializer,
                    out var valueReader,
                    out var sequenceReader,
                    out var mappingReader,
                    out _, out _) &&
                (valueReader || sequenceReader || mappingReader))
            {
                // TODO type tags
                if (customResolved.Add(serializer))
                    namespaces.Add(serializer.ContainingNamespace);

                if (mappingNodeOverride)
                {
                    reader.AppendLine($@"
        var {field.Name}Reader = serialization.EnsureCustomReader<{serializer.ToDisplayString(_displayFormat)}, {field.ToDisplayString(_displayFormat)}, MappingDataNode();
        this.{field.Name} = {field.Name}Reader.Read(serialization, mapping, dependencies, hooks, context, null);
");
                }
                else
                {
                    if (valueReader)
                    {
                        reader.AppendLine($@"
        if ({field.Name}Node is ValueDataNode {field.Name}ValueNode)
        {{
            var reader = serialization.EnsureCustomReader<{serializer.ToDisplayString(_displayFormat)}, {field.ToDisplayString(_displayFormat)}, ValueDataNode>();
            this.{field.Name} = reader.Read(serialization, {field.Name}ValueNode, dependencies, hooks, context, null);
        }}");
                    }

                    if (sequenceReader)
                    {
                        reader.AppendLine($@"
        if ({field.Name}Node is SequenceDataNode {field.Name}SequenceNode)
        {{
            var reader = serialization.EnsureCustomReader<{serializer.ToDisplayString(_displayFormat)}, {field.ToDisplayString(_displayFormat)}, SequenceDataNode>();
            this.{field.Name} = reader.Read(serialization, {field.Name}SequenceNode, dependencies, hooks, context, null);
        }}");
                    }

                    if (mappingReader)
                    {
                        reader.AppendLine($@"
        if ({field.Name}Node is MappingDataNode {field.Name}MappingNode)
        {{
            var reader = serialization.EnsureCustomReader<{serializer.ToDisplayString(_displayFormat)}, {field.ToDisplayString(_displayFormat)}, MappingDataNode>();
            this.{field.Name} = reader.Read(serialization, {field.Name}MappingNode, dependencies, hooks, context, null);
        }}");
                    }

                    reader.AppendLine($@"
        else
        {{
            throw new ArgumentException($""Cannot read {type.Name} from data node type {{{field.Name}Node.GetType()}}"");
        }}");
                }
            }
            else if (type.TypeKind == TypeKind.Enum)
            {
                namespaces.Add(type.ContainingNamespace);
                reader.AppendLine(
                    $@"        this.{field.Name} = {interfaceName}.ReadEnum<{type.ToDisplayString(_displayFormat)}>({field.Name}Node);");
            }
            else if (IsSelfSerialize(type))
            {
                namespaces.Add(type.ContainingNamespace);
                reader.AppendLine(
                    $@"        this.{field.Name} = {interfaceName}.ReadSelfSerialize<{type.ToDisplayString(_displayFormat)}>({field.Name}Node);");
            }
            else
            {
                namespaces.Add(type.ContainingNamespace);

                if (mappingNodeOverride)
                {
                    reader.AppendLine($@"
        {{
            var reader = serialization.GetReader<{type.ToDisplayString(_displayFormat)}, MappingDataNode>();
            this.{field.Name} = reader.Read(serialization, mapping, dependencies, hooks, context, null);
        }}");
                }
                else
                {
                    reader.AppendLine($@"
        if ({field.Name}Node is ValueDataNode {field.Name}ValueNode)
        {{
            var reader = serialization.GetReader<{type.ToDisplayString(_displayFormat)}, ValueDataNode>();
            this.{field.Name} = reader.Read(serialization, {field.Name}ValueNode, dependencies, hooks, context, null);
        }}
        else if ({field.Name}Node is SequenceDataNode {field.Name}SequenceNode)
        {{
            var reader = serialization.GetReader<{type.ToDisplayString(_displayFormat)}, SequenceDataNode>();
            this.{field.Name} = reader.Read(serialization, {field.Name}SequenceNode, dependencies, hooks, context, null);
        }}
        else
        {{
            throw new ArgumentException($""Cannot read array from data node type {{{field.Name}Node.GetType()}}"");
        }}");
                }
            }

            // TODO run serialization hook after deserialization

            return reader.ToString();
        }

        private string GetArrayReader(
            HashSet<ITypeSymbol> customResolved,
            HashSet<INamespaceSymbol> namespaces,
            ITypeSymbol definition,
            ISymbol field)
        {
            var type = (IArrayTypeSymbol)GetFieldType(field);

            var elementType = type.ElementType;
            if (elementType.TypeKind == TypeKind.Array)
            {
                // TODO
                return "// TODO nested array";
            }

            var reader = new StringBuilder();
            var interfaceName = $"ISerializationGenerated<{definition.Name}>";

            // If IncludeDataField is used instead of DataField, then we need to use MappingDataNode as the
            // type parameter for serializers, instead of what the node type would have usually been
            // after getting it from the mapping
            bool mappingNodeOverride;

            if (GetAttribute(field, _dataFieldSymbol) != null)
            {
                reader.AppendLine($@"        var {field.Name}Node = mapping[""{GetDataFieldId(field)}""];");
                mappingNodeOverride = false;
            }
            else
            {
                // Attribute has to be IncludeDataField
                mappingNodeOverride = true;
            }

            var arrayCommas = type.Rank - 1;
            var arrayDimension = $"[{new string(',', arrayCommas)}]";
            var arrayNestersStart = new string('{', arrayCommas);
            var arrayNestersEnd = new string('}', arrayCommas);

            if (TryGetCustomTypeSerializer(
                    field,
                    out var serializer,
                    out var valueReader,
                    out var sequenceReader,
                    out var mappingReader,
                    out _, out _) &&
                valueReader || sequenceReader)
            {
                // TODO type tags
                if (customResolved.Add(serializer))
                    namespaces.Add(serializer.ContainingNamespace);

                if (mappingNodeOverride)
                {
                    reader.AppendLine($@"
        var {field.Name}Reader = serialization.EnsureCustomReader<{serializer.ToDisplayString(_displayFormat)}, {field.ToDisplayString(_displayFormat)}, MappingDataNode();
        this.{field.Name} = {field.Name}Reader.Read(serialization, mapping, dependencies, hooks, context, null);
");
                }
                else
                {
                    if (valueReader)
                    {
                        reader.AppendLine($@"
        if ({field.Name}Node is ValueDataNode {field.Name}ValueNode)
        {{
            var reader = serialization.EnsureCustomReader<{serializer.ToDisplayString(_displayFormat)}, {field.ToDisplayString(_displayFormat)}, ValueDataNode>();
            this.{field.Name} = new {elementType.ToDisplayString(_displayFormat)}{arrayDimension}
            {{
                {arrayNestersStart}reader.Read(serialization, {field.Name}ValueNode, dependencies, hooks, context, null){arrayNestersEnd}
            }};
        }}");
                    }

                    if (sequenceReader)
                    {
                        reader.AppendLine($@"
        if ({field.Name}Node is SequenceDataNode {field.Name}SequenceNode)
        {{
            var reader = serialization.EnsureCustomReader<{serializer.ToDisplayString(_displayFormat)}, {field.ToDisplayString(_displayFormat)}, SequenceDataNode>();
            this.{field.Name} = new {elementType.ToDisplayString(_displayFormat)}[{field.Name}SequenceNode.Count];
            for (var i = 0; i < {field.Name}SequenceNode.Count; i++)
            {{
                this.{field.Name}[i] = reader.Read(serialization, {field.Name}SequenceNode[i], dependencies, hooks, context, null);
            }}
        }}");
                    }

                    if (mappingReader)
                    {
                        reader.AppendLine($@"
        if ({field.Name}Node is MappingDataNode {field.Name}MappingNode)
        {{
            var reader = serialization.EnsureCustomReader<{serializer.ToDisplayString(_displayFormat)}, {field.ToDisplayString(_displayFormat)}, MappingDataNode>();
            this.{field.Name} = reader.Read(serialization, {field.Name}MappingNode, dependencies, hooks, context, null);
        }}");
                    }

                    reader.AppendLine($@"
        else
        {{
            throw new ArgumentException($""Cannot read array from data node type {{{field.Name}Node.GetType()}}"");
        }}");
                }
            }
            else if (elementType.TypeKind == TypeKind.Enum)
            {
                namespaces.Add(elementType.ContainingNamespace);
                reader.AppendLine(
                    $@"        this.{field.Name} = {interfaceName}.ReadEnumArray<{elementType.ToDisplayString(_displayFormat)}>({field.Name}Node);");
            }
            else if (IsSelfSerialize(elementType))
            {
                namespaces.Add(elementType.ContainingNamespace);
                reader.AppendLine(
                    $@"        this.{field.Name} = {interfaceName}.ReadSelfSerializeArray<{elementType.ToDisplayString(_displayFormat)}>({field.Name}Node);");
            }
            else
            {
                if (mappingNodeOverride)
                {
                    reader.AppendLine($@"
        {{        
            var reader = serialization.GetReader<{type.ToDisplayString(_displayFormat)}, MappingDataNode>();
            this.{field.Name} = reader.Read(serialization, mapping, dependencies, hooks, context, null);
        }}");
                }
                else
                {
                    reader.AppendLine($@"
        if ({field.Name}Node is ValueDataNode {field.Name}ValueNode)
        {{
            var reader = serialization.GetReader<{elementType.ToDisplayString(_displayFormat)}, ValueDataNode>();
            this.{field.Name} = new {elementType.ToDisplayString(_displayFormat)}{arrayDimension}
            {{
                {arrayNestersStart}reader.Read(serialization, {field.Name}ValueNode, dependencies, hooks, context, null){arrayNestersEnd}
            }};
        }}");

                    if (arrayCommas == 0)
                    {
                        reader.AppendLine($@"
        else if ({field.Name}Node is SequenceDataNode {field.Name}SequenceNode)
        {{
            var reader = serialization.GetReader<{elementType.ToDisplayString(_displayFormat)}, ValueDataNode>();
            this.{field.Name} = new {elementType.ToDisplayString(_displayFormat)}[{field.Name}SequenceNode.Count];
            for (var i = 0; i < {field.Name}SequenceNode.Count; i++)
            {{
                this.{field.Name}[i] = reader.Read(serialization, (ValueDataNode) {field.Name}SequenceNode[i], dependencies, hooks, context, null);
            }}
        }}
        else
        {{
            throw new ArgumentException($""Cannot read array from data node type {{{field.Name}Node.GetType()}}"");
        }}");
                    }
                    else
                    {
                        reader.AppendLine($@"
        else
        {{
            throw new ArgumentException($""Cannot read multi dimensional array from data node type {{{field.Name}Node.GetType()}}"");
        }}");
                    }
                }
            }

            return reader.ToString();
        }

        private string GetCopyConstructorAssigner(HashSet<INamespaceSymbol> namespaces, ISymbol field)
        {
            var type = GetFieldType(field);

            // TODO
            if (type.IsAbstract) /* || type.IsInterface) */
                return $"// TODO abstract/interface ${field.Name}";

            var copier = new StringBuilder();

            if (TryGetCustomTypeSerializer(field, out var serializer, out _, out _, out _,
                    out var isCopier,
                    out var isCopyCreator) &&
                (isCopier || isCopyCreator))
            {
                // TODO type tags
                namespaces.Add(serializer.ContainingNamespace);

                if (isCopier)
                {
                    copier.AppendLine(
                        $@"        if (instance.{field.Name} == default)
        {{
            this.{field.Name} = default!;
        }}
        else
        {{
            var copier = serialization.EnsureCustomCopier<{serializer.ToDisplayString(_displayFormat)}, {field.ToDisplayString(_displayFormat)}, ValueDataNode>();
            copier.CopyTo(serialization, instance.{field.Name}, ref this.{field.Name}, hooks, context);
        }}
");
                }
                else
                {
                    copier.AppendLine(
                        $@"        if (instance.{field.Name} == default)
        {{
            this.{field.Name} = default!;
        }}
        else
        {{
            var copier = serialization.EnsureCustomCopyCreator<{serializer.ToDisplayString(_displayFormat)}, {field.ToDisplayString(_displayFormat)}, ValueDataNode>();
            this.{field.Name} = copier.CreateCopy(serialization, instance.{field.Name}, hooks, context);
        }}");
                }
            }
            else if (type.TypeKind == TypeKind.Array)
            {
                // TODO nested arrays
                var element = ((IArrayTypeSymbol)type).ElementType;

                if (type.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    copier.AppendLine($@"        if (instance.{field.Name} == default)
        {{
            this.{field.Name} = default!;
        }}
        else
        {{");
                }

                if (ShouldReturnSource(element))
                {
                    copier.AppendLine(
                        $@"        this.{field.Name} = new {element.ToDisplayString(_displayFormat)}[instance.{field.Name}.Length];
        for (var i = 0; i < instance.{field.Name}.Length; i++)
        {{
            this.{field.Name}[i] = instance.{field.Name}[i];
        }}");
                }
                else if (IsDataDefinition(element))
                {
                    var nullable = CanHaveNullableOperator(GetFieldType(field));
                    copier.AppendLine(
                        $@"        this.{field.Name} = new {element.ToDisplayString(_displayFormat)}[instance.{field.Name}.Length];
        for (var i = 0; i < instance.{field.Name}.Length; i++)
        {{
            this.{field.Name}[i] = instance.{field.Name}[i]{(nullable ? "?" : "")}.Copy(dependencies, hooks, context)!;
        }}");
                }
                else
                {
                    copier.AppendLine($@"
        if (instance.{field.Name} == default)
        {{
            this.{field.Name} = default!;
        }}
        else if (serialization.TryGetCopier<{element.ToDisplayString(_displayFormat)}>(out var {element.Name}Copier))
        {{
            this.{field.Name} = new {element.ToDisplayString(_displayFormat)}[instance.{field.Name}.Length];
            for (var i = 0; i < instance.{field.Name}.Length; i++)
            {{
                {element.Name}Copier.CopyTo(serialization, instance.{field.Name}[i], ref this.{field.Name}[i], hooks, context);
            }}
        }}
        else if (serialization.TryGetCopyCreator<{element.ToDisplayString(_displayFormat)}>(out var {element.Name}CopyCreator))
        {{
            this.{field.Name} = new {element.ToDisplayString(_displayFormat)}[instance.{field.Name}.Length];
            for (var i = 0; i < instance.{field.Name}.Length; i++)
            {{
                this.{field.Name}[i] = {element.Name}CopyCreator.CreateCopy(serialization, instance.{field.Name}[i], hooks, context);
            }}
        }}
        else
        {{
            throw new ArgumentException(""No copier found for type {element.ToDisplayString(FullyQualifiedFormat)}"");
        }}");
                }

                if (type.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    copier.AppendLine($@"        }}");
                }
            }
            else if (ShouldReturnSource(type))
            {
                copier.AppendLine($@"        this.{field.Name} = instance.{field.Name};");
            }
            else if (IsDataDefinition(type))
            {
                var nullable = CanHaveNullableOperator(GetFieldType(field));
                copier.AppendLine(
                    $@"        this.{field.Name} = instance.{field.Name}{(nullable ? "?" : "")}.Copy(dependencies, hooks, context)!;");
            }
            else
            {
                var fieldType = GetFieldType(field);
                var nonNullableType = fieldType.ToDisplayString(_displayFormat);
                if (nonNullableType.EndsWith("?"))
                    nonNullableType = nonNullableType.Substring(0, nonNullableType.Length - 1);

                var defaultFieldConstructor = ((INamedTypeSymbol)fieldType).InstanceConstructors
                    .Any(c => c.Parameters.IsEmpty);
                var isNullableStruct = fieldType.IsValueType &&
                                       fieldType.NullableAnnotation == NullableAnnotation.Annotated;

                Func<string, string> defaulter = name => name;
                Func<string, string> accessor = name => name;
                if (isNullableStruct)
                {
                    defaulter = name => $"{name} ?? default({nonNullableType})";
                    accessor = name => $"{name}!.Value";
                }
                else if (fieldType.IsValueType)
                {
                    defaulter = name => $"{name}";
                    accessor = name => $"{name}";
                }
                else if (defaultFieldConstructor)
                {
                    defaulter = name => $"{name} ?? new()";
                }

                copier.AppendLine($@"
        if (instance.{field.Name} == default)
        {{
            this.{field.Name} = default!;
        }}
        else if (serialization.TryGetCopier<{nonNullableType}>(out var {field.Name}Copier))
        {{");

                if (field is IFieldSymbol && !isNullableStruct)
                {
                    if (!fieldType.IsValueType)
                    {
                        copier.AppendLine($@"            this.{field.Name} = {defaulter(field.Name)};");
                    }

                    copier.AppendLine($@"            {field.Name}Copier.CopyTo(serialization, instance.{accessor(field.Name)}, ref this.{field.Name}, hooks, context);");
                }
                else
                {
                    copier.AppendLine($@"            var {field.Name}Target = this.{defaulter(field.Name)};
            {field.Name}Copier.CopyTo(serialization, instance.{accessor(field.Name)}, ref {field.Name}Target, hooks, context);
            this.{field.Name} = {field.Name}Target;");
                }

                copier.AppendLine($@"        }}
        else if (serialization.TryGetCopyCreator<{nonNullableType}>(out var {field.Name}CopyCreator))
        {{
            this.{field.Name} = {field.Name}CopyCreator.CreateCopy(serialization, instance.{accessor(field.Name)}!, hooks, context);
        }}
        else
        {{
            throw new ArgumentException(""No copier found for type {field.ToDisplayString(FullyQualifiedFormat)}"");
        }}");
            }

            // TODO run serialization hook after deserialization
            return copier.ToString();
        }

        private bool IsDataDefinition(ITypeSymbol type)
        {
            if (ShouldReturnSource(type))
                return false;

            // TODO type serializers
            if (GetAttribute(type, _dataDefinitionSymbol) == null)
                return false;

            return true;
        }

        private bool IsSelfSerialize(ITypeSymbol type)
        {
            return type.AllInterfaces.Any(fieldInterface =>
                fieldInterface.Equals(_selfSerializeSymbol, Default));
        }

        public bool CanHaveNullableOperator(ITypeSymbol type)
        {
            return type.IsReferenceType || type.NullableAnnotation == NullableAnnotation.Annotated;
        }
    }
}
