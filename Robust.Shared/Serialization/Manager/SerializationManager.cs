using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager : ISerializationManager
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        public const string LogCategory = "serialization";

        private bool _initializing;
        private bool _initialized;

        private readonly Dictionary<Type, SerializationDataDefinition> _dataDefinitions = new();
        private readonly HashSet<Type> _copyByRefRegistrations = new();

        public IDependencyCollection DependencyCollection { get; private set; } = default!;

        public void Initialize()
        {
            if (_initializing)
            {
                throw new InvalidOperationException($"{nameof(SerializationManager)} is already being initialized.");
            }

            if (_initialized)
            {
                throw new InvalidOperationException($"{nameof(SerializationManager)} has already been initialized.");
            }

            _initializing = true;

            DependencyCollection = IoCManager.Instance ?? throw new NullReferenceException();

            InitializeFlagsAndConstants();
            InitializeTypeSerializers();

            //var registrations = _reflectionManager.FindTypesWithAttribute<MeansDataDefinition>().ToHashSet();
            var registrations = new HashSet<Type>();

            foreach (var baseType in _reflectionManager.FindTypesWithAttribute<ImplicitDataDefinitionForInheritorsAttribute>())
            {
                if (!baseType.IsAbstract && !baseType.IsInterface && !baseType.IsGenericTypeDefinition) registrations.Add(baseType);
                foreach (var child in _reflectionManager.GetAllChildren(baseType))
                {
                    if (child.IsAbstract || child.IsInterface || child.IsGenericTypeDefinition) continue;
                    registrations.Add(child);
                }
            }

            foreach (var meansAttr in _reflectionManager.FindTypesWithAttribute<MeansDataDefinition>())
            {
                foreach (var type in _reflectionManager.FindTypesWithAttribute(meansAttr))
                {
                    registrations.Add(type);
                }
            }

            foreach (var type in registrations)
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                {
                    Logger.DebugS(LogCategory, $"Skipping registering data definition for type {type} since it is abstract or an interface");
                    continue;
                }

                if (!type.IsValueType && type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(m => m.GetParameters().Length == 0) == null)
                {
                    Logger.DebugS(LogCategory, $"Skipping registering data definition for type {type} since it has no parameterless ctor");
                    continue;
                }

                _dataDefinitions.Add(type, new SerializationDataDefinition(type));
            }

            var error = new StringBuilder();

            foreach (var (type, definition) in _dataDefinitions)
            {
                if (definition.TryGetDuplicates(out var definitionDuplicates))
                {
                    error.Append($"{type}: [{string.Join(", ", definitionDuplicates)}]\n");
                }
            }

            if (error.Length > 0)
            {
                throw new ArgumentException($"Duplicate data field tags found in:\n{error}");
            }

            foreach (var type in _reflectionManager.FindTypesWithAttribute<CopyByRefAttribute>())
            {
                _copyByRefRegistrations.Add(type);
            }

            _initialized = true;
            _initializing = false;
        }

        public bool HasDataDefinition(Type type)
        {
            if (type.IsGenericTypeDefinition) throw new NotImplementedException($"Cannot yet check data definitions for generic types. ({type})");
            return _dataDefinitions.ContainsKey(type);
        }

        public ValidationNode ValidateNode(Type type, DataNode node, ISerializationContext? context = null)
        {
            var underlyingType = type.EnsureNotNullableType();

            if (underlyingType.IsArray)
            {
                if (node is not SequenceDataNode sequenceDataNode) return new ErrorNode(node, "Invalid nodetype for array.", true);
                var elementType = underlyingType.GetElementType();
                if (elementType == null)
                    throw new ArgumentException($"Failed to get elementtype of arraytype {underlyingType}", nameof(underlyingType));
                var validatedList = new List<ValidationNode>();
                foreach (var dataNode in sequenceDataNode.Sequence)
                {
                    validatedList.Add(ValidateNode(elementType, dataNode, context));
                }

                return new ValidatedSequenceNode(validatedList);
            }

            if (underlyingType.IsEnum)
            {
                var enumName = node switch
                {
                    ValueDataNode valueNode => valueNode.Value,
                    SequenceDataNode sequenceNode => string.Join(", ", sequenceNode.Sequence),
                    _ => null
                };

                if (enumName == null)
                {
                    return new ErrorNode(node, $"Invalid node type {node.GetType().Name} for enum {underlyingType}.");
                }

                if (!Enum.TryParse(underlyingType, enumName, true, out var enumValue))
                {
                    return new ErrorNode(node, $"{enumValue} is not a valid enum value of type {underlyingType}", false);
                }

                return new ValidatedValueNode(node);
            }

            if (node.Tag?.StartsWith("!type:") == true)
            {
                var typeString = node.Tag.Substring(6);
                try
                {
                    underlyingType = ResolveConcreteType(underlyingType, typeString);
                }
                catch (InvalidOperationException)
                {
                    return new ErrorNode(node, $"Failed to resolve !type tag: {typeString}", false);
                }
            }

            if (TryValidateWithTypeValidator(underlyingType, node, DependencyCollection, context, out var valid)) return valid;

            if (typeof(ISelfSerialize).IsAssignableFrom(underlyingType))
                return node is ValueDataNode valueDataNode ? new ValidatedValueNode(valueDataNode) : new ErrorNode(node, "Invalid nodetype for ISelfSerialize", true);

            if (TryGetDataDefinition(underlyingType, out var dataDefinition))
            {
                return node switch
                {
                    ValueDataNode valueDataNode => valueDataNode.Value == "" ? new ValidatedValueNode(valueDataNode) : new ErrorNode(node, "Invalid nodetype for Datadefinition", false),
                    MappingDataNode mappingDataNode => dataDefinition.Validate(this, mappingDataNode, context),
                    _ => new ErrorNode(node, "Invalid nodetype for Datadefinition", true)
                };
            }

            return new ErrorNode(node, "Failed to read node.", false);
        }

        public ValidationNode ValidateNode<T>(DataNode node, ISerializationContext? context = null)
        {
            return ValidateNode(typeof(T), node, context);
        }

        public ValidationNode ValidateNodeWith(Type type, Type typeSerializer, DataNode node,
            ISerializationContext? context = null)
        {
            var method =
                typeof(SerializationManager).GetRuntimeMethods().First(m => m.Name == nameof(ValidateWithCustomTypeSerializer))!.MakeGenericMethod(
                    type, node.GetType(), typeSerializer);
            return (ValidationNode)method.Invoke(this, new object?[] {node, context})!;
        }

        public ValidationNode ValidateNodeWith<TType, TSerializer, TNode>(TNode node,
            ISerializationContext? context = null)
            where TSerializer : ITypeValidator<TType, TNode>
            where TNode: DataNode
        {
            return ValidateNodeWith(typeof(TType), typeof(TSerializer), node, context);
        }

        public DeserializationResult CreateDataDefinition<T>(DeserializedFieldEntry[] fields, bool skipHook = false)
            where T : notnull, new()
        {
            var obj = new T();
            return PopulateDataDefinition(obj, new DeserializedDefinition<T>(obj, fields), skipHook);
        }

        public DeserializationResult PopulateDataDefinition<T>(T obj, DeserializedDefinition<T> definition, bool skipHook = false)
            where T : notnull, new()
        {
            return PopulateDataDefinition(obj, (IDeserializedDefinition) definition, skipHook);
        }

        public DeserializationResult PopulateDataDefinition(object obj, IDeserializedDefinition definition, bool skipHook = false)
        {
            if (!TryGetDataDefinition(obj.GetType(), out var dataDefinition))
                throw new ArgumentException($"Provided Type is not a data definition ({obj.GetType()})");

            if (obj is IPopulateDefaultValues populateDefaultValues)
            {
                populateDefaultValues.PopulateDefaultValues();
            }

            var res = dataDefinition.InvokePopulateDelegate(obj, definition.Mapping);

            if (!skipHook && res.RawValue is ISerializationHooks serializationHooksAfter)
            {
                serializationHooksAfter.AfterDeserialization();
            }

            return res;
        }

        private SerializationDataDefinition? GetDataDefinition(Type type)
        {
            if (_dataDefinitions.TryGetValue(type, out var dataDefinition)) return dataDefinition;

            return null;
        }

        private bool TryGetDataDefinition(Type type, [NotNullWhen(true)] out SerializationDataDefinition? dataDefinition)
        {
            dataDefinition = GetDataDefinition(type);
            return dataDefinition != null;
        }

        public DeserializationResult Read(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false)
        {
            var underlyingType = type.EnsureNotNullableType();

            // array
            if (underlyingType.IsArray)
            {
                if (node is not SequenceDataNode sequenceDataNode) throw new InvalidNodeTypeException();
                var newArray = (Array) Activator.CreateInstance(type, sequenceDataNode.Sequence.Count)!;
                var results = new List<DeserializationResult>();

                var idx = 0;
                foreach (var entryNode in sequenceDataNode.Sequence)
                {
                    var value = Read(type.GetElementType()!, entryNode, context, skipHook);
                    results.Add(value);
                    newArray.SetValue(value.RawValue, idx++);
                }

                return new DeserializedArray(newArray, results);
            }

            if (underlyingType.IsEnum)
            {
                return new DeserializedValue(node switch
                {
                    ValueDataNode valueNode => Enum.Parse(underlyingType, valueNode.Value, true),
                    SequenceDataNode sequenceNode => Enum.Parse(underlyingType, string.Join(", ", sequenceNode.Sequence), true),
                    _ => throw new InvalidNodeTypeException($"Cannot serialize node as {underlyingType}, unsupported node type {node.GetType()}")
                });
            }

            if (node.Tag?.StartsWith("!type:") == true)
            {
                var typeString = node.Tag.Substring(6);
                underlyingType = ResolveConcreteType(underlyingType, typeString);
            }

            if (TryReadRaw(underlyingType, node, DependencyCollection, out var serializedObj, skipHook, context))
            {
                return serializedObj;
            }

            if (typeof(ISelfSerialize).IsAssignableFrom(underlyingType))
            {
                if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();

                var selfSerObj = (ISelfSerialize) Activator.CreateInstance(underlyingType)!;
                selfSerObj.Deserialize(valueDataNode.Value);

                return new DeserializedValue(selfSerObj);
            }

            //if (node is not MappingDataNode mappingDataNode) throw new InvalidNodeTypeException();

            if (underlyingType.IsInterface || underlyingType.IsAbstract)
            {
                 throw new InvalidOperationException($"Unable to create an instance of an interface or abstract type. Type: {underlyingType}");
            }

            var obj = Activator.CreateInstance(underlyingType)!;

            if (obj is IPopulateDefaultValues populateDefaultValues)
            {
                populateDefaultValues.PopulateDefaultValues();
            }

            if (!TryGetDataDefinition(underlyingType, out var dataDef))
            {
                throw new InvalidOperationException($"No data definition found for type {underlyingType} with node type {node.GetType()} when reading");
            }

            if (node is not MappingDataNode mappingDataNode)
            {
                if(node is not ValueDataNode emptyValueDataNode || emptyValueDataNode.Value != "")
                    throw new ArgumentException($"No mapping node provided for type {type}");
                mappingDataNode = new MappingDataNode(); //if we get an emptyValueDataNode we just use an empty mapping
            }

            var res = dataDef.InvokePopulateDelegate(obj, mappingDataNode, this, context, skipHook);

            if (!skipHook && res.RawValue is ISerializationHooks serHooks)
            {
                serHooks.AfterDeserialization();
            }

            return res;
        }

        public object? ReadValue(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false)
        {
            return Read(type, node, context, skipHook).RawValue;
        }

        public T? ReadValueCast<T>(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false)
        {
            var value = Read(type, node, context, skipHook);

            if (value.RawValue == null)
            {
                return default;
            }

            return (T?) value.RawValue;
        }

        public T? ReadValue<T>(DataNode node, ISerializationContext? context = null, bool skipHook = false)
        {
            return ReadValueCast<T>(typeof(T), node, context, skipHook);
        }

        public DeserializationResult ReadWithTypeSerializer(Type type, Type typeSerializer, DataNode node, ISerializationContext? context = null,
            bool skipHook = false)
        {
            var method = typeof(SerializationManager).GetRuntimeMethods().First(m => m.Name == nameof(ReadWithCustomTypeSerializer))!
                .MakeGenericMethod(type, node.GetType(), typeSerializer);
            return (DeserializationResult) method.Invoke(this, new object?[] {node, context, skipHook})!;
        }

        public DataNode WriteValue<T>(T value, bool alwaysWrite = false,
            ISerializationContext? context = null) where T : notnull
        {
            return WriteValue(typeof(T), value, alwaysWrite, context);
        }

        public DataNode WriteValue(Type type, object? value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (value == null) return new MappingDataNode();

            if (underlyingType.IsEnum)
            {
                // Enums implement IConvertible.
                // Need it for the culture overload.
                var convertible = (IConvertible) value;
                return new ValueDataNode(convertible.ToString(CultureInfo.InvariantCulture));
            }

            // array
            if (underlyingType.IsArray)
            {
                var sequenceNode = new SequenceDataNode();
                var array = (Array) value;

                foreach (var val in array)
                {
                    var serializedVal = WriteValue(val.GetType(), val, alwaysWrite, context);
                    sequenceNode.Add(serializedVal);
                }

                return sequenceNode;
            }

            if (value is ISerializationHooks serHook)
                serHook.BeforeSerialization();

            if (TryWriteWithTypeSerializers(underlyingType, value, out var node, alwaysWrite, context))
            {
                return node;
            }

            if (typeof(ISelfSerialize).IsAssignableFrom(underlyingType))
            {
                var selfSerObj = (ISelfSerialize)value;
                return new ValueDataNode(selfSerObj.Serialize());
            }

            var currentType = underlyingType;
            var mapping = new MappingDataNode();
            if (underlyingType.IsAbstract || underlyingType.IsInterface)
            {
                mapping.Tag = $"!type:{value.GetType().Name}";
                currentType = value.GetType();
            }

            if (!TryGetDataDefinition(currentType, out var dataDef))
            {
                throw new InvalidOperationException($"No data definition found for type {type} when writing");
            }

            if (dataDef.CanCallWith(value) != true)
            {
                throw new ArgumentException($"Supplied value does not fit with data definition of {type}.");
            }

            var newMapping = dataDef.InvokeSerializeDelegate(value, this, context, alwaysWrite);
            mapping = mapping.Merge(newMapping);

            return mapping;
        }

        public DataNode WriteWithTypeSerializer(Type type, Type typeSerializer, object? value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            if (type.IsNullable() && value == null) return new MappingDataNode(); //todo just return null

            var method = typeof(SerializationManager).GetRuntimeMethods().First(m => m.Name == nameof(WriteWithCustomTypeSerializer))!
                .MakeGenericMethod(type, typeSerializer);
            return (DataNode) method.Invoke(this, new object?[] {value, context, alwaysWrite})!;
        }

        private object? CopyToTarget(object? source, object? target, ISerializationContext? context = null, bool skipHook = false)
        {
            if (source == null || target == null)
            {
                return source;
            }

            var sourceType = source.GetType();
            var targetType = target.GetType();

            if (sourceType.IsValueType && targetType.IsValueType)
            {
                return source;
            }

            if (source.GetType().IsValueType != target.GetType().IsValueType)
            {
                throw new InvalidOperationException(
                    $"Source and target do not match. Source ({sourceType}) is value type? {sourceType.IsValueType}. Target ({targetType}) is value type? {targetType.IsValueType}");
            }

            // array
            if (sourceType.IsArray && targetType.IsArray)
            {
                var sourceArray = (Array) source;
                var targetArray = (Array) target;

                Array newArray;
                if(sourceArray.Length == targetArray.Length)
                {
                    newArray = targetArray;
                }
                else
                {
                    newArray = (Array) Activator.CreateInstance(sourceArray.GetType(), sourceArray.Length)!;
                }

                for (int i = 0; i < sourceArray.Length; i++)
                {
                    newArray.SetValue(CreateCopy(sourceArray.GetValue(i), context, skipHook), i);
                }

                return newArray;
            }

            if (source.GetType().IsArray != target.GetType().IsArray)
            {
                throw new InvalidOperationException(
                    $"Source and target do not match. Source ({sourceType}) is array type? {sourceType.IsArray}. Target ({targetType}) is array type? {targetType.IsArray}");
            }

            var commonType = TypeHelpers.SelectCommonType(source.GetType(), target.GetType());
            if (commonType == null)
            {
                throw new InvalidOperationException("Could not find common type in Copy!");
            }

            if (_copyByRefRegistrations.Contains(commonType) || commonType.IsEnum)
            {
                return source;
            }

            if (TryCopyRaw(commonType, source, ref target, skipHook, context))
            {
                return target;
            }

            if (target is IPopulateDefaultValues populateDefaultValues)
            {
                populateDefaultValues.PopulateDefaultValues();
            }

            if (!TryGetDataDefinition(commonType, out var dataDef))
            {
                throw new InvalidOperationException($"No data definition found for type {commonType} when copying");
            }

            target = dataDef.InvokeCopyDelegate(source, target, this, context);

            if (!skipHook && target is ISerializationHooks afterHooks)
            {
                afterHooks.AfterDeserialization();
            }

            return target;
        }

        [MustUseReturnValue]
        public object? Copy(object? source, object? target, ISerializationContext? context = null, bool skipHook = false)
        {
            return CopyToTarget(source, target, context, skipHook);
        }

        [MustUseReturnValue]
        public T? Copy<T>(T? source, T? target, ISerializationContext? context = null, bool skipHook = false)
        {
            var copy = CopyToTarget(source, target, context, skipHook);

            return copy == null ? default : (T?) copy;
        }

        public object? CopyWithTypeSerializer(Type typeSerializer, object? source, object? target,
            ISerializationContext? context = null, bool skipHook = false)
        {
            if (source == null || target == null) return source;
            var commonType = TypeHelpers.SelectCommonType(source.GetType(), target.GetType());
            if (commonType == null)
            {
                throw new InvalidOperationException($"Could not find common type in {nameof(CopyWithTypeSerializer)}!");
            }

            var method = typeof(SerializationManager).GetRuntimeMethods().First(m => m.Name == nameof(CopyWithCustomTypeSerializer))!
                .MakeGenericMethod(commonType, source.GetType(), target.GetType(), typeSerializer);
            return method.Invoke(this, new object?[] {source, target, skipHook, context});
        }

        private object? CreateCopyInternal(Type type, object? source, ISerializationContext? context = null, bool skipHook = false)
        {
            if (source == null ||
                type.IsPrimitive ||
                type.IsEnum ||
                source is string ||
                _copyByRefRegistrations.Contains(type))
            {
                return source;
            }

            var target = Activator.CreateInstance(source.GetType())!;
            return Copy(source, target, context, skipHook);
        }

        public object? CreateCopy(object? source, ISerializationContext? context = null, bool skipHook = false)
        {
            if (source == null) return null;
            return CreateCopyInternal(source.GetType(), source, context, skipHook);
        }

        public T? CreateCopy<T>(T? source, ISerializationContext? context = null, bool skipHook = false)
        {
            var copy = CreateCopyInternal(typeof(T), source, context, skipHook);

            if (copy == null)
            {
                return default;
            }

            return (T?) copy;
        }

        private static Type ResolveConcreteType(Type baseType, string typeName)
        {
            var reflection = IoCManager.Resolve<IReflectionManager>();
            var type = reflection.YamlTypeTagLookup(baseType, typeName);
            if (type == null)
            {
                throw new InvalidOperationException($"Type '{baseType}' is abstract, but could not find concrete type '{typeName}'.");
            }

            return type;
        }
    }
}
